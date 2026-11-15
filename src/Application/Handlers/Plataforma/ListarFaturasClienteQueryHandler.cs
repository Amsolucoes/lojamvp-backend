using LojaApi.Shared.Application;

namespace LojaApi.Application.Plataforma;

// Todas as faturas da loja do assinante, da mais nova para a mais antiga.
public class ListarFaturasClienteQueryHandler(IPlataformaRepository repositorio) : IQueryHandler<ListarFaturasClienteQuery>
{
    public async Task<Resultado<object>> HandleAsync(ListarFaturasClienteQuery query)
    {
        var loja = await repositorio.ObterMinhaLojaComPagamentosAsync(query.UsuarioId);
        if (loja is null) return Resultado.NaoEncontrado();

        return Resultado.Ok(loja.Pagamentos.OrderByDescending(p => p.Vencimento).Select(PlataformaApoio.ParaDto).ToList());
    }
}
