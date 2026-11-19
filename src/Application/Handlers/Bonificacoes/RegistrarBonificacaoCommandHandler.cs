using LojaApi.Application.Produtos;
using LojaApi.Domain.Produtos;
using LojaApi.Domain.Bonificacoes;
using LojaApi.Models;
using LojaApi.Shared.Application;

namespace LojaApi.Application.Bonificacoes;

// Dá um brinde avulso a um cliente: registra no cliente, baixa o estoque (do produto ou da variação) e grava o movimento de estoque com a
// observação "Bonificação - <cliente>", tudo na mesma gravação. O custo do produto fica guardado para o relatório.
public class RegistrarBonificacaoCommandHandler(IBonificacoesRepository bonificacoes, IProdutosRepository produtos) : ICommandHandler<RegistrarBonificacaoCommand>
{
    public async Task<Resultado<object>> HandleAsync(RegistrarBonificacaoCommand command)
    {
        var (lojaIdOuNulo, usuarioId, req) = command;
        if (lojaIdOuNulo is not { } lojaId) return Resultado.Invalido("Loja não encontrada.");

        var nomeCliente = await bonificacoes.ObterNomeDoClienteAsync(req.ClienteId, lojaId);
        if (nomeCliente is null) return Resultado.Invalido("Cliente não encontrado.");

        var produto = await produtos.ObterProdutoAsync(req.ProdutoId);
        if (produto is null || produto.LojaId != lojaId) return Resultado.NaoEncontrado("Produto não encontrado.");
        if (!produto.Ativo) return Resultado.Invalido($"Produto '{produto.Nome}' está inativo.");

        var erroQuantidade = BonificacoesRegras.ValidarQuantidade(req.Quantidade, req.VariacaoId.HasValue);
        if (erroQuantidade is not null) return Resultado.Invalido(erroQuantidade);

        var nomeProduto = produto.Nome;
        if (req.VariacaoId is { } variacaoId)
        {
            var variacao = await produtos.ObterVariacaoDoProdutoAsync(req.ProdutoId, variacaoId, lojaId);
            if (variacao is null) return Resultado.NaoEncontrado("Variação não encontrada.");
            if (req.Quantidade > variacao.Estoque)
                return Resultado.Invalido($"A quantidade ({req.Quantidade}) é maior que o estoque atual desta variação ({variacao.Estoque}).");

            nomeProduto = BonificacoesRegras.NomeDoProduto(produto.Nome, variacao.Tamanho, variacao.Cor);
            variacao.Estoque -= (int)req.Quantidade;
            variacao.AtualizadoEm = DateTime.UtcNow;
        }
        else
        {
            if (req.Quantidade > produto.Estoque)
                return Resultado.Invalido($"A quantidade ({req.Quantidade}) é maior que o estoque atual ({ProdutosRegras.FormatarEstoque(produto.Estoque)}).");

            produto.Estoque -= req.Quantidade;
            produto.AtualizadoEm = DateTime.UtcNow;
        }

        var observacao = BonificacoesRegras.LimparObservacao(req.Observacao);
        var bonificacao = new Bonificacao
        {
            LojaId = lojaId, ClienteId = req.ClienteId, NomeCliente = nomeCliente,
            ProdutoId = produto.Id, VariacaoId = req.VariacaoId, NomeProduto = nomeProduto,
            Quantidade = req.Quantidade, CustoUnitario = produto.PrecoCusto,
            Observacao = observacao, UsuarioId = usuarioId,
        };
        bonificacoes.Adicionar(bonificacao);
        produtos.Adicionar(new MovimentoEstoque
        {
            ProdutoId = produto.Id, Tipo = ProdutosRegras.MovimentoSaida, Quantidade = req.Quantidade,
            Observacao = BonificacoesRegras.ObservacaoDoMovimento(nomeCliente, observacao), LojaId = lojaId,
        });

        await bonificacoes.SalvarAsync();
        return Resultado.Ok(BonificacoesApoio.ParaDto(bonificacao));
    }
}
