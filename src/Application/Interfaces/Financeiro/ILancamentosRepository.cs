using LojaApi.Models;

namespace LojaApi.Application.Financeiro;

// Acesso aos dados dos lançamentos financeiros (avulsos, parcelados e fixos). Os handlers decidem as regras; aqui só se lê e grava.
// Implementado em Infrastructure/Financeiro (EF Core). Ao salvar, vale a mesma unidade de trabalho do IFinanceiroRepository.
public interface ILancamentosRepository
{
    // ── Leituras prontas para a API ──
    Task<List<LancamentoAReceberDto>> ListarAReceberAsync(Guid lojaId, DateTime? de, DateTime? ate);
    Task<List<MensalidadeDePlanoDto>> ListarMensalidadesDePlanosAsync(Guid lojaId, DateTime? de, DateTime? ate);
    Task<List<string>> ListarDescricoesRecentesAsync(Guid lojaId, string tipo);                     // distintas, da mais nova, até 50

    // ── Lançamentos ──
    Task<LancamentoFinanceiro?> ObterAsync(Guid id, Guid lojaId);
    Task<List<LancamentoFinanceiro>> ListarGrupoAsync(Guid grupoId, Guid lojaId);                    // por número da parcela e vencimento
    Task<List<LancamentoFinanceiro>> ListarPendentesParaPagarEmLoteAsync(List<Guid> ids, Guid lojaId); // a pagar, pendentes e que não vieram de cartão
    Task<List<LancamentoFinanceiro>> ListarPendentesDoGrupoAsync(Guid grupoId, DateTime? aPartirDe);
    Task<List<LancamentoFinanceiro>> ListarPendentesDoFixoAsync(Guid fixoId, DateTime aPartirDe);
    Task<LancamentoFinanceiro?> ObterPrimeiraOcorrenciaDoFixoAsync(Guid fixoId);
    void Adicionar(LancamentoFinanceiro lancamento);
    void Adicionar(IEnumerable<LancamentoFinanceiro> lancamentos);
    void Remover(LancamentoFinanceiro lancamento);
    void Remover(IEnumerable<LancamentoFinanceiro> lancamentos);

    // ── Fixos ──
    Task<LancamentoFixo?> ObterFixoAsync(Guid id, Guid lojaId);
    Task<LancamentoFixo?> ObterFixoPorIdAsync(Guid id);
    void Adicionar(LancamentoFixo fixo);

    // ── Cartão (pagamento em lote no cartão) ──
    Task<CartaoCredito?> ObterCartaoAsync(Guid id, Guid lojaId);
    void Adicionar(LancamentoCartao compra);

    Task SalvarAsync();
}

// Gera e limpa as ocorrências mensais de um lançamento fixo (hoje o FinanceiroService; ver Infrastructure/Financeiro).
public interface IGeradorDeLancamentosFixos
{
    Task GerarLoteAsync(LancamentoFixo fixo, DateTime desde);
    Task LimparFuturosAsync(Guid lancamentoFixoId);
}
