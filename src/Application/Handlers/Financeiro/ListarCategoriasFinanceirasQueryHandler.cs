using LojaApi.Shared.Application;

namespace LojaApi.Application.Financeiro;

// Categorias ativas da loja por nome; com tipo (pagar/receber), traz também as "ambos". Sem loja: lista vazia.
public class ListarCategoriasFinanceirasQueryHandler(IFinanceiroRepository repositorio) : IQueryHandler<ListarCategoriasFinanceirasQuery>
{
    public async Task<Resultado<object>> HandleAsync(ListarCategoriasFinanceirasQuery query) =>
        query.LojaId is { } lojaId
            ? Resultado.Ok(await repositorio.ListarCategoriasAtivasAsync(lojaId, query.Tipo))
            : Resultado.Ok(Array.Empty<CategoriaFinanceiraDto>());
}
