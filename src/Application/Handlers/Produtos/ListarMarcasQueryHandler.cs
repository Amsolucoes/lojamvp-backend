using LojaApi.Shared.Application;

namespace LojaApi.Application.Produtos;

// Marcas ativas da loja, por nome.
public class ListarMarcasQueryHandler(IProdutosRepository repositorio) : IQueryHandler<ListarMarcasQuery>
{
    public async Task<Resultado<object>> HandleAsync(ListarMarcasQuery query) =>
        query.LojaId is null
            ? Resultado.Ok(Array.Empty<object>())
            : Resultado.Ok(await repositorio.ListarMarcasAsync(query.LojaId.Value));
}
