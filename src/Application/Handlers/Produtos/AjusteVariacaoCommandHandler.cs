using LojaApi.Domain.Produtos;
using LojaApi.Models;
using LojaApi.Shared.Application;

namespace LojaApi.Application.Produtos;

// Entrada, saída ou ajuste do estoque de uma variação (tamanho/cor), com o movimento no histórico.
public class AjusteVariacaoCommandHandler(IProdutosRepository repositorio) : ICommandHandler<AjusteVariacaoCommand>
{
    public async Task<Resultado<object>> HandleAsync(AjusteVariacaoCommand command)
    {
        var (lojaId, req) = command;
        var variacao = await repositorio.ObterVariacaoDoProdutoAsync(req.ProdutoId, req.VariacaoId, lojaId);
        if (variacao is null) return Resultado.NaoEncontrado("Variação não encontrada.");

        var erroTipo = ProdutosRegras.ValidarMovimento(req.Tipo, req.Quantidade);
        if (erroTipo is not null) return Resultado.Invalido(erroTipo);

        if (req.Tipo == ProdutosRegras.MovimentoSaida && req.Quantidade > variacao.Estoque)
            return Resultado.Invalido($"A quantidade da saída ({req.Quantidade}) é maior que o estoque atual desta variação ({variacao.Estoque}).");

        variacao.Estoque = ProdutosRegras.NovoEstoque(req.Tipo, variacao.Estoque, req.Quantidade);
        variacao.AtualizadoEm = DateTime.UtcNow;

        repositorio.Adicionar(new MovimentoEstoque
        {
            ProdutoId = req.ProdutoId,
            Tipo = req.Tipo,
            Quantidade = req.Quantidade,
            Observacao = ProdutosRegras.ObservacaoDoAjusteDeVariacao(req.Tipo, req.Observacao, ProdutosRegras.RotuloDaVariacao(variacao.Tamanho, variacao.Cor)),
            LojaId = lojaId,
        });

        await repositorio.SalvarAsync();
        return Resultado.Ok(new { mensagem = "Estoque atualizado.", estoqueAgora = variacao.Estoque });
    }
}
