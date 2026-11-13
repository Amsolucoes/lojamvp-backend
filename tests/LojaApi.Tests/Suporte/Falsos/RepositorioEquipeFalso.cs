using LojaApi.Application.Equipe;
using LojaApi.Models;
using LojaApi.src.Models.Funcionarios;

namespace LojaApi.Tests.Suporte.Falsos;

// Repositório da Equipe em memória: permite testar os SERVICES (regras) sem banco.
// "Salvamentos" conta as gravações pedidas pelo service (zero = nada foi gravado).
public sealed class RepositorioEquipeFalso : IEquipeRepository
{
    public List<Profissional> Profissionais { get; } = [];
    public List<ComissaoServicoProfissional> ComissoesServico { get; } = [];
    public List<(Guid Id, Guid LojaId)> Servicos { get; } = [];
    public List<(Guid Id, Guid LojaId)> Contas { get; } = [];
    public List<CategoriaFinanceira> Categorias { get; } = [];
    public List<LancamentoFixo> Fixos { get; } = [];
    public List<LancamentoFinanceiro> Lancamentos { get; } = [];
    public List<ComissaoFuncionario> Comissoes { get; } = [];
    public List<FechamentoComissao> Fechamentos { get; } = [];
    public HashSet<Guid> ProfissionaisEmUso { get; } = [];
    public int Salvamentos { get; private set; }

    public Task<List<ProfissionalDetalheDto>> ListarProfissionaisAsync(Guid lojaId) => Task.FromResult(Profissionais.Where(p => p.LojaId == lojaId).OrderBy(p => p.Nome)
        .Select(p => new ProfissionalDetalheDto(p.Id, p.Nome, p.Ativo, p.ComissaoPadraoPercentual, p.DiaPagamentoPadrao, p.TipoRemuneracao, p.SalarioFixo, p.ValorDiaria,
            p.Telefone, p.Cep, p.Endereco, p.Numero, p.Bairro, p.Cidade, p.Uf, p.ComissaoBaseCalculo, p.ComissaoVendasAtiva, p.ComissaoVendasPercentual,
            ComissoesServico.Where(c => c.ProfissionalId == p.Id).Select(c => new ComissaoServicoDto(c.Id, c.ServicoId, c.ComissaoPercentual)).ToList())).ToList());

    public Task<List<ProfissionalAtivoDto>> ListarProfissionaisAtivosAsync(Guid lojaId) => Task.FromResult(
        Profissionais.Where(p => p.LojaId == lojaId && p.Ativo).OrderBy(p => p.Nome).Select(p => new ProfissionalAtivoDto(p.Id, p.Nome)).ToList());

    public Task<Profissional?> ObterProfissionalAsync(Guid id, Guid? lojaId) => Task.FromResult(Profissionais.FirstOrDefault(p => p.Id == id && p.LojaId == lojaId));
    public Task<List<Profissional>> ListarProfissionaisPorIdsAsync(List<Guid> ids) => Task.FromResult(Profissionais.Where(p => ids.Contains(p.Id)).ToList());
    public Task<bool> ProfissionalEmUsoAsync(Guid id) => Task.FromResult(ProfissionaisEmUso.Contains(id));
    public void Adicionar(Profissional profissional) => Profissionais.Add(profissional);
    public void Remover(Profissional profissional) => Profissionais.Remove(profissional);

    public Task<bool> ServicoDaLojaExisteAsync(Guid servicoId, Guid lojaId) => Task.FromResult(Servicos.Any(s => s.Id == servicoId && s.LojaId == lojaId));
    public Task<ComissaoServicoProfissional?> ObterComissaoServicoAsync(Guid profissionalId, Guid servicoId) =>
        Task.FromResult(ComissoesServico.FirstOrDefault(c => c.ProfissionalId == profissionalId && c.ServicoId == servicoId));
    public Task<ComissaoServicoProfissional?> ObterComissaoServicoPorIdAsync(Guid id, Guid? lojaId) => Task.FromResult(ComissoesServico.FirstOrDefault(c => c.Id == id && c.LojaId == lojaId));
    public void Adicionar(ComissaoServicoProfissional comissao) => ComissoesServico.Add(comissao);
    public void Remover(ComissaoServicoProfissional comissao) => ComissoesServico.Remove(comissao);

    public Task<bool> ContaBancariaDaLojaExisteAsync(Guid contaId, Guid lojaId) => Task.FromResult(Contas.Any(c => c.Id == contaId && c.LojaId == lojaId));
    public Task<CategoriaFinanceira?> ObterCategoriaFinanceiraAsync(Guid lojaId, string nome) => Task.FromResult(Categorias.FirstOrDefault(c => c.LojaId == lojaId && c.Nome == nome));
    public void Adicionar(CategoriaFinanceira categoria) => Categorias.Add(categoria);
    public Task<LancamentoFixo?> ObterLancamentoFixoAsync(Guid id) => Task.FromResult(Fixos.FirstOrDefault(f => f.Id == id));
    public void Adicionar(LancamentoFixo fixo) => Fixos.Add(fixo);
    public Task<LancamentoFinanceiro?> ObterLancamentoFinanceiroAsync(Guid id) => Task.FromResult(Lancamentos.FirstOrDefault(l => l.Id == id));
    public void Adicionar(LancamentoFinanceiro lancamento) => Lancamentos.Add(lancamento);
    public void Remover(LancamentoFinanceiro lancamento) => Lancamentos.Remove(lancamento);

    public Task<List<ComissaoFuncionario>> ListarComissoesPendentesAsync(Guid lojaId, DateTime? deInclusivo, DateTime? ateInclusivo) => Task.FromResult(Comissoes
        .Where(c => c.LojaId == lojaId && c.Status == "pendente" && (!deInclusivo.HasValue || c.CriadoEm >= deInclusivo) && (!ateInclusivo.HasValue || c.CriadoEm <= ateInclusivo)).ToList());

    public Task<List<ComissaoFuncionario>> ListarComissoesDoProfissionalAsync(Guid lojaId, Guid profissionalId, string status) => Task.FromResult(Comissoes
        .Where(c => c.LojaId == lojaId && c.ProfissionalId == profissionalId && c.Status == status).OrderByDescending(c => c.CriadoEm).ToList());

    public Task<OrigensDasComissoesDto> ObterOrigensDasComissoesAsync(List<Guid> orcamentoIds, List<Guid> vendaIds) =>
        Task.FromResult(new OrigensDasComissoesDto([], [], [], [], []));

    public Task<List<ComissaoFuncionario>> ListarComissoesPendentesDoPeriodoAsync(Guid lojaId, Guid profissionalId, DateTime inicio, DateTime fimExclusivo) => Task.FromResult(Comissoes
        .Where(c => c.LojaId == lojaId && c.ProfissionalId == profissionalId && c.Status == "pendente" && c.CriadoEm >= inicio && c.CriadoEm < fimExclusivo).ToList());

    public Task<List<ComissaoFuncionario>> ListarComissoesDoFechamentoAsync(Guid fechamentoId) => Task.FromResult(Comissoes.Where(c => c.FechamentoId == fechamentoId).ToList());

    public Task<List<FechamentoListaDto>> ListarFechamentosAsync(Guid lojaId, Guid? profissionalId) => Task.FromResult(Fechamentos
        .Where(f => f.LojaId == lojaId && (!profissionalId.HasValue || f.ProfissionalId == profissionalId)).OrderByDescending(f => f.PagoEm)
        .Select(f => new FechamentoListaDto(f.Id, f.ProfissionalId, Profissionais.FirstOrDefault(p => p.Id == f.ProfissionalId)?.Nome ?? "—", f.PeriodoInicio, f.PeriodoFim, f.ValorTotal, f.QtdAtendimentos, f.PagoEm)).ToList());

    public Task<FechamentoComissao?> ObterFechamentoAsync(Guid id, Guid? lojaId) => Task.FromResult(Fechamentos.FirstOrDefault(f => f.Id == id && f.LojaId == lojaId));
    public void Adicionar(FechamentoComissao fechamento) => Fechamentos.Add(fechamento);
    public void Remover(FechamentoComissao fechamento) => Fechamentos.Remove(fechamento);

    public Task SalvarAsync() { Salvamentos++; return Task.CompletedTask; }
}

// A porta do Financeiro de mentira: só anota o que o service pediu, na ordem.
public sealed class LancamentoFixoFinanceiroFalso : LojaApi.Application.Equipe.ILancamentoFixoFinanceiro
{
    public List<string> Chamadas { get; } = [];

    public Task GerarLoteAsync(LancamentoFixo fixo, DateTime desde) { Chamadas.Add($"gerar:{fixo.Id}"); return Task.CompletedTask; }
    public Task LimparFuturosAsync(Guid lancamentoFixoId) { Chamadas.Add($"limpar:{lancamentoFixoId}"); return Task.CompletedTask; }
}
