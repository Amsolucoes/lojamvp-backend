using LojaApi.Domain.Produtos;
using LojaApi.Models;
using LojaApi.Shared.Application;
using static LojaApi.Application.Produtos.ProdutosApoio;

namespace LojaApi.Application.Produtos;

// Atualiza uma variação; se o estoque mudou por aqui, registra o ajuste no histórico.
public class AtualizarVariacaoCommandHandler(IProdutosRepository repositorio) : ICommandHandler<AtualizarVariacaoCommand>
{
    public async Task<Resultado<object>> HandleAsync(AtualizarVariacaoCommand command)
    {
        var (lojaId, produtoId, variacaoId, req) = command;
        var variacao = await repositorio.ObterVariacaoDoProdutoAsync(produtoId, variacaoId, lojaId);
        if (variacao is null) return Resultado.NaoEncontrado();

        if (!string.IsNullOrWhiteSpace(req.CodigoBarras) && req.CodigoBarras != variacao.CodigoBarras
            && await repositorio.CodigoDeBarrasDeVariacaoEmUsoAsync(req.CodigoBarras, lojaId, variacaoId))
            return Resultado.Conflito($"O código de barras '{req.CodigoBarras}' já está cadastrado em outra variação da sua loja.");

        var estoqueAnterior = variacao.Estoque;

        variacao.Tamanho = req.Tamanho;
        variacao.Cor = req.Cor;
        variacao.OutroCampo = req.OutroCampo;
        variacao.CodigoBarras = ProdutosRegras.CodigoDaVariacao(req.CodigoBarras);
        variacao.Estoque = req.Estoque;
        variacao.EstoqueMinimo = req.EstoqueMinimo;
        variacao.AtualizadoEm = DateTime.UtcNow;

        // Estoque mudou editando o produto direto (fora da tela Estoque) — registra o
        // movimento mesmo assim, senão a diferença fica invisível no histórico.
        if (req.Estoque != estoqueAnterior)
        {
            repositorio.Adicionar(new MovimentoEstoque
            {
                ProdutoId = produtoId,
                Tipo = "ajuste",
                Quantidade = req.Estoque,
                Observacao = ProdutosRegras.ObservacaoDaEdicaoDeVariacao(ProdutosRegras.RotuloDaVariacao(req.Tamanho, req.Cor)),
                LojaId = lojaId,
            });
        }

        await repositorio.SalvarAsync();
        return Resultado.Ok(ParaDto(variacao));
    }
}
