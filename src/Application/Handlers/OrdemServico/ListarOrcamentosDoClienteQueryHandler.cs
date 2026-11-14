using LojaApi.Shared.Application;

namespace LojaApi.Application.OrdemServico;

// Histórico de ordens de um cliente (usado na tela de Clientes).
public class ListarOrcamentosDoClienteQueryHandler(IOrdemServicoRepository repositorio) : IQueryHandler<ListarOrcamentosDoClienteQuery>
{
    public async Task<Resultado<object>> HandleAsync(ListarOrcamentosDoClienteQuery query) =>
        query.LojaId is null
            ? Resultado.Ok(Array.Empty<object>())
            : Resultado.Ok(await repositorio.ListarOrcamentosDoClienteAsync(query.LojaId.Value, query.ClienteId));
}
