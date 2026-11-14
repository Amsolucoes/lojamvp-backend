using LojaApi.Application.OrdemServico;
using LojaApi.Data;
using LojaApi.Models;
using LojaApi.src.Models.Funcionarios;
using LojaApi.src.Models.OrdemServico;
using Microsoft.EntityFrameworkCore;

namespace LojaApi.Infrastructure.OrdemServico;

// Acesso aos dados de OrdemServico (EF Core). Só lê e grava; as regras ficam nos handlers.
// Nas buscas "(id, lojaId)" de OrdemServico, com lojaId nulo nada é encontrado. Nas de outros módulos (cliente, produto, serviço), lojaId nulo não filtra.
public class OrdemServicoRepository(AppDbContext db) : IOrdemServicoRepository
{
    // ── Checklist ──

    public Task<List<ChecklistCategoriaDto>> ListarChecklistAsync(Guid lojaId) =>
        db.ChecklistCategorias
            .Where(c => c.LojaId == lojaId)
            .OrderBy(c => c.Ordem)
            .Select(c => new ChecklistCategoriaDto(
                c.Id, c.Nome, c.Ordem, c.Ativa,
                c.Itens.OrderBy(i => i.Ordem).Select(i => new ChecklistItemListaDto(i.Id, i.Nome, i.Ordem, i.Ativo)).ToList()))
            .ToListAsync();

    public Task<ChecklistCategoria?> ObterCategoriaAsync(Guid id, Guid? lojaId) =>
        db.ChecklistCategorias.FirstOrDefaultAsync(c => c.Id == id && c.LojaId == lojaId);

    public Task<ChecklistCategoria?> ObterCategoriaComItensAsync(Guid id, Guid? lojaId) =>
        db.ChecklistCategorias.Include(c => c.Itens).FirstOrDefaultAsync(c => c.Id == id && c.LojaId == lojaId);

    public Task<ChecklistItem?> ObterItemChecklistAsync(Guid id, Guid? lojaId) =>
        db.ChecklistItens.FirstOrDefaultAsync(i => i.Id == id && i.LojaId == lojaId);

    public Task<ChecklistItem?> ObterItemChecklistDeQualquerLojaAsync(Guid id) =>
        db.ChecklistItens.FirstOrDefaultAsync(i => i.Id == id);

    public Task<bool> ItensDeChecklistEmUsoAsync(IEnumerable<Guid> itemIds)
    {
        var ids = itemIds.ToList();
        return db.ChecklistRespostasItem.AnyAsync(r => ids.Contains(r.ChecklistItemId));
    }

    public Task<bool> ItemDeChecklistEmUsoAsync(Guid itemId) => db.ChecklistRespostasItem.AnyAsync(r => r.ChecklistItemId == itemId);

    public void Adicionar(ChecklistCategoria categoria) => db.ChecklistCategorias.Add(categoria);
    public void Remover(ChecklistCategoria categoria) => db.ChecklistCategorias.Remove(categoria);
    public void Adicionar(ChecklistItem item) => db.ChecklistItens.Add(item);
    public void Remover(ChecklistItem item) => db.ChecklistItens.Remove(item);

    // ── Orçamentos e ordens ──

    public Task<List<OrcamentoListaDto>> ListarOrcamentosAsync(Guid lojaId, string? status, string? placaBusca)
    {
        var q = db.OrcamentosServico.Where(o => o.LojaId == lojaId);
        if (status != null) q = q.Where(o => o.Status == status);
        if (placaBusca != null) q = q.Where(o => o.Placa != null && o.Placa.Contains(placaBusca));

        return q.OrderByDescending(o => o.CriadoEm)
            .Select(o => new OrcamentoListaDto(
                o.Id, o.ClienteId, o.VeiculoDescricao, o.Placa, o.Status, o.ValorTotal, o.CriadoEm, o.AprovadoEm, o.ConcluidoEm,
                o.Mecanicos.Count, o.Mecanicos.Select(m => m.Profissional!.Nome).ToList()))
            .ToListAsync();
    }

    public Task<OrcamentoServico?> ObterOrcamentoDetalheAsync(Guid id, Guid? lojaId) =>
        db.OrcamentosServico
            .Include(x => x.Itens)
            .Include(x => x.Mecanicos).ThenInclude(m => m.Profissional)
            .Include(x => x.ChecklistRespostas).ThenInclude(r => r.ChecklistItem)
            .FirstOrDefaultAsync(x => x.Id == id && x.LojaId == lojaId);

    public Task<List<OrcamentoDoClienteDto>> ListarOrcamentosDoClienteAsync(Guid lojaId, Guid clienteId) =>
        db.OrcamentosServico
            .Where(o => o.LojaId == lojaId && o.ClienteId == clienteId)
            .OrderByDescending(o => o.CriadoEm)
            .Select(o => new OrcamentoDoClienteDto(
                o.Id, o.VeiculoDescricao, o.Placa, o.Status, o.ValorTotal, o.CriadoEm, o.ConcluidoEm,
                o.Itens.Select(i => new ItemDoClienteDto(i.Descricao, i.Quantidade, i.ValorTotal)).ToList()))
            .ToListAsync();

    public Task<List<ResumoClienteOrdemDto>> ResumoPorClienteAsync(Guid lojaId) =>
        db.OrcamentosServico
            .Where(o => o.LojaId == lojaId && o.Status != "cancelado")
            .GroupBy(o => o.ClienteId)
            .Select(g => new ResumoClienteOrdemDto(g.Key, g.Count(), g.Sum(o => o.ValorTotal), g.Any(o => o.Status != "concluido")))
            .ToListAsync();

    public Task<OrcamentoServico?> ObterOrcamentoAsync(Guid id, Guid? lojaId) =>
        db.OrcamentosServico.FirstOrDefaultAsync(o => o.Id == id && o.LojaId == lojaId);

    public Task<OrcamentoServico?> ObterOrcamentoParaEdicaoAsync(Guid id, Guid? lojaId) =>
        db.OrcamentosServico
            .Include(o => o.Itens)
            .Include(o => o.Mecanicos)
            .Include(o => o.ChecklistRespostas)
            .FirstOrDefaultAsync(o => o.Id == id && o.LojaId == lojaId);

    public Task<OrcamentoServico?> ObterOrcamentoComItensAsync(Guid id, Guid? lojaId) =>
        db.OrcamentosServico.Include(o => o.Itens).FirstOrDefaultAsync(o => o.Id == id && o.LojaId == lojaId);

    public Task<OrcamentoServico?> ObterOrcamentoParaConclusaoAsync(Guid id, Guid? lojaId) =>
        db.OrcamentosServico
            .Include(o => o.Itens)
            .Include(o => o.Mecanicos).ThenInclude(m => m.Profissional)
            .FirstOrDefaultAsync(o => o.Id == id && o.LojaId == lojaId);

    public void Adicionar(OrcamentoServico orcamento) => db.OrcamentosServico.Add(orcamento);
    public void Remover(OrcamentoServico orcamento) => db.OrcamentosServico.Remove(orcamento);
    public void Adicionar(ItemOrcamentoServico item) => db.ItensOrcamentoServico.Add(item);
    public void Adicionar(MecanicoOrcamento mecanico) => db.MecanicosOrcamento.Add(mecanico);
    public void Adicionar(ChecklistRespostaItem resposta) => db.ChecklistRespostasItem.Add(resposta);
    public void RemoverItens(IEnumerable<ItemOrcamentoServico> itens) => db.ItensOrcamentoServico.RemoveRange(itens);
    public void RemoverMecanicos(IEnumerable<MecanicoOrcamento> mecanicos) => db.MecanicosOrcamento.RemoveRange(mecanicos);
    public void RemoverRespostas(IEnumerable<ChecklistRespostaItem> respostas) => db.ChecklistRespostasItem.RemoveRange(respostas);

    // ── Dados de outros módulos ──

    public Task<Cliente?> ObterClienteAsync(Guid id, Guid? lojaId) =>
        db.Clientes.FirstOrDefaultAsync(c => c.Id == id && (lojaId == null || c.LojaId == lojaId));

    public Task<Profissional?> ObterMecanicoAsync(Guid id, Guid? lojaId) =>
        db.Profissionais.FirstOrDefaultAsync(p => p.Id == id && p.LojaId == lojaId);

    public Task<Produto?> ObterProdutoAsync(Guid id, Guid? lojaId) =>
        db.Produtos.FirstOrDefaultAsync(p => p.Id == id && (lojaId == null || p.LojaId == lojaId));

    public Task<Servico?> ObterServicoAsync(Guid id, Guid? lojaId) =>
        db.Servicos.FirstOrDefaultAsync(s => s.Id == id && (lojaId == null || s.LojaId == lojaId));

    public Task<ContaBancaria?> ObterContaBancariaAsync(Guid id, Guid? lojaId) =>
        db.ContasBancarias.FirstOrDefaultAsync(c => c.Id == id && c.LojaId == lojaId);

    public Task<CategoriaFinanceira?> ObterCategoriaFinanceiraAsync(Guid lojaId, string nome) =>
        db.CategoriasFinanceiras.FirstOrDefaultAsync(c => c.LojaId == lojaId && c.Nome == nome);

    public Task<LancamentoFinanceiro?> ObterLancamentoAsync(Guid id) => db.LancamentosFinanceiros.FirstOrDefaultAsync(l => l.Id == id);

    public Task<List<ComissaoFuncionario>> ListarComissoesDaOrdemAsync(Guid orcamentoId) =>
        db.ComissoesFuncionario.Where(c => c.OrigemTipo == "ordem_servico" && c.OrigemId == orcamentoId).ToListAsync();

    public Task<List<ComissaoServicoProfissional>> ListarExcecoesDeComissaoAsync(IEnumerable<Guid> servicoIds, IEnumerable<Guid> profissionalIds)
    {
        var servicos = servicoIds.ToList();
        var profissionais = profissionalIds.ToList();
        return db.ComissoesServicoProfissional.Where(e => servicos.Contains(e.ServicoId) && profissionais.Contains(e.ProfissionalId)).ToListAsync();
    }

    public void Adicionar(MovimentoEstoque movimento) => db.Movimentos.Add(movimento);
    public void Adicionar(CategoriaFinanceira categoria) => db.CategoriasFinanceiras.Add(categoria);
    public void Adicionar(LancamentoFinanceiro lancamento) => db.LancamentosFinanceiros.Add(lancamento);
    public void Remover(LancamentoFinanceiro lancamento) => db.LancamentosFinanceiros.Remove(lancamento);
    public void Adicionar(ComissaoFuncionario comissao) => db.ComissoesFuncionario.Add(comissao);
    public void RemoverComissoes(IEnumerable<ComissaoFuncionario> comissoes) => db.ComissoesFuncionario.RemoveRange(comissoes);

    // ── Patrimônio ──

    public Task<List<ItemPatrimonioListaDto>> ListarItensPatrimonioAsync(Guid lojaId, bool incluirInativos)
    {
        var q = db.ItensPatrimonio.Where(i => i.LojaId == lojaId);
        if (!incluirInativos) q = q.Where(i => i.Ativo);

        return q.OrderBy(i => i.Categoria).ThenBy(i => i.Nome)
            .Select(i => new ItemPatrimonioListaDto(
                i.Id, i.Nome, i.Categoria, i.QuantidadeEsperada, i.ValorUnitario, i.Observacao, i.Ativo, i.QuantidadeEsperada * i.ValorUnitario))
            .ToListAsync();
    }

    public Task<ItemPatrimonio?> ObterItemPatrimonioAsync(Guid id, Guid? lojaId) =>
        db.ItensPatrimonio.FirstOrDefaultAsync(i => i.Id == id && i.LojaId == lojaId);

    public Task<List<ItemPatrimonio>> ListarItensPatrimonioPorIdsAsync(IEnumerable<Guid> ids, Guid lojaId)
    {
        var lista = ids.ToList();
        return db.ItensPatrimonio.Where(i => lista.Contains(i.Id) && i.LojaId == lojaId).ToListAsync();
    }

    public Task<List<ItemPatrimonio>> ListarItensAtivosDePatrimonioAsync(Guid lojaId) =>
        db.ItensPatrimonio.Where(i => i.LojaId == lojaId && i.Ativo).OrderBy(i => i.Categoria).ThenBy(i => i.Nome).ToListAsync();

    public Task<bool> ItemPatrimonioJaContadoAsync(Guid itemId) => db.ItensContagemPatrimonio.AnyAsync(c => c.ItemPatrimonioId == itemId);

    public void Adicionar(ItemPatrimonio item) => db.ItensPatrimonio.Add(item);
    public void Remover(ItemPatrimonio item) => db.ItensPatrimonio.Remove(item);

    public Task<List<ContagemListaDto>> ListarContagensAsync(Guid lojaId) =>
        db.ContagensPatrimonio
            .Where(c => c.LojaId == lojaId)
            .OrderByDescending(c => c.DataContagem)
            .Select(c => new ContagemListaDto(
                c.Id, c.DataContagem, c.Responsavel, c.Observacao,
                c.Itens.Count, c.Itens.Count(i => i.QuantidadeContada != i.QuantidadeEsperadaNoMomento)))
            .ToListAsync();

    public Task<ContagemPatrimonio?> ObterContagemAsync(Guid id, Guid? lojaId) =>
        db.ContagensPatrimonio.FirstOrDefaultAsync(c => c.Id == id && c.LojaId == lojaId);

    public Task<ContagemPatrimonio?> ObterContagemCompletaAsync(Guid id, Guid? lojaId) =>
        db.ContagensPatrimonio
            .Include(c => c.Itens).ThenInclude(i => i.ItemPatrimonio)
            .FirstOrDefaultAsync(c => c.Id == id && c.LojaId == lojaId);

    public void Adicionar(ContagemPatrimonio contagem) => db.ContagensPatrimonio.Add(contagem);
    public void Remover(ContagemPatrimonio contagem) => db.ContagensPatrimonio.Remove(contagem);

    public async Task<string?> ObterNomeDaLojaAsync(Guid lojaId) =>
        (await db.Lojas.FindAsync(lojaId))?.Nome;

    public Task SalvarAsync() => db.SaveChangesAsync();
}
