using LojaApi.Shared.Application;

namespace LojaApi.Application.Financeiro;

// Descrições usadas recentemente nos lançamentos do tipo (para sugerir no formulário).
public class ListarDescricoesRecentesQueryHandler(ILancamentosRepository repositorio) : IQueryHandler<ListarDescricoesRecentesQuery>
{
    public async Task<Resultado<object>> HandleAsync(ListarDescricoesRecentesQuery query)
    {
        if (query.LojaId is not { } lojaId) return Resultado.Ok(Array.Empty<string>());
        return Resultado.Ok(await repositorio.ListarDescricoesRecentesAsync(lojaId, query.Tipo));
    }
}
