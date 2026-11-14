using LojaApi.Shared.Application;
using static LojaApi.Application.Produtos.ProdutosApoio;

namespace LojaApi.Application.Produtos;

// Detalhe de um fornecedor da loja.
public class BuscarFornecedorQueryHandler(IProdutosRepository repositorio) : IQueryHandler<BuscarFornecedorQuery>
{
    public async Task<Resultado<object>> HandleAsync(BuscarFornecedorQuery query)
    {
        var (lojaId, id) = query;
        var f = await repositorio.ObterFornecedorComProdutosAsync(id);
        if (f is null || (lojaId.HasValue && f.LojaId != lojaId)) return Resultado.NaoEncontrado();

        return Resultado.Ok(ParaDto(f, f.Produtos.Count(p => p.Ativo)));
    }
}
