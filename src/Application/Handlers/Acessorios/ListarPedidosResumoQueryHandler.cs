using LojaApi.Domain.Acessorios;
using LojaApi.Shared.Application;
using LojaApi.src.Models;
using static LojaApi.Application.Acessorios.AcessoriosApoio;

namespace LojaApi.Application.Acessorios;

// Lista os pedidos (superadmin) em resumo, do mais novo para o mais antigo, com filtro opcional de status.
public class ListarPedidosResumoQueryHandler(IAcessoriosRepository repositorio) : IQueryHandler<ListarPedidosResumoQuery>
{
    public async Task<Resultado<object>> HandleAsync(ListarPedidosResumoQuery query) =>
        Resultado.Ok(await repositorio.ListarPedidosResumoAsync(query.Status));
}
