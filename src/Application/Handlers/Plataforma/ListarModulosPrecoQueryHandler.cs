using LojaApi.Shared.Application;

namespace LojaApi.Application.Plataforma;

// Preço de cada módulo, por nome (qualquer usuário logado vê).
public class ListarModulosPrecoQueryHandler(IPlataformaRepository repositorio) : IQueryHandler<ListarModulosPrecoQuery>
{
    public async Task<Resultado<object>> HandleAsync(ListarModulosPrecoQuery query) =>
        Resultado.Ok(await repositorio.ListarModulosPrecoAsync());
}
