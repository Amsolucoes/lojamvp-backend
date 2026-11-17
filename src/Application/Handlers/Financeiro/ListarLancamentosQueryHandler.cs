using LojaApi.Shared.Application;

namespace LojaApi.Application.Financeiro;

// Lançamentos do tipo (pagar ou receber) da loja, por vencimento, opcionalmente de um mês. Antes, corrige as parcelas de financiamento.
public class ListarLancamentosQueryHandler(ILancamentosRepository repositorio, CorrecaoDeFinanciamentos correcao) : IQueryHandler<ListarLancamentosQuery>
{
    public async Task<Resultado<object>> HandleAsync(ListarLancamentosQuery query)
    {
        var (lojaIdOuNulo, tipo, ano, mes) = query;
        if (lojaIdOuNulo is not { } lojaId) return Resultado.Ok(Array.Empty<object>());

        await correcao.CorrigirAsync(lojaId);

        DateTime? inicio = null, fim = null;
        if (ano.HasValue && mes.HasValue)
        {
            inicio = new DateTime(ano.Value, mes.Value, 1, 0, 0, 0, DateTimeKind.Utc);
            fim = inicio.Value.AddMonths(1);
        }

        return Resultado.Ok(await repositorio.ListarPorTipoAsync(lojaId, tipo, inicio, fim));
    }
}
