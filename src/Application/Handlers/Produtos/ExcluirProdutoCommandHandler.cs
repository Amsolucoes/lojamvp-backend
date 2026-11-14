using LojaApi.Shared.Application;

namespace LojaApi.Application.Produtos;

// Exclui um produto (com variações e movimentos), a menos que já tenha sido vendido: aí só dá para inativar.
public class ExcluirProdutoCommandHandler(IProdutosRepository repositorio) : ICommandHandler<ExcluirProdutoCommand>
{
    public async Task<Resultado<object>> HandleAsync(ExcluirProdutoCommand command)
    {
        var (lojaId, id) = command;
        var produto = await repositorio.ObterProdutoAsync(id);
        if (produto is null || (lojaId.HasValue && produto.LojaId != lojaId)) return Resultado.NaoEncontrado();

        if (await repositorio.ProdutoTemVendasAsync(id))
            return Resultado.Invalido("Não é possível excluir: este produto já tem vendas registradas. Você pode apenas inativá-lo.");

        await repositorio.ExcluirProdutoComDependenciasAsync(id);
        return Resultado.Ok(new { mensagem = "Produto excluído com sucesso." });
    }
}
