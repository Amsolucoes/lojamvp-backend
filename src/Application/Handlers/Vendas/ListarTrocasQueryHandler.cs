using LojaApi.Shared.Application;

namespace LojaApi.Application.Vendas;

// As 50 trocas mais recentes da loja, com os itens.
public class ListarTrocasQueryHandler(IVendasRepository repositorio) : IQueryHandler<ListarTrocasQuery>
{
    public async Task<Resultado<object>> HandleAsync(ListarTrocasQuery query) =>
        Resultado.Ok(await repositorio.ListarTrocasAsync(query.LojaId));
}
