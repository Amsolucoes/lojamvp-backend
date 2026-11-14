using LojaApi.Domain.OrdemServico;
using LojaApi.Shared.Application;
using LojaApi.src.Models.OrdemServico;

namespace LojaApi.Application.OrdemServico;

// Edita um orçamento ainda pendente ou em andamento, recriando itens, mecânicos e checklist.
public class AtualizarOrcamentoCommandHandler(IOrdemServicoRepository repositorio) : ICommandHandler<AtualizarOrcamentoCommand>
{
    public async Task<Resultado<object>> HandleAsync(AtualizarOrcamentoCommand command)
    {
        var (lojaId, id, req) = command;
        var orcamento = await repositorio.ObterOrcamentoParaEdicaoAsync(id, lojaId);
        if (orcamento is null) return Resultado.NaoEncontrado();

        if (!OrdemServicoRegras.PodeEditar(orcamento.Status))
            return Resultado.Invalido("Só é possível editar uma ordem pendente ou em andamento. Ordens concluídas já geraram financeiro, comissão e baixa de estoque.");

        if (req.Itens is null || req.Itens.Count == 0)
            return Resultado.Invalido("O orçamento precisa ter ao menos um item.");

        var cliente = await repositorio.ObterClienteAsync(req.ClienteId, lojaId);
        if (cliente is null) return Resultado.Invalido("Cliente não encontrado.");

        orcamento.ClienteId = req.ClienteId;
        orcamento.VeiculoDescricao = req.VeiculoDescricao;
        orcamento.Placa = OrdemServicoRegras.NormalizarPlaca(req.Placa);
        orcamento.Observacoes = req.Observacoes;

        // Remove itens/mecânicos/checklist antigos e recria do zero — seguro aqui
        // porque a ordem ainda não gerou nenhum efeito colateral (estoque/financeiro/comissão)
        repositorio.RemoverItens(orcamento.Itens);
        repositorio.RemoverMecanicos(orcamento.Mecanicos);
        repositorio.RemoverRespostas(orcamento.ChecklistRespostas);

        decimal valorTotal = 0;
        foreach (var i in req.Itens)
        {
            if (!OrdemServicoRegras.TipoDeItemValido(i.Tipo))
                return Resultado.Invalido("Tipo de item inválido — use 'peca' ou 'servico'.");

            var subtotal = OrdemServicoRegras.SubtotalDoItem(i.Quantidade, i.ValorUnitario);
            valorTotal += subtotal;

            repositorio.Adicionar(new ItemOrcamentoServico
            {
                LojaId = lojaId!.Value,
                OrcamentoId = orcamento.Id,
                Tipo = i.Tipo,
                ProdutoId = i.ProdutoId,
                ServicoId = i.ServicoId,
                Descricao = i.Descricao.Trim(),
                Quantidade = i.Quantidade,
                ValorUnitario = i.ValorUnitario,
                ValorTotal = subtotal,
            });
        }
        orcamento.ValorTotal = valorTotal;

        if (req.Mecanicos != null)
        {
            foreach (var m in req.Mecanicos)
            {
                var profissional = await repositorio.ObterMecanicoAsync(m.ProfissionalId, lojaId);
                if (profissional is null) return Resultado.Invalido("Mecânico/profissional não encontrado.");

                repositorio.Adicionar(new MecanicoOrcamento
                {
                    LojaId = lojaId!.Value,
                    OrcamentoId = orcamento.Id,
                    ProfissionalId = profissional.Id,
                    ComissaoPercentual = OrdemServicoRegras.PercentualDoMecanico(m.ComissaoPercentual, profissional.ComissaoPadraoPercentual),
                });
            }
        }

        var erroReferencia = await OrdemServicoApoio.ValidarReferenciasAsync(repositorio, lojaId!.Value, req);
        if (erroReferencia != null) return Resultado.Invalido(erroReferencia);

        if (req.ChecklistRespostas != null)
        {
            foreach (var r in req.ChecklistRespostas)
            {
                repositorio.Adicionar(new ChecklistRespostaItem
                {
                    LojaId = lojaId.Value,
                    OrcamentoId = orcamento.Id,
                    ChecklistItemId = r.ChecklistItemId,
                    Estado = r.Estado,
                    Observacao = r.Observacao,
                });
            }
        }

        await repositorio.SalvarAsync();

        return Resultado.Ok(new { orcamento.Id, orcamento.Status, orcamento.ValorTotal });
    }
}
