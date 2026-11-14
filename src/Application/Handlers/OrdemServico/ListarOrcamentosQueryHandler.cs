using LojaApi.Shared.Application;

namespace LojaApi.Application.OrdemServico;

// Orçamentos e ordens da loja, do mais novo ao mais antigo, com filtro de situação e de placa.
public class ListarOrcamentosQueryHandler(IOrdemServicoRepository repositorio) : IQueryHandler<ListarOrcamentosQuery>
{
    public async Task<Resultado<object>> HandleAsync(ListarOrcamentosQuery query)
    {
        var (lojaId, status, placa) = query;
        if (lojaId is null) return Resultado.Ok(Array.Empty<object>());

        var placaBusca = string.IsNullOrWhiteSpace(placa) ? null : placa.Trim().ToUpperInvariant();
        var statusFiltro = string.IsNullOrWhiteSpace(status) ? null : status;

        return Resultado.Ok(await repositorio.ListarOrcamentosAsync(lojaId.Value, statusFiltro, placaBusca));
    }
}
