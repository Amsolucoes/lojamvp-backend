using LojaApi.Shared.Application;

namespace LojaApi.Application.Plataforma;

// Faturas de uma loja, da mais nova para a mais antiga.
public class ListarPagamentosDaLojaQueryHandler(IAdminRepository repositorio) : IQueryHandler<ListarPagamentosDaLojaQuery>
{
    public async Task<Resultado<object>> HandleAsync(ListarPagamentosDaLojaQuery query) =>
        Resultado.Ok((await repositorio.ListarPagamentosDaLojaAsync(query.Id)).Select(PlataformaApoio.ParaDto).ToList());
}
