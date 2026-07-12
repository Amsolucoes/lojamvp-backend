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
            resultado.Add(new { conta.Id, conta.Nome, conta.SaldoInicial, conta.Ativa, saldoAtual = saldo });
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
        };
        db.ContasBancarias.Add(conta);
        await db.SaveChangesAsync();

        return Ok(new { conta.Id, conta.Nome, conta.SaldoInicial, conta.Ativa, saldoAtual = conta.SaldoInicial });
    }

    [HttpPut("contas/{id:guid}")]
    public async Task<IActionResult> AtualizarConta(Guid id, [FromBody] SalvarContaBancariaRequest req)
    {
        var lojaId = await GetLojaId();
        var conta = await db.ContasBancarias.FirstOrDefaultAsync(c => c.Id == id && c.LojaId == lojaId);
        if (conta is null) return NotFound();

        conta.Nome = req.Nome.Trim();
        conta.SaldoInicial = req.SaldoInicial;
        await db.SaveChangesAsync();

        var saldo = await CalcularSaldoAsync(conta.Id);
        return Ok(new { conta.Id, conta.Nome, conta.SaldoInicial, conta.Ativa, saldoAtual = saldo });
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
            .Where(f => f.CartaoCredito!.ContaBancariaId == contaId && f.Status == "pago")
            .SumAsync(f => (decimal?)f.Total) ?? 0;

        return conta.SaldoInicial + recebidos - pagos - faturasCartaoPagas + entradasAjuste - saidasAjuste + diferencasAjuste;
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

        var lancamentos = await qLanc.Select(l => new
        {
            l.Id,
            descricao = l.Descricao,
            valor = l.Valor,
            vencimento = l.Vencimento,
            status = l.Status,
            pagoEm = l.PagoEm,
            origem = "avulso",
        }).ToListAsync();

        var pagamentosPlano = await db.PagamentosPlano
            .Where(p => p.LojaId == lojaId)
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
            Valor = req.Valor,
            Vencimento = DateTime.SpecifyKind(req.Vencimento.Date, DateTimeKind.Utc).AddHours(12),
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

        if (req.TotalParcelas < 2 || req.TotalParcelas > 60)
            return BadRequest(new { erro = "Número de parcelas inválido." });

        var grupoId = Guid.NewGuid();
        var lista = new List<LancamentoFinanceiro>();

        for (int i = 0; i < req.TotalParcelas; i++)
        {
            var venc = DateTime.SpecifyKind(req.PrimeiroVencimento.Date, DateTimeKind.Utc).AddMonths(i).AddHours(12);

            lista.Add(new LancamentoFinanceiro
            {
                LojaId = lojaId.Value,
                ContaBancariaId = req.ContaBancariaId,
                Tipo = req.Tipo,
                Modo = "parcelada",
                Descricao = req.Descricao.Trim(),
                CategoriaId = req.CategoriaId,
                Valor = req.ValorParcela,
                Vencimento = venc,
                GrupoParcelamentoId = grupoId,
                NumeroParcela = i + 1,
                TotalParcelas = req.TotalParcelas,
            });
        }

        db.LancamentosFinanceiros.AddRange(lista);
        await db.SaveChangesAsync();

        return Ok(new { grupoParcelamentoId = grupoId, totalGerado = lista.Count });
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
            Valor = req.Valor,
            DiaVencimento = req.DiaVencimento is >= 1 and <= 28 ? req.DiaVencimento : 10,
        };
        db.LancamentosFixos.Add(fixo);
        await db.SaveChangesAsync();

        var agora = DateTime.UtcNow;
        var mesAtual = new DateTime(agora.Year, agora.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        await financeiroService.GerarLoteFixoAsync(fixo, mesAtual);
        await db.SaveChangesAsync();

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
    public async Task<IActionResult> PagarUnificado([FromQuery] int ano, [FromQuery] int mes, [FromQuery] string modo = "agrupado")
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return Ok(Array.Empty<object>());

        var inicioMes = new DateTime(ano, mes, 1, 0, 0, 0, DateTimeKind.Utc);
        var fimMes = inicioMes.AddMonths(1);

        var lancamentos = await db.LancamentosFinanceiros
            .Where(l => l.LojaId == lojaId && l.Tipo == "pagar" && l.Vencimento >= inicioMes && l.Vencimento < fimMes)
            .Include(l => l.Categoria)
            .Select(l => new
            {
                l.Id,
                descricao = l.Descricao,
                categoriaNome = l.Categoria != null ? l.Categoria.Nome : null,
                modo = l.Modo,
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

        foreach (var cartao in cartoes)
        {
            var vencimentoFatura = CalcularVencimentoFatura(cartao, ano, mes);
            var (inicio, fim) = CicloDaFatura(cartao, vencimentoFatura);

            var itensCiclo = await db.LancamentosCartao
                .Where(l => l.CartaoCreditoId == cartao.Id && l.DataCompra >= inicio && l.DataCompra < fim)
                .Include(l => l.Categoria)
                .OrderBy(l => l.DataCompra)
                .ToListAsync();

            if (itensCiclo.Count == 0) continue;

            var total = itensCiclo.Sum(i => i.Valor);
            var faturaExistente = await db.FaturasCartao
                .FirstOrDefaultAsync(f => f.CartaoCreditoId == cartao.Id && f.MesReferencia.Year == ano && f.MesReferencia.Month == mes);

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
                linhasCartao.Add(new
                {
                    id = cartao.Id,
                    descricao = $"Fatura {cartao.Nome}",
                    categoriaNome = (string?)null,
                    valor = total,
                    vencimento = vencimentoFatura,
                    status = faturaExistente?.Status ?? "pendente",
                    pagoEm = faturaExistente?.PagoEm,
                    numeroParcela = (int?)null,
                    totalParcelas = (int?)null,
                    origem = "cartao_fatura",
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

        object Resumo(string tipo)
        {
            var itens = doMes.Where(l => l.Tipo == tipo).ToList();
            return new
            {
                totalPago = itens.Where(l => l.Status == "pago").Sum(l => l.Valor),
                qtdPago = itens.Count(l => l.Status == "pago"),
                totalPendente = itens.Where(l => l.Status == "pendente" && l.Vencimento.Date >= hoje).Sum(l => l.Valor),
                qtdPendente = itens.Count(l => l.Status == "pendente" && l.Vencimento.Date >= hoje),
                totalVencido = itens.Where(l => l.Status == "pendente" && l.Vencimento.Date < hoje).Sum(l => l.Valor),
                qtdVencido = itens.Count(l => l.Status == "pendente" && l.Vencimento.Date < hoje),
            };
        }

        return Ok(new { pagar = Resumo("pagar"), receber = Resumo("receber") });
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
            .Select(c => new { c.Id, c.Nome, c.Limite, c.DiaFechamento, c.DiaVencimento, c.ContaBancariaId, c.Ativo })
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
            DataCompra = DateTime.SpecifyKind(req.DataCompra, DateTimeKind.Utc),
            CategoriaId = req.CategoriaId,
        });
        await db.SaveChangesAsync();
        return Ok(new { mensagem = "Lançamento adicionado." });
    }

    // ── Calcula a fatura de um mês (sob demanda, sem precisar de job) ──
    private DateTime CalcularVencimentoFatura(CartaoCredito cartao, int ano, int mes)
        => new DateTime(ano, mes, Math.Min(cartao.DiaVencimento, DateTime.DaysInMonth(ano, mes)), 12, 0, 0, DateTimeKind.Utc);

    private (DateTime Inicio, DateTime Fim) CicloDaFatura(CartaoCredito cartao, DateTime vencimento)
    {
        // Fechamento do ciclo = 1 mês antes do vencimento, no dia de fechamento
        var fechamentoAtual = new DateTime(vencimento.Year, vencimento.Month, Math.Min(cartao.DiaFechamento, DateTime.DaysInMonth(vencimento.Year, vencimento.Month)), 0, 0, 0, DateTimeKind.Utc);
        var fechamentoAnterior = fechamentoAtual.AddMonths(-1);
        return (fechamentoAnterior, fechamentoAtual);
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
            .Where(l => l.CartaoCreditoId == id && l.DataCompra >= inicio && l.DataCompra < fim)
            .Include(l => l.Categoria)
            .OrderBy(l => l.DataCompra)
            .Select(l => new { l.Id, l.Descricao, l.Valor, l.DataCompra, categoriaNome = l.Categoria != null ? l.Categoria.Nome : null })
            .ToListAsync();

        var total = itens.Sum(i => i.Valor);

        var faturaExistente = await db.FaturasCartao
            .FirstOrDefaultAsync(f => f.CartaoCreditoId == id && f.MesReferencia.Year == ano && f.MesReferencia.Month == mes);

        return Ok(new
        {
            vencimento,
            total,
            status = faturaExistente?.Status ?? "pendente",
            pagoEm = faturaExistente?.PagoEm,
            itens,
        });
    }

    // ── Pagar/desfazer a fatura do mês ──────────────────────────────
    [HttpPost("cartoes/{id:guid}/fatura/pagamento")]
    public async Task<IActionResult> PagarFatura(Guid id, [FromQuery] int ano, [FromQuery] int mes, [FromBody] MarcarPagamentoRequest req)
    {
        var lojaId = await GetLojaId();
        var cartao = await db.CartoesCredito.FirstOrDefaultAsync(c => c.Id == id && c.LojaId == lojaId);
        if (cartao is null) return NotFound();

        var vencimento = CalcularVencimentoFatura(cartao, ano, mes);
        var mesReferencia = new DateTime(ano, mes, 1, 0, 0, 0, DateTimeKind.Utc);
        var (inicio, fim) = CicloDaFatura(cartao, vencimento);

        var total = await db.LancamentosCartao
            .Where(l => l.CartaoCreditoId == id && l.DataCompra >= inicio && l.DataCompra < fim)
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
        fatura.Status = req.Pago ? "pago" : "pendente";
        fatura.PagoEm = req.Pago ? DateTime.UtcNow : null;

        await db.SaveChangesAsync();
        return Ok(new { fatura.Id, fatura.Status, fatura.Total });
    }

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

        var meses = Enumerable.Range(1, 12).Select(mes =>
        {
            var doMes = doAno.Where(l => l.Vencimento.Month == mes).ToList();
            var pagar = doMes.Where(l => l.Tipo == "pagar" && l.Status == "pago").Sum(l => l.Valor);
            var receber = doMes.Where(l => l.Tipo == "receber" && l.Status == "pago").Sum(l => l.Valor);
            return new { mes, pagar, receber, saldo = receber - pagar };
        }).ToList();

        return Ok(meses);
    }
}

public record SalvarCartaoRequest(string Nome, decimal Limite, int DiaFechamento, int DiaVencimento, Guid ContaBancariaId);
public record SalvarLancamentoCartaoRequest(string Descricao, decimal Valor, DateTime DataCompra, Guid? CategoriaId);
public record SalvarCategoriaFinanceiraRequest(string Nome, string Tipo, string? Icone);
public record SalvarContaBancariaRequest(string Nome, decimal SaldoInicial);
public record AjusteSaldoRequest(string Tipo, decimal? Valor, decimal NovoSaldo, string? Observacao);
public record SalvarLancamentoAvulsoRequest(Guid ContaBancariaId, string Tipo, string Descricao, Guid? CategoriaId, decimal Valor, DateTime Vencimento);
public record SalvarLancamentoParceladoRequest(Guid ContaBancariaId, string Tipo, string Descricao, Guid? CategoriaId, decimal ValorParcela, int TotalParcelas, DateTime PrimeiroVencimento);
public record SalvarLancamentoFixoRequest(Guid ContaBancariaId, string Tipo, string Descricao, Guid? CategoriaId, decimal Valor, int DiaVencimento);
public record MarcarPagamentoRequest(bool Pago);