using LojaApi.Domain.Financeiro;
using LojaApi.Shared.Application;

namespace LojaApi.Application.Financeiro;

// A fatura de um cartão num mês: compras do ciclo, parcelas de financiamento que vencem no mês, adiantamentos, total e o que falta pagar.
public class VerFaturaQueryHandler(IFaturasRepository faturas, ICartoesRepository cartoes, CorrecaoDeFinanciamentos correcao) : IQueryHandler<VerFaturaQuery>
{
    public async Task<Resultado<object>> HandleAsync(VerFaturaQuery query)
    {
        var (lojaIdOuNulo, cartaoId, ano, mes) = query;
        if (lojaIdOuNulo is not { } lojaId) return Resultado.NaoEncontrado();
        var cartao = await cartoes.ObterCartaoAsync(cartaoId, lojaId);
        if (cartao is null) return Resultado.NaoEncontrado();

        await correcao.CorrigirAsync(lojaId);

        var vencimento = CartaoRegras.VencimentoDaFatura(cartao.DiaVencimento, ano, mes);
        var (inicio, fim) = CartaoRegras.CicloDaFatura(cartao.DiaFechamento, vencimento);

        var itens = await faturas.ListarItensDoCicloAsync(cartaoId, inicio, fim);
        var faturaExistente = await faturas.ObterFaturaDoMesAsync(cartaoId, ano, mes);

        // Parcelas do financiamento que caem NESTE mês (independente de qual fatura originou o parcelamento)
        var parcelasRaw = await faturas.ListarParcelasDoMesAsync(cartaoId, ano, mes);
        var faturasOrigemIds = parcelasRaw.Where(l => l.FaturaCartaoId.HasValue).Select(l => l.FaturaCartaoId!.Value).Distinct().ToList();
        var faturasOrigem = await faturas.ListarFaturasPorIdsAsync(faturasOrigemIds);

        var parcelasFinanciamento = parcelasRaw.Select(l =>
        {
            var faturaOrigem = l.FaturaCartaoId.HasValue ? faturasOrigem.FirstOrDefault(f => f.Id == l.FaturaCartaoId.Value) : null;
            return new ParcelaFinanciamentoDto(l.Id, l.Descricao, l.Valor, l.Vencimento, l.Status, l.NumeroParcela, l.TotalParcelas, l.ContaBancariaId, l.CategoriaId,
                l.Observacao, l.Modo, faturaOrigem?.MesReferencia.Month, faturaOrigem?.MesReferencia.Year);
        }).ToList();

        var total = itens.Sum(i => i.Valor) + parcelasFinanciamento.Sum(p => p.Valor);

        var antecipados = await faturas.ListarAntecipadosAsync(faturaExistente?.Id ?? Guid.Empty);
        var totalAntecipado = antecipados.Sum(a => a.Valor);

        return Resultado.Ok(new
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
}
