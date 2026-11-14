using LojaApi.Shared.Application;

namespace LojaApi.Application.Vendas;

// Origens de venda ativas da loja (para escolher na venda).
public class ListarOrigensVendaQueryHandler(IVendasRepository repositorio) : IQueryHandler<ListarOrigensVendaQuery>
{
    public async Task<Resultado<object>> HandleAsync(ListarOrigensVendaQuery query) =>
        query.LojaId is null
            ? Resultado.Ok(Array.Empty<object>())
            : Resultado.Ok(await repositorio.ListarOrigensAtivasAsync(query.LojaId.Value));
}
