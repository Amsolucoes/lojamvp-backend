using LojaApi.Domain.Financeiro;
using LojaApi.Shared.Application;

namespace LojaApi.Application.Financeiro;

// Faturas dos cartões ativos que vencem no mês: total (compras do ciclo + parcelas de financiamento antigas), status
// (pendente | parcial | pago | financiada), valor pago, adiantamentos e, se foi parcelada, o cronograma das parcelas.
// Usada pelas telas de Cartões e Contas a Pagar.
public class FaturasDoMesQueryHandler(IContasAPagarConsultas consultas) : IQueryHandler<FaturasDoMesQuery>
{
    public async Task<Resultado<object>> HandleAsync(FaturasDoMesQuery query)
    {
        var (lojaIdOuNulo, ano, mes) = query;
        if (lojaIdOuNulo is not { } lojaId) return Resultado.Ok(Array.Empty<object>());

        var cartoes = await consultas.ListarCartoesAtivosAsync(lojaId);
        if (cartoes.Count == 0) return Resultado.Ok(Array.Empty<object>());

        var inicioMes = new DateTime(ano, mes, 1, 0, 0, 0, DateTimeKind.Utc);
        var fimMes = inicioMes.AddMonths(1);
        var cartaoIds = cartoes.Select(c => c.Id).ToList();

        var compras = await consultas.ListarComprasDeCartaoAsync(cartaoIds, inicioMes.AddMonths(-1), fimMes);
        var dados = await DadosDeCartoes.CarregarAsync(consultas, cartaoIds, inicioMes, fimMes);

        // Parcelas de financiamento (de faturas antigas) que vencem neste mês — entram no total da fatura
        var parcelasDoMes = await consultas.ListarParcelasDeFinanciamentoAsync(cartaoIds, inicioMes, fimMes);

        // Cronograma completo das parcelas geradas pelo financiamento das faturas deste mês
        var parcelasOrigem = dados.Faturas.Count == 0
            ? []
            : await consultas.ListarParcelasDasFaturasAsync(dados.Faturas.Select(f => f.Id).ToList());

        var resultado = new List<object>();
        foreach (var cartao in cartoes)
        {
            var vencimento = CartaoRegras.VencimentoDaFatura(cartao.DiaVencimento, ano, mes);
            var (cInicio, cFim) = CartaoRegras.CicloDaFatura(cartao.DiaFechamento, vencimento);

            var totalCompras = compras.Where(c => c.CartaoId == cartao.Id && c.DataCompra.Date >= cInicio.Date && c.DataCompra.Date <= cFim.Date).Sum(c => c.Valor);
            var totalParcelasFinanciamento = parcelasDoMes.Where(l => l.CartaoOrigemId == cartao.Id).Sum(l => l.Valor);
            var total = totalCompras + totalParcelasFinanciamento;

            var fatura = dados.Fatura(cartao.Id, ano, mes);
            if (total <= 0 && fatura is null) continue;

            var totalAntecipado = dados.Antecipado(fatura);
            var parcelasDaFatura = fatura is null ? [] : parcelasOrigem.Where(l => l.FaturaCartaoId == fatura.Id).ToList();

            resultado.Add(new
            {
                cartaoId = cartao.Id,
                cartaoNome = cartao.Nome,
                ano,
                mes,
                vencimento,
                totalCompras,
                totalParcelasFinanciamento,
                total,
                totalAntecipado,
                restante = total - totalAntecipado,
                status = fatura?.Status ?? "pendente",
                valorPago = fatura?.ValorPago ?? 0,
                pagoEm = fatura?.PagoEm,
                parcelas = parcelasDaFatura.Select(l => new
                {
                    l.Id,
                    l.NumeroParcela,
                    l.TotalParcelas,
                    l.Valor,
                    l.Vencimento,
                    l.Status,
                    l.PagoEm,
                }).ToList(),
            });
        }

        return Resultado.Ok(resultado);
    }
}
