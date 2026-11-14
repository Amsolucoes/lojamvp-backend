using LojaApi.Application.OrdemServico;
using LojaApi.Models;
using LojaApi.src.Models.Funcionarios;
using LojaApi.src.Models.OrdemServico;

namespace LojaApi.Tests.Suporte.Falsos;

// Repositório de OrdemServico em memória: permite testar os HANDLERS (regras) sem banco.
// "Salvamentos" conta as gravações pedidas pelo handler (zero = nada foi gravado).
// As buscas "(id, lojaId)" de OrdemServico não acham nada com lojaId nulo; as de outros módulos não filtram com lojaId nulo (como o repositório de verdade).
public sealed class RepositorioOrdemServicoFalso : IOrdemServicoRepository
{
    public List<ChecklistCategoria> Categorias { get; } = [];
    public List<ChecklistItem> ItensChecklist { get; } = [];
    public HashSet<Guid> ItensChecklistEmUso { get; } = [];
    public List<OrcamentoServico> Orcamentos { get; } = [];
    public List<Cliente> Clientes { get; } = [];
    public List<Profissional> Mecanicos { get; } = [];
    public List<Produto> Produtos { get; } = [];
    public List<Servico> Servicos { get; } = [];
    public List<ContaBancaria> Contas { get; } = [];
    public List<CategoriaFinanceira> CategoriasFinanceiras { get; } = [];
    public List<LancamentoFinanceiro> Lancamentos { get; } = [];
    public List<ComissaoFuncionario> Comissoes { get; } = [];
    public List<ComissaoServicoProfissional> Excecoes { get; } = [];
    public List<MovimentoEstoque> MovimentosEstoque { get; } = [];
    public List<ItemPatrimonio> ItensPatrimonio { get; } = [];
    public HashSet<Guid> ItensPatrimonioContados { get; } = [];
    public List<ContagemPatrimonio> Contagens { get; } = [];
    public string? NomeDaLoja { get; set; } = "Oficina Teste";
    public int Salvamentos { get; private set; }

    // ── Checklist ──
    public Task<List<ChecklistCategoriaDto>> ListarChecklistAsync(Guid lojaId) => Task.FromResult(
        Categorias.Where(c => c.LojaId == lojaId).OrderBy(c => c.Ordem)
            .Select(c => new ChecklistCategoriaDto(c.Id, c.Nome, c.Ordem, c.Ativa,
                ItensChecklist.Where(i => i.CategoriaId == c.Id).OrderBy(i => i.Ordem).Select(i => new ChecklistItemListaDto(i.Id, i.Nome, i.Ordem, i.Ativo)).ToList())).ToList());
    public Task<ChecklistCategoria?> ObterCategoriaAsync(Guid id, Guid? lojaId) => Task.FromResult(Categorias.FirstOrDefault(c => c.Id == id && c.LojaId == lojaId));
    public Task<ChecklistCategoria?> ObterCategoriaComItensAsync(Guid id, Guid? lojaId)
    {
        var categoria = Categorias.FirstOrDefault(c => c.Id == id && c.LojaId == lojaId);
        if (categoria != null) categoria.Itens = ItensChecklist.Where(i => i.CategoriaId == id).ToList();
        return Task.FromResult(categoria);
    }
    public Task<ChecklistItem?> ObterItemChecklistAsync(Guid id, Guid? lojaId) => Task.FromResult(ItensChecklist.FirstOrDefault(i => i.Id == id && i.LojaId == lojaId));
    public Task<ChecklistItem?> ObterItemChecklistDeQualquerLojaAsync(Guid id) => Task.FromResult(ItensChecklist.FirstOrDefault(i => i.Id == id));
    public Task<bool> ItensDeChecklistEmUsoAsync(IEnumerable<Guid> itemIds) => Task.FromResult(itemIds.Any(ItensChecklistEmUso.Contains));
    public Task<bool> ItemDeChecklistEmUsoAsync(Guid itemId) => Task.FromResult(ItensChecklistEmUso.Contains(itemId));
    public void Adicionar(ChecklistCategoria categoria) => Categorias.Add(categoria);
    public void Remover(ChecklistCategoria categoria) => Categorias.Remove(categoria);
    public void Adicionar(ChecklistItem item) => ItensChecklist.Add(item);
    public void Remover(ChecklistItem item) => ItensChecklist.Remove(item);

    // ── Orçamentos ──
    public Task<List<OrcamentoListaDto>> ListarOrcamentosAsync(Guid lojaId, string? status, string? placaBusca) => Task.FromResult(
        Orcamentos.Where(o => o.LojaId == lojaId && (status == null || o.Status == status) && (placaBusca == null || (o.Placa != null && o.Placa.Contains(placaBusca))))
            .OrderByDescending(o => o.CriadoEm)
            .Select(o => new OrcamentoListaDto(o.Id, o.ClienteId, o.VeiculoDescricao, o.Placa, o.Status, o.ValorTotal, o.CriadoEm, o.AprovadoEm, o.ConcluidoEm,
                o.Mecanicos.Count, o.Mecanicos.Select(m => m.Profissional?.Nome ?? "").ToList())).ToList());
    public Task<OrcamentoServico?> ObterOrcamentoDetalheAsync(Guid id, Guid? lojaId) => Task.FromResult(Orcamentos.FirstOrDefault(o => o.Id == id && o.LojaId == lojaId));
    public Task<List<OrcamentoDoClienteDto>> ListarOrcamentosDoClienteAsync(Guid lojaId, Guid clienteId) => Task.FromResult(new List<OrcamentoDoClienteDto>());
    public Task<List<ResumoClienteOrdemDto>> ResumoPorClienteAsync(Guid lojaId) => Task.FromResult(new List<ResumoClienteOrdemDto>());
    public Task<OrcamentoServico?> ObterOrcamentoAsync(Guid id, Guid? lojaId) => Task.FromResult(Orcamentos.FirstOrDefault(o => o.Id == id && o.LojaId == lojaId));
    public Task<OrcamentoServico?> ObterOrcamentoParaEdicaoAsync(Guid id, Guid? lojaId) => ObterOrcamentoAsync(id, lojaId);
    public Task<OrcamentoServico?> ObterOrcamentoComItensAsync(Guid id, Guid? lojaId) => ObterOrcamentoAsync(id, lojaId);
    public Task<OrcamentoServico?> ObterOrcamentoParaConclusaoAsync(Guid id, Guid? lojaId) => ObterOrcamentoAsync(id, lojaId);
    public void Adicionar(OrcamentoServico orcamento) => Orcamentos.Add(orcamento);
    public void Remover(OrcamentoServico orcamento) => Orcamentos.Remove(orcamento);
    public void Adicionar(ItemOrcamentoServico item) { }
    public void Adicionar(MecanicoOrcamento mecanico) { }
    public void Adicionar(ChecklistRespostaItem resposta) { }
    public void RemoverItens(IEnumerable<ItemOrcamentoServico> itens) { }
    public void RemoverMecanicos(IEnumerable<MecanicoOrcamento> mecanicos) { }
    public void RemoverRespostas(IEnumerable<ChecklistRespostaItem> respostas) { }

    // ── Outros módulos ──
    public Task<Cliente?> ObterClienteAsync(Guid id, Guid? lojaId) => Task.FromResult(Clientes.FirstOrDefault(c => c.Id == id && (lojaId == null || c.LojaId == lojaId)));
    public Task<Profissional?> ObterMecanicoAsync(Guid id, Guid? lojaId) => Task.FromResult(Mecanicos.FirstOrDefault(p => p.Id == id && p.LojaId == lojaId));
    public Task<Produto?> ObterProdutoAsync(Guid id, Guid? lojaId) => Task.FromResult(Produtos.FirstOrDefault(p => p.Id == id && (lojaId == null || p.LojaId == lojaId)));
    public Task<Servico?> ObterServicoAsync(Guid id, Guid? lojaId) => Task.FromResult(Servicos.FirstOrDefault(s => s.Id == id && (lojaId == null || s.LojaId == lojaId)));
    public Task<ContaBancaria?> ObterContaBancariaAsync(Guid id, Guid? lojaId) => Task.FromResult(Contas.FirstOrDefault(c => c.Id == id && c.LojaId == lojaId));
    public Task<CategoriaFinanceira?> ObterCategoriaFinanceiraAsync(Guid lojaId, string nome) => Task.FromResult(CategoriasFinanceiras.FirstOrDefault(c => c.LojaId == lojaId && c.Nome == nome));
    public Task<LancamentoFinanceiro?> ObterLancamentoAsync(Guid id) => Task.FromResult(Lancamentos.FirstOrDefault(l => l.Id == id));
    public Task<List<ComissaoFuncionario>> ListarComissoesDaOrdemAsync(Guid orcamentoId) => Task.FromResult(Comissoes.Where(c => c.OrigemTipo == "ordem_servico" && c.OrigemId == orcamentoId).ToList());
    public Task<List<ComissaoServicoProfissional>> ListarExcecoesDeComissaoAsync(IEnumerable<Guid> servicoIds, IEnumerable<Guid> profissionalIds) => Task.FromResult(
        Excecoes.Where(e => servicoIds.Contains(e.ServicoId) && profissionalIds.Contains(e.ProfissionalId)).ToList());
    public void Adicionar(MovimentoEstoque movimento) => MovimentosEstoque.Add(movimento);
    public void Adicionar(CategoriaFinanceira categoria) => CategoriasFinanceiras.Add(categoria);
    public void Adicionar(LancamentoFinanceiro lancamento) => Lancamentos.Add(lancamento);
    public void Remover(LancamentoFinanceiro lancamento) => Lancamentos.Remove(lancamento);
    public void Adicionar(ComissaoFuncionario comissao) => Comissoes.Add(comissao);
    public void RemoverComissoes(IEnumerable<ComissaoFuncionario> comissoes) { foreach (var c in comissoes.ToList()) Comissoes.Remove(c); }

    // ── Patrimônio ──
    public Task<List<ItemPatrimonioListaDto>> ListarItensPatrimonioAsync(Guid lojaId, bool incluirInativos) => Task.FromResult(
        ItensPatrimonio.Where(i => i.LojaId == lojaId && (incluirInativos || i.Ativo))
            .Select(i => new ItemPatrimonioListaDto(i.Id, i.Nome, i.Categoria, i.QuantidadeEsperada, i.ValorUnitario, i.Observacao, i.Ativo, i.QuantidadeEsperada * i.ValorUnitario)).ToList());
    public Task<ItemPatrimonio?> ObterItemPatrimonioAsync(Guid id, Guid? lojaId) => Task.FromResult(ItensPatrimonio.FirstOrDefault(i => i.Id == id && i.LojaId == lojaId));
    public Task<List<ItemPatrimonio>> ListarItensPatrimonioPorIdsAsync(IEnumerable<Guid> ids, Guid lojaId) => Task.FromResult(ItensPatrimonio.Where(i => ids.Contains(i.Id) && i.LojaId == lojaId).ToList());
    public Task<List<ItemPatrimonio>> ListarItensAtivosDePatrimonioAsync(Guid lojaId) => Task.FromResult(ItensPatrimonio.Where(i => i.LojaId == lojaId && i.Ativo).ToList());
    public Task<bool> ItemPatrimonioJaContadoAsync(Guid itemId) => Task.FromResult(ItensPatrimonioContados.Contains(itemId));
    public void Adicionar(ItemPatrimonio item) => ItensPatrimonio.Add(item);
    public void Remover(ItemPatrimonio item) => ItensPatrimonio.Remove(item);
    public Task<List<ContagemListaDto>> ListarContagensAsync(Guid lojaId) => Task.FromResult(new List<ContagemListaDto>());
    public Task<ContagemPatrimonio?> ObterContagemAsync(Guid id, Guid? lojaId) => Task.FromResult(Contagens.FirstOrDefault(c => c.Id == id && c.LojaId == lojaId));
    public Task<ContagemPatrimonio?> ObterContagemCompletaAsync(Guid id, Guid? lojaId) => ObterContagemAsync(id, lojaId);
    public void Adicionar(ContagemPatrimonio contagem) => Contagens.Add(contagem);
    public void Remover(ContagemPatrimonio contagem) => Contagens.Remove(contagem);
    public Task<string?> ObterNomeDaLojaAsync(Guid lojaId) => Task.FromResult(NomeDaLoja);

    public Task SalvarAsync() { Salvamentos++; return Task.CompletedTask; }
}
