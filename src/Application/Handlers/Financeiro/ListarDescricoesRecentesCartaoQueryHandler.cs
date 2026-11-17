using LojaApi.Shared.Application;

namespace LojaApi.Application.Financeiro;

// Descrições usadas recentemente nas compras de cartão (para sugerir no formulário).
public class ListarDescricoesRecentesCartaoQueryHandler(ICartoesRepository repositorio) : IQueryHandler<ListarDescricoesRecentesCartaoQuery>
{
    public async Task<Resultado<object>> HandleAsync(ListarDescricoesRecentesCartaoQuery query)
    {
        if (query.LojaId is not { } lojaId) return Resultado.Ok(Array.Empty<string>());
        return Resultado.Ok(await repositorio.ListarDescricoesRecentesAsync(lojaId));
    }
}
