using LojaApi.Shared.Application;

namespace LojaApi.Application.Produtos;

// Histórico de movimentos de estoque da loja, do mais novo ao mais antigo, com filtros.
public class ListarMovimentosEstoqueQueryHandler(IProdutosRepository repositorio) : IQueryHandler<ListarMovimentosEstoqueQuery>
{
    public async Task<Resultado<object>> HandleAsync(ListarMovimentosEstoqueQuery query)
    {
        var (lojaId, produtoId, tipo, de, ate) = query;
        return Resultado.Ok(await repositorio.ListarMovimentosAsync(lojaId, produtoId, tipo, de, ate));
    }
}
