using LojaApi.Domain.Produtos;
using LojaApi.Models;
using LojaApi.Shared.Application;

namespace LojaApi.Application.Produtos;

// Entrada, saída ou ajuste do estoque de um produto, com o movimento no histórico.
public class AjusteEstoqueCommandHandler(IProdutosRepository repositorio) : ICommandHandler<AjusteEstoqueCommand>
{
    public async Task<Resultado<object>> HandleAsync(AjusteEstoqueCommand command)
    {
        var (lojaId, req) = command;
        var produto = await repositorio.ObterProdutoAsync(req.ProdutoId);
        if (produto is null || (lojaId.HasValue && produto.LojaId != lojaId))
            return Resultado.NaoEncontrado("Produto não encontrado.");

        var erroTipo = ProdutosRegras.ValidarMovimento(req.Tipo, req.Quantidade);
        if (erroTipo is not null) return Resultado.Invalido(erroTipo);

        if (req.Tipo == ProdutosRegras.MovimentoSaida && req.Quantidade > produto.Estoque)
            return Resultado.Invalido($"A quantidade da saída ({req.Quantidade}) é maior que o estoque atual ({produto.Estoque}).");

        produto.Estoque = ProdutosRegras.NovoEstoque(req.Tipo, produto.Estoque, req.Quantidade);
        produto.AtualizadoEm = DateTime.UtcNow;

        repositorio.Adicionar(new MovimentoEstoque
        {
            ProdutoId = req.ProdutoId,
            Tipo = req.Tipo,
            Quantidade = req.Quantidade,
            Observacao = req.Observacao,
            LojaId = lojaId,
        });

        await repositorio.SalvarAsync();

        return Resultado.Ok(new
        {
            produtoId = produto.Id,
            nome = produto.Nome,
            estoqueAgora = produto.Estoque,
        });
    }
}
