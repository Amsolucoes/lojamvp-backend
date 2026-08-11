using LojaApi.Data;
using LojaApi.Models;
using LojaApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LojaApi.src.Controllers;

[ApiController]
[Route("api/financeiro")]
[Authorize]
public class FinanceiroController(AppDbContext db, FinanceiroService financeiroService) : ControllerBase
{
    private Guid UsuarioId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private async Task<Guid?> GetLojaId()
    {
        var vinculo = await db.UsuariosLoja
            .FirstOrDefaultAsync(ul => ul.UsuarioId == UsuarioId && ul.Ativo);
        return vinculo?.LojaId;
    }

    // ══════════════════ CONTAS BANCÁRIAS ══════════════════

    [HttpGet("contas")]
    public async Task<IActionResult> ListarContas()
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return Ok(Array.Empty<object>());

        var contas = await db.ContasBancarias.Where(c => c.LojaId == lojaId).ToListAsync();

        var resultado = new List<object>();
        foreach (var conta in contas)
        {
            var saldo = await CalcularSaldoAsync(conta.Id);
            resultado.Add(new { conta.Id, conta.Nome, conta.SaldoInicial, conta.Ativa, conta.Banco, conta.Limite, saldoAtual = saldo });
        }
        return Ok(resultado);
    }

    [HttpPost("contas")]
    public async Task<IActionResult> CriarConta([FromBody] SalvarContaBancariaRequest req)
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return BadRequest(new { erro = "Loja não encontrada." });

        var conta = new ContaBancaria
        {
            LojaId = lojaId.Value,
            Nome = req.Nome.Trim(),
            SaldoInicial = req.SaldoInicial,
            Banco = req.Banco,
            Limite = req.Limite,
        };
        db.ContasBancarias.Add(conta);
        await db.SaveChangesAsync();

        return Ok(new { conta.Id, conta.Nome, conta.SaldoInicial, conta.Ativa, conta.Banco, conta.Limite, saldoAtual = conta.SaldoInicial });
    }

    [HttpPut("contas/{id:guid}")]
    public async Task<IActionResult> AtualizarConta(Guid id, [FromBody] SalvarContaBancariaRequest req)
    {
        var lojaId = await GetLojaId();
        var conta = await db.ContasBancarias.FirstOrDefaultAsync(c => c.Id == id && c.LojaId == lojaId);
        if (conta is null) return NotFound();

        conta.Nome = req.Nome.Trim();
        conta.SaldoInicial = req.SaldoInicial;
        conta.Banco = req.Banco;
        conta.Limite = req.Limite;
        await db.SaveChangesAsync();

        var saldo = await CalcularSaldoAsync(conta.Id);
        return Ok(new { conta.Id, conta.Nome, conta.SaldoInicial, conta.Ativa, conta.Banco, conta.Limite, saldoAtual = saldo });
    }

    [HttpPatch("contas/{id:guid}/ativo")]
    public async Task<IActionResult> AlternarAtivaConta(Guid id)
    {
        var lojaId = await GetLojaId();
        var conta = await db.ContasBancarias.FirstOrDefaultAsync(c => c.Id == id && c.LojaId == lojaId);
        if (conta is null) return NotFound();

        conta.Ativa = !conta.Ativa;
        await db.SaveChangesAsync();
        return Ok(new { conta.Id, conta.Ativa });
    }

    // ── Ajuste manual de saldo ──────────────────────────────────────
    [HttpPost("contas/{id:guid}/ajuste")]
    public async Task<IActionResult> AjustarSaldo(Guid id, [FromBody] AjusteSaldoRequest req)
    {
        var lojaId = await GetLojaId();
        var conta = await db.ContasBancarias.FirstOrDefaultAsync(c => c.Id == id && c.LojaId == lojaId);
        if (conta is null) return NotFound();

        decimal valorRegistrado;

        if (req.Tipo == "ajuste")
        {
            var saldoAtual = await CalcularSaldoAsync(id);
            valorRegistrado = req.NovoSaldo - saldoAtual; // diferença a aplicar
        }
        else
        {
            valorRegistrado = req.Valor ?? 0;
        }

        db.AjustesContaBancaria.Add(new AjusteContaBancaria
        {
            LojaId = lojaId!.Value,
            ContaBancariaId = id,
            Tipo = req.Tipo,
            Valor = valorRegistrado,
            Observacao = req.Observacao,
        });
        await db.SaveChangesAsync();

        var novoSaldo = await CalcularSaldoAsync(id);
        return Ok(new { saldoAtual = novoSaldo });
    }

    private async Task<decimal> CalcularSaldoAsync(Guid contaId)
    {
        var conta = await db.ContasBancarias.FindAsync(contaId);
        if (conta is null) return 0;

        var recebidos = await db.LancamentosFinanceiros
            .Where(l => l.ContaBancariaId == contaId && l.Tipo == "receber" && l.Status == "pago")
            .SumAsync(l => (decimal?)l.Valor) ?? 0;

        var pagos = await db.LancamentosFinanceiros
            .Where(l => l.ContaBancariaId == contaId && l.Tipo == "pagar" && l.Status == "pago")
            .SumAsync(l => (decimal?)l.Valor) ?? 0;

        var entradasAjuste = await db.AjustesContaBancaria
            .Where(a => a.ContaBancariaId == contaId && a.Tipo == "entrada")
            .SumAsync(a => (decimal?)a.Valor) ?? 0;

        var saidasAjuste = await db.AjustesContaBancaria
            .Where(a => a.ContaBancariaId == contaId && a.Tipo == "saida")
            .SumAsync(a => (decimal?)a.Valor) ?? 0;

        var diferencasAjuste = await db.AjustesContaBancaria
            .Where(a => a.ContaBancariaId == contaId && a.Tipo == "ajuste")
            .SumAsync(a => (decimal?)a.Valor) ?? 0;

        var faturasCartaoPagas = await db.FaturasCartao
            .Include(f => f.CartaoCredito)
            .Where(f => (f.ContaBancariaId ?? f.CartaoCredito!.ContaBancariaId) == contaId && (f.Status == "pago" || f.Status == "parcial" || f.Status == "financiada"))
            .SumAsync(f => (decimal?)f.ValorPago) ?? 0;

        var antecipadosPagos = await db.PagamentosAntecipadosFatura
            .Where(p => p.ContaBancariaId == contaId)
            .SumAsync(p => (decimal?)p.Valor) ?? 0;

        return conta.SaldoInicial + recebidos - pagos - faturasCartaoPagas - antecipadosPagos + entradasAjuste - saidasAjuste + diferencasAjuste;
    }

    // ══════════════════ LANÇAMENTOS ══════════════════

    [HttpGet("lancamentos")]
    public async Task<IActionResult> ListarLancamentos([FromQuery] string tipo, [FromQuery] int? ano, [FromQuery] int? mes)
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return Ok(Array.Empty<object>());

        var q = db.LancamentosFinanceiros.Where(l => l.LojaId == lojaId && l.Tipo == tipo);

        if (ano.HasValue && mes.HasValue)
        {
            var inicio = new DateTime(ano.Value, mes.Value, 1, 0, 0, 0, DateTimeKind.Utc);
            var fim = inicio.AddMonths(1);
            q = q.Where(l => l.Vencimento >= inicio && l.Vencimento < fim);
        }

        var lista = await q.Include(l => l.Categoria).OrderBy(l => l.Vencimento)
            .Select(l => new
            {
                l.Id,
                l.ContaBancariaId,
                l.Descricao,
                categoriaNome = l.Categoria != null ? l.Categoria.Nome : null,
                l.Modo,
                l.Valor,
                l.Vencimento,
                l.Status,
                l.PagoEm,
                l.NumeroParcela,
                l.TotalParcelas,
                origem = "lancamento",
            })
            .ToListAsync();

        return Ok(lista);
    }

    // ── Contas a Receber — visão unificada (Lançamentos + Planos) ──
    [HttpGet("receber-unificado")]
    public async Task<IActionResult> ReceberUnificado([FromQuery] DateTime? de, [FromQuery] DateTime? ate)
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return Ok(Array.Empty<object>());

        var qLanc = db.LancamentosFinanceiros.Where(l => l.LojaId == lojaId && l.Tipo == "receber");
        if (de.HasValue) qLanc = qLanc.Where(l => l.Vencimento >= de.Value);
        if (ate.HasValue) qLanc = qLanc.Where(l => l.Vencimento <= ate.Value.AddDays(1));

        var lancamentos = await qLanc.Include(l => l.Categoria).Select(l => new
        {
            l.Id,
            descricao = l.Descricao,
            observacao = l.Observacao,
            categoriaNome = l.Categoria != null ? l.Categoria.Nome : null,
            modo = l.Modo,
            categoriaId = l.CategoriaId,
            contaBancariaId = l.ContaBancariaId,
            numeroParcela = l.NumeroParcela,
            totalParcelas = l.TotalParcelas,
            valor = l.Valor,
            vencimento = l.Vencimento,
            status = l.Status,
            pagoEm = l.PagoEm,
            origem = "avulso",
        }).ToListAsync();

        var qPlano = db.PagamentosPlano.Where(p => p.LojaId == lojaId);
        if (de.HasValue) qPlano = qPlano.Where(p => p.MesReferencia.Date >= de.Value.Date);
        if (ate.HasValue) qPlano = qPlano.Where(p => p.MesReferencia.Date <= ate.Value.Date);

        var pagamentosPlano = await qPlano
            .Join(db.AssinaturasCliente, p => p.AssinaturaId, a => a.Id, (p, a) => new { p, a })
            .Join(db.Clientes, x => x.a.ClienteId, c => c.Id, (x, c) => new
            {
                x.p.Id,
                descricao = "Mensalidade - " + c.Nome,
                valor = x.p.Valor,
                vencimento = x.p.MesReferencia,
                status = x.p.Status,
                pagoEm = x.p.PagoEm,
                origem = "plano",
            })
            .ToListAsync();

        var unificado = lancamentos.Cast<object>().Concat(pagamentosPlano.Cast<object>())
            .OrderBy(x => ((dynamic)x).vencimento)
            .ToList();

        return Ok(unificado);
    }

    // ── Criar lançamento avulso ─────────────────────────────────────
    [HttpPost("lancamentos/avulso")]
    public async Task<IActionResult> CriarAvulso([FromBody] SalvarLancamentoAvulsoRequest req)
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return BadRequest(new { erro = "Loja não encontrada." });

        var lancamento = new LancamentoFinanceiro
        {
            LojaId = lojaId.Value,
            ContaBancariaId = req.ContaBancariaId,
            Tipo = req.Tipo,
            Modo = "avulsa",
            Descricao = req.Descricao.Trim(),
            CategoriaId = req.CategoriaId,
            Observacao = req.Observacao,
            Valor = req.Valor,
            Vencimento = DateTime.SpecifyKind(req.Vencimento.Date, DateTimeKind.Utc).AddHours(12),
            Status = req.JaPago ? "pago" : "pendente",
            PagoEm = req.JaPago ? DateTime.UtcNow : null,
        };
        db.LancamentosFinanceiros.Add(lancamento);
        await db.SaveChangesAsync();

        return Ok(new { lancamento.Id });
    }

    // ── Criar lançamento parcelado (gera N parcelas de uma vez) ─────
    [HttpPost("lancamentos/parcelado")]
    public async Task<IActionResult> CriarParcelado([FromBody] SalvarLancamentoParceladoRequest req)
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return BadRequest(new { erro = "Loja não encontrada." });

        int totalParcelas;
        if (req.DataFim.HasValue)
        {
            totalParcelas = ((req.DataFim.Value.Year - req.PrimeiroVencimento.Year) * 12)
                + (req.DataFim.Value.Month - req.PrimeiroVencimento.Month) + 1;
        }
        else
        {
            totalParcelas = req.TotalParcelas ?? 0;
        }

        if (totalParcelas < 2 || totalParcelas > 120)
            return BadRequest(new { erro = "Número de parcelas deve ficar entre 2 e 120. Ajuste a quantidade ou a data fim." });

        var grupoId = Guid.NewGuid();
        var lista = new List<LancamentoFinanceiro>();

        for (int i = 0; i < totalParcelas; i++)
        {
            lista.Add(new LancamentoFinanceiro
            {
                LojaId = lojaId.Value,
                ContaBancariaId = req.ContaBancariaId,
                Tipo = req.Tipo,
                Modo = "parcelada",
                Descricao = req.Descricao.Trim(),
                CategoriaId = req.CategoriaId,
                Observacao = req.Observacao,
                Valor = req.ValorParcela,
                Vencimento = DateTime.SpecifyKind(req.PrimeiroVencimento.Date, DateTimeKind.Utc).AddMonths(i).AddHours(12),
                GrupoParcelamentoId = grupoId,
                NumeroParcela = i + 1,
                TotalParcelas = totalParcelas,
                Status = (req.JaPago && i == 0) ? "pago" : "pendente",
                PagoEm = (req.JaPago && i == 0) ? DateTime.UtcNow : null,
            });
        }

        db.LancamentosFinanceiros.AddRange(lista);
        await db.SaveChangesAsync();

        return Ok(new { grupoParcelamentoId = grupoId, totalGerado = totalParcelas });
    }

    [HttpPost("fixos")]
    public async Task<IActionResult> CriarFixo([FromBody] SalvarLancamentoFixoRequest req)
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return BadRequest(new { erro = "Loja não encontrada." });

        var fixo = new LancamentoFixo
        {
            LojaId = lojaId.Value,
            ContaBancariaId = req.ContaBancariaId,
            Tipo = req.Tipo,
            Descricao = req.Descricao.Trim(),
            CategoriaId = req.CategoriaId,
            Observacao = req.Observacao,
            Valor = req.Valor,
            DiaVencimento = req.DiaVencimento is >= 1 and <= 28 ? req.DiaVencimento : 10,
            DataInicio = req.DataInicio.HasValue
                ? DateTime.SpecifyKind(req.DataInicio.Value.Date, DateTimeKind.Utc)
                : null,
        };
        db.LancamentosFixos.Add(fixo);
        await db.SaveChangesAsync();

        var agora = DateTime.UtcNow;
        var mesAtual = new DateTime(agora.Year, agora.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        await financeiroService.GerarLoteFixoAsync(fixo, mesAtual);
        await db.SaveChangesAsync();

        if (req.JaPago)
        {
            var primeiraOcorrencia = await db.LancamentosFinanceiros
                .Where(l => l.LancamentoFixoId == fixo.Id)
                .OrderBy(l => l.Vencimento)
                .FirstOrDefaultAsync();
            if (primeiraOcorrencia != null)
            {
                primeiraOcorrencia.Status = "pago";
                primeiraOcorrencia.PagoEm = DateTime.UtcNow;
                await db.SaveChangesAsync();
            }
        }

        return Ok(new { fixo.Id, geradoAte = fixo.GeradoAte });
    }

    // ── Editar um fixo (valor, dia, categoria) e regenerar o lote futuro ──
    [HttpPut("fixos/{id:guid}")]
    public async Task<IActionResult> AtualizarFixo(Guid id, [FromBody] SalvarLancamentoFixoRequest req)
    {
        var lojaId = await GetLojaId();
        var fixo = await db.LancamentosFixos.FirstOrDefaultAsync(f => f.Id == id && f.LojaId == lojaId);
        if (fixo is null) return NotFound();

        fixo.ContaBancariaId = req.ContaBancariaId;
        fixo.Descricao = req.Descricao.Trim();
        fixo.CategoriaId = req.CategoriaId;
        fixo.Valor = req.Valor;
        fixo.DiaVencimento = req.DiaVencimento is >= 1 and <= 28 ? req.DiaVencimento : 10;

        await financeiroService.LimparFuturosAsync(id);

        var agora = DateTime.UtcNow;
        var mesAtual = new DateTime(agora.Year, agora.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        await financeiroService.GerarLoteFixoAsync(fixo, mesAtual);

        await db.SaveChangesAsync();
        return Ok(new { fixo.Id, geradoAte = fixo.GeradoAte });
    }

    // ── "Errei, apaga tudo que criou pra frente e gera de novo" ─────
    [HttpPost("fixos/{id:guid}/regenerar")]
    public async Task<IActionResult> RegenerarFixo(Guid id)
    {
        var lojaId = await GetLojaId();
        var fixo = await db.LancamentosFixos.FirstOrDefaultAsync(f => f.Id == id && f.LojaId == lojaId);
        if (fixo is null) return NotFound();

        await financeiroService.LimparFuturosAsync(id);
        await db.SaveChangesAsync();

        var agora = DateTime.UtcNow;
        var mesAtual = new DateTime(agora.Year, agora.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        await financeiroService.GerarLoteFixoAsync(fixo, mesAtual);

        await db.SaveChangesAsync();
        return Ok(new { fixo.Id, geradoAte = fixo.GeradoAte });
    }

    [HttpPatch("fixos/{id:guid}/ativo")]
    public async Task<IActionResult> AlternarFixo(Guid id)
    {
        var lojaId = await GetLojaId();
        var fixo = await db.LancamentosFixos.FirstOrDefaultAsync(f => f.Id == id && f.LojaId == lojaId);
        if (fixo is null) return NotFound();

        fixo.Ativa = !fixo.Ativa;
        await db.SaveChangesAsync();
        return Ok(new { fixo.Id, fixo.Ativa });
    }

    // ── Marcar lançamento como pago/pendente ──────────────────────────
    [HttpPost("lancamentos/{id:guid}/pagamento")]
    public async Task<IActionResult> MarcarPagamento(Guid id, [FromBody] MarcarPagamentoRequest req)
    {
        var lojaId = await GetLojaId();
        var lanc = await db.LancamentosFinanceiros.FirstOrDefaultAsync(l => l.Id == id && l.LojaId == lojaId);
        if (lanc is null) return NotFound();

        lanc.Status = req.Pago ? "pago" : "pendente";
        lanc.PagoEm = req.Pago ? DateTime.UtcNow : null;
        await db.SaveChangesAsync();

        return Ok(new { lanc.Id, lanc.Status });
    }

    [HttpPut("lancamentos/{id:guid}")]
    public async Task<IActionResult> EditarLancamento(Guid id, [FromQuery] string modo, [FromBody] EditarLancamentoRequest req)
    {
        var lojaId = await GetLojaId();
        var lanc = await db.LancamentosFinanceiros.FirstOrDefaultAsync(l => l.Id == id && l.LojaId == lojaId);
        if (lanc is null) return NotFound();

        var novoVencimento = DateTime.SpecifyKind(req.Vencimento.Date, DateTimeKind.Utc).AddHours(12);

        if (modo == "todas" && lanc.Modo == "fixa" && lanc.LancamentoFixoId.HasValue)
        {
            var fixo = await db.LancamentosFixos.FindAsync(lanc.LancamentoFixoId.Value);
            if (fixo is null) return NotFound();

            fixo.Descricao = req.Descricao.Trim();
            fixo.CategoriaId = req.CategoriaId;
            fixo.ContaBancariaId = req.ContaBancariaId;
            fixo.Valor = req.Valor;
            fixo.Observacao = req.Observacao;
            fixo.DiaVencimento = novoVencimento.Day;

            await financeiroService.LimparFuturosAsync(fixo.Id);
            await db.SaveChangesAsync();

            var agora = DateTime.UtcNow;
            var mesAtual = new DateTime(agora.Year, agora.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            await financeiroService.GerarLoteFixoAsync(fixo, mesAtual);
        }
        else if (modo == "todas" && lanc.Modo == "parcelada" && lanc.GrupoParcelamentoId.HasValue)
        {
            var pendentes = await db.LancamentosFinanceiros
                .Where(l => l.GrupoParcelamentoId == lanc.GrupoParcelamentoId.Value && l.Status == "pendente")
                .ToListAsync();

            var novoDia = novoVencimento.Day;
            foreach (var p in pendentes)
            {
                p.Descricao = req.Descricao.Trim();
                p.CategoriaId = req.CategoriaId;
                p.ContaBancariaId = req.ContaBancariaId;
                p.Valor = req.Valor;
                p.Observacao = req.Observacao;
                var diasNoMes = DateTime.DaysInMonth(p.Vencimento.Year, p.Vencimento.Month);
                p.Vencimento = new DateTime(p.Vencimento.Year, p.Vencimento.Month, Math.Min(novoDia, diasNoMes), 12, 0, 0, DateTimeKind.Utc);
            }
        }
        else
        {
            lanc.Descricao = req.Descricao.Trim();
            lanc.CategoriaId = req.CategoriaId;
            lanc.ContaBancariaId = req.ContaBancariaId;
            lanc.Valor = req.Valor;
            lanc.Observacao = req.Observacao;
            lanc.Vencimento = novoVencimento;
        }

        await db.SaveChangesAsync();
        return Ok(new { mensagem = "Lançamento atualizado." });
    }

    [HttpDelete("lancamentos/{id:guid}")]
    public async Task<IActionResult> Excluir(Guid id, [FromQuery] string modo = "unica")
    {
        var lojaId = await GetLojaId();
        var lanc = await db.LancamentosFinanceiros.FirstOrDefaultAsync(l => l.Id == id && l.LojaId == lojaId);
        if (lanc is null) return NotFound();

        if (modo == "todas" && lanc.Modo == "fixa" && lanc.LancamentoFixoId.HasValue)
        {
            var fixo = await db.LancamentosFixos.FindAsync(lanc.LancamentoFixoId.Value);
            if (fixo != null) fixo.Ativa = false; // para de gerar novos meses

            var futurosPendentes = await db.LancamentosFinanceiros
                .Where(l => l.LancamentoFixoId == lanc.LancamentoFixoId.Value && l.Status == "pendente" && l.Vencimento >= lanc.Vencimento)
                .ToListAsync();
            db.LancamentosFinanceiros.RemoveRange(futurosPendentes);
        }
        else if (modo == "todas" && lanc.Modo == "parcelada" && lanc.GrupoParcelamentoId.HasValue)
        {
            var pendentesGrupo = await db.LancamentosFinanceiros
                .Where(l => l.GrupoParcelamentoId == lanc.GrupoParcelamentoId.Value && l.Status == "pendente")
                .ToListAsync();
            db.LancamentosFinanceiros.RemoveRange(pendentesGrupo);
        }
        else
        {
            db.LancamentosFinanceiros.Remove(lanc);
        }

        await db.SaveChangesAsync();
        return Ok(new { mensagem = "Lançamento excluído." });
    }

    // ── Contas a Pagar unificado (lançamentos + cartões) ───────────
    [HttpGet("pagar-unificado")]
    public async Task<IActionResult> PagarUnificado([FromQuery] int? ano, [FromQuery] int? mes, [FromQuery] string modo = "agrupado", [FromQuery] DateTime? de = null, [FromQuery] DateTime? ate = null)
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return Ok(Array.Empty<object>());

        DateTime inicioMes, fimMes;
        if (de.HasValue && ate.HasValue)
        {
            inicioMes = DateTime.SpecifyKind(de.Value.Date, DateTimeKind.Utc);
            fimMes = DateTime.SpecifyKind(ate.Value.Date, DateTimeKind.Utc).AddDays(1);
        }
        else
        {
            var a = ano ?? DateTime.UtcNow.Year;
            var m = mes ?? DateTime.UtcNow.Month;
            inicioMes = new DateTime(a, m, 1, 0, 0, 0, DateTimeKind.Utc);
            fimMes = inicioMes.AddMonths(1);
        }

        var lancamentos = await db.LancamentosFinanceiros
            .Where(l => l.LojaId == lojaId && l.Tipo == "pagar" && l.Vencimento >= inicioMes && l.Vencimento < fimMes)
            .Include(l => l.Categoria)
            .Select(l => new
            {
                l.Id,
                descricao = l.Descricao,
                observacao = l.Observacao,
                categoriaNome = l.Categoria != null ? l.Categoria.Nome : null,
                categoriaId = l.CategoriaId,
                contaBancariaId = l.ContaBancariaId,
                modo = l.Modo,
                avisar = l.Avisar,
                valor = l.Valor,
                vencimento = l.Vencimento,
                status = l.Status,
                pagoEm = l.PagoEm,
                numeroParcela = l.NumeroParcela,
                totalParcelas = l.TotalParcelas,
                origem = "avulso",
                cartaoId = (Guid?)null,
                cartaoNome = (string?)null,
            })
            .ToListAsync();

        var cartoes = await db.CartoesCredito.Where(c => c.LojaId == lojaId && c.Ativo).ToListAsync();
        var linhasCartao = new List<object>();

        var mesesCursor = new DateTime(inicioMes.Year, inicioMes.Month, 1);
        var mesesLimite = new DateTime(fimMes.AddDays(-1).Year, fimMes.AddDays(-1).Month, 1);
        var mesesParaChecar = new List<(int Ano, int Mes)>();
        int seguranca = 0;
        while (mesesCursor <= mesesLimite && seguranca < 36)
        {
            mesesParaChecar.Add((mesesCursor.Year, mesesCursor.Month));
            mesesCursor = mesesCursor.AddMonths(1);
            seguranca++;
        }

        foreach (var cartao in cartoes)
            foreach (var (anoC, mesC) in mesesParaChecar)
            {
                var vencimentoFatura = CalcularVencimentoFatura(cartao, anoC, mesC);
                if (vencimentoFatura < inicioMes || vencimentoFatura >= fimMes) continue;
                var (inicio, fim) = CicloDaFatura(cartao, vencimentoFatura);

                var itensCiclo = await db.LancamentosCartao
                .Where(l => l.CartaoCreditoId == cartao.Id && l.DataCompra.Date >= inicio.Date && l.DataCompra.Date <= fim.Date)
                .Include(l => l.Categoria)
                .OrderBy(l => l.DataCompra)
                .ToListAsync();

                if (itensCiclo.Count == 0) continue;

                var total = itensCiclo.Sum(i => i.Valor);
                var faturaExistente = await db.FaturasCartao
                    .FirstOrDefaultAsync(f => f.CartaoCreditoId == cartao.Id && f.MesReferencia.Year == anoC && f.MesReferencia.Month == mesC);

                if (modo == "detalhado")
                {
                    foreach (var item in itensCiclo)
                    {
                        linhasCartao.Add(new
                        {
                            id = item.Id,
                            descricao = $"{cartao.Nome} — {item.Descricao}",
                            categoriaNome = item.Categoria?.Nome,
                            valor = item.Valor,
                            vencimento = vencimentoFatura,
                            status = faturaExistente?.Status ?? "pendente",
                            pagoEm = faturaExistente?.PagoEm,
                            numeroParcela = (int?)null,
                            totalParcelas = (int?)null,
                            origem = "cartao_item",
                            cartaoId = cartao.Id,
                            cartaoNome = cartao.Nome,
                        });
                    }
                }
                else
                {
                    var totalAntecipadoLinha = faturaExistente is null ? 0 : await db.PagamentosAntecipadosFatura
                        .Where(p => p.FaturaCartaoId == faturaExistente.Id)
                        .SumAsync(p => (decimal?)p.Valor) ?? 0;
                    var totalRestanteLinha = total - totalAntecipadoLinha;
                    if (totalRestanteLinha <= 0) continue;

                    linhasCartao.Add(new
                    {
                        id = cartao.Id,
                        descricao = $"Fatura {cartao.Nome}" + (totalAntecipadoLinha > 0 ? $" (já antecipado {totalAntecipadoLinha:C})" : ""),
                        categoriaNome = (string?)null,
                        valor = totalRestanteLinha,
                        vencimento = vencimentoFatura,
                        status = faturaExistente?.Status ?? "pendente",
                        pagoEm = faturaExistente?.PagoEm,
                        numeroParcela = (int?)null,
                        totalParcelas = (int?)null,
                        origem = faturaExistente?.Status == "financiada" ? "cartao_fatura_financiada" : "cartao_fatura",
                        cartaoId = cartao.Id,
                        cartaoNome = cartao.Nome,
                    });
                }
            }

                var unificado = lancamentos.Cast<object>().Concat(linhasCartao)
            .OrderBy(x => ((dynamic)x).vencimento)
            .ToList();

        return Ok(unificado);
    }

    // ══════════════════ CATEGORIAS ══════════════════

    [HttpGet("lancamentos/descricoes")]
    public async Task<IActionResult> ListarDescricoesRecentes([FromQuery] string tipo)
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return Ok(Array.Empty<string>());

        var descricoes = await db.LancamentosFinanceiros
            .Where(l => l.LojaId == lojaId && l.Tipo == tipo)
            .OrderByDescending(l => l.CriadoEm)
            .Select(l => l.Descricao)
            .Distinct()
            .Take(50)
            .ToListAsync();

        return Ok(descricoes);
    }

    [HttpGet("cartoes/lancamentos/descricoes")]
    public async Task<IActionResult> ListarDescricoesRecentesCartao()
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return Ok(Array.Empty<string>());

        var descricoes = await db.LancamentosCartao
            .Where(l => l.LojaId == lojaId)
            .OrderByDescending(l => l.CriadoEm)
            .Select(l => l.Descricao)
            .Distinct()
            .Take(50)
            .ToListAsync();

        return Ok(descricoes);
    }

    [HttpGet("categorias")]
    public async Task<IActionResult> ListarCategorias([FromQuery] string? tipo)
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return Ok(Array.Empty<object>());

        var q = db.CategoriasFinanceiras.Where(c => c.LojaId == lojaId && c.Ativa);
        if (!string.IsNullOrEmpty(tipo))
            q = q.Where(c => c.Tipo == tipo || c.Tipo == "ambos");

        var lista = await q.OrderBy(c => c.Nome)
            .Select(c => new { c.Id, c.Nome, c.Tipo, c.Icone })
            .ToListAsync();

        return Ok(lista);
    }



    [HttpPost("categorias")]
    public async Task<IActionResult> CriarCategoria([FromBody] SalvarCategoriaFinanceiraRequest req)
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return BadRequest(new { erro = "Loja não encontrada." });

        var cat = new CategoriaFinanceira
        {
            LojaId = lojaId.Value,
            Nome = req.Nome.Trim(),
            Tipo = req.Tipo,
            Icone = req.Icone,
        };
        db.CategoriasFinanceiras.Add(cat);
        await db.SaveChangesAsync();

        return Ok(new { cat.Id, cat.Nome, cat.Tipo, cat.Icone });
    }

    [HttpPut("categorias/{id:guid}")]
    public async Task<IActionResult> AtualizarCategoria(Guid id, [FromBody] SalvarCategoriaFinanceiraRequest req)
    {
        var lojaId = await GetLojaId();
        var cat = await db.CategoriasFinanceiras.FirstOrDefaultAsync(c => c.Id == id && c.LojaId == lojaId);
        if (cat is null) return NotFound();

        cat.Nome = req.Nome.Trim();
        cat.Tipo = req.Tipo;
        cat.Icone = req.Icone;
        await db.SaveChangesAsync();

        return Ok(new { cat.Id, cat.Nome, cat.Tipo, cat.Icone });
    }

    [HttpDelete("categorias/{id:guid}")]
    public async Task<IActionResult> ExcluirCategoria(Guid id)
    {
        var lojaId = await GetLojaId();
        var cat = await db.CategoriasFinanceiras.FirstOrDefaultAsync(c => c.Id == id && c.LojaId == lojaId);
        if (cat is null) return NotFound();

        var emUso = await db.LancamentosFinanceiros.AnyAsync(l => l.CategoriaId == id)
                 || await db.LancamentosFixos.AnyAsync(l => l.CategoriaId == id);
        if (emUso)
        {
            cat.Ativa = false; // desativa em vez de excluir, se já usada
            await db.SaveChangesAsync();
            return Ok(new { mensagem = "Categoria em uso — foi desativada em vez de excluída." });
        }

        db.CategoriasFinanceiras.Remove(cat);
        await db.SaveChangesAsync();
        return Ok(new { mensagem = "Categoria excluída." });
    }

    // ── Resumo mensal — Pagar e Receber lado a lado ────────────────
    [HttpGet("resumo-mensal")]
    public async Task<IActionResult> ResumoMensal([FromQuery] int ano, [FromQuery] int mes)
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return Ok(new { });

        var inicio = new DateTime(ano, mes, 1, 0, 0, 0, DateTimeKind.Utc);
        var fim = inicio.AddMonths(1);
        var hoje = DateTime.UtcNow.Date;

        var doMes = await db.LancamentosFinanceiros
            .Where(l => l.LojaId == lojaId && l.Vencimento >= inicio && l.Vencimento < fim)
            .ToListAsync();

        decimal pagarPago = doMes.Where(l => l.Tipo == "pagar" && l.Status == "pago").Sum(l => l.Valor);
        int pagarQtdPago = doMes.Count(l => l.Tipo == "pagar" && l.Status == "pago");
        decimal pagarPendente = doMes.Where(l => l.Tipo == "pagar" && l.Status == "pendente" && l.Vencimento.Date >= hoje).Sum(l => l.Valor);
        int pagarQtdPendente = doMes.Count(l => l.Tipo == "pagar" && l.Status == "pendente" && l.Vencimento.Date >= hoje);
        decimal pagarVencido = doMes.Where(l => l.Tipo == "pagar" && l.Status == "pendente" && l.Vencimento.Date < hoje).Sum(l => l.Valor);
        int pagarQtdVencido = doMes.Count(l => l.Tipo == "pagar" && l.Status == "pendente" && l.Vencimento.Date < hoje);

        decimal receberPago = doMes.Where(l => l.Tipo == "receber" && l.Status == "pago").Sum(l => l.Valor);
        int receberQtdPago = doMes.Count(l => l.Tipo == "receber" && l.Status == "pago");
        decimal receberPendente = doMes.Where(l => l.Tipo == "receber" && l.Status == "pendente" && l.Vencimento.Date >= hoje).Sum(l => l.Valor);
        int receberQtdPendente = doMes.Count(l => l.Tipo == "receber" && l.Status == "pendente" && l.Vencimento.Date >= hoje);
        decimal receberVencido = doMes.Where(l => l.Tipo == "receber" && l.Status == "pendente" && l.Vencimento.Date < hoje).Sum(l => l.Valor);
        int receberQtdVencido = doMes.Count(l => l.Tipo == "receber" && l.Status == "pendente" && l.Vencimento.Date < hoje);

        var assinaturaIdsResumo = await db.AssinaturasCliente.Where(a => a.LojaId == lojaId).Select(a => a.Id).ToListAsync();
        var pagamentosPlanoMes = await db.PagamentosPlano
            .Where(p => assinaturaIdsResumo.Contains(p.AssinaturaId) && p.MesReferencia >= inicio && p.MesReferencia < fim)
            .ToListAsync();

        foreach (var p in pagamentosPlanoMes)
        {
            if (p.Status == "pago") { receberPago += p.Valor; receberQtdPago++; }
            else if (p.MesReferencia.Date < hoje) { receberVencido += p.Valor; receberQtdVencido++; }
            else { receberPendente += p.Valor; receberQtdPendente++; }
        }

        var detalheCartoesPagar = new List<object>();
        var cartoes = await db.CartoesCredito.Where(c => c.LojaId == lojaId && c.Ativo).ToListAsync();
        foreach (var cartao in cartoes)
        {
            var vencimentoFatura = CalcularVencimentoFatura(cartao, ano, mes);
            if (vencimentoFatura < inicio || vencimentoFatura >= fim) continue;

            var (cInicio, cFim) = CicloDaFatura(cartao, vencimentoFatura);
            var totalFatura = await db.LancamentosCartao
                .Where(l => l.CartaoCreditoId == cartao.Id && l.DataCompra.Date >= cInicio.Date && l.DataCompra.Date <= cFim.Date)
                .SumAsync(l => (decimal?)l.Valor) ?? 0;

            if (totalFatura <= 0) continue;

            var faturaExistente = await db.FaturasCartao
                .FirstOrDefaultAsync(f => f.CartaoCreditoId == cartao.Id && f.MesReferencia.Year == ano && f.MesReferencia.Month == mes);

            var totalAntecipadoResumo = faturaExistente is null ? 0 : await db.PagamentosAntecipadosFatura
                .Where(p => p.FaturaCartaoId == faturaExistente.Id)
                .SumAsync(p => (decimal?)p.Valor) ?? 0;
            var totalFaturaRestante = totalFatura - totalAntecipadoResumo;

            if (faturaExistente?.Status == "pago" || faturaExistente?.Status == "parcial" || faturaExistente?.Status == "financiada")
            {
                // Fatura resolvida (paga, parcialmente paga ou financiada em parcelas) — não conta como pendente/vencida
                pagarPago += totalFaturaRestante; pagarQtdPago++;
            }
            else if (totalFaturaRestante <= 0)
            {
                pagarPago += totalFatura; pagarQtdPago++;
            }
            else if (vencimentoFatura.Date < hoje) { pagarVencido += totalFaturaRestante; pagarQtdVencido++; }
            else { pagarPendente += totalFaturaRestante; pagarQtdPendente++; }

            detalheCartoesPagar.Add(new { nome = cartao.Nome, valor = totalFaturaRestante > 0 ? totalFaturaRestante : totalFatura, status = totalFaturaRestante <= 0 ? "pago" : (faturaExistente?.Status ?? "pendente") });
        }

        var totalLancamentosPagar = pagarPago + pagarPendente + pagarVencido - detalheCartoesPagar.Sum(c => (decimal)((dynamic)c).valor);

        var previstoReceita = receberPago + receberPendente + receberVencido;
        var previstoDespesa = pagarPago + pagarPendente + pagarVencido;

        return Ok(new
        {
            pagar = new { totalPago = pagarPago, qtdPago = pagarQtdPago, totalPendente = pagarPendente, qtdPendente = pagarQtdPendente, totalVencido = pagarVencido, qtdVencido = pagarQtdVencido },
            receber = new { totalPago = receberPago, qtdPago = receberQtdPago, totalPendente = receberPendente, qtdPendente = receberQtdPendente, totalVencido = receberVencido, qtdVencido = receberQtdVencido },
            previsao = new { receitaPrevista = previstoReceita, despesaPrevista = previstoDespesa, saldoPrevisto = previstoReceita - previstoDespesa },
            detalhePagar = new { lancamentos = totalLancamentosPagar, cartoes = detalheCartoesPagar }
        });
    }

    // ── Balanço mensal por categoria (Receitas x Despesas agrupado) ──
    [HttpGet("balanco-por-categoria")]
    public async Task<IActionResult> BalancoPorCategoria([FromQuery] int ano, [FromQuery] int mes)
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return Ok(new { });

        var inicio = new DateTime(ano, mes, 1, 0, 0, 0, DateTimeKind.Utc);
        var fim = inicio.AddMonths(1);

        var doMes = await db.LancamentosFinanceiros
            .Where(l => l.LojaId == lojaId && l.Vencimento >= inicio && l.Vencimento < fim)
            .Include(l => l.Categoria)
            .ToListAsync();

        // Planos entram como receita agrupada em "Mensalidade/Assinatura"
        var assinaturaIds = await db.AssinaturasCliente.Where(a => a.LojaId == lojaId).Select(a => a.Id).ToListAsync();
        var pagamentosPlanoMes = await db.PagamentosPlano
            .Where(p => assinaturaIds.Contains(p.AssinaturaId) && p.MesReferencia >= inicio && p.MesReferencia < fim)
            .SumAsync(p => (decimal?)p.Valor) ?? 0;

        // Cartões entram como despesa agrupada em "Cartão de Crédito"
        var cartoes = await db.CartoesCredito.Where(c => c.LojaId == lojaId && c.Ativo).ToListAsync();
        decimal totalCartoesMes = 0;
        foreach (var cartao in cartoes)
        {
            var vencimentoFatura = CalcularVencimentoFatura(cartao, ano, mes);
            if (vencimentoFatura < inicio || vencimentoFatura >= fim) continue;
            var (cInicio, cFim) = CicloDaFatura(cartao, vencimentoFatura);
            totalCartoesMes += await db.LancamentosCartao
                .Where(l => l.CartaoCreditoId == cartao.Id && l.DataCompra.Date >= cInicio.Date && l.DataCompra.Date <= cFim.Date)
                .SumAsync(l => (decimal?)l.Valor) ?? 0;
        }

        var receitasPorCategoria = doMes
            .Where(l => l.Tipo == "receber")
            .GroupBy(l => new { Nome = l.Categoria?.Nome ?? "Sem categoria", Icone = l.Categoria?.Icone ?? "📁" })
            .Select(g => new { nome = g.Key.Nome, icone = g.Key.Icone, valor = g.Sum(x => x.Valor) })
            .ToList();

        var despesasPorCategoria = doMes
            .Where(l => l.Tipo == "pagar")
            .GroupBy(l => new { Nome = l.Categoria?.Nome ?? "Sem categoria", Icone = l.Categoria?.Icone ?? "📁" })
            .Select(g => new { nome = g.Key.Nome, icone = g.Key.Icone, valor = g.Sum(x => x.Valor) })
            .ToList();

        if (pagamentosPlanoMes > 0)
        {
            var existente = receitasPorCategoria.FirstOrDefault(r => r.nome == "Mensalidade/Assinatura");
            receitasPorCategoria = receitasPorCategoria.Where(r => r.nome != "Mensalidade/Assinatura")
                .Append(new { nome = "Mensalidade/Assinatura", icone = "💳", valor = (existente?.valor ?? 0) + pagamentosPlanoMes })
                .ToList();
        }

        if (totalCartoesMes > 0)
        {
            var existente = despesasPorCategoria.FirstOrDefault(d => d.nome == "Cartão de Crédito");
            despesasPorCategoria = despesasPorCategoria.Where(d => d.nome != "Cartão de Crédito")
                .Append(new { nome = "Cartão de Crédito", icone = "💳", valor = (existente?.valor ?? 0) + totalCartoesMes })
                .ToList();
        }

        var totalReceitas = receitasPorCategoria.Sum(r => r.valor);
        var totalDespesas = despesasPorCategoria.Sum(d => d.valor);

        return Ok(new
        {
            receitas = receitasPorCategoria.OrderByDescending(r => r.valor),
            despesas = despesasPorCategoria.OrderByDescending(d => d.valor),
            totalReceitas,
            totalDespesas,
            saldo = totalReceitas - totalDespesas,
        });
    }

    [HttpPost("categorias/seed-padrao")]
    public async Task<IActionResult> SeedCategoriasPadrao()
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return BadRequest(new { erro = "Loja não encontrada." });

        var jaTem = await db.CategoriasFinanceiras.AnyAsync(c => c.LojaId == lojaId);
        if (jaTem)
            return BadRequest(new { erro = "Você já tem categorias cadastradas." });

        var padrao = new (string Nome, string Tipo, string Icone)[]
        {
            ("Aluguel", "pagar", "🏠"),
            ("Água", "pagar", "💧"),
            ("Luz", "pagar", "💡"),
            ("Internet", "pagar", "📶"),
            ("Fornecedor", "pagar", "📦"),
            ("Salário/Folha", "pagar", "👤"),
            ("Impostos", "pagar", "🧾"),
            ("Mensalidade/Assinatura", "receber", "💳"),
            ("Venda avulsa", "receber", "🛒"),
            ("Outros", "ambos", "📁"),
        };

        foreach (var (nome, tipo, icone) in padrao)
            db.CategoriasFinanceiras.Add(new CategoriaFinanceira
            {
                LojaId = lojaId.Value,
                Nome = nome,
                Tipo = tipo,
                Icone = icone,
            });

        await db.SaveChangesAsync();
        return Ok(new { mensagem = "Categorias padrão criadas." });
    }

    // ══════════════════ CARTÕES DE CRÉDITO ══════════════════

    [HttpGet("cartoes")]
    public async Task<IActionResult> ListarCartoes()
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return Ok(Array.Empty<object>());

        var cartoes = await db.CartoesCredito.Where(c => c.LojaId == lojaId)
            .Select(c => new { c.Id, c.Nome, c.Limite, c.DiaFechamento, c.DiaVencimento, c.ContaBancariaId, c.Ativo, c.TaxaJurosMensal })
            .ToListAsync();

        return Ok(cartoes);
    }

    [HttpPost("cartoes")]
    public async Task<IActionResult> CriarCartao([FromBody] SalvarCartaoRequest req)
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return BadRequest(new { erro = "Loja não encontrada." });

        var cartao = new CartaoCredito
        {
            LojaId = lojaId.Value,
            Nome = req.Nome.Trim(),
            Limite = req.Limite,
            DiaFechamento = req.DiaFechamento,
            DiaVencimento = req.DiaVencimento,
            ContaBancariaId = req.ContaBancariaId,
            TaxaJurosMensal = req.TaxaJurosMensal,
        };
        db.CartoesCredito.Add(cartao);
        await db.SaveChangesAsync();
        return Ok(new { cartao.Id });
    }

    [HttpPut("cartoes/{id:guid}")]
    public async Task<IActionResult> AtualizarCartao(Guid id, [FromBody] SalvarCartaoRequest req)
    {
        var lojaId = await GetLojaId();
        var cartao = await db.CartoesCredito.FirstOrDefaultAsync(c => c.Id == id && c.LojaId == lojaId);
        if (cartao is null) return NotFound();

        cartao.Nome = req.Nome.Trim();
        cartao.Limite = req.Limite;
        cartao.DiaFechamento = req.DiaFechamento;
        cartao.DiaVencimento = req.DiaVencimento;
        cartao.ContaBancariaId = req.ContaBancariaId;
        cartao.TaxaJurosMensal = req.TaxaJurosMensal;
        await db.SaveChangesAsync();
        return Ok(new { cartao.Id });
    }

    // ── Lançar uma compra no cartão ────────────────────────────────
    [HttpPost("cartoes/{id:guid}/lancamentos")]
    public async Task<IActionResult> LancarCompra(Guid id, [FromBody] SalvarLancamentoCartaoRequest req)
    {
        var lojaId = await GetLojaId();
        var cartao = await db.CartoesCredito.FirstOrDefaultAsync(c => c.Id == id && c.LojaId == lojaId);
        if (cartao is null) return NotFound();

        db.LancamentosCartao.Add(new LancamentoCartao
        {
            LojaId = lojaId!.Value,
            CartaoCreditoId = id,
            Descricao = req.Descricao.Trim(),
            Valor = req.Valor,
            DataCompra = DateTime.SpecifyKind(req.DataCompra.Date, DateTimeKind.Utc).AddHours(12),
            Modo = "avulsa",
            CategoriaId = req.CategoriaId,
            Observacao = req.Observacao,
        });
        await db.SaveChangesAsync();
        return Ok(new { mensagem = "Lançamento adicionado." });
    }

    // ── Compra parcelada no cartão (ex: TV em 10x) ──────────────────
    public record CompraParceladaCartaoRequest(string Descricao, decimal ValorParcela, int TotalParcelas, DateTime DataCompra, Guid? CategoriaId, string? Observacao = null);

    [HttpPost("cartoes/{id:guid}/lancamentos/parcelado")]
    public async Task<IActionResult> LancarCompraParcelada(Guid id, [FromBody] CompraParceladaCartaoRequest req)
    {
        var lojaId = await GetLojaId();
        var cartao = await db.CartoesCredito.FirstOrDefaultAsync(c => c.Id == id && c.LojaId == lojaId);
        if (cartao is null) return NotFound();

        if (req.TotalParcelas < 2 || req.TotalParcelas > 24)
            return BadRequest(new { erro = "Escolha entre 2 e 24 parcelas." });

        var grupoId = Guid.NewGuid();
        var dataBase = DateTime.SpecifyKind(req.DataCompra.Date, DateTimeKind.Utc).AddHours(12);

        for (int i = 0; i < req.TotalParcelas; i++)
        {
            db.LancamentosCartao.Add(new LancamentoCartao
            {
                LojaId = lojaId!.Value,
                CartaoCreditoId = id,
                Descricao = $"{req.Descricao.Trim()} ({i + 1}/{req.TotalParcelas})",
                Valor = req.ValorParcela,
                DataCompra = dataBase.AddMonths(i),
                Modo = "parcelada",
                GrupoParcelamentoId = grupoId,
                NumeroParcela = i + 1,
                TotalParcelas = req.TotalParcelas,
                CategoriaId = req.CategoriaId,
                Observacao = req.Observacao,
            });
        }

        await db.SaveChangesAsync();
        return Ok(new { grupoParcelamentoId = grupoId, totalGerado = req.TotalParcelas });
    }

    // ── Lançamento fixo/recorrente no cartão (ex: Netflix) ──────────
    public record CartaoFixoRequest(string Descricao, decimal Valor, Guid? CategoriaId, string? Observacao = null, int DiaCompra = 1);

    [HttpPost("cartoes/{id:guid}/fixos")]
    public async Task<IActionResult> CriarCartaoFixo(Guid id, [FromBody] CartaoFixoRequest req)
    {
        var lojaId = await GetLojaId();
        var cartao = await db.CartoesCredito.FirstOrDefaultAsync(c => c.Id == id && c.LojaId == lojaId);
        if (cartao is null) return NotFound();

        var fixo = new CartaoLancamentoFixo
        {
            LojaId = lojaId!.Value,
            CartaoCreditoId = id,
            Descricao = req.Descricao.Trim(),
            Valor = req.Valor,
            CategoriaId = req.CategoriaId,
            Observacao = req.Observacao,
            DiaCompra = req.DiaCompra is >= 1 and <= 28 ? req.DiaCompra : 1,
        };
        db.CartaoLancamentosFixos.Add(fixo);
        await db.SaveChangesAsync();

        var agora = DateTime.UtcNow;
        var mesAtual = new DateTime(agora.Year, agora.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        await financeiroService.GerarLoteFixoCartaoAsync(fixo, cartao, mesAtual);
        await db.SaveChangesAsync();

        return Ok(new { fixo.Id, geradoAte = fixo.GeradoAte });
    }

    [HttpGet("cartoes/{id:guid}/fixos")]
    public async Task<IActionResult> ListarCartaoFixos(Guid id)
    {
        var lojaId = await GetLojaId();
        var lista = await db.CartaoLancamentosFixos
            .Where(f => f.CartaoCreditoId == id && f.CartaoCredito!.LojaId == lojaId)
            .Select(f => new { f.Id, f.Descricao, f.Valor, f.Ativo })
            .ToListAsync();
        return Ok(lista);
    }

    [HttpPatch("cartoes/fixos/{id:guid}/ativo")]
    public async Task<IActionResult> AlternarCartaoFixo(Guid id)
    {
        var lojaId = await GetLojaId();
        var fixo = await db.CartaoLancamentosFixos.Include(f => f.CartaoCredito)
            .FirstOrDefaultAsync(f => f.Id == id && f.CartaoCredito!.LojaId == lojaId);
        if (fixo is null) return NotFound();

        fixo.Ativo = !fixo.Ativo;
        if (!fixo.Ativo) await financeiroService.LimparFuturosCartaoAsync(id);
        await db.SaveChangesAsync();
        return Ok(new { fixo.Id, fixo.Ativo });
    }

    // ── Calcula a fatura de um mês (sob demanda, sem precisar de job) ──
    private DateTime CalcularVencimentoFatura(CartaoCredito cartao, int ano, int mes)
        => new DateTime(ano, mes, Math.Min(cartao.DiaVencimento, DateTime.DaysInMonth(ano, mes)), 12, 0, 0, DateTimeKind.Utc);

    private (DateTime Inicio, DateTime Fim) CicloDaFatura(CartaoCredito cartao, DateTime vencimento)
    {
        // Fechamento do ciclo = 1 mês antes do vencimento, no dia de fechamento.
        // O DIA DE FECHAMENTO em si pertence à fatura que está fechando (inclusive),
        // então o ciclo vai do dia seguinte ao fechamento anterior até o fechamento atual, inclusive.
        var fechamentoAtual = new DateTime(vencimento.Year, vencimento.Month, Math.Min(cartao.DiaFechamento, DateTime.DaysInMonth(vencimento.Year, vencimento.Month)), 0, 0, 0, DateTimeKind.Utc);
        var fechamentoAnterior = fechamentoAtual.AddMonths(-1);
        return (fechamentoAnterior.AddDays(1), fechamentoAtual);
    }

    // ── Listar faturas + total, agrupado ou detalhado ──────────────
    [HttpGet("cartoes/{id:guid}/fatura")]
    public async Task<IActionResult> VerFatura(Guid id, [FromQuery] int ano, [FromQuery] int mes)
    {
        var lojaId = await GetLojaId();
        var cartao = await db.CartoesCredito.FirstOrDefaultAsync(c => c.Id == id && c.LojaId == lojaId);
        if (cartao is null) return NotFound();

        var vencimento = CalcularVencimentoFatura(cartao, ano, mes);
        var (inicio, fim) = CicloDaFatura(cartao, vencimento);

        var itens = await db.LancamentosCartao
            .Where(l => l.CartaoCreditoId == id && l.DataCompra.Date >= inicio.Date && l.DataCompra.Date <= fim.Date)
            .Include(l => l.Categoria)
            .OrderBy(l => l.DataCompra)
            .Select(l => new { l.Id, l.Descricao, l.Valor, l.DataCompra, categoriaNome = l.Categoria != null ? l.Categoria.Nome : null, categoriaId = l.CategoriaId, l.Modo, l.Observacao })
            .ToListAsync();

        var faturaExistente = await db.FaturasCartao
            .FirstOrDefaultAsync(f => f.CartaoCreditoId == id && f.MesReferencia.Year == ano && f.MesReferencia.Month == mes);

        // Parcelas do financiamento que caem NESTE mês (independente de qual fatura originou o parcelamento)
        var parcelasFinanciamentoRaw = await db.LancamentosFinanceiros
            .Where(l => l.CartaoOrigemId == id && l.Vencimento.Year == ano && l.Vencimento.Month == mes)
            .OrderBy(l => l.NumeroParcela)
            .ToListAsync();

        var faturasOrigemIds = parcelasFinanciamentoRaw.Where(l => l.FaturaCartaoId.HasValue).Select(l => l.FaturaCartaoId!.Value).Distinct().ToList();
        var faturasOrigem = await db.FaturasCartao.Where(f => faturasOrigemIds.Contains(f.Id)).ToListAsync();

        var parcelasFinanciamento = parcelasFinanciamentoRaw.Select(l =>
        {
            var faturaOrigem = l.FaturaCartaoId.HasValue ? faturasOrigem.FirstOrDefault(f => f.Id == l.FaturaCartaoId.Value) : null;
            return new
            {
                l.Id,
                l.Descricao,
                l.Valor,
                l.Vencimento,
                l.Status,
                l.NumeroParcela,
                l.TotalParcelas,
                l.ContaBancariaId,
                l.CategoriaId,
                l.Observacao,
                l.Modo,
                mesOrigemFatura = faturaOrigem != null ? (int?)faturaOrigem.MesReferencia.Month : null,
                anoOrigemFatura = faturaOrigem != null ? (int?)faturaOrigem.MesReferencia.Year : null,
            };
        }).ToList();

        var total = itens.Sum(i => i.Valor) + parcelasFinanciamento.Sum(p => p.Valor);

        var faturaIdAtual = faturaExistente?.Id ?? Guid.Empty;
        var antecipados = await db.PagamentosAntecipadosFatura
            .Where(p => p.FaturaCartaoId == faturaIdAtual)
            .OrderBy(p => p.Data)
            .Select(p => new { p.Id, p.Valor, p.Data, p.ContaBancariaId, p.Observacao })
            .ToListAsync();
        var totalAntecipado = antecipados.Sum(a => a.Valor);

        return Ok(new
        {
            vencimento,
            total,
            totalAntecipado,
            restante = total - totalAntecipado,
            status = faturaExistente?.Status ?? "pendente",
            pagoEm = faturaExistente?.PagoEm,
            valorEntrada = faturaExistente?.Status == "financiada" ? faturaExistente.ValorPago : (decimal?)null,
            itens,
            parcelasFinanciamento,
            antecipados,
        });
    }

    public record PagarFaturaRequest(string Modo, decimal? ValorPago, int? TotalParcelas, decimal? ValorEntrada, DateTime? PrimeiraParcela, Guid? ContaBancariaId = null);
    public record AntecipadoFaturaRequest(decimal Valor, DateTime Data, Guid ContaBancariaId, string? Observacao);

    [HttpPost("cartoes/{id:guid}/fatura/antecipado")]
    public async Task<IActionResult> AdicionarAntecipadoFatura(Guid id, [FromQuery] int ano, [FromQuery] int mes, [FromBody] AntecipadoFaturaRequest req)
    {
        var lojaId = await GetLojaId();
        var cartao = await db.CartoesCredito.FirstOrDefaultAsync(c => c.Id == id && c.LojaId == lojaId);
        if (cartao is null) return NotFound();

        if (req.Valor <= 0) return BadRequest(new { erro = "Valor deve ser maior que zero." });
        if (req.Data.Date > DateTime.UtcNow.Date) return BadRequest(new { erro = "A data não pode ser no futuro." });

        var contaValida = await db.ContasBancarias.AnyAsync(c => c.Id == req.ContaBancariaId && c.LojaId == lojaId);
        if (!contaValida) return BadRequest(new { erro = "Conta bancária inválida." });

        var vencimento = CalcularVencimentoFatura(cartao, ano, mes);
        var mesReferencia = new DateTime(ano, mes, 1, 0, 0, 0, DateTimeKind.Utc);
        var (inicio, fim) = CicloDaFatura(cartao, vencimento);

        var total = await db.LancamentosCartao
            .Where(l => l.CartaoCreditoId == id && l.DataCompra.Date >= inicio.Date && l.DataCompra.Date <= fim.Date)
            .SumAsync(l => (decimal?)l.Valor) ?? 0;

        var fatura = await db.FaturasCartao
            .FirstOrDefaultAsync(f => f.CartaoCreditoId == id && f.MesReferencia == mesReferencia);

        if (fatura is null)
        {
            fatura = new FaturaCartao
            {
                LojaId = lojaId!.Value,
                CartaoCreditoId = id,
                MesReferencia = mesReferencia,
                Vencimento = vencimento,
                Total = total,
            };
            db.FaturasCartao.Add(fatura);
            await db.SaveChangesAsync();
        }

        var jaAntecipado = await db.PagamentosAntecipadosFatura
            .Where(p => p.FaturaCartaoId == fatura.Id)
            .SumAsync(p => (decimal?)p.Valor) ?? 0;

        if (jaAntecipado + req.Valor > total)
            return BadRequest(new { erro = "O valor adiantado não pode ultrapassar o total da fatura." });

        db.PagamentosAntecipadosFatura.Add(new PagamentoAntecipadoFatura
        {
            LojaId = lojaId!.Value,
            FaturaCartaoId = fatura.Id,
            ContaBancariaId = req.ContaBancariaId,
            Valor = req.Valor,
            Data = DateTime.SpecifyKind(req.Data.Date, DateTimeKind.Utc).AddHours(12),
            Observacao = req.Observacao,
        });
        await db.SaveChangesAsync();

        return Ok(new { mensagem = "Pagamento antecipado registrado." });
    }

    [HttpDelete("cartoes/fatura/antecipado/{id:guid}")]
    public async Task<IActionResult> ExcluirAntecipadoFatura(Guid id)
    {
        var lojaId = await GetLojaId();
        var antecipado = await db.PagamentosAntecipadosFatura
            .FirstOrDefaultAsync(p => p.Id == id && p.LojaId == lojaId);
        if (antecipado is null) return NotFound();

        db.PagamentosAntecipadosFatura.Remove(antecipado);
        await db.SaveChangesAsync();
        return Ok(new { mensagem = "Pagamento antecipado excluído." });
    }

    [HttpPost("cartoes/{id:guid}/fatura/pagamento")]
    public async Task<IActionResult> PagarFatura(Guid id, [FromQuery] int ano, [FromQuery] int mes, [FromBody] PagarFaturaRequest req)
    {
        var lojaId = await GetLojaId();
        var cartao = await db.CartoesCredito.FirstOrDefaultAsync(c => c.Id == id && c.LojaId == lojaId);
        if (cartao is null) return NotFound();

        var vencimento = CalcularVencimentoFatura(cartao, ano, mes);
        var mesReferencia = new DateTime(ano, mes, 1, 0, 0, 0, DateTimeKind.Utc);
        var (inicio, fim) = CicloDaFatura(cartao, vencimento);

        var total = await db.LancamentosCartao
            .Where(l => l.CartaoCreditoId == id && l.DataCompra.Date >= inicio.Date && l.DataCompra.Date <= fim.Date)
            .SumAsync(l => (decimal?)l.Valor) ?? 0;

        var fatura = await db.FaturasCartao
            .FirstOrDefaultAsync(f => f.CartaoCreditoId == id && f.MesReferencia == mesReferencia);

        if (fatura is null)
        {
            fatura = new FaturaCartao
            {
                LojaId = lojaId!.Value,
                CartaoCreditoId = id,
                MesReferencia = mesReferencia,
                Vencimento = vencimento,
                Total = total,
            };
            db.FaturasCartao.Add(fatura);
        }
        fatura.Total = total;

        var totalAntecipado = await db.PagamentosAntecipadosFatura
            .Where(p => p.FaturaCartaoId == fatura.Id)
            .SumAsync(p => (decimal?)p.Valor) ?? 0;
        var totalDevido = total - totalAntecipado;

        if (req.Modo != "desfazer" && req.ContaBancariaId.HasValue)
        {
            var contaEscolhidaValida = await db.ContasBancarias.AnyAsync(c => c.Id == req.ContaBancariaId.Value && c.LojaId == lojaId);
            if (!contaEscolhidaValida) return BadRequest(new { erro = "Conta bancária inválida." });
            fatura.ContaBancariaId = req.ContaBancariaId.Value;
        }

        switch (req.Modo)
        {
            case "desfazer":
                fatura.ContaBancariaId = null;
                // Se era um financiamento, remove todas as parcelas geradas (só as ainda não pagas, por segurança)
                if (fatura.Status == "financiada")
                {
                    var parcelasParaRemover = await db.LancamentosFinanceiros
                        .Where(l => l.FaturaCartaoId == fatura.Id && l.Status == "pendente")
                        .ToListAsync();
                    db.LancamentosFinanceiros.RemoveRange(parcelasParaRemover);
                }

                fatura.Status = "pendente";
                fatura.ValorPago = 0;
                fatura.PagoEm = null;
                break;

            case "total":
                fatura.Status = "pago";
                fatura.ValorPago = totalDevido;
                fatura.PagoEm = DateTime.UtcNow;
                break;

            case "parcial":
                {
                    var pago = req.ValorPago ?? 0;
                    if (pago <= 0 || pago >= totalDevido)
                        return BadRequest(new { erro = "Valor parcial deve ser maior que zero e menor que o total (já descontado o que foi antecipado)." });

                    var restante = totalDevido - pago;
                    var comJuros = restante * (1 + (cartao.TaxaJurosMensal / 100m));

                    // Lança o saldo devedor + juros como um item na PRÓXIMA fatura
                    var proximoVencimento = CalcularVencimentoFatura(cartao, mesReferencia.AddMonths(1).Year, mesReferencia.AddMonths(1).Month);
                    var (proxInicio, _) = CicloDaFatura(cartao, proximoVencimento);

                    db.LancamentosCartao.Add(new LancamentoCartao
                    {
                        LojaId = lojaId!.Value,
                        CartaoCreditoId = id,
                        Descricao = $"Saldo rotativo (fatura {MESES[mesReferencia.Month - 1]}) + juros",
                        Valor = comJuros,
                        DataCompra = proxInicio,
                        EhJurosRotativo = true,
                    });

                    fatura.Status = "parcial";
                    fatura.ValorPago = pago;
                    fatura.PagoEm = DateTime.UtcNow;
                    break;
                }

            case "parcelado":
                {
                    var parcelas = req.TotalParcelas ?? 0;
                    if (parcelas < 2 || parcelas > 24)
                        return BadRequest(new { erro = "Escolha entre 2 e 24 parcelas." });

                    var entrada = req.ValorEntrada ?? 0;
                    if (entrada < 0 || entrada >= totalDevido)
                        return BadRequest(new { erro = "O valor de entrada deve ser maior ou igual a zero e menor que o total (já descontado o que foi antecipado)." });

                    var valorFinanciar = totalDevido - entrada;
                    var comJuros = valorFinanciar * (1 + (cartao.TaxaJurosMensal / 100m) * parcelas);
                    var valorParcela = Math.Round(comJuros / parcelas, 2);
                    var grupoId = Guid.NewGuid();

                    // A 1ª parcela sempre cai na data de vencimento REAL do cartão (não numa
                    // data arbitrária) — isso é o que garante que ela some na fatura certa em
                    // vez de virar uma linha separada. Se o usuário escolheu uma data, usamos
                    // só o mês/ano dela; o dia é sempre recalculado pelo dia de vencimento do cartão.
                    var mesInicioParcelas = req.PrimeiraParcela.HasValue
                        ? new DateTime(req.PrimeiraParcela.Value.Year, req.PrimeiraParcela.Value.Month, 1)
                        : mesReferencia.AddMonths(1);

                    for (int i = 0; i < parcelas; i++)
                    {
                        var mesParcela = mesInicioParcelas.AddMonths(i);
                        var vencimentoParcela = CalcularVencimentoFatura(cartao, mesParcela.Year, mesParcela.Month);

                        db.LancamentosFinanceiros.Add(new LancamentoFinanceiro
                        {
                            LojaId = lojaId!.Value,
                            ContaBancariaId = cartao.ContaBancariaId,
                            Tipo = "pagar",
                            Modo = "parcelada",
                            Descricao = $"Financiamento fatura {cartao.Nome} ({MESES[mesReferencia.Month - 1]})",
                            Valor = valorParcela,
                            Vencimento = vencimentoParcela,
                            GrupoParcelamentoId = grupoId,
                            NumeroParcela = i + 1,
                            TotalParcelas = parcelas,
                            FaturaCartaoId = fatura.Id,
                            CartaoOrigemId = cartao.Id,
                        });
                    }

                    fatura.Status = "financiada";
                    fatura.ValorPago = entrada;
                    fatura.PagoEm = DateTime.UtcNow;
                    break;
                }

            default:
                return BadRequest(new { erro = "Modo inválido." });
        }

        await db.SaveChangesAsync();
        return Ok(new { fatura.Id, fatura.Status, fatura.Total, fatura.ValorPago });
    }

    private static readonly string[] MESES = { "Jan", "Fev", "Mar", "Abr", "Mai", "Jun", "Jul", "Ago", "Set", "Out", "Nov", "Dez" };

    [HttpGet("resumo-anual")]
    public async Task<IActionResult> ResumoAnual([FromQuery] int ano)
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return Ok(Array.Empty<object>());

        var inicio = new DateTime(ano, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var fim = inicio.AddYears(1);

        var doAno = await db.LancamentosFinanceiros
            .Where(l => l.LojaId == lojaId && l.Vencimento >= inicio && l.Vencimento < fim)
            .ToListAsync();

        // Planos: busca pagamentos já gerados no ano
        var assinaturas = await db.AssinaturasCliente
            .Where(a => a.LojaId == lojaId && a.Status == "ativa")
            .ToListAsync();
        var assinaturaIds = assinaturas.Select(a => a.Id).ToList();

        var pagamentosPlanoAno = await db.PagamentosPlano
            .Where(p => assinaturaIds.Contains(p.AssinaturaId) && p.MesReferencia >= inicio && p.MesReferencia < fim)
            .ToListAsync();

        // Soma o valor dos planos vinculados às assinaturas ativas para projeção mensal
        var planoIds = assinaturas.Select(a => a.PlanoId).Distinct().ToList();
        var planos = await db.Planos.Where(p => planoIds.Contains(p.Id)).ToListAsync();
        var receitaProjetadaMensal = assinaturas
            .Sum(a => planos.FirstOrDefault(p => p.Id == a.PlanoId)?.Valor ?? 0);

        // Meses com pagamento já gerado — para os demais, projeta com base nas assinaturas ativas
        var mesesComPagamento = pagamentosPlanoAno.Select(p => p.MesReferencia.Month).ToHashSet();

        var cartoes = await db.CartoesCredito.Where(c => c.LojaId == lojaId && c.Ativo).ToListAsync();
        var totalCartaoPorMes = new decimal[13]; // índice 1-12

        foreach (var cartao in cartoes)
        {
            for (int mes = 1; mes <= 12; mes++)
            {
                var vencimentoFatura = CalcularVencimentoFatura(cartao, ano, mes);
                var (cInicio, cFim) = CicloDaFatura(cartao, vencimentoFatura);

                var totalFatura = await db.LancamentosCartao
                    .Where(l => l.CartaoCreditoId == cartao.Id && l.DataCompra.Date >= cInicio.Date && l.DataCompra.Date <= cFim.Date)
                    .SumAsync(l => (decimal?)l.Valor) ?? 0;

                totalCartaoPorMes[mes] += totalFatura;
            }
        }

        var meses = Enumerable.Range(1, 12).Select(mes =>
        {
            // Previsão: soma TUDO previsto pra esse mês (pago + pendente), não só o pago
            var pagarLancamentos = doAno.Where(l => l.Tipo == "pagar" && l.Vencimento.Month == mes).Sum(l => l.Valor);
            var pagar = pagarLancamentos + totalCartaoPorMes[mes];

            var receberLancamentos = doAno.Where(l => l.Tipo == "receber" && l.Vencimento.Month == mes).Sum(l => l.Valor);
            var mesAtual = DateTime.UtcNow.Month;
            var anoAtual = DateTime.UtcNow.Year;
            var ehFuturo = ano > anoAtual || (ano == anoAtual && mes >= mesAtual);

            var receberPlanos = mesesComPagamento.Contains(mes)
                ? pagamentosPlanoAno.Where(p => p.MesReferencia.Month == mes).Sum(p => p.Valor)
                : ehFuturo ? receitaProjetadaMensal : 0; // só projeta mês atual em diante
            var receber = receberLancamentos + receberPlanos;

            return new { mes, pagar, receber, saldo = receber - pagar };
        }).ToList();

        return Ok(meses);
    }

    [HttpGet("alertas-vencimento")]
    public async Task<IActionResult> AlertasVencimento([FromQuery] int dias = 7)
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return Ok(Array.Empty<object>());

        var hoje = DateTime.UtcNow.Date;
        var limite = hoje.AddDays(dias);

        var lancamentos = await db.LancamentosFinanceiros
            .Where(l => l.LojaId == lojaId && l.Status == "pendente" && l.Vencimento >= hoje && l.Vencimento <= limite)
            .OrderBy(l => l.Vencimento)
            .Select(l => new
            {
                l.Id,
                descricao = l.Descricao,
                tipo = l.Tipo,
                valor = l.Valor,
                vencimento = l.Vencimento,
                origem = "lancamento",
            })
            .ToListAsync();

        var alertasCartao = new List<object>();
        var cartoes = await db.CartoesCredito.Where(c => c.LojaId == lojaId && c.Ativo).ToListAsync();

        foreach (var cartao in cartoes)
        {
            var (vencimento, total, _) = await CicloAtualCartaoAsync(cartao);
            if (total > 0 && vencimento >= hoje && vencimento <= limite)
            {
                var faturaExistente = await db.FaturasCartao
                    .FirstOrDefaultAsync(f => f.CartaoCreditoId == cartao.Id && f.MesReferencia.Year == vencimento.Year && f.MesReferencia.Month == vencimento.Month);
                if (faturaExistente?.Status != "pago")
                {
                    alertasCartao.Add(new
                    {
                        id = cartao.Id,
                        descricao = $"Fatura {cartao.Nome}",
                        tipo = "pagar",
                        valor = total,
                        vencimento,
                        origem = "cartao",
                    });
                }
            }
        }

        var resultado = lancamentos.Cast<object>().Concat(alertasCartao)
            .OrderBy(x => ((dynamic)x).vencimento)
            .ToList();

        return Ok(resultado);
    }

    // ── Resumo de cartões (limite usado somando TODAS as faturas em aberto) ────
    [HttpGet("cartoes-resumo")]
    public async Task<IActionResult> CartoesResumo()
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return Ok(Array.Empty<object>());

        var cartoes = await db.CartoesCredito.Where(c => c.LojaId == lojaId && c.Ativo).ToListAsync();
        var resultado = new List<object>();
        var hoje = DateTime.UtcNow;

        foreach (var cartao in cartoes)
        {
            // Descobre o ciclo em aberto (ainda acumulando, não fechou)
            int anoAberta = hoje.Year, mesAberta = hoje.Month;
            DateTime vencimentoAberta = default;
            (DateTime Inicio, DateTime Fim) cicloAberta = default;
            for (int i = 0; i < 3; i++)
            {
                vencimentoAberta = CalcularVencimentoFatura(cartao, anoAberta, mesAberta);
                cicloAberta = CicloDaFatura(cartao, vencimentoAberta);
                if (hoje.Date >= cicloAberta.Inicio.Date && hoje.Date <= cicloAberta.Fim.Date) break;
                mesAberta++;
                if (mesAberta > 12) { mesAberta = 1; anoAberta++; }
            }

            // Soma TODAS as faturas dos últimos 12 meses (até a aberta) que ainda não estão pagas/financiadas
            decimal usado = 0;
            var cursor = new DateTime(anoAberta, mesAberta, 1).AddMonths(-11);
            for (int i = 0; i < 12; i++)
            {
                var vencimentoCiclo = CalcularVencimentoFatura(cartao, cursor.Year, cursor.Month);
                var (cInicio, cFim) = CicloDaFatura(cartao, vencimentoCiclo);

                var totalCiclo = await db.LancamentosCartao
                    .Where(l => l.CartaoCreditoId == cartao.Id && l.DataCompra.Date >= cInicio.Date && l.DataCompra.Date <= cFim.Date)
                    .SumAsync(l => (decimal?)l.Valor) ?? 0;

                if (totalCiclo > 0)
                {
                    var faturaExistente = await db.FaturasCartao
                        .FirstOrDefaultAsync(f => f.CartaoCreditoId == cartao.Id && f.MesReferencia.Year == cursor.Year && f.MesReferencia.Month == cursor.Month);

                    // "financiada": virou pagamento parcelado à parte, não conta mais nesse ciclo.
                    // "parcial": o restante já foi lançado como compra real no próximo ciclo (com juros),
                    // então não soma aqui de novo — senão a mesma dívida é contada duas vezes.
                    if (faturaExistente?.Status != "pago" && faturaExistente?.Status != "financiada" && faturaExistente?.Status != "parcial")
                    {
                        var totalAntecipadoCiclo = faturaExistente is null ? 0 : await db.PagamentosAntecipadosFatura
                            .Where(p => p.FaturaCartaoId == faturaExistente.Id)
                            .SumAsync(p => (decimal?)p.Valor) ?? 0;
                        usado += totalCiclo - totalAntecipadoCiclo;
                    }
                }

                cursor = cursor.AddMonths(1);
            }

            // Parcelas futuras (compras parceladas) que caem em ciclos além do aberto — ainda não contadas
            // acima. Cobrança fixa/recorrente futura NÃO entra aqui: só conta quando já estiver dentro de
            // uma fatura gerada (fechada ou aberta), que já é somada no loop dos últimos 12 meses acima.
            var parcelasFuturas = await db.LancamentosCartao
                .Where(l => l.CartaoCreditoId == cartao.Id && l.Modo == "parcelada" && l.DataCompra.Date > cicloAberta.Fim.Date)
                .ToListAsync();

            decimal totalParcelasFuturas = 0;
            foreach (var p in parcelasFuturas)
            {
                var vencTeste = CalcularVencimentoFatura(cartao, p.DataCompra.Year, p.DataCompra.Month);
                var (ini, fim) = CicloDaFatura(cartao, vencTeste);
                if (p.DataCompra.Date < ini.Date || p.DataCompra.Date > fim.Date)
                    vencTeste = CalcularVencimentoFatura(cartao, p.DataCompra.AddMonths(1).Year, p.DataCompra.AddMonths(1).Month);

                var faturaFutura = await db.FaturasCartao
                    .FirstOrDefaultAsync(f => f.CartaoCreditoId == cartao.Id && f.MesReferencia.Year == vencTeste.Year && f.MesReferencia.Month == vencTeste.Month);

                if (faturaFutura?.Status != "pago" && faturaFutura?.Status != "financiada")
                    totalParcelasFuturas += p.Valor;
            }

            usado += totalParcelasFuturas;

            // Parcelas de financiamento de faturas antigas (ficam em LancamentosFinanceiros, não em
            // LancamentosCartao — por isso não entram nas duas somas acima) — soma tudo que ainda
            // não foi pago, independente do mês de vencimento.
            var totalFinanciamentoPendente = await db.LancamentosFinanceiros
                .Where(l => l.CartaoOrigemId == cartao.Id && l.Status == "pendente")
                .SumAsync(l => (decimal?)l.Valor) ?? 0;

            usado += totalFinanciamentoPendente;

            // Fatura "principal" pra exibir vencimento/status no card (a mais antiga fechada e pendente, senão a aberta)
            var (vencimentoPrincipal, _, statusPrincipal) = await CicloAtualCartaoAsync(cartao);
            var (piInicio, piFim) = CicloDaFatura(cartao, vencimentoPrincipal);
            var qtdCompras = await db.LancamentosCartao
                .CountAsync(l => l.CartaoCreditoId == cartao.Id && l.DataCompra.Date >= piInicio.Date && l.DataCompra.Date <= piFim.Date);

            resultado.Add(new
            {
                cartao.Id,
                cartao.Nome,
                cartao.Limite,
                usado,
                disponivel = cartao.Limite - usado,
                vencimentoAtual = vencimentoPrincipal,
                status = statusPrincipal,
                qtdCompras,
            });
        }

        return Ok(resultado);
    }

    // ── Editar item de compra do cartão ─────────────────────────────
    public record EditarLancamentoCartaoRequest(string Descricao, decimal Valor, DateTime DataCompra, Guid? CategoriaId, string? Observacao = null);

    [HttpPut("cartoes/lancamentos/{id:guid}")]
    public async Task<IActionResult> EditarLancamentoCartao(Guid id, [FromQuery] string modo, [FromBody] EditarLancamentoCartaoRequest req)
    {
        var lojaId = await GetLojaId();
        var item = await db.LancamentosCartao.Include(l => l.CartaoCredito)
            .FirstOrDefaultAsync(l => l.Id == id && l.CartaoCredito!.LojaId == lojaId);
        if (item is null) return NotFound();

        var novaData = DateTime.SpecifyKind(req.DataCompra.Date, DateTimeKind.Utc).AddHours(12);

        if (modo == "todas" && item.Modo == "fixa" && item.CartaoFixoId.HasValue)
        {
            var fixo = await db.CartaoLancamentosFixos.FindAsync(item.CartaoFixoId.Value);
            if (fixo is null) return NotFound();

            fixo.Descricao = req.Descricao.Trim();
            fixo.Valor = req.Valor;
            fixo.CategoriaId = req.CategoriaId;
            fixo.Observacao = req.Observacao;
            fixo.DiaCompra = novaData.Day;

            await financeiroService.LimparFuturosCartaoAsync(fixo.Id);
            await db.SaveChangesAsync();

            var agora = DateTime.UtcNow;
            var mesAtual = new DateTime(agora.Year, agora.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            await financeiroService.GerarLoteFixoCartaoAsync(fixo, item.CartaoCredito!, mesAtual);
        }
        else if (modo == "todas" && item.Modo == "parcelada" && item.GrupoParcelamentoId.HasValue)
        {
            var cartao = item.CartaoCredito!;
            var todasDoGrupo = await db.LancamentosCartao
                .Where(l => l.GrupoParcelamentoId == item.GrupoParcelamentoId.Value)
                .ToListAsync();

            var mesesPagos = (await db.FaturasCartao
                .Where(f => f.CartaoCreditoId == cartao.Id && f.Status == "pago")
                .ToListAsync())
                .Select(f => (f.MesReferencia.Year, f.MesReferencia.Month))
                .ToHashSet();

            foreach (var p in todasDoGrupo)
            {
                // Descobre o mês de vencimento da fatura à qual essa compra pertence
                var vencTeste = CalcularVencimentoFatura(cartao, p.DataCompra.Year, p.DataCompra.Month);
                var (ini, fim) = CicloDaFatura(cartao, vencTeste);
                if (p.DataCompra.Date < ini.Date || p.DataCompra.Date > fim.Date)
                    vencTeste = CalcularVencimentoFatura(cartao, p.DataCompra.AddMonths(1).Year, p.DataCompra.AddMonths(1).Month);

                if (mesesPagos.Contains((vencTeste.Year, vencTeste.Month))) continue; // já foi paga — não mexe

                var baseDescricao = req.Descricao.Trim();
                p.Descricao = p.TotalParcelas.HasValue ? $"{baseDescricao} ({p.NumeroParcela}/{p.TotalParcelas})" : baseDescricao;
                p.Valor = req.Valor;
                p.CategoriaId = req.CategoriaId;
            }
        }
        else
        {
            item.Descricao = req.Descricao.Trim();
            item.Valor = req.Valor;
            item.DataCompra = novaData;
            item.CategoriaId = req.CategoriaId;
        }

        await db.SaveChangesAsync();
        return Ok(new { mensagem = "Lançamento atualizado." });
    }

    // ── Excluir item de compra do cartão ────────────────────────────
    [HttpDelete("cartoes/lancamentos/{id:guid}")]
    public async Task<IActionResult> ExcluirLancamentoCartao(Guid id, [FromQuery] string modo = "unica")
    {
        var lojaId = await GetLojaId();
        var item = await db.LancamentosCartao.Include(l => l.CartaoCredito)
            .FirstOrDefaultAsync(l => l.Id == id && l.CartaoCredito!.LojaId == lojaId);
        if (item is null) return NotFound();

        if (modo == "todas" && item.Modo == "fixa" && item.CartaoFixoId.HasValue)
        {
            var fixo = await db.CartaoLancamentosFixos.FindAsync(item.CartaoFixoId.Value);
            if (fixo != null) fixo.Ativo = false;

            var agora = DateTime.UtcNow;
            var mesAtual = new DateTime(agora.Year, agora.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var futuros = await db.LancamentosCartao
                .Where(l => l.CartaoFixoId == item.CartaoFixoId.Value && l.DataCompra >= mesAtual)
                .ToListAsync();
            db.LancamentosCartao.RemoveRange(futuros);
        }
        else if (modo == "todas" && item.Modo == "parcelada" && item.GrupoParcelamentoId.HasValue)
        {
            var cartao = item.CartaoCredito!;
            var todasDoGrupo = await db.LancamentosCartao
                .Where(l => l.GrupoParcelamentoId == item.GrupoParcelamentoId.Value)
                .ToListAsync();

            var mesesPagos = (await db.FaturasCartao
                .Where(f => f.CartaoCreditoId == cartao.Id && f.Status == "pago")
                .ToListAsync())
                .Select(f => (f.MesReferencia.Year, f.MesReferencia.Month))
                .ToHashSet();

            var removiveis = new List<LancamentoCartao>();
            foreach (var p in todasDoGrupo)
            {
                var vencTeste = CalcularVencimentoFatura(cartao, p.DataCompra.Year, p.DataCompra.Month);
                var (ini, fim) = CicloDaFatura(cartao, vencTeste);
                if (p.DataCompra.Date < ini.Date || p.DataCompra.Date > fim.Date)
                    vencTeste = CalcularVencimentoFatura(cartao, p.DataCompra.AddMonths(1).Year, p.DataCompra.AddMonths(1).Month);

                if (!mesesPagos.Contains((vencTeste.Year, vencTeste.Month)))
                    removiveis.Add(p);
            }
            db.LancamentosCartao.RemoveRange(removiveis);
        }
        else
        {
            db.LancamentosCartao.Remove(item);
        }

        await db.SaveChangesAsync();
        return Ok(new { mensagem = "Lançamento excluído." });
    }

    public record TransferenciaContaRequest(Guid ContaOrigemId, Guid ContaDestinoId, decimal Valor, bool Registrar, string? Observacao);

    [HttpPost("contas/transferencia")]
    public async Task<IActionResult> TransferirEntreContas([FromBody] TransferenciaContaRequest req)
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return BadRequest(new { erro = "Loja não encontrada." });

        if (req.ContaOrigemId == req.ContaDestinoId)
            return BadRequest(new { erro = "Escolha duas contas diferentes." });
        if (req.Valor <= 0)
            return BadRequest(new { erro = "Valor deve ser maior que zero." });

        var origem = await db.ContasBancarias.FirstOrDefaultAsync(c => c.Id == req.ContaOrigemId && c.LojaId == lojaId);
        var destino = await db.ContasBancarias.FirstOrDefaultAsync(c => c.Id == req.ContaDestinoId && c.LojaId == lojaId);
        if (origem is null || destino is null) return NotFound(new { erro = "Conta não encontrada." });

        if (req.Registrar)
        {
            var grupoId = Guid.NewGuid();
            db.AjustesContaBancaria.Add(new AjusteContaBancaria
            {
                LojaId = lojaId.Value,
                ContaBancariaId = origem.Id,
                Tipo = "saida",
                Valor = req.Valor,
                Observacao = $"Transferência para {destino.Nome}" + (string.IsNullOrWhiteSpace(req.Observacao) ? "" : $" — {req.Observacao}"),
                TransferenciaGrupoId = grupoId,
            });
            db.AjustesContaBancaria.Add(new AjusteContaBancaria
            {
                LojaId = lojaId.Value,
                ContaBancariaId = destino.Id,
                Tipo = "entrada",
                Valor = req.Valor,
                Observacao = $"Transferência de {origem.Nome}" + (string.IsNullOrWhiteSpace(req.Observacao) ? "" : $" — {req.Observacao}"),
                TransferenciaGrupoId = grupoId,
            });
        }
        else
        {
            // Sem registro: move direto no saldo inicial de cada conta, sem deixar rastro no histórico de ajustes
            origem.SaldoInicial -= req.Valor;
            destino.SaldoInicial += req.Valor;
        }

        await db.SaveChangesAsync();

        var saldoOrigem = await CalcularSaldoAsync(origem.Id);
        var saldoDestino = await CalcularSaldoAsync(destino.Id);

        return Ok(new { saldoOrigem, saldoDestino });
    }

    public record ToggleAvisarRequest(bool Avisar);

    [HttpPatch("lancamentos/{id:guid}/avisar")]
    public async Task<IActionResult> AlternarAvisar(Guid id, [FromBody] ToggleAvisarRequest req)
    {
        var lojaId = await GetLojaId();
        var lanc = await db.LancamentosFinanceiros.FirstOrDefaultAsync(l => l.Id == id && l.LojaId == lojaId);
        if (lanc is null) return NotFound();

        lanc.Avisar = req.Avisar;
        await db.SaveChangesAsync();
        return Ok(new { lanc.Id, lanc.Avisar });
    }

    // Retorna a fatura "a pagar agora": a mais antiga já FECHADA e ainda não paga
    // (bate com o que aparece em Contas a Pagar). Se não houver nenhuma pendente
    // já fechada, cai no ciclo que ainda está acumulando (informativo).
    private async Task<(DateTime Vencimento, decimal Total, string Status)> CicloAtualCartaoAsync(CartaoCredito cartao)
    {
        var hoje = DateTime.UtcNow;

        for (int offset = -3; offset <= 0; offset++)
        {
            var refBase = new DateTime(hoje.Year, hoje.Month, 1).AddMonths(offset);
            var vencimentoCheck = CalcularVencimentoFatura(cartao, refBase.Year, refBase.Month);
            var cicloCheck = CicloDaFatura(cartao, vencimentoCheck);

            var cicloJaFechou = hoje.Date > cicloCheck.Fim.Date;
            if (!cicloJaFechou) continue;

            var totalCheck = await db.LancamentosCartao
                .Where(l => l.CartaoCreditoId == cartao.Id && l.DataCompra.Date >= cicloCheck.Inicio.Date && l.DataCompra.Date <= cicloCheck.Fim.Date)
                .SumAsync(l => (decimal?)l.Valor) ?? 0;

            if (totalCheck <= 0) continue;

            var faturaCheck = await db.FaturasCartao
                .FirstOrDefaultAsync(f => f.CartaoCreditoId == cartao.Id && f.MesReferencia.Year == vencimentoCheck.Year && f.MesReferencia.Month == vencimentoCheck.Month);

            if (faturaCheck?.Status == "pago") continue;

            return (vencimentoCheck, totalCheck, faturaCheck?.Status ?? "pendente");
        }

        // Nenhuma fatura fechada pendente — mostra o ciclo em aberto (só informativo)
        int ano = hoje.Year, mes = hoje.Month;
        DateTime vencimento = default;
        (DateTime Inicio, DateTime Fim) ciclo = default;
        for (int i = 0; i < 3; i++)
        {
            vencimento = CalcularVencimentoFatura(cartao, ano, mes);
            ciclo = CicloDaFatura(cartao, vencimento);
            if (hoje.Date >= ciclo.Inicio.Date && hoje.Date <= ciclo.Fim.Date) break;
            mes++;
            if (mes > 12) { mes = 1; ano++; }
        }

        var total = await db.LancamentosCartao
            .Where(l => l.CartaoCreditoId == cartao.Id && l.DataCompra.Date >= ciclo.Inicio.Date && l.DataCompra.Date <= ciclo.Fim.Date)
            .SumAsync(l => (decimal?)l.Valor) ?? 0;

        var fatura = await db.FaturasCartao
            .FirstOrDefaultAsync(f => f.CartaoCreditoId == cartao.Id && f.MesReferencia.Year == vencimento.Year && f.MesReferencia.Month == vencimento.Month);

        return (vencimento, total, fatura?.Status ?? "pendente");
    }

    [HttpGet("cartoes/{id:guid}/faturas-referencia")]
    public async Task<IActionResult> FaturasReferencia(Guid id)
    {
        var lojaId = await GetLojaId();
        var cartao = await db.CartoesCredito.FirstOrDefaultAsync(c => c.Id == id && c.LojaId == lojaId);
        if (cartao is null) return NotFound();

        // Ciclo em aberto (ainda acumulando, ainda não fechou)
        var hoje = DateTime.UtcNow;
        int anoAberta = hoje.Year, mesAberta = hoje.Month;
        DateTime vencimentoAberta = default;
        (DateTime Inicio, DateTime Fim) cicloAberta = default;
        for (int i = 0; i < 3; i++)
        {
            vencimentoAberta = CalcularVencimentoFatura(cartao, anoAberta, mesAberta);
            cicloAberta = CicloDaFatura(cartao, vencimentoAberta);
            if (hoje.Date >= cicloAberta.Inicio.Date && hoje.Date <= cicloAberta.Fim.Date) break;
            mesAberta++;
            if (mesAberta > 12) { mesAberta = 1; anoAberta++; }
        }

        // Última fatura já fechada e pendente (a que já foi "para cobrança")
        var (vencimentoFechada, totalFechada, statusFechada) = await CicloAtualCartaoAsync(cartao);

        return Ok(new
        {
            aberta = new { ano = anoAberta, mes = mesAberta },
            fechada = new { ano = vencimentoFechada.Year, mes = vencimentoFechada.Month, total = totalFechada, status = statusFechada },
        });
    }
}

public record SalvarCartaoRequest(string Nome, decimal Limite, int DiaFechamento, int DiaVencimento, Guid ContaBancariaId, decimal TaxaJurosMensal = 0);
public record SalvarLancamentoCartaoRequest(string Descricao, decimal Valor, DateTime DataCompra, Guid? CategoriaId, string? Observacao = null);
public record SalvarCategoriaFinanceiraRequest(string Nome, string Tipo, string? Icone);
public record SalvarContaBancariaRequest(string Nome, decimal SaldoInicial, string? Banco = null, decimal Limite = 0);
public record AjusteSaldoRequest(string Tipo, decimal? Valor, decimal NovoSaldo, string? Observacao);
public record SalvarLancamentoAvulsoRequest(Guid ContaBancariaId, string Tipo, string Descricao, Guid? CategoriaId, decimal Valor, DateTime Vencimento, string? Observacao, bool JaPago = false, bool Avisar = true);
public record SalvarLancamentoParceladoRequest(Guid ContaBancariaId, string Tipo, string Descricao, Guid? CategoriaId, decimal ValorParcela, int? TotalParcelas, DateTime PrimeiroVencimento, DateTime? DataFim, string? Observacao, bool JaPago = false, bool Avisar = true);
public record SalvarLancamentoFixoRequest(Guid ContaBancariaId, string Tipo, string Descricao, Guid? CategoriaId, decimal Valor, int DiaVencimento, string? Observacao, DateTime? DataInicio = null, bool JaPago = false, bool Avisar = true);
public record MarcarPagamentoRequest(bool Pago);
public record EditarLancamentoRequest(string Descricao, Guid? CategoriaId, Guid ContaBancariaId, decimal Valor, DateTime Vencimento, string? Observacao);