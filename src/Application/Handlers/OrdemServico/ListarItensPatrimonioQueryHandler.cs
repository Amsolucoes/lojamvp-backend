using LojaApi.Shared.Application;

namespace LojaApi.Application.OrdemServico;

// Itens de patrimônio da loja (ferramentas e equipamentos), com o valor total de cada um.
public class ListarItensPatrimonioQueryHandler(IOrdemServicoRepository repositorio) : IQueryHandler<ListarItensPatrimonioQuery>
{
    public async Task<Resultado<object>> HandleAsync(ListarItensPatrimonioQuery query) =>
        query.LojaId is null
            ? Resultado.Ok(Array.Empty<object>())
            : Resultado.Ok(await repositorio.ListarItensPatrimonioAsync(query.LojaId.Value, query.IncluirInativos));
}
