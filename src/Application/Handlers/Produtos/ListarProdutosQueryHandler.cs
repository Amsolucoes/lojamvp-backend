using LojaApi.Shared.Application;
using static LojaApi.Application.Produtos.ProdutosApoio;

namespace LojaApi.Application.Produtos;

// Produtos da loja por nome, com filtros de busca (nome ou código), categoria, situação e marca.
public class ListarProdutosQueryHandler(IProdutosRepository repositorio) : IQueryHandler<ListarProdutosQuery>
{
    public async Task<Resultado<object>> HandleAsync(ListarProdutosQuery query)
    {
        var (lojaId, busca, categoria, ativo, marcaId) = query;
        var produtos = await repositorio.ListarProdutosAsync(lojaId, busca, categoria, ativo, marcaId);
        return Resultado.Ok(produtos.Select(ParaDto).ToList());
    }
}
