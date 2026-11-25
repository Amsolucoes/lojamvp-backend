using LojaApi.Application.Equipe;
using LojaApi.Data;
using LojaApi.Domain.Equipe;
using LojaApi.Models;
using LojaApi.src.Models.Funcionarios;
using LojaApi.src.Models.OrdemServico;
using Microsoft.EntityFrameworkCore;

namespace LojaApi.Infrastructure.Equipe;

// Implementação EF Core do acesso aos dados da equipe.
public class EquipeRepository(AppDbContext db) : IEquipeRepository
{
    // ── Funcionários ──

    public Task<List<ProfissionalDetalheDto>> ListarProfissionaisAsync(Guid lojaId) =>
        db.Profissionais
            .Where(p => p.LojaId == lojaId)
            .Include(p => p.ComissoesPorServico)
            .OrderBy(p => p.Nome)
            .Select(p => new ProfissionalDetalheDto(
                p.Id, p.Nome, p.Ativo, p.ComissaoPadraoPercentual, p.DiaPagamentoPadrao, p.TipoRemuneracao, p.SalarioFixo, p.ValorDiaria,
                p.Telefone, p.Cep, p.Endereco, p.Numero, p.Bairro, p.Cidade, p.Uf, p.ComissaoBaseCalculo, p.ComissaoVendasAtiva,
                p.ComissaoVendasPercentual,
                p.ComissoesPorServico.Select(c => new ComissaoServicoDto(c.Id, c.ServicoId, c.ComissaoPercentual))))
            .ToListAsync();

    public Task<List<ProfissionalAtivoDto>> ListarProfissionaisAtivosAsync(Guid lojaId) =>
        db.Profissionais
            .Where(p => p.LojaId == lojaId && p.Ativo)
            .OrderBy(p => p.Nome)
            .Select(p => new ProfissionalAtivoDto(p.Id, p.Nome))
            .ToListAsync();

    public Task<Profissional?> ObterProfissionalAsync(Guid id, Guid? lojaId) =>
        db.Profissionais.FirstOrDefaultAsync(p => p.Id == id && p.LojaId == lojaId);

    public Task<List<Profissional>> ListarProfissionaisPorIdsAsync(List<Guid> ids) =>
        db.Profissionais.Where(p => ids.Contains(p.Id)).ToListAsync();

    public async Task<bool> ProfissionalEmUsoAsync(Guid id) =>
        await db.Agendamentos.AnyAsync(a => a.ProfissionalId == id) || await db.ComissoesFuncionario.AnyAsync(c => c.ProfissionalId == id);

    public void Adicionar(Profissional profissional) => db.Profissionais.Add(profissional);
    public void Remover(Profissional profissional) => db.Profissionais.Remove(profissional);

    // ── Comissão por serviço ──

    public Task<bool> ServicoDaLojaExisteAsync(Guid servicoId, Guid lojaId) =>
        db.Servicos.AnyAsync(s => s.Id == servicoId && s.LojaId == lojaId);

    public Task<ComissaoServicoProfissional?> ObterComissaoServicoAsync(Guid profissionalId, Guid servicoId) =>
        db.ComissoesServicoProfissional.FirstOrDefaultAsync(c => c.ProfissionalId == profissionalId && c.ServicoId == servicoId);

    public Task<ComissaoServicoProfissional?> ObterComissaoServicoPorIdAsync(Guid id, Guid? lojaId) =>
        db.ComissoesServicoProfissional.FirstOrDefaultAsync(c => c.Id == id && c.LojaId == lojaId);

    public void Adicionar(ComissaoServicoProfissional comissao) => db.ComissoesServicoProfissional.Add(comissao);
    public void Remover(ComissaoServicoProfissional comissao) => db.ComissoesServicoProfissional.Remove(comissao);

    // ── Financeiro ──

    public Task<bool> ContaBancariaDaLojaExisteAsync(Guid contaId, Guid lojaId) =>
        db.ContasBancarias.AnyAsync(c => c.Id == contaId && c.LojaId == lojaId);

    public Task<CategoriaFinanceira?> ObterCategoriaFinanceiraAsync(Guid lojaId, string nome) =>
        db.CategoriasFinanceiras.FirstOrDefaultAsync(c => c.LojaId == lojaId && c.Nome == nome);

    public void Adicionar(CategoriaFinanceira categoria) => db.CategoriasFinanceiras.Add(categoria);

    public Task<LancamentoFixo?> ObterLancamentoFixoAsync(Guid id) => db.LancamentosFixos.FindAsync(id).AsTask();
    public void Adicionar(LancamentoFixo fixo) => db.LancamentosFixos.Add(fixo);

    public Task<LancamentoFinanceiro?> ObterLancamentoFinanceiroAsync(Guid id) => db.LancamentosFinanceiros.FindAsync(id).AsTask();
    public void Adicionar(LancamentoFinanceiro lancamento) => db.LancamentosFinanceiros.Add(lancamento);
    public void Remover(LancamentoFinanceiro lancamento) => db.LancamentosFinanceiros.Remove(lancamento);

    // ── Comissões ──

    public Task<List<ComissaoFuncionario>> ListarComissoesPendentesAsync(Guid lojaId, DateTime? deInclusivo, DateTime? ateInclusivo)
    {
        var q = db.ComissoesFuncionario.Where(c => c.LojaId == lojaId && c.Status == StatusComissao.Pendente);
        if (deInclusivo.HasValue) q = q.Where(c => c.CriadoEm >= deInclusivo.Value);
        if (ateInclusivo.HasValue) q = q.Where(c => c.CriadoEm <= ateInclusivo.Value);
        return q.ToListAsync();
    }

    public Task<List<ComissaoFuncionario>> ListarComissoesDoProfissionalAsync(Guid lojaId, Guid profissionalId, string status) =>
        db.ComissoesFuncionario
            .Where(c => c.LojaId == lojaId && c.ProfissionalId == profissionalId && c.Status == status)
            .Include(c => c.Agendamento)
            .OrderByDescending(c => c.CriadoEm)
            .ToListAsync();

    public async Task<OrigensDasComissoesDto> ObterOrigensDasComissoesAsync(List<Guid> orcamentoIds, List<Guid> vendaIds)
    {
        var orcamentos = orcamentoIds.Count == 0
            ? new List<OrcamentoServico>()
            : await db.OrcamentosServico.Where(o => orcamentoIds.Contains(o.Id)).ToListAsync();

        var vendas = vendaIds.Count == 0
            ? new List<Venda>()
            : await db.Vendas.Where(v => vendaIds.Contains(v.Id)).ToListAsync();

        var itensVenda = vendaIds.Count == 0
            ? new List<ItemVenda>()
            : await db.ItensVenda.Where(i => vendaIds.Contains(i.VendaId)).ToListAsync();
        var itensOs = orcamentoIds.Count == 0
            ? new List<ItemOrcamentoServico>()
            : await db.ItensOrcamentoServico.Where(i => orcamentoIds.Contains(i.OrcamentoId)).ToListAsync();

        var clienteIds = orcamentos.Select(o => o.ClienteId)
            .Concat(vendas.Where(v => v.ClienteId.HasValue).Select(v => v.ClienteId!.Value))
            .Distinct()
            .ToList();
        var clientes = clienteIds.Count == 0
            ? new List<Cliente>()
            : await db.Clientes.Where(cl => clienteIds.Contains(cl.Id)).ToListAsync();

        return new OrigensDasComissoesDto(orcamentos, vendas, itensVenda, itensOs, clientes);
    }

    public Task<List<ComissaoFuncionario>> ListarComissoesPendentesDoPeriodoAsync(Guid lojaId, Guid profissionalId, DateTime inicio, DateTime fimExclusivo) =>
        db.ComissoesFuncionario
            .Where(c => c.LojaId == lojaId && c.ProfissionalId == profissionalId
                && c.Status == StatusComissao.Pendente && c.CriadoEm >= inicio && c.CriadoEm < fimExclusivo)
            .ToListAsync();

    public Task<List<ComissaoFuncionario>> ListarComissoesDoFechamentoAsync(Guid fechamentoId) =>
        db.ComissoesFuncionario.Where(c => c.FechamentoId == fechamentoId).ToListAsync();

    // ── Fechamentos ──

    public Task<List<FechamentoListaDto>> ListarFechamentosAsync(Guid lojaId, Guid? profissionalId)
    {
        var q = db.FechamentosComissao.Where(f => f.LojaId == lojaId);
        if (profissionalId.HasValue) q = q.Where(f => f.ProfissionalId == profissionalId.Value);

        return q
            .Include(f => f.Profissional)
            .OrderByDescending(f => f.PagoEm)
            .Select(f => new FechamentoListaDto(
                f.Id, f.ProfissionalId, f.Profissional != null ? f.Profissional.Nome : "—",
                f.PeriodoInicio, f.PeriodoFim, f.ValorTotal, f.QtdAtendimentos, f.PagoEm))
            .ToListAsync();
    }

    public Task<FechamentoComissao?> ObterFechamentoAsync(Guid id, Guid? lojaId) =>
        db.FechamentosComissao.FirstOrDefaultAsync(f => f.Id == id && f.LojaId == lojaId);

    public void Adicionar(FechamentoComissao fechamento) => db.FechamentosComissao.Add(fechamento);
    public void Remover(FechamentoComissao fechamento) => db.FechamentosComissao.Remove(fechamento);

    public Task SalvarAsync() => db.SaveChangesAsync();
}
