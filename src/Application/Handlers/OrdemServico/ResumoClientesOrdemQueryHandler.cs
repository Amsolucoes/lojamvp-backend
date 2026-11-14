using LojaApi.Shared.Application;

namespace LojaApi.Application.OrdemServico;

// Quantidade e total de ordens (menos as canceladas) por cliente, e se há alguma em andamento — para os cards da lista de Clientes.
public class ResumoClientesOrdemQueryHandler(IOrdemServicoRepository repositorio) : IQueryHandler<ResumoClientesOrdemQuery>
{
    public async Task<Resultado<object>> HandleAsync(ResumoClientesOrdemQuery query) =>
        query.LojaId is null
            ? Resultado.Ok(Array.Empty<object>())
            : Resultado.Ok(await repositorio.ResumoPorClienteAsync(query.LojaId.Value));
}
