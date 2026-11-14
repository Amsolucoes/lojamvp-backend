using LojaApi.Shared.Application;

namespace LojaApi.Application.Produtos;

// Variações (tamanho/cor) de um produto da loja, por tamanho e cor.
public class ListarVariacoesQueryHandler(IProdutosRepository repositorio) : IQueryHandler<ListarVariacoesQuery>
{
    public async Task<Resultado<object>> HandleAsync(ListarVariacoesQuery query)
    {
        var (lojaId, produtoId) = query;
        var produto = await repositorio.ObterProdutoAsync(produtoId);
        if (produto is null || (lojaId.HasValue && produto.LojaId != lojaId)) return Resultado.NaoEncontrado();

        return Resultado.Ok(await repositorio.ListarVariacoesAsync(produtoId));
    }
}
