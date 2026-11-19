using LojaApi.Application.Produtos;
using LojaApi.Domain.Bonificacoes;
using LojaApi.Domain.Produtos;
using LojaApi.Models;
using LojaApi.Shared.Application;

namespace LojaApi.Application.Bonificacoes;

// Desfaz um brinde avulso (lançado por engano): o estoque volta (com um movimento de entrada) e a bonificação fica marcada como estornada.
// Brinde dado dentro de uma venda não se estorna aqui: some junto com a venda, ao excluí-la.
public class EstornarBonificacaoCommandHandler(IBonificacoesRepository bonificacoes, IProdutosRepository produtos) : ICommandHandler<EstornarBonificacaoCommand>
{
    public async Task<Resultado<object>> HandleAsync(EstornarBonificacaoCommand command)
    {
        var (lojaIdOuNulo, id) = command;
        if (lojaIdOuNulo is not { } lojaId) return Resultado.NaoEncontrado();

        var bonificacao = await bonificacoes.ObterAsync(id, lojaId);
        if (bonificacao is null) return Resultado.NaoEncontrado();

        var motivo = BonificacoesRegras.MotivoParaNaoEstornar(bonificacao.Estornada(), bonificacao.VendaId);
        if (motivo is not null) return Resultado.Invalido(motivo);

        var produto = bonificacao.ProdutoId is { } produtoId ? await produtos.ObterProdutoAsync(produtoId) : null;
        if (produto is null || produto.LojaId != lojaId)
            return Resultado.Invalido("O produto desta bonificação foi excluído; não dá para devolver o estoque.");

        // Volta para a variação (se ela ainda existe) ou, senão, para o estoque do produto
        var variacao = bonificacao.VariacaoId is { } variacaoId ? await produtos.ObterVariacaoDoProdutoAsync(produto.Id, variacaoId, lojaId) : null;
        if (variacao is not null)
        {
            variacao.Estoque += (int)bonificacao.Quantidade;
            variacao.AtualizadoEm = DateTime.UtcNow;
        }
        else
        {
            produto.Estoque += bonificacao.Quantidade;
            produto.AtualizadoEm = DateTime.UtcNow;
        }

        produtos.Adicionar(new MovimentoEstoque
        {
            ProdutoId = produto.Id, Tipo = ProdutosRegras.MovimentoEntrada, Quantidade = bonificacao.Quantidade,
            Observacao = BonificacoesRegras.ObservacaoDoEstorno(bonificacao.NomeCliente, bonificacao.NomeProduto), LojaId = lojaId,
        });
        bonificacao.EstornadaEm = DateTime.UtcNow;

        await bonificacoes.SalvarAsync();
        return Resultado.Ok(BonificacoesApoio.ParaDto(bonificacao));
    }
}
