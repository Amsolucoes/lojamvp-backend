using LojaApi.Shared.Application;

namespace LojaApi.Application.Plataforma;

// Campos extras ativos da loja, por ordem (sem loja: lista vazia).
public class ListarCamposExtrasDaLojaQueryHandler(IPlataformaRepository repositorio) : IQueryHandler<ListarCamposExtrasDaLojaQuery>
{
    public async Task<Resultado<object>> HandleAsync(ListarCamposExtrasDaLojaQuery query) =>
        query.LojaId is { } lojaId
            ? Resultado.Ok(await repositorio.ListarCamposExtrasAtivosAsync(lojaId))
            : Resultado.Ok(Array.Empty<CampoExtraLojaDto>());
}
