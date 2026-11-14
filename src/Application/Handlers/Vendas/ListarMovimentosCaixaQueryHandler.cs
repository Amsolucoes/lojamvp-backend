using LojaApi.Shared.Application;

namespace LojaApi.Application.Vendas;

// Reforços e sangrias do caixa da loja, do mais novo ao mais antigo, com filtro de período.
public class ListarMovimentosCaixaQueryHandler(IVendasRepository repositorio) : IQueryHandler<ListarMovimentosCaixaQuery>
{
    public async Task<Resultado<object>> HandleAsync(ListarMovimentosCaixaQuery query)
    {
        var (lojaId, de, ate) = query;
        if (lojaId is null) return Resultado.Ok(Array.Empty<object>());

        DateTime? deUtc = de.HasValue ? DateTime.SpecifyKind(de.Value.Date, DateTimeKind.Utc) : null;
        DateTime? ateUtc = ate.HasValue ? DateTime.SpecifyKind(ate.Value.Date, DateTimeKind.Utc).AddDays(1) : null;

        return Resultado.Ok(await repositorio.ListarMovimentosCaixaAsync(lojaId.Value, deUtc, ateUtc));
    }
}
