using LojaApi.Shared.Application;
using static LojaApi.Application.Vendas.VendasApoio;

namespace LojaApi.Application.Vendas;

// Lista as vendas da loja, da mais nova para a mais antiga, com filtro de período e de cliente.
public class ListarVendasQueryHandler(IVendasRepository repositorio) : IQueryHandler<ListarVendasQuery>
{
    public async Task<Resultado<object>> HandleAsync(ListarVendasQuery query)
    {
        var (lojaId, de, ate, clienteId) = query;
        var vendas = await repositorio.ListarVendasAsync(lojaId, de, ate, clienteId);
        return Resultado.Ok(vendas.Select(ParaDto).ToList());
    }
}
