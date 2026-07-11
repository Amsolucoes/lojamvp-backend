using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using LojaApi.Data;
using LojaApi.Models;

namespace LojaApi.src.Controllers;

[ApiController]
[Route("api/financeiro")]
[Authorize]
public class FinanceiroController(AppDbContext db) : ControllerBase
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

        return conta.SaldoInicial + recebidos - pagos + entradasAjuste - saidasAjuste + diferencasAjuste;
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
            Vencimento = DateTime.SpecifyKind(req.Vencimento, DateTimeKind.Utc),
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
            var venc = DateTime.SpecifyKind(req.PrimeiroVencimento.AddMonths(i), DateTimeKind.Utc);

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

    // ── Criar lançamento fixo (aluguel — recorrente sem fim) ─────────
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

        return Ok(new { fixo.Id });
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
    public async Task<IActionResult> Excluir(Guid id)
    {
        var lojaId = await GetLojaId();
        var lanc = await db.LancamentosFinanceiros.FirstOrDefaultAsync(l => l.Id == id && l.LojaId == lojaId);
        if (lanc is null) return NotFound();

        db.LancamentosFinanceiros.Remove(lanc);
        await db.SaveChangesAsync();
        return Ok(new { mensagem = "Lançamento excluído." });
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
}

public record SalvarCategoriaFinanceiraRequest(string Nome, string Tipo, string? Icone);
public record SalvarContaBancariaRequest(string Nome, decimal SaldoInicial);
public record AjusteSaldoRequest(string Tipo, decimal? Valor, decimal NovoSaldo, string? Observacao);
public record SalvarLancamentoAvulsoRequest(Guid ContaBancariaId, string Tipo, string Descricao, Guid? CategoriaId, decimal Valor, DateTime Vencimento);
public record SalvarLancamentoParceladoRequest(Guid ContaBancariaId, string Tipo, string Descricao, Guid? CategoriaId, decimal ValorParcela, int TotalParcelas, DateTime PrimeiroVencimento);
public record SalvarLancamentoFixoRequest(Guid ContaBancariaId, string Tipo, string Descricao, Guid? CategoriaId, decimal Valor, int DiaVencimento);
public record MarcarPagamentoRequest(bool Pago);