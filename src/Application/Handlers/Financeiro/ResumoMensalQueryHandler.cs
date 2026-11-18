using LojaApi.Domain.Financeiro;
using LojaApi.Shared.Application;

namespace LojaApi.Application.Financeiro;

// Resumo do mês: Pagar e Receber lado a lado (pago, a vencer e vencido), a previsão e o detalhe do que compõe o "a pagar" (contas e cartões).
// Parcelas de financiamento de cartão ficam fora das contas: entram pelo cartão de origem, para não vazarem para as contas avulsas.
public class ResumoMensalQueryHandler(IRelatoriosConsultas consultas, IContasAPagarConsultas contasAPagar, CorrecaoDeFinanciamentos correcao) : IQueryHandler<ResumoMensalQuery>
{
    public async Task<Resultado<object>> HandleAsync(ResumoMensalQuery query)
    {
        var (lojaIdOuNulo, ano, mes) = query;
        if (lojaIdOuNulo is not { } lojaId) return Resultado.Ok(new { });

        await correcao.CorrigirAsync(lojaId);

        var inicio = new DateTime(ano, mes, 1, 0, 0, 0, DateTimeKind.Utc);
        var fim = inicio.AddMonths(1);
        var hoje = DateTime.UtcNow.Date;

        var doMes = await consultas.ListarLancamentosDoPeriodoAsync(lojaId, inicio, fim);

        var pagarAvulsos = doMes.Where(l => l.Tipo == "pagar" && l.CartaoOrigemId == null).ToList();
        decimal pagarPago = pagarAvulsos.Where(l => l.Status == "pago").Sum(l => l.Valor);
        int pagarQtdPago = pagarAvulsos.Count(l => l.Status == "pago");
        decimal pagarPendente = pagarAvulsos.Where(l => l.Status == "pendente" && l.Vencimento.Date >= hoje).Sum(l => l.Valor);
        int pagarQtdPendente = pagarAvulsos.Count(l => l.Status == "pendente" && l.Vencimento.Date >= hoje);
        decimal pagarVencido = pagarAvulsos.Where(l => l.Status == "pendente" && l.Vencimento.Date < hoje).Sum(l => l.Valor);
        int pagarQtdVencido = pagarAvulsos.Count(l => l.Status == "pendente" && l.Vencimento.Date < hoje);

        var receber = doMes.Where(l => l.Tipo == "receber").ToList();
        decimal receberPago = receber.Where(l => l.Status == "pago").Sum(l => l.Valor);
        int receberQtdPago = receber.Count(l => l.Status == "pago");
        decimal receberPendente = receber.Where(l => l.Status == "pendente" && l.Vencimento.Date >= hoje).Sum(l => l.Valor);
        int receberQtdPendente = receber.Count(l => l.Status == "pendente" && l.Vencimento.Date >= hoje);
        decimal receberVencido = receber.Where(l => l.Status == "pendente" && l.Vencimento.Date < hoje).Sum(l => l.Valor);
        int receberQtdVencido = receber.Count(l => l.Status == "pendente" && l.Vencimento.Date < hoje);

        foreach (var p in await consultas.ListarPagamentosDePlanoAsync(lojaId, inicio, fim, apenasAssinaturasAtivas: false))
        {
            if (p.Status == "pago") { receberPago += p.Valor; receberQtdPago++; }
            else if (p.MesReferencia.Date < hoje) { receberVencido += p.Valor; receberQtdVencido++; }
            else { receberPendente += p.Valor; receberQtdPendente++; }
        }

        var detalheCartoesPagar = new List<object>();
        decimal totalCartoesVisiveis = 0;
        var cartoes = await consultas.ListarCartoesAtivosAsync(lojaId);
        // Dados dos cartões carregados de uma vez (em vez de ~4 consultas por cartão)
        var dados = await DadosDosCartoes.CarregarAsync(consultas, contasAPagar, cartoes.Select(c => c.Id).ToList(), inicio.AddMonths(-1), fim, inicio, fim);
        foreach (var cartao in cartoes)
        {
            var parcelas = doMes.Where(l => l.CartaoOrigemId == cartao.Id).Select(l => new RelatoriosRegras.ParcelaDoMes(l.Status, l.Vencimento, l.Valor));
            var contribuicao = dados.ContribuicaoNoMes(cartao, ano, mes, hoje, parcelas);

            if (contribuicao.Pago > 0) { pagarPago += contribuicao.Pago; pagarQtdPago++; }
            if (contribuicao.Pendente > 0) { pagarPendente += contribuicao.Pendente; pagarQtdPendente++; }
            if (contribuicao.Vencido > 0) { pagarVencido += contribuicao.Vencido; pagarQtdVencido++; }

            var valorVisivelCartao = contribuicao.Pendente + contribuicao.Vencido;
            totalCartoesVisiveis += valorVisivelCartao;
            if (valorVisivelCartao > 0)
                detalheCartoesPagar.Add(new { nome = cartao.Nome, valor = valorVisivelCartao });
        }

        // "Contas" no detalhamento reflete só o que ainda está em aberto (pendente + vencido), como cada linha de cartão: a soma de "Contas" + cada cartão
        // bate com o "A Pagar" mostrado em cima, em vez de bater com o "Total do mês" (que inclui o que já foi pago).
        var totalLancamentosPagar = pagarPendente + pagarVencido - totalCartoesVisiveis;

        var previstoReceita = receberPago + receberPendente + receberVencido;
        var previstoDespesa = pagarPago + pagarPendente + pagarVencido;

        return Resultado.Ok(new
        {
            pagar = new { totalPago = pagarPago, qtdPago = pagarQtdPago, totalPendente = pagarPendente, qtdPendente = pagarQtdPendente, totalVencido = pagarVencido, qtdVencido = pagarQtdVencido },
            receber = new { totalPago = receberPago, qtdPago = receberQtdPago, totalPendente = receberPendente, qtdPendente = receberQtdPendente, totalVencido = receberVencido, qtdVencido = receberQtdVencido },
            previsao = new { receitaPrevista = previstoReceita, despesaPrevista = previstoDespesa, saldoPrevisto = previstoReceita - previstoDespesa },
            detalhePagar = new { lancamentos = totalLancamentosPagar, cartoes = detalheCartoesPagar },
        });
    }
}
