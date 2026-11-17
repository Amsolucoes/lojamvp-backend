using LojaApi.Application.Financeiro;
using LojaApi.Data;
using LojaApi.Domain.Financeiro;
using LojaApi.Models;
using LojaApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LojaApi.Controllers.Financeiro;

[ApiController]
[Route("api/financeiro")]
[Authorize]
public partial class FinanceiroController(AppDbContext db, FinanceiroService financeiroService, CorrecaoDeFinanciamentos correcao, CicloAtualDoCartao cicloAtual) : ControllerBase
{
    private Guid UsuarioId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private async Task<Guid?> GetLojaId()
    {
        var vinculo = await db.UsuariosLoja
            .FirstOrDefaultAsync(ul => ul.UsuarioId == UsuarioId && ul.Ativo);
        return vinculo?.LojaId;
    }

    // ── Resumo mensal — Pagar e Receber lado a lado ────────────────
    [HttpGet("resumo-mensal")]
    public async Task<IActionResult> ResumoMensal([FromQuery] int ano, [FromQuery] int mes)
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return Ok(new { });

        await CorrigirParcelasFinanciamentoEmCicloFechadoAsync(lojaId.Value);

        var inicio = new DateTime(ano, mes, 1, 0, 0, 0, DateTimeKind.Utc);
        var fim = inicio.AddMonths(1);
        var hoje = DateTime.UtcNow.Date;

        var doMes = await db.LancamentosFinanceiros.AsNoTracking()
            .Where(l => l.LojaId == lojaId && l.Vencimento >= inicio && l.Vencimento < fim)
            .ToListAsync();

        // Parcelas de financiamento de cartão (CartaoOrigemId preenchido) ficam de fora
        // daqui — são tratadas à parte no loop de cartões abaixo, pra serem atribuídas
        // ao cartão de origem certo em vez de vazarem pro balde de "Contas" avulsas.
        decimal pagarPago = doMes.Where(l => l.Tipo == "pagar" && l.Status == "pago" && l.CartaoOrigemId == null).Sum(l => l.Valor);
        int pagarQtdPago = doMes.Count(l => l.Tipo == "pagar" && l.Status == "pago" && l.CartaoOrigemId == null);
        decimal pagarPendente = doMes.Where(l => l.Tipo == "pagar" && l.Status == "pendente" && l.Vencimento.Date >= hoje && l.CartaoOrigemId == null).Sum(l => l.Valor);
        int pagarQtdPendente = doMes.Count(l => l.Tipo == "pagar" && l.Status == "pendente" && l.Vencimento.Date >= hoje && l.CartaoOrigemId == null);
        decimal pagarVencido = doMes.Where(l => l.Tipo == "pagar" && l.Status == "pendente" && l.Vencimento.Date < hoje && l.CartaoOrigemId == null).Sum(l => l.Valor);
        int pagarQtdVencido = doMes.Count(l => l.Tipo == "pagar" && l.Status == "pendente" && l.Vencimento.Date < hoje && l.CartaoOrigemId == null);

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
        decimal totalContribuicaoCartoes = 0;
        decimal totalCartoesVisiveis = 0;
        var cartoes = await db.CartoesCredito.AsNoTracking().Where(c => c.LojaId == lojaId && c.Ativo).ToListAsync();
        // Dados dos cartões carregados de uma vez (antes: ~4 consultas por cartão).
        var dadosCartoesMes = await CarregarDadosCartoesAsync(
            cartoes.Select(c => c.Id).ToList(), inicio.AddMonths(-1), fim, inicio, fim);
        foreach (var cartao in cartoes)
        {
            var (pagoCartao, pendenteCartao, vencidoCartao) = CalcularContribuicaoCartaoMes(
                cartao, ano, mes, hoje, dadosCartoesMes, doMes.Where(l => l.CartaoOrigemId == cartao.Id));

            if (pagoCartao > 0) { pagarPago += pagoCartao; pagarQtdPago++; }
            if (pendenteCartao > 0) { pagarPendente += pendenteCartao; pagarQtdPendente++; }
            if (vencidoCartao > 0) { pagarVencido += vencidoCartao; pagarQtdVencido++; }

            totalContribuicaoCartoes += pagoCartao + pendenteCartao + vencidoCartao;

            var valorVisivelCartao = pendenteCartao + vencidoCartao;
            totalCartoesVisiveis += valorVisivelCartao;
            if (valorVisivelCartao > 0)
                detalheCartoesPagar.Add(new { nome = cartao.Nome, valor = valorVisivelCartao });
        }

        // "Contas" no detalhamento reflete só o que ainda está em aberto (pendente + vencido),
        // igual cada linha de cartão já mostra — nunca inclui o que já foi pago. Assim a soma
        // de "Contas" + cada cartão bate exatamente com o "A Pagar" mostrado em cima, em vez
        // de bater com o "Total do mês" (que inclui o que já foi pago).
        var totalLancamentosPagar = pagarPendente + pagarVencido - totalCartoesVisiveis;

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

        await CorrigirParcelasFinanciamentoEmCicloFechadoAsync(lojaId.Value);

        var inicio = new DateTime(ano, mes, 1, 0, 0, 0, DateTimeKind.Utc);
        var fim = inicio.AddMonths(1);

        var doMes = await db.LancamentosFinanceiros.AsNoTracking()
            .Where(l => l.LojaId == lojaId && l.Vencimento >= inicio && l.Vencimento < fim)
            .Include(l => l.Categoria)
            .ToListAsync();

        // Planos entram como receita agrupada em "Mensalidade/Assinatura"
        var assinaturaIds = await db.AssinaturasCliente.Where(a => a.LojaId == lojaId).Select(a => a.Id).ToListAsync();
        var pagamentosPlanoMes = await db.PagamentosPlano
            .Where(p => assinaturaIds.Contains(p.AssinaturaId) && p.MesReferencia >= inicio && p.MesReferencia < fim)
            .SumAsync(p => (decimal?)p.Valor) ?? 0;

        // Cartões entram como despesa agrupada em "Cartão de Crédito" — usa a mesma função
        // do Resumo Mensal/Contas a Pagar, pra garantir que os números sempre batam.
        var hojeBalanco = DateTime.UtcNow.Date;
        var cartoes = await db.CartoesCredito.AsNoTracking().Where(c => c.LojaId == lojaId && c.Ativo).ToListAsync();
        var dadosCartoesBalanco = await CarregarDadosCartoesAsync(
            cartoes.Select(c => c.Id).ToList(), inicio.AddMonths(-1), fim, inicio, fim);
        decimal totalCartoesMes = 0;
        foreach (var cartao in cartoes)
        {
            var (pagoCartao, pendenteCartao, vencidoCartao) = CalcularContribuicaoCartaoMes(
                cartao, ano, mes, hojeBalanco, dadosCartoesBalanco, doMes.Where(l => l.CartaoOrigemId == cartao.Id));
            totalCartoesMes += pagoCartao + pendenteCartao + vencidoCartao;
        }

        var receitasPorCategoria = doMes
            .Where(l => l.Tipo == "receber")
            .GroupBy(l => new { Nome = l.Categoria?.Nome ?? "Sem categoria", Icone = l.Categoria?.Icone ?? "📁" })
            .Select(g => new { nome = g.Key.Nome, icone = g.Key.Icone, valor = g.Sum(x => x.Valor) })
            .ToList();

        var despesasPorCategoria = doMes
            .Where(l => l.Tipo == "pagar" && !l.CartaoOrigemId.HasValue)
            // Parcelas de financiamento de cartão (CartaoOrigemId preenchido) ficam de fora
            // de propósito: o valor da compra original já foi contado como despesa no mês em
            // que a fatura dela venceu (via totalCartoesMes, logo abaixo). Contar de novo cada
            // parcela no mês em que ela vence duplicaria a mesma dívida — e triplicaria (ou
            // mais) se essa fatura já financiada for refinanciada de novo depois.
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

        // Visão "desmembrada": em vez de uma linha única "Cartão de Crédito", distribui o valor do cartão
        // entre as categorias das compras da fatura (e as soma às categorias das despesas avulsas).
        var desmembradas = await CalcularDespesasPorCategoriaAsync(lojaId.Value, new List<(int Ano, int Mes)> { (ano, mes) }, true);
        var despesasDesmembradas = desmembradas[(ano, mes)]
            .Select(kv => new { nome = kv.Key, icone = kv.Value.Icone, valor = kv.Value.Total, valorCartao = kv.Value.Cartao, valorAvulso = kv.Value.Avulso })
            .OrderByDescending(x => x.valor)
            .ToList();

        return Ok(new
        {
            receitas = receitasPorCategoria.OrderByDescending(r => r.valor),
            despesas = despesasPorCategoria.OrderByDescending(d => d.valor),
            despesasDesmembradas,
            totalReceitas,
            totalDespesas,
            saldo = totalReceitas - totalDespesas,
        });
    }

    // ══════════════════ CARTÕES DE CRÉDITO ══════════════════

    // ── Autocorreção das parcelas de financiamento (ver CorrecaoDeFinanciamentos no Application) ──
    private Task CorrigirParcelasFinanciamentoEmCicloFechadoAsync(Guid lojaId) => correcao.CorrigirAsync(lojaId);

    // ── Calcula a fatura de um mês (sob demanda, sem precisar de job) ──
    private DateTime CalcularVencimentoFatura(CartaoCredito cartao, int ano, int mes)
        => CartaoRegras.VencimentoDaFatura(cartao.DiaVencimento, ano, mes);

    private (DateTime Inicio, DateTime Fim) CicloDaFatura(CartaoCredito cartao, DateTime vencimento)
        => CartaoRegras.CicloDaFatura(cartao.DiaFechamento, vencimento);

    // ── Fonte única de verdade pra "quanto de cartão conta nesse mês" (regime de caixa) ──
    // Usada por ResumoMensal, BalancoPorCategoria e ResumoAnual, pra garantir que os três
    // relatórios NUNCA saiam de sincronia entre si de novo. Conta: (1) o que sobrou da
    // fatura do próprio ciclo desse cartão que vence nesse mês, se ainda não coberta por
    // financiamento; (2) as parcelas de financiamento de faturas antigas (deste cartão)
    // que vencem nesse mês, pagas ou pendentes.
    private async Task<(decimal Pago, decimal Pendente, decimal Vencido)> CalcularContribuicaoCartaoMesAsync(CartaoCredito cartao, int ano, int mes, DateTime hoje)
    {
        decimal pago = 0, pendente = 0, vencido = 0;

        var inicioMes = new DateTime(ano, mes, 1, 0, 0, 0, DateTimeKind.Utc);
        var fimMes = inicioMes.AddMonths(1);
        var vencimentoFatura = CalcularVencimentoFatura(cartao, ano, mes);

        if (vencimentoFatura >= inicioMes && vencimentoFatura < fimMes)
        {
            var (cInicio, cFim) = CicloDaFatura(cartao, vencimentoFatura);
            var totalFatura = await db.LancamentosCartao
                .Where(l => l.CartaoCreditoId == cartao.Id && l.DataCompra.Date >= cInicio.Date && l.DataCompra.Date <= cFim.Date)
                .SumAsync(l => (decimal?)l.Valor) ?? 0;

            if (totalFatura > 0)
            {
                var faturaExistente = await db.FaturasCartao
                    .FirstOrDefaultAsync(f => f.CartaoCreditoId == cartao.Id && f.MesReferencia.Year == ano && f.MesReferencia.Month == mes);
                var totalAntecipado = faturaExistente is null ? 0 : await db.PagamentosAntecipadosFatura
                    .Where(p => p.FaturaCartaoId == faturaExistente.Id)
                    .SumAsync(p => (decimal?)p.Valor) ?? 0;
                var totalRestante = totalFatura - totalAntecipado;
                var resolvida = faturaExistente?.Status == "pago" || faturaExistente?.Status == "parcial" || faturaExistente?.Status == "financiada";

                if (resolvida) pago += totalRestante;
                else if (totalRestante <= 0) pago += totalFatura;
                else if (vencimentoFatura.Date < hoje) vencido += totalRestante;
                else pendente += totalRestante;
            }
        }

        var parcelas = await db.LancamentosFinanceiros
            .Where(l => l.CartaoOrigemId == cartao.Id && l.Vencimento.Year == ano && l.Vencimento.Month == mes)
            .ToListAsync();
        foreach (var p in parcelas)
        {
            if (p.Status == "pago") pago += p.Valor;
            else if (p.Vencimento.Date < hoje) vencido += p.Valor;
            else pendente += p.Valor;
        }

        return (pago, pendente, vencido);
    }

    // ── Despesas por categoria (várias competências de uma vez) ─────────
    // Base do balanço desmembrado e do comparativo mês a mês. Soma as despesas avulsas por categoria e o
    // cartão de crédito de cada mês (mesma regra de CalcularContribuicaoCartaoMes). Com desmembrar=true, o
    // valor do cartão é distribuído entre as categorias das compras da fatura (proporcionalmente, para o
    // total continuar igual ao do balanço normal); as parcelas de financiamento viram uma linha própria.
    private sealed class LinhaCategoriaBalanco
    {
        public string Icone { get; set; } = "📁";
        public decimal Avulso { get; set; }
        public decimal Cartao { get; set; }
        public decimal Total => Avulso + Cartao;
    }

    private sealed record ItemCartaoCategoria(Guid CartaoId, DateTime Data, decimal Valor, string? Nome, string? Icone);

    private async Task<Dictionary<(int Ano, int Mes), Dictionary<string, LinhaCategoriaBalanco>>> CalcularDespesasPorCategoriaAsync(
        Guid lojaId, List<(int Ano, int Mes)> meses, bool desmembrar)
    {
        var resultado = new Dictionary<(int Ano, int Mes), Dictionary<string, LinhaCategoriaBalanco>>();
        foreach (var m in meses) resultado[m] = new Dictionary<string, LinhaCategoriaBalanco>();
        if (meses.Count == 0) return resultado;

        var ordenados = meses.OrderBy(m => m.Ano * 12 + m.Mes).ToList();
        var inicio = new DateTime(ordenados[0].Ano, ordenados[0].Mes, 1, 0, 0, 0, DateTimeKind.Utc);
        var fim = new DateTime(ordenados[ordenados.Count - 1].Ano, ordenados[ordenados.Count - 1].Mes, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(1);
        var hoje = DateTime.UtcNow.Date;

        void Somar(Dictionary<string, LinhaCategoriaBalanco> cats, string nome, string? icone, decimal avulso, decimal cartao)
        {
            if (!cats.TryGetValue(nome, out var linha))
            {
                linha = new LinhaCategoriaBalanco { Icone = icone ?? "📁" };
                cats[nome] = linha;
            }
            linha.Avulso += avulso;
            linha.Cartao += cartao;
        }

        // Despesas avulsas/parceladas/fixas. Parcelas de financiamento de cartão ficam de fora: o valor
        // delas já entra pelo cartão (senão a mesma dívida seria contada duas vezes).
        var lancamentos = await db.LancamentosFinanceiros.AsNoTracking()
            .Where(l => l.LojaId == lojaId && l.Tipo == "pagar" && l.CartaoOrigemId == null && l.Vencimento >= inicio && l.Vencimento < fim)
            .Select(l => new { l.Vencimento, l.Valor, Nome = l.Categoria != null ? l.Categoria.Nome : null, Icone = l.Categoria != null ? l.Categoria.Icone : null })
            .ToListAsync();
        foreach (var l in lancamentos)
        {
            if (!resultado.TryGetValue((l.Vencimento.Year, l.Vencimento.Month), out var cats)) continue;
            Somar(cats, l.Nome ?? "Sem categoria", l.Icone, l.Valor, 0);
        }

        var cartoes = await db.CartoesCredito.AsNoTracking().Where(c => c.LojaId == lojaId && c.Ativo).ToListAsync();
        if (cartoes.Count == 0) return resultado;

        var cartaoIds = cartoes.Select(c => c.Id).ToList();
        var cartaoIdsN = cartaoIds.Cast<Guid?>().ToList();
        var dados = await CarregarDadosCartoesAsync(cartaoIds, inicio.AddMonths(-1), fim, inicio, fim);

        var parcelasFinanciamento = await db.LancamentosFinanceiros.AsNoTracking()
            .Where(l => cartaoIdsN.Contains(l.CartaoOrigemId) && l.Vencimento >= inicio && l.Vencimento < fim)
            .ToListAsync();

        var itens = new List<ItemCartaoCategoria>();
        if (desmembrar)
        {
            itens = await db.LancamentosCartao.AsNoTracking()
                .Where(l => cartaoIds.Contains(l.CartaoCreditoId) && l.DataCompra >= inicio && l.DataCompra < fim)
                .Select(l => new ItemCartaoCategoria(l.CartaoCreditoId, l.DataCompra, l.Valor, l.Categoria != null ? l.Categoria.Nome : null, l.Categoria != null ? l.Categoria.Icone : null))
                .ToListAsync();
        }

        foreach (var (ano, mes) in meses)
        {
            var cats = resultado[(ano, mes)];

            if (desmembrar)
            {
                // Desmembrado: compras do cartão pela DATA DA COMPRA dentro do mês (não pela fatura que vence nele)
                foreach (var grupo in itens.Where(i => i.Data.Year == ano && i.Data.Month == mes).GroupBy(i => i.Nome ?? "Sem categoria"))
                    Somar(cats, grupo.Key, grupo.First().Icone, 0, grupo.Sum(i => i.Valor));

                var somaParcelas = parcelasFinanciamento
                    .Where(p => p.Vencimento.Year == ano && p.Vencimento.Month == mes)
                    .Sum(p => p.Valor);
                if (somaParcelas > 0)
                    Somar(cats, "Financiamento de fatura", "🏦", 0, somaParcelas);
                continue;
            }

            foreach (var cartao in cartoes)
            {
                var parcelasDoMes = parcelasFinanciamento
                    .Where(p => p.CartaoOrigemId == cartao.Id && p.Vencimento.Year == ano && p.Vencimento.Month == mes)
                    .ToList();
                var (pago, pendente, vencido) = CalcularContribuicaoCartaoMes(cartao, ano, mes, hoje, dados, parcelasDoMes);
                var total = pago + pendente + vencido;
                if (total <= 0) continue;
                Somar(cats, "Cartão de Crédito", "💳", 0, total);
            }
        }

        return resultado;
    }

    // ── Lançamentos que compõem uma categoria do balanço (inclusive as compras dentro do cartão) ──
    // O balanço desmembrado soma, na categoria, despesas avulsas + compras do cartão. Contas a Pagar só
    // lista as avulsas (as compras do cartão ficam dentro da linha da fatura, e some quando a fatura é
    // paga), então o clique na categoria precisa deste detalhe para fechar com o valor do balanço.
    private sealed record DetalheBalancoItem(string Origem, string Descricao, decimal Valor, DateTime Data, string Status, string? CartaoNome, string? ContaNome = null, Guid? Id = null, string? Modo = null);

    [HttpGet("balanco-detalhe")]
    public async Task<IActionResult> BalancoDetalhe([FromQuery] int ano, [FromQuery] int mes, [FromQuery] string categoria, [FromQuery] bool desmembrar = false)
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return Ok(new { });

        await CorrigirParcelasFinanciamentoEmCicloFechadoAsync(lojaId.Value);

        var inicio = new DateTime(ano, mes, 1, 0, 0, 0, DateTimeKind.Utc);
        var fim = inicio.AddMonths(1);
        var hoje = DateTime.UtcNow.Date;
        var semCategoria = categoria == "Sem categoria";
        var itens = new List<DetalheBalancoItem>();

        // Despesas avulsas/parceladas/fixas da categoria
        var avulsos = await db.LancamentosFinanceiros.AsNoTracking()
            .Where(l => l.LojaId == lojaId && l.Tipo == "pagar" && l.CartaoOrigemId == null
                        && l.Vencimento >= inicio && l.Vencimento < fim
                        && (semCategoria ? l.Categoria == null : (l.Categoria != null && l.Categoria.Nome == categoria)))
            .Select(l => new { l.Id, l.Modo, l.Descricao, l.Valor, l.Vencimento, l.Status, Conta = l.ContaBancaria != null ? l.ContaBancaria.Nome : null })
            .ToListAsync();
        itens.AddRange(avulsos.Select(l => new DetalheBalancoItem("avulso", l.Descricao, l.Valor, l.Vencimento, l.Status, null, l.Conta, l.Id, l.Modo)));

        var cartoes = await db.CartoesCredito.AsNoTracking().Where(c => c.LojaId == lojaId && c.Ativo).ToListAsync();
        if (cartoes.Count > 0)
        {
            var cartaoIds = cartoes.Select(c => c.Id).ToList();
            var cartaoIdsN = cartaoIds.Cast<Guid?>().ToList();
            var dados = await CarregarDadosCartoesAsync(cartaoIds, inicio.AddMonths(-1), fim, inicio, fim);

            var parcelasFinanciamento = await db.LancamentosFinanceiros.AsNoTracking()
                .Where(l => cartaoIdsN.Contains(l.CartaoOrigemId) && l.Vencimento >= inicio && l.Vencimento < fim)
                .ToListAsync();

            if (categoria == "Cartão de Crédito")
            {
                // Visão agrupada: uma linha por cartão, com o valor que o balanço considera da fatura do mês
                foreach (var cartao in cartoes)
                {
                    var parcelasDoMes = parcelasFinanciamento.Where(p => p.CartaoOrigemId == cartao.Id).ToList();
                    var (pago, pendente, vencido) = CalcularContribuicaoCartaoMes(cartao, ano, mes, hoje, dados, parcelasDoMes);
                    var total = pago + pendente + vencido;
                    if (total <= 0) continue;
                    var status = dados.Fatura(cartao.Id, ano, mes)?.Status;
                    itens.Add(new DetalheBalancoItem("cartao_fatura", $"Fatura {cartao.Nome}", total, CalcularVencimentoFatura(cartao, ano, mes),
                        status == "pago" || status == "parcial" || status == "financiada" ? "pago" : "pendente", cartao.Nome));
                }
            }
            else if (desmembrar && categoria == "Financiamento de fatura")
            {
                foreach (var p in parcelasFinanciamento)
                {
                    var cartao = cartoes.First(c => c.Id == p.CartaoOrigemId);
                    itens.Add(new DetalheBalancoItem("cartao_parcela", p.Descricao, p.Valor, p.Vencimento, p.Status, cartao.Nome));
                }
            }
            else if (desmembrar)
            {
                // Compras no cartão feitas (data da compra) dentro do mês escolhido, qualquer que seja a fatura
                var compras = await db.LancamentosCartao.AsNoTracking()
                    .Where(l => cartaoIds.Contains(l.CartaoCreditoId) && l.DataCompra >= inicio && l.DataCompra < fim
                                && (semCategoria ? l.Categoria == null : (l.Categoria != null && l.Categoria.Nome == categoria)))
                    .Select(l => new { l.Id, l.Modo, l.CartaoCreditoId, l.Descricao, l.Valor, l.DataCompra })
                    .ToListAsync();

                foreach (var c in compras)
                {
                    var cartao = cartoes.First(x => x.Id == c.CartaoCreditoId);
                    itens.Add(new DetalheBalancoItem("cartao_item", c.Descricao, c.Valor, c.DataCompra, "cartao", cartao.Nome, null, c.Id, c.Modo));
                }
            }
        }

        // Valor que o balanço mostra para essa categoria (para o usuário conferir com a soma da lista)
        var calc = await CalcularDespesasPorCategoriaAsync(lojaId.Value, new List<(int Ano, int Mes)> { (ano, mes) }, desmembrar);
        var valorBalanco = calc[(ano, mes)].TryGetValue(categoria, out var linha) ? linha.Total : 0m;

        return Ok(new
        {
            categoria,
            ano,
            mes,
            valorBalanco,
            totalItens = itens.Sum(i => i.Valor),
            itens = itens.OrderBy(i => i.Data).Select(i => new
            {
                origem = i.Origem,
                descricao = i.Descricao,
                valor = i.Valor,
                data = i.Data,
                status = i.Status,
                cartaoNome = i.CartaoNome,
                contaNome = i.ContaNome,
                id = i.Id,
                modo = i.Modo,
            }).ToList(),
        });
    }

    // ── Comparativo de despesas por categoria: mês escolhido x mês anterior + melhor mês do ano ──
    [HttpGet("balanco-comparativo")]
    public async Task<IActionResult> BalancoComparativo([FromQuery] int ano, [FromQuery] int mes, [FromQuery] bool desmembrar = false)
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return Ok(new { });

        await CorrigirParcelasFinanciamentoEmCicloFechadoAsync(lojaId.Value);

        var anterior = mes == 1 ? (Ano: ano - 1, Mes: 12) : (Ano: ano, Mes: mes - 1);
        var meses = new List<(int Ano, int Mes)>();
        for (int m = 1; m <= 12; m++) meses.Add((ano, m));
        if (!meses.Contains(anterior)) meses.Add(anterior);

        var dados = await CalcularDespesasPorCategoriaAsync(lojaId.Value, meses, desmembrar);

        // Só considera "melhor mês" os meses que já aconteceram (o resto do ano ainda é previsão)
        var agora = DateTime.UtcNow;
        var ultimoMesValido = ano < agora.Year ? 12 : ano == agora.Year ? agora.Month : 0;

        decimal ValorDe((int Ano, int Mes) chave, string nome)
            => dados[chave].TryGetValue(nome, out var linha) ? linha.Total : 0;

        var nomes = dados.Values.SelectMany(d => d.Keys).Distinct().ToList();
        var categorias = nomes.Select(nome =>
        {
            var serie = Enumerable.Range(1, 12).Select(m => ValorDe((ano, m), nome)).ToList();
            var atual = serie[mes - 1];
            var anteriorValor = ValorDe(anterior, nome);

            int? melhorMes = null;
            decimal? melhorValor = null;
            for (int m = 1; m <= ultimoMesValido; m++)
            {
                var v = serie[m - 1];
                if (v > 0 && (melhorValor is null || v < melhorValor))
                {
                    melhorValor = v;
                    melhorMes = m;
                }
            }

            var icone = dados.Values.Select(d => d.TryGetValue(nome, out var l) ? l.Icone : null).FirstOrDefault(i => i != null) ?? "📁";

            return new
            {
                nome,
                icone,
                atual,
                anterior = anteriorValor,
                diferenca = atual - anteriorValor,
                variacaoPct = anteriorValor > 0 ? (decimal?)((atual - anteriorValor) / anteriorValor * 100m) : null,
                melhorMes,
                melhorValor,
                serie,
            };
        })
        .Where(c => c.serie.Any(v => v > 0))
        .OrderByDescending(c => c.serie.Sum())
        .ToList();

        return Ok(new
        {
            ano,
            mes,
            anterior = new { ano = anterior.Ano, mes = anterior.Mes },
            totalAtual = categorias.Sum(c => c.atual),
            totalAnterior = categorias.Sum(c => c.anterior),
            totaisMes = Enumerable.Range(0, 12).Select(i => categorias.Sum(c => c.serie[i])).ToList(),
            categorias,
        });
    }

    // ── Dados de cartão pré-carregados ─────────────────────────────────
    // Resumo anual e resumo de cartões consultavam o banco dentro de loops (cartão × mês/ciclo),
    // chegando a centenas de consultas por requisição. Agora os dados necessários são lidos em
    // poucas consultas e as mesmas regras de cálculo rodam em memória.
    private sealed record CompraLeve(Guid CartaoId, DateTime DataCompra, decimal Valor, string Modo);

    private sealed class DadosCartoes
    {
        public Dictionary<Guid, List<CompraLeve>> ComprasPorCartao { get; } = new();
        public List<FaturaCartao> Faturas { get; } = new();
        public Dictionary<Guid, decimal> AntecipadoPorFatura { get; } = new();

        public IReadOnlyList<CompraLeve> Compras(Guid cartaoId)
            => ComprasPorCartao.TryGetValue(cartaoId, out var lista) ? lista : new List<CompraLeve>();

        public decimal SomaCompras(Guid cartaoId, DateTime inicio, DateTime fim)
            => Compras(cartaoId)
                .Where(c => c.DataCompra.Date >= inicio.Date && c.DataCompra.Date <= fim.Date)
                .Sum(c => c.Valor);

        public FaturaCartao? Fatura(Guid cartaoId, int ano, int mes)
            => Faturas.FirstOrDefault(f => f.CartaoCreditoId == cartaoId && f.MesReferencia.Year == ano && f.MesReferencia.Month == mes);

        public decimal Antecipado(FaturaCartao? fatura)
            => fatura is not null && AntecipadoPorFatura.TryGetValue(fatura.Id, out var v) ? v : 0;
    }

    private async Task<DadosCartoes> CarregarDadosCartoesAsync(
        List<Guid> cartaoIds, DateTime comprasDesde, DateTime? comprasAte, DateTime faturasDesde, DateTime? faturasAte)
    {
        var dados = new DadosCartoes();
        if (cartaoIds.Count == 0) return dados;

        var comprasQuery = db.LancamentosCartao.AsNoTracking()
            .Where(l => cartaoIds.Contains(l.CartaoCreditoId) && l.DataCompra >= comprasDesde);
        if (comprasAte.HasValue) comprasQuery = comprasQuery.Where(l => l.DataCompra < comprasAte.Value);

        var compras = await comprasQuery
            .Select(l => new CompraLeve(l.CartaoCreditoId, l.DataCompra, l.Valor, l.Modo))
            .ToListAsync();
        foreach (var grupo in compras.GroupBy(c => c.CartaoId))
            dados.ComprasPorCartao[grupo.Key] = grupo.ToList();

        await CarregarFaturasEAntecipadosAsync(dados, cartaoIds, faturasDesde, faturasAte);

        return dados;
    }

    private async Task CarregarFaturasEAntecipadosAsync(DadosCartoes dados, List<Guid> cartaoIds, DateTime faturasDesde, DateTime? faturasAte)
    {
        var faturasQuery = db.FaturasCartao.AsNoTracking()
            .Where(f => cartaoIds.Contains(f.CartaoCreditoId) && f.MesReferencia >= faturasDesde);
        if (faturasAte.HasValue) faturasQuery = faturasQuery.Where(f => f.MesReferencia < faturasAte.Value);
        dados.Faturas.AddRange(await faturasQuery.ToListAsync());

        var faturaIds = dados.Faturas.Select(f => f.Id).ToList();
        if (faturaIds.Count > 0)
        {
            var antecipados = await db.PagamentosAntecipadosFatura.AsNoTracking()
                .Where(p => faturaIds.Contains(p.FaturaCartaoId))
                .GroupBy(p => p.FaturaCartaoId)
                .Select(g => new { FaturaId = g.Key, Total = g.Sum(x => x.Valor) })
                .ToListAsync();
            foreach (var a in antecipados) dados.AntecipadoPorFatura[a.FaturaId] = a.Total;
        }
    }

    // Mesma regra de CalcularContribuicaoCartaoMesAsync, só que em memória. parcelasDoMes =
    // parcelas de financiamento (LancamentosFinanceiros) deste cartão que vencem nesse mês.
    private (decimal Pago, decimal Pendente, decimal Vencido) CalcularContribuicaoCartaoMes(
        CartaoCredito cartao, int ano, int mes, DateTime hoje, DadosCartoes dados, IEnumerable<LancamentoFinanceiro> parcelasDoMes)
    {
        decimal pago = 0, pendente = 0, vencido = 0;

        var inicioMes = new DateTime(ano, mes, 1, 0, 0, 0, DateTimeKind.Utc);
        var fimMes = inicioMes.AddMonths(1);
        var vencimentoFatura = CalcularVencimentoFatura(cartao, ano, mes);

        if (vencimentoFatura >= inicioMes && vencimentoFatura < fimMes)
        {
            var (cInicio, cFim) = CicloDaFatura(cartao, vencimentoFatura);
            var totalFatura = dados.SomaCompras(cartao.Id, cInicio, cFim);

            if (totalFatura > 0)
            {
                var faturaExistente = dados.Fatura(cartao.Id, ano, mes);
                var totalAntecipado = dados.Antecipado(faturaExistente);
                var totalRestante = totalFatura - totalAntecipado;
                var resolvida = faturaExistente?.Status == "pago" || faturaExistente?.Status == "parcial" || faturaExistente?.Status == "financiada";

                if (resolvida) pago += totalRestante;
                else if (totalRestante <= 0) pago += totalFatura;
                else if (vencimentoFatura.Date < hoje) vencido += totalRestante;
                else pendente += totalRestante;
            }
        }

        foreach (var p in parcelasDoMes)
        {
            if (p.Status == "pago") pago += p.Valor;
            else if (p.Vencimento.Date < hoje) vencido += p.Valor;
            else pendente += p.Valor;
        }

        return (pago, pendente, vencido);
    }

    // Mesma regra de CicloAtualCartaoAsync, em memória.
    private (DateTime Vencimento, decimal Total, string Status) CicloAtualCartaoMem(CartaoCredito cartao, DadosCartoes dados, DateTime hoje)
    {
        for (int offset = -3; offset <= 0; offset++)
        {
            var refBase = new DateTime(hoje.Year, hoje.Month, 1).AddMonths(offset);
            var vencimentoCheck = CalcularVencimentoFatura(cartao, refBase.Year, refBase.Month);
            var cicloCheck = CicloDaFatura(cartao, vencimentoCheck);

            var cicloJaFechou = hoje.Date > cicloCheck.Fim.Date;
            if (!cicloJaFechou) continue;

            var totalCheck = dados.SomaCompras(cartao.Id, cicloCheck.Inicio, cicloCheck.Fim);
            if (totalCheck <= 0) continue;

            var faturaCheck = dados.Fatura(cartao.Id, vencimentoCheck.Year, vencimentoCheck.Month);
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

        var total = dados.SomaCompras(cartao.Id, ciclo.Inicio, ciclo.Fim);
        var fatura = dados.Fatura(cartao.Id, vencimento.Year, vencimento.Month);
        return (vencimento, total, fatura?.Status ?? "pendente");
    }

    [HttpGet("resumo-anual")]
    public async Task<IActionResult> ResumoAnual([FromQuery] int ano)
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return Ok(Array.Empty<object>());

        await CorrigirParcelasFinanciamentoEmCicloFechadoAsync(lojaId.Value);

        var inicio = new DateTime(ano, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var fim = inicio.AddYears(1);

        var doAno = await db.LancamentosFinanceiros.AsNoTracking()
            .Where(l => l.LojaId == lojaId && l.Vencimento >= inicio && l.Vencimento < fim)
            .ToListAsync();

        // Planos: busca pagamentos já gerados no ano
        var assinaturas = await db.AssinaturasCliente.AsNoTracking()
            .Where(a => a.LojaId == lojaId && a.Status == "ativa")
            .ToListAsync();
        var assinaturaIds = assinaturas.Select(a => a.Id).ToList();

        var pagamentosPlanoAno = await db.PagamentosPlano.AsNoTracking()
            .Where(p => assinaturaIds.Contains(p.AssinaturaId) && p.MesReferencia >= inicio && p.MesReferencia < fim)
            .ToListAsync();

        // Soma o valor dos planos vinculados às assinaturas ativas para projeção mensal
        var planoIds = assinaturas.Select(a => a.PlanoId).Distinct().ToList();
        var planos = await db.Planos.AsNoTracking().Where(p => planoIds.Contains(p.Id)).ToListAsync();
        var receitaProjetadaMensal = assinaturas
            .Sum(a => planos.FirstOrDefault(p => p.Id == a.PlanoId)?.Valor ?? 0);

        // Meses com pagamento já gerado — para os demais, projeta com base nas assinaturas ativas
        var mesesComPagamento = pagamentosPlanoAno.Select(p => p.MesReferencia.Month).ToHashSet();

        var cartoes = await db.CartoesCredito.AsNoTracking().Where(c => c.LojaId == lojaId && c.Ativo).ToListAsync();
        var totalCartaoPorMes = new decimal[13]; // índice 1-12
        var totalCartaoPagoPorMes = new decimal[13]; // parte da fatura já paga
        var hojeAnual = DateTime.UtcNow.Date;

        if (cartoes.Count > 0)
        {
            var cartaoIds = cartoes.Select(c => c.Id).ToList();
            var cartaoIdsN = cartaoIds.Cast<Guid?>().ToList();

            // Os ciclos do ano começam em dezembro do ano anterior e terminam em dezembro.
            var dadosCartoes = await CarregarDadosCartoesAsync(
                cartaoIds,
                new DateTime(ano - 1, 12, 1, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(ano + 1, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                inicio,
                fim);

            var parcelasFinanciamento = await db.LancamentosFinanceiros.AsNoTracking()
                .Where(l => cartaoIdsN.Contains(l.CartaoOrigemId) && l.Vencimento >= inicio && l.Vencimento < fim)
                .ToListAsync();

            foreach (var cartao in cartoes)
            {
                var parcelasDoCartao = parcelasFinanciamento.Where(l => l.CartaoOrigemId == cartao.Id).ToList();

                // Mesma regra do Resumo Mensal e do Balanço — os três relatórios ficam
                // sempre consistentes entre si.
                for (int mes = 1; mes <= 12; mes++)
                {
                    var parcelasDoMes = parcelasDoCartao.Where(l => l.Vencimento.Month == mes);
                    var (pagoCartao, pendenteCartao, vencidoCartao) = CalcularContribuicaoCartaoMes(cartao, ano, mes, hojeAnual, dadosCartoes, parcelasDoMes);
                    totalCartaoPorMes[mes] += pagoCartao + pendenteCartao + vencidoCartao;
                    totalCartaoPagoPorMes[mes] += pagoCartao;
                }
            }
        }

        var meses = Enumerable.Range(1, 12).Select(mes =>
        {
            // Previsão: soma TUDO previsto pra esse mês (pago + pendente), não só o pago.
            // Parcelas de financiamento de cartão ficam de fora (mesmo motivo do Balanço
            // Mensal): o valor já está em totalCartaoPorMes, no mês da compra original.
            var pagarLancamentos = doAno.Where(l => l.Tipo == "pagar" && l.Vencimento.Month == mes && l.CartaoOrigemId == null).Sum(l => l.Valor);
            var pagar = pagarLancamentos + totalCartaoPorMes[mes];

            var receberLancamentos = doAno.Where(l => l.Tipo == "receber" && l.Vencimento.Month == mes).Sum(l => l.Valor);
            var mesAtual = DateTime.UtcNow.Month;
            var anoAtual = DateTime.UtcNow.Year;
            var ehFuturo = ano > anoAtual || (ano == anoAtual && mes >= mesAtual);

            var receberPlanos = mesesComPagamento.Contains(mes)
                ? pagamentosPlanoAno.Where(p => p.MesReferencia.Month == mes).Sum(p => p.Valor)
                : ehFuturo ? receitaProjetadaMensal : 0; // só projeta mês atual em diante
            var receber = receberLancamentos + receberPlanos;

            // Quanto das despesas do mês já foi pago e quanto ainda falta pagar
            var pagarPagoLancamentos = doAno.Where(l => l.Tipo == "pagar" && l.Vencimento.Month == mes && l.CartaoOrigemId == null && l.Status == "pago").Sum(l => l.Valor);
            var pagarPago = pagarPagoLancamentos + totalCartaoPagoPorMes[mes];

            return new { mes, pagar, receber, saldo = receber - pagar, pagarPago, pagarAberto = pagar - pagarPago };
        }).ToList();

        return Ok(meses);
    }

    [HttpGet("alertas-vencimento")]
    public async Task<IActionResult> AlertasVencimento([FromQuery] int dias = 7)
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return Ok(Array.Empty<object>());

        await CorrigirParcelasFinanciamentoEmCicloFechadoAsync(lojaId.Value);

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

        await CorrigirParcelasFinanciamentoEmCicloFechadoAsync(lojaId.Value);

        var cartoes = await db.CartoesCredito.AsNoTracking().Where(c => c.LojaId == lojaId && c.Ativo).ToListAsync();
        var resultado = new List<object>();
        if (cartoes.Count == 0) return Ok(resultado);

        var hoje = DateTime.UtcNow;

        // Performance: tudo que o cálculo precisa é carregado de uma vez (antes eram dezenas de
        // consultas por cartão, dentro de loops). A janela cobre os 12 ciclos somados + folga;
        // compras futuras (parceladas) e faturas futuras entram porque não há limite superior.
        var cartaoIds = cartoes.Select(c => c.Id).ToList();
        var cartaoIdsN = cartaoIds.Cast<Guid?>().ToList();
        var inicioJanela = new DateTime(hoje.Year, hoje.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-16);
        var dados = await CarregarDadosCartoesAsync(cartaoIds, inicioJanela, null, inicioJanela, null);

        var financiamentoPendente = (await db.LancamentosFinanceiros.AsNoTracking()
            .Where(l => cartaoIdsN.Contains(l.CartaoOrigemId) && l.Status == "pendente")
            .GroupBy(l => l.CartaoOrigemId)
            .Select(g => new { CartaoId = g.Key, Total = g.Sum(x => x.Valor) })
            .ToListAsync())
            .Where(x => x.CartaoId != null)
            .ToDictionary(x => x.CartaoId!.Value, x => x.Total);

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

                var totalCiclo = dados.SomaCompras(cartao.Id, cInicio, cFim);

                if (totalCiclo > 0)
                {
                    var faturaExistente = dados.Fatura(cartao.Id, cursor.Year, cursor.Month);

                    // "financiada": virou pagamento parcelado à parte, não conta mais nesse ciclo.
                    // "parcial": o restante já foi lançado como compra real no próximo ciclo (com juros),
                    // então não soma aqui de novo — senão a mesma dívida é contada duas vezes.
                    if (faturaExistente?.Status != "pago" && faturaExistente?.Status != "financiada" && faturaExistente?.Status != "parcial")
                    {
                        usado += totalCiclo - dados.Antecipado(faturaExistente);
                    }
                    else if (faturaExistente is not null && faturaExistente.Total > 0 && totalCiclo > faturaExistente.Total)
                    {
                        // Fatura já quitada/financiada: o que foi comprado nesse ciclo DEPOIS do pagamento
                        // (total atual acima do total registrado no pagamento) ainda consome o limite
                        usado += totalCiclo - faturaExistente.Total;
                    }
                }

                cursor = cursor.AddMonths(1);
            }

            // Compras futuras (parceladas ou avulsas com data de um ciclo além do aberto) — ainda não contadas
            // acima. Cobrança fixa/recorrente futura NÃO entra aqui: só conta quando já estiver dentro de
            // uma fatura gerada (fechada ou aberta), que já é somada no loop dos últimos 12 meses acima.
            var parcelasFuturas = dados.Compras(cartao.Id)
                .Where(l => l.Modo != "fixa" && l.DataCompra.Date > cicloAberta.Fim.Date)
                .ToList();

            decimal totalParcelasFuturas = 0;
            foreach (var p in parcelasFuturas)
            {
                var vencTeste = CalcularVencimentoFatura(cartao, p.DataCompra.Year, p.DataCompra.Month);
                var (ini, fim) = CicloDaFatura(cartao, vencTeste);
                if (p.DataCompra.Date < ini.Date || p.DataCompra.Date > fim.Date)
                    vencTeste = CalcularVencimentoFatura(cartao, p.DataCompra.AddMonths(1).Year, p.DataCompra.AddMonths(1).Month);

                var faturaFutura = dados.Fatura(cartao.Id, vencTeste.Year, vencTeste.Month);

                if (faturaFutura?.Status != "pago" && faturaFutura?.Status != "financiada")
                    totalParcelasFuturas += p.Valor;
            }

            usado += totalParcelasFuturas;

            // Parcelas de financiamento de faturas antigas (ficam em LancamentosFinanceiros, não em
            // LancamentosCartao — por isso não entram nas duas somas acima) — soma tudo que ainda
            // não foi pago, independente do mês de vencimento.
            usado += financiamentoPendente.TryGetValue(cartao.Id, out var totalFinanciamentoPendente) ? totalFinanciamentoPendente : 0;

            // Fatura "principal" pra exibir vencimento/status no card (a mais antiga fechada e pendente, senão a aberta)
            var (vencimentoPrincipal, _, statusPrincipal) = CicloAtualCartaoMem(cartao, dados, hoje);
            var (piInicio, piFim) = CicloDaFatura(cartao, vencimentoPrincipal);
            var qtdCompras = dados.Compras(cartao.Id)
                .Count(l => l.DataCompra.Date >= piInicio.Date && l.DataCompra.Date <= piFim.Date);

            resultado.Add(new
            {
                cartao.Id,
                cartao.Nome,
                cartao.Bandeira,
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

    // Fatura "a pagar agora" do cartão (ver CicloAtualDoCartao no Application)
    private Task<(DateTime Vencimento, decimal Total, string Status)> CicloAtualCartaoAsync(CartaoCredito cartao) => cicloAtual.ObterAsync(cartao);
}
