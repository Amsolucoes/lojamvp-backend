using LojaApi.Shared.Application;
using static LojaApi.Application.Produtos.ProdutosApoio;

namespace LojaApi.Application.Produtos;

// Detalhe de um produto da loja, com variações, marca e fornecedor.
public class BuscarProdutoQueryHandler(IProdutosRepository repositorio) : IQueryHandler<BuscarProdutoQuery>
{
    public async Task<Resultado<object>> HandleAsync(BuscarProdutoQuery query)
    {
        var (lojaId, id) = query;
        var p = await repositorio.ObterProdutoCompletoAsync(id);
        if (p is null || (lojaId.HasValue && p.LojaId != lojaId)) return Resultado.NaoEncontrado();
        return Resultado.Ok(ParaDto(p));
    }
}
