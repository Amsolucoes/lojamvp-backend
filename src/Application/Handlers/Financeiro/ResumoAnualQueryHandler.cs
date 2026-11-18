using LojaApi.Domain.Financeiro;
using LojaApi.Shared.Application;

namespace LojaApi.Application.Financeiro;

// Resumo do ano: para cada mês, o previsto a pagar e a receber (pago + pendente), o saldo e quanto das despesas já foi pago.
// O cartão entra pela mesma regra do resumo mensal e do balanço (os três relatórios ficam consistentes). As mensalidades dos planos usam os pagamentos já
// gerados e, nos meses sem pagamento (do atual em diante), projetam o valor dos planos das assinaturas ativas.
public class ResumoAnualQueryHandler(IRelatoriosConsultas consultas, IContasAPagarConsultas contasAPagar, CorrecaoDeFinanciamentos correcao) : IQueryHandler<ResumoAnualQuery>
{
    public async Task<Resultado<object>> HandleAsync(ResumoAnualQuery query)
    {
        var (lojaIdOuNulo, ano) = query;
        if (lojaIdOuNulo is not { } lojaId) return Resultado.Ok(Array.Empty<object>());

        await correcao.CorrigirAsync(lojaId);

        var inicio = new DateTime(ano, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var fim = inicio.AddYears(1);

        var doAno = await consultas.ListarLancamentosDoPeriodoAsync(lojaId, inicio, fim);

        // Planos: pagamentos já gerados no ano e, para os demais meses, a projeção pelas assinaturas ativas
        var pagamentosPlanoAno = await consultas.ListarPagamentosDePlanoAsync(lojaId, inicio, fim, apenasAssinaturasAtivas: true);
        var receitaProjetadaMensal = (await consultas.ListarValoresDosPlanosAtivosAsync(lojaId)).Sum();
        var mesesComPagamento = pagamentosPlanoAno.Select(p => p.MesReferencia.Month).ToHashSet();

        var cartoes = await consultas.ListarCartoesAtivosAsync(lojaId);
        var totalCartaoPorMes = new decimal[13];       // índice 1-12
        var totalCartaoPagoPorMes = new decimal[13];   // parte da fatura já paga
        var hoje = DateTime.UtcNow.Date;

        if (cartoes.Count > 0)
        {
            var cartaoIds = cartoes.Select(c => c.Id).ToList();

            // Os ciclos do ano começam em dezembro do ano anterior e terminam em dezembro
            var dados = await DadosDosCartoes.CarregarAsync(consultas, contasAPagar, cartaoIds,
                new DateTime(ano - 1, 12, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(ano + 1, 1, 1, 0, 0, 0, DateTimeKind.Utc), inicio, fim);
            var parcelasFinanciamento = await contasAPagar.ListarParcelasDeFinanciamentoAsync(cartaoIds, inicio, fim);

            foreach (var cartao in cartoes)
            {
                var parcelasDoCartao = parcelasFinanciamento.Where(l => l.CartaoOrigemId == cartao.Id).ToList();
                for (int mes = 1; mes <= 12; mes++)
                {
                    var parcelasDoMes = parcelasDoCartao.Where(l => l.Vencimento.Month == mes).Select(p => new RelatoriosRegras.ParcelaDoMes(p.Status, p.Vencimento, p.Valor));
                    var contribuicao = dados.ContribuicaoNoMes(cartao, ano, mes, hoje, parcelasDoMes);
                    totalCartaoPorMes[mes] += contribuicao.Total;
                    totalCartaoPagoPorMes[mes] += contribuicao.Pago;
                }
            }
        }

        var agora = DateTime.UtcNow;
        var meses = Enumerable.Range(1, 12).Select(mes =>
        {
            // Previsão: soma TUDO previsto pra esse mês (pago + pendente). Parcelas de financiamento de cartão ficam de fora (mesmo motivo do balanço):
            // o valor já está no cartão, no mês da compra original.
            var pagarLancamentos = doAno.Where(l => l.Tipo == "pagar" && l.Vencimento.Month == mes && l.CartaoOrigemId == null).Sum(l => l.Valor);
            var pagar = pagarLancamentos + totalCartaoPorMes[mes];

            var receberLancamentos = doAno.Where(l => l.Tipo == "receber" && l.Vencimento.Month == mes).Sum(l => l.Valor);
            var ehFuturo = ano > agora.Year || (ano == agora.Year && mes >= agora.Month);

            var receberPlanos = mesesComPagamento.Contains(mes)
                ? pagamentosPlanoAno.Where(p => p.MesReferencia.Month == mes).Sum(p => p.Valor)
                : ehFuturo ? receitaProjetadaMensal : 0; // só projeta mês atual em diante
            var receber = receberLancamentos + receberPlanos;

            // Quanto das despesas do mês já foi pago e quanto ainda falta pagar
            var pagarPagoLancamentos = doAno.Where(l => l.Tipo == "pagar" && l.Vencimento.Month == mes && l.CartaoOrigemId == null && l.Status == "pago").Sum(l => l.Valor);
            var pagarPago = pagarPagoLancamentos + totalCartaoPagoPorMes[mes];

            return new { mes, pagar, receber, saldo = receber - pagar, pagarPago, pagarAberto = pagar - pagarPago };
        }).ToList();

        return Resultado.Ok(meses);
    }
}
