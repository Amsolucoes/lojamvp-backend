using LojaApi.Shared.Application;

namespace LojaApi.Application.Plataforma;

// Uma loja com faturas e usuários, para o painel do superadmin.
public class BuscarLojaAdminQueryHandler(IAdminRepository repositorio) : IQueryHandler<BuscarLojaAdminQuery>
{
    public async Task<Resultado<object>> HandleAsync(BuscarLojaAdminQuery query)
    {
        var loja = await repositorio.ObterLojaCompletaAsync(query.Id);
        return loja is null ? Resultado.NaoEncontrado() : Resultado.Ok(PlataformaApoio.ParaLojaDto(loja, DateTime.UtcNow));
    }
}
