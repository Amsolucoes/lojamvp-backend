using LojaApi.Shared.Application;

namespace LojaApi.Application.Financeiro;

// Cartões de crédito da loja.
public class ListarCartoesQueryHandler(ICartoesRepository repositorio) : IQueryHandler<ListarCartoesQuery>
{
    public async Task<Resultado<object>> HandleAsync(ListarCartoesQuery query)
    {
        if (query.LojaId is not { } lojaId) return Resultado.Ok(Array.Empty<object>());
        return Resultado.Ok(await repositorio.ListarCartoesAsync(lojaId));
    }
}
