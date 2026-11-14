using LojaApi.Domain.OrdemServico;

namespace LojaApi.Application.OrdemServico;

// O que criar e atualizar orçamento compartilham.
public static class OrdemServicoApoio
{
    // Produto, serviço e item de checklist citados na ordem não podem ser de outra loja.
    // (Um id que não existe em lugar nenhum continua como antes: não é barrado aqui.)
    public static async Task<string?> ValidarReferenciasAsync(IOrdemServicoRepository repositorio, Guid lojaId, CriarOrcamentoRequest req)
    {
        foreach (var item in req.Itens)
        {
            if (item.ProdutoId.HasValue)
            {
                var produto = await repositorio.ObterProdutoAsync(item.ProdutoId.Value, null);
                if (produto != null && produto.LojaId != lojaId) return $"Produto do item '{item.Descricao.Trim()}' não encontrado.";
            }

            if (item.ServicoId.HasValue)
            {
                var servico = await repositorio.ObterServicoAsync(item.ServicoId.Value, null);
                if (servico != null && servico.LojaId != lojaId) return $"Serviço do item '{item.Descricao.Trim()}' não encontrado.";
            }
        }

        if (req.ChecklistRespostas != null)
        {
            foreach (var r in req.ChecklistRespostas)
            {
                var itemChecklist = await repositorio.ObterItemChecklistDeQualquerLojaAsync(r.ChecklistItemId);
                if (itemChecklist != null && itemChecklist.LojaId != lojaId) return "Item de checklist não encontrado.";
            }
        }

        return null;
    }
}
