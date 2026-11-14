using LojaApi.Shared.Application;
using static LojaApi.Application.Vendas.VendasApoio;

namespace LojaApi.Application.Vendas;

// Detalhe de uma venda da loja, com cliente e itens.
public class BuscarVendaQueryHandler(IVendasRepository repositorio) : IQueryHandler<BuscarVendaQuery>
{
    public async Task<Resultado<object>> HandleAsync(BuscarVendaQuery query)
    {
        var (lojaId, id) = query;
        var v = await repositorio.ObterVendaCompletaAsync(id);

        if (v is null || (lojaId.HasValue && v.LojaId != lojaId)) return Resultado.NaoEncontrado();
        return Resultado.Ok(ParaDto(v));
    }
}
