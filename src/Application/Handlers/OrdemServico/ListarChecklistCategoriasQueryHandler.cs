using LojaApi.Shared.Application;

namespace LojaApi.Application.OrdemServico;

// Categorias do checklist da oficina, por ordem, cada uma com os seus itens.
public class ListarChecklistCategoriasQueryHandler(IOrdemServicoRepository repositorio) : IQueryHandler<ListarChecklistCategoriasQuery>
{
    public async Task<Resultado<object>> HandleAsync(ListarChecklistCategoriasQuery query) =>
        query.LojaId is null
            ? Resultado.Ok(Array.Empty<object>())
            : Resultado.Ok(await repositorio.ListarChecklistAsync(query.LojaId.Value));
}
