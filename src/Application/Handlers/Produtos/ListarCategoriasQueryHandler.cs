using LojaApi.Shared.Application;

namespace LojaApi.Application.Produtos;

// Categorias ativas da loja, por ordem e nome, com a configuração da grade.
public class ListarCategoriasQueryHandler(IProdutosRepository repositorio) : IQueryHandler<ListarCategoriasQuery>
{
    public async Task<Resultado<object>> HandleAsync(ListarCategoriasQuery query) =>
        query.LojaId is null
            ? Resultado.Ok(Array.Empty<object>())
            : Resultado.Ok(await repositorio.ListarCategoriasAsync(query.LojaId.Value));
}
