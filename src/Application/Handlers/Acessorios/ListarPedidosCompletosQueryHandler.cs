using LojaApi.Domain.Acessorios;
using LojaApi.Shared.Application;
using LojaApi.src.Models;
using static LojaApi.Application.Acessorios.AcessoriosApoio;

namespace LojaApi.Application.Acessorios;

// Lista os pedidos (superadmin) inteiros, com os itens, do mais novo para o mais antigo.
public class ListarPedidosCompletosQueryHandler(IAcessoriosRepository repositorio) : IQueryHandler<ListarPedidosCompletosQuery>
{
    public async Task<Resultado<object>> HandleAsync(ListarPedidosCompletosQuery query) =>
        Resultado.Ok(await repositorio.ListarPedidosComItensAsync(query.Status));
}
