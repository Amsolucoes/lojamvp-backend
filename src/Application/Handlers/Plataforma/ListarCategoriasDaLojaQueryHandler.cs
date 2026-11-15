using LojaApi.Shared.Application;

namespace LojaApi.Application.Plataforma;

// Categorias ativas da loja, por ordem (sem loja: lista vazia).
public class ListarCategoriasDaLojaQueryHandler(IPlataformaRepository repositorio) : IQueryHandler<ListarCategoriasDaLojaQuery>
{
    public async Task<Resultado<object>> HandleAsync(ListarCategoriasDaLojaQuery query) =>
        query.LojaId is { } lojaId
            ? Resultado.Ok(await repositorio.ListarCategoriasAtivasAsync(lojaId))
            : Resultado.Ok(Array.Empty<CategoriaLojaDto>());
}
