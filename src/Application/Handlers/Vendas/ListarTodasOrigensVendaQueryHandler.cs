using LojaApi.Shared.Application;

namespace LojaApi.Application.Vendas;

// Todas as origens de venda da loja, inclusive as desativadas (tela de gestão).
public class ListarTodasOrigensVendaQueryHandler(IVendasRepository repositorio) : IQueryHandler<ListarTodasOrigensVendaQuery>
{
    public async Task<Resultado<object>> HandleAsync(ListarTodasOrigensVendaQuery query) =>
        query.LojaId is null
            ? Resultado.Ok(Array.Empty<object>())
            : Resultado.Ok(await repositorio.ListarOrigensAsync(query.LojaId.Value));
}
