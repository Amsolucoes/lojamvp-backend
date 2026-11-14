using LojaApi.Domain.Produtos;
using LojaApi.Models;
using LojaApi.Shared.Application;
using static LojaApi.Application.Produtos.ProdutosApoio;

namespace LojaApi.Application.Produtos;

// Adiciona uma variação (tamanho/cor) a um produto da loja.
public class AdicionarVariacaoCommandHandler(IProdutosRepository repositorio) : ICommandHandler<AdicionarVariacaoCommand>
{
    public async Task<Resultado<object>> HandleAsync(AdicionarVariacaoCommand command)
    {
        var (lojaId, produtoId, req) = command;
        var produto = await repositorio.ObterProdutoAsync(produtoId);
        if (produto is null || (lojaId.HasValue && produto.LojaId != lojaId)) return Resultado.NaoEncontrado();

        if (!string.IsNullOrWhiteSpace(req.CodigoBarras) && await repositorio.CodigoDeBarrasDeVariacaoEmUsoAsync(req.CodigoBarras, lojaId, null))
            return Resultado.Conflito($"O código de barras '{req.CodigoBarras}' já está cadastrado em outra variação da sua loja.");

        var variacao = new ProdutoVariacao
        {
            ProdutoId = produtoId,
            Tamanho = req.Tamanho,
            Cor = req.Cor,
            OutroCampo = req.OutroCampo,
            CodigoBarras = ProdutosRegras.CodigoDaVariacao(req.CodigoBarras),
            Estoque = req.Estoque,
            EstoqueMinimo = req.EstoqueMinimo,
        };
        repositorio.Adicionar(variacao);
        await repositorio.SalvarAsync();

        return Resultado.Ok(ParaDto(variacao));
    }
}
