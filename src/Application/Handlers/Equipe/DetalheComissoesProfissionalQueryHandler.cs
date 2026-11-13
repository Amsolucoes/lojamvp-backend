using LojaApi.Domain.Equipe;
using LojaApi.Models;
using LojaApi.Shared.Application;
using LojaApi.src.Models.Funcionarios;

namespace LojaApi.Application.Equipe;

// Detalhe das comissões de um profissional, com a origem (agendamento, venda ou ordem de serviço).
public class DetalheComissoesProfissionalQueryHandler(IEquipeRepository repositorio) : IQueryHandler<DetalheComissoesProfissionalQuery>
{
    public async Task<Resultado<object>> HandleAsync(DetalheComissoesProfissionalQuery query)
    {
        var (lojaId, profissionalId, status) = query;
        if (lojaId is null) return Resultado.Ok(Array.Empty<object>());

        var comissoes = await repositorio.ListarComissoesDoProfissionalAsync(lojaId.Value, profissionalId, status);

        // Comissões de Ordem de Serviço e de Venda (Caixa) não têm Agendamento: nome, cliente e itens vêm da origem, buscados em lote
        var orcamentoIds = comissoes.Where(c => c.OrigemTipo == OrigemComissao.OrdemServico).Select(c => c.OrigemId).Distinct().ToList();
        var vendaIds = comissoes.Where(c => c.OrigemTipo == OrigemComissao.Venda).Select(c => c.OrigemId).Distinct().ToList();
        var origens = await repositorio.ObterOrigensDasComissoesAsync(orcamentoIds, vendaIds);

        var lista = comissoes.Select(c => c.OrigemTipo switch
        {
            OrigemComissao.OrdemServico => DeOrdemDeServico(c, origens),
            OrigemComissao.Venda => DeVenda(c, origens),
            _ => DeAgendamento(c),
        }).ToList();

        return Resultado.Ok(lista);
    }

    private static ComissaoDetalheDto DeOrdemDeServico(ComissaoFuncionario c, OrigensDasComissoesDto origens)
    {
        var orcamento = origens.Orcamentos.FirstOrDefault(o => o.Id == c.OrigemId);
        var cliente = orcamento != null ? origens.Clientes.FirstOrDefault(cl => cl.Id == orcamento.ClienteId) : null;

        return new ComissaoDetalheDto(
            c.Id, c.ValorServico, c.ComissaoPercentual, c.ValorComissao, c.Status, c.PagoEm, c.CriadoEm,
            orcamento != null ? EquipeRegras.NomeDaOrdemDeServico(orcamento.VeiculoDescricao, orcamento.Placa) : "Ordem de Serviço",
            cliente?.Nome, orcamento?.ConcluidoEm,
            OrigemComissao.OrdemServico,
            origens.ItensOrdemServico.Where(i => i.OrcamentoId == c.OrigemId)
                .Select(i => new ComissaoItemDto(i.Descricao, i.Quantidade, i.ValorTotal, i.Tipo)).ToList());
    }

    private static ComissaoDetalheDto DeVenda(ComissaoFuncionario c, OrigensDasComissoesDto origens)
    {
        var venda = origens.Vendas.FirstOrDefault(v => v.Id == c.OrigemId);
        var cliente = venda?.ClienteId.HasValue == true ? origens.Clientes.FirstOrDefault(cl => cl.Id == venda.ClienteId) : null;

        return new ComissaoDetalheDto(
            c.Id, c.ValorServico, c.ComissaoPercentual, c.ValorComissao, c.Status, c.PagoEm, c.CriadoEm,
            "Venda", cliente?.Nome, venda?.CriadaEm,
            OrigemComissao.Venda,
            origens.ItensVenda.Where(i => i.VendaId == c.OrigemId)
                .Select(i => new ComissaoItemDto(i.NomeProduto, i.Quantidade, i.Subtotal, i.ServicoId.HasValue ? "servico" : "produto")).ToList());
    }

    private static ComissaoDetalheDto DeAgendamento(ComissaoFuncionario c) => new(
        c.Id, c.ValorServico, c.ComissaoPercentual, c.ValorComissao, c.Status, c.PagoEm, c.CriadoEm,
        c.Agendamento?.NomeServico,
        c.Agendamento?.NomeCliente,
        c.Agendamento?.DataHora,
        OrigemComissao.Agendamento,
        c.Agendamento != null
            ? new List<ComissaoItemDto> { new(c.Agendamento.NomeServico, 1, c.Agendamento.Preco, "servico") }
            : null);
}
