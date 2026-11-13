using LojaApi.Models;
using LojaApi.src.Models.Funcionarios;

namespace LojaApi.Application.Equipe;

// Acesso aos dados da equipe (funcionários, comissões e fechamentos). Implementado em Infrastructure/Equipe (EF Core).
public interface IEquipeRepository
{
    // ── Funcionários ──
    Task<List<ProfissionalDetalheDto>> ListarProfissionaisAsync(Guid lojaId);
    Task<List<ProfissionalAtivoDto>> ListarProfissionaisAtivosAsync(Guid lojaId);
    Task<Profissional?> ObterProfissionalAsync(Guid id, Guid? lojaId);
    Task<List<Profissional>> ListarProfissionaisPorIdsAsync(List<Guid> ids);
    // Já tem agendamento ou comissão? (então só se desativa)
    Task<bool> ProfissionalEmUsoAsync(Guid id);
    void Adicionar(Profissional profissional);
    void Remover(Profissional profissional);

    // ── Comissão por serviço ──
    Task<bool> ServicoDaLojaExisteAsync(Guid servicoId, Guid lojaId);
    Task<ComissaoServicoProfissional?> ObterComissaoServicoAsync(Guid profissionalId, Guid servicoId);
    Task<ComissaoServicoProfissional?> ObterComissaoServicoPorIdAsync(Guid id, Guid? lojaId);
    void Adicionar(ComissaoServicoProfissional comissao);
    void Remover(ComissaoServicoProfissional comissao);

    // ── Financeiro (salário fixo e conta a pagar do fechamento) ──
    Task<bool> ContaBancariaDaLojaExisteAsync(Guid contaId, Guid lojaId);
    Task<CategoriaFinanceira?> ObterCategoriaFinanceiraAsync(Guid lojaId, string nome);
    void Adicionar(CategoriaFinanceira categoria);
    Task<LancamentoFixo?> ObterLancamentoFixoAsync(Guid id);
    void Adicionar(LancamentoFixo fixo);
    Task<LancamentoFinanceiro?> ObterLancamentoFinanceiroAsync(Guid id);
    void Adicionar(LancamentoFinanceiro lancamento);
    void Remover(LancamentoFinanceiro lancamento);

    // ── Comissões ──
    // Pendentes da loja; de/ate já vêm como limites (inclusive) do período
    Task<List<ComissaoFuncionario>> ListarComissoesPendentesAsync(Guid lojaId, DateTime? deInclusivo, DateTime? ateInclusivo);
    Task<List<ComissaoFuncionario>> ListarComissoesDoProfissionalAsync(Guid lojaId, Guid profissionalId, string status);
    Task<OrigensDasComissoesDto> ObterOrigensDasComissoesAsync(List<Guid> orcamentoIds, List<Guid> vendaIds);
    Task<List<ComissaoFuncionario>> ListarComissoesPendentesDoPeriodoAsync(Guid lojaId, Guid profissionalId, DateTime inicio, DateTime fimExclusivo);
    Task<List<ComissaoFuncionario>> ListarComissoesDoFechamentoAsync(Guid fechamentoId);

    // ── Fechamentos ──
    Task<List<FechamentoListaDto>> ListarFechamentosAsync(Guid lojaId, Guid? profissionalId);
    Task<FechamentoComissao?> ObterFechamentoAsync(Guid id, Guid? lojaId);
    void Adicionar(FechamentoComissao fechamento);
    void Remover(FechamentoComissao fechamento);

    Task SalvarAsync();
}
