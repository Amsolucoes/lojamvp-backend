using LojaApi.Shared.Application;

namespace LojaApi.Application.OrdemServico;

// Contagens de patrimônio da loja, da mais nova para a mais antiga, com a quantidade de itens e de divergências.
public class ListarContagensPatrimonioQueryHandler(IOrdemServicoRepository repositorio) : IQueryHandler<ListarContagensPatrimonioQuery>
{
    public async Task<Resultado<object>> HandleAsync(ListarContagensPatrimonioQuery query) =>
        query.LojaId is null
            ? Resultado.Ok(Array.Empty<object>())
            : Resultado.Ok(await repositorio.ListarContagensAsync(query.LojaId.Value));
}
