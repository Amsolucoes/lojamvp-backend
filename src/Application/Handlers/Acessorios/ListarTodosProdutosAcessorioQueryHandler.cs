using LojaApi.Domain.Acessorios;
using LojaApi.Shared.Application;
using LojaApi.src.Models;
using static LojaApi.Application.Acessorios.AcessoriosApoio;

namespace LojaApi.Application.Acessorios;

// Todos os produtos (superadmin), inclusive inativos, por ordem e nome.
public class ListarTodosProdutosAcessorioQueryHandler(IAcessoriosRepository repositorio) : IQueryHandler<ListarTodosProdutosAcessorioQuery>
{
    public async Task<Resultado<object>> HandleAsync(ListarTodosProdutosAcessorioQuery query) =>
        Resultado.Ok(await repositorio.ListarProdutosAsync());
}
