using LojaApi.Shared.Application;

namespace LojaApi.Application.OrdemServico;

// Detalhe de um orçamento/ordem: itens, mecânicos e respostas do checklist.
public class BuscarOrcamentoQueryHandler(IOrdemServicoRepository repositorio) : IQueryHandler<BuscarOrcamentoQuery>
{
    public async Task<Resultado<object>> HandleAsync(BuscarOrcamentoQuery query)
    {
        var (lojaId, id) = query;
        var o = await repositorio.ObterOrcamentoDetalheAsync(id, lojaId);
        if (o is null) return Resultado.NaoEncontrado();

        return Resultado.Ok(new OrcamentoDetalheDto(
            o.Id, o.ClienteId, o.VeiculoDescricao, o.Placa, o.Status, o.Observacoes, o.ValorTotal, o.CriadoEm, o.AprovadoEm, o.ConcluidoEm,
            o.Itens.Select(i => new ItemOrcamentoDto(i.Id, i.Tipo, i.ProdutoId, i.ServicoId, i.Descricao, i.Quantidade, i.ValorUnitario, i.ValorTotal)).ToList(),
            o.Mecanicos.Select(m => new MecanicoOrcamentoDto(m.Id, m.ProfissionalId, m.Profissional!.Nome, m.ComissaoPercentual)).ToList(),
            o.ChecklistRespostas.Select(r => new ChecklistRespostaDto(r.Id, r.ChecklistItemId, r.ChecklistItem!.Nome, r.Estado, r.Observacao)).ToList()));
    }
}
