using LojaApi.Shared.Application;

namespace LojaApi.Application.Plataforma;

// Histórico de comunicados enviados, do mais recente.
public class ListarComunicadosQueryHandler(IAdminRepository repositorio) : IQueryHandler<ListarComunicadosQuery>
{
    public async Task<Resultado<object>> HandleAsync(ListarComunicadosQuery query) =>
        Resultado.Ok(await repositorio.ListarComunicadosAsync());
}
