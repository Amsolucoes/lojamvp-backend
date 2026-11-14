using LojaApi.Domain.OrdemServico;
using LojaApi.Shared.Application;
using LojaApi.src.Models.OrdemServico;

namespace LojaApi.Application.OrdemServico;

// Cria um orçamento (nasce pendente) com itens, mecânicos e respostas do checklist.
public class CriarOrcamentoCommandHandler(IOrdemServicoRepository repositorio) : ICommandHandler<CriarOrcamentoCommand>
{
    public async Task<Resultado<object>> HandleAsync(CriarOrcamentoCommand command)
    {
        var (lojaId, req) = command;
        if (lojaId is null) return Resultado.Invalido("Loja não encontrada.");

        if (req.Itens is null || req.Itens.Count == 0)
            return Resultado.Invalido("O orçamento precisa ter ao menos um item.");

        var cliente = await repositorio.ObterClienteAsync(req.ClienteId, lojaId);
        if (cliente is null) return Resultado.Invalido("Cliente não encontrado.");

        var orcamento = new OrcamentoServico
        {
            LojaId = lojaId.Value,
            ClienteId = req.ClienteId,
            VeiculoDescricao = req.VeiculoDescricao,
            Placa = OrdemServicoRegras.NormalizarPlaca(req.Placa),
            Observacoes = req.Observacoes,
            Status = OrdemServicoRegras.Pendente,
        };

        decimal valorTotal = 0;
        foreach (var i in req.Itens)
        {
            if (!OrdemServicoRegras.TipoDeItemValido(i.Tipo))
                return Resultado.Invalido("Tipo de item inválido — use 'peca' ou 'servico'.");

            var subtotal = OrdemServicoRegras.SubtotalDoItem(i.Quantidade, i.ValorUnitario);
            valorTotal += subtotal;

            orcamento.Itens.Add(new ItemOrcamentoServico
            {
                LojaId = lojaId.Value,
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

                orcamento.Mecanicos.Add(new MecanicoOrcamento
                {
                    LojaId = lojaId.Value,
                    ProfissionalId = profissional.Id,
                    ComissaoPercentual = OrdemServicoRegras.PercentualDoMecanico(m.ComissaoPercentual, profissional.ComissaoPadraoPercentual),
                });
            }
        }

        var erroReferencia = await OrdemServicoApoio.ValidarReferenciasAsync(repositorio, lojaId.Value, req);
        if (erroReferencia != null) return Resultado.Invalido(erroReferencia);

        if (req.ChecklistRespostas != null)
        {
            foreach (var r in req.ChecklistRespostas)
            {
                orcamento.ChecklistRespostas.Add(new ChecklistRespostaItem
                {
                    LojaId = lojaId.Value,
                    ChecklistItemId = r.ChecklistItemId,
                    Estado = r.Estado,
                    Observacao = r.Observacao,
                });
            }
        }

        repositorio.Adicionar(orcamento);
        await repositorio.SalvarAsync();

        return Resultado.Ok(new { orcamento.Id, orcamento.Status, orcamento.ValorTotal });
    }
}
