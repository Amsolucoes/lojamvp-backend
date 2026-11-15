using LojaApi.Shared.Application;

namespace LojaApi.Application.NfImportacao;

// Histórico das notas da loja (importadas por XML e lançadas à mão), da mais nova para a mais antiga.
public class HistoricoNfImportacaoQueryHandler(INfImportacaoRepository repositorio) : IQueryHandler<HistoricoNfImportacaoQuery>
{
    public async Task<Resultado<object>> HandleAsync(HistoricoNfImportacaoQuery query) =>
        query.LojaId is { } lojaId
            ? Resultado.Ok(await repositorio.ListarHistoricoAsync(lojaId))
            : Resultado.Ok(Array.Empty<NfHistoricoDto>());
}
