using LojaApi.Domain.Acessorios;
using LojaApi.Shared.Application;
using LojaApi.src.Models;
using static LojaApi.Application.Acessorios.AcessoriosApoio;

namespace LojaApi.Application.Acessorios;

// Categorias ativas, por ordem e nome.
public class ListarCategoriasAcessorioQueryHandler(IAcessoriosRepository repositorio) : IQueryHandler<ListarCategoriasAcessorioQuery>
{
    public async Task<Resultado<object>> HandleAsync(ListarCategoriasAcessorioQuery query) =>
        Resultado.Ok(await repositorio.ListarCategoriasAtivasAsync());
}
