using LojaApi.Domain.OrdemServico;
using LojaApi.Models;
using LojaApi.Shared.Application;

namespace LojaApi.Application.OrdemServico;

// Desfaz a conclusão: repõe o estoque, remove o lançamento financeiro e as comissões e volta a ordem para em andamento.
public class DesfazerConclusaoOrcamentoCommandHandler(IOrdemServicoRepository repositorio) : ICommandHandler<DesfazerConclusaoOrcamentoCommand>
{
    public async Task<Resultado<object>> HandleAsync(DesfazerConclusaoOrcamentoCommand command)
    {
        var (lojaId, id) = command;
        var orcamento = await repositorio.ObterOrcamentoComItensAsync(id, lojaId);
        if (orcamento is null) return Resultado.NaoEncontrado();

        if (!OrdemServicoRegras.PodeDesfazerConclusao(orcamento.Status))
            return Resultado.Invalido("Só é possível desfazer uma ordem concluída.");

        // Trava de segurança: se o lançamento financeiro já foi pago, ou alguma comissão
        // já foi paga, não desfaz sozinho — o dinheiro pode já ter saído/entrado de verdade.
        LancamentoFinanceiro? lancamento = null;
        if (orcamento.LancamentoFinanceiroId.HasValue)
        {
            lancamento = await repositorio.ObterLancamentoAsync(orcamento.LancamentoFinanceiroId.Value);
            if (lancamento != null && lancamento.Status == "pago")
                return Resultado.Invalido("O lançamento financeiro desta ordem já foi marcado como pago. Reverta o pagamento no Financeiro antes de desfazer a conclusão.");
        }

        var comissoes = await repositorio.ListarComissoesDaOrdemAsync(orcamento.Id);

        if (comissoes.Any(c => c.Status == "pago"))
            return Resultado.Invalido("Uma ou mais comissões geradas por esta ordem já foram pagas. Reverta o pagamento em Comissões antes de desfazer a conclusão.");

        // 1) Repõe o estoque das peças que vieram do estoque
        foreach (var item in orcamento.Itens.Where(i => i.Tipo == OrdemServicoRegras.TipoPeca && i.ProdutoId.HasValue))
        {
            var produto = await repositorio.ObterProdutoAsync(item.ProdutoId!.Value, lojaId);
            if (produto is null) continue;

            produto.Estoque += item.Quantidade;
            produto.AtualizadoEm = DateTime.UtcNow;

            repositorio.Adicionar(new MovimentoEstoque
            {
                ProdutoId = produto.Id,
                Tipo = "entrada",
                Quantidade = item.Quantidade,
                Observacao = $"Estorno — desfeita conclusão da Ordem de Serviço #{orcamento.Id.ToString()[..8]} - {item.Descricao}",
                LojaId = lojaId,
            });
        }

        // 2) Remove o lançamento financeiro (ainda pendente, já validado acima)
        if (orcamento.LancamentoFinanceiroId.HasValue)
        {
            if (lancamento != null) repositorio.Remover(lancamento);
            orcamento.LancamentoFinanceiroId = null;
        }

        // 3) Remove as comissões geradas (ainda pendentes, já validado acima)
        repositorio.RemoverComissoes(comissoes);

        // 4) Volta status — fica em_andamento, pronta pra editar e concluir de novo
        orcamento.Status = OrdemServicoRegras.EmAndamento;
        orcamento.ConcluidoEm = null;

        await repositorio.SalvarAsync();

        return Resultado.Ok(new { orcamento.Id, orcamento.Status, mensagem = "Conclusão desfeita — estoque, financeiro e comissão foram revertidos." });
    }
}
