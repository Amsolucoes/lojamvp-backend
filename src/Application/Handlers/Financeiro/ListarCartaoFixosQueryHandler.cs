using LojaApi.Shared.Application;

namespace LojaApi.Application.Financeiro;

// Compras fixas de um cartão da loja.
public class ListarCartaoFixosQueryHandler(ICartoesRepository repositorio) : IQueryHandler<ListarCartaoFixosQuery>
{
    public async Task<Resultado<object>> HandleAsync(ListarCartaoFixosQuery query)
    {
        var (lojaIdOuNulo, cartaoId) = query;
        if (lojaIdOuNulo is not { } lojaId) return Resultado.Ok(Array.Empty<object>());
        return Resultado.Ok(await repositorio.ListarFixosDoCartaoAsync(cartaoId, lojaId));
    }
}
