using LojaApi.Models;

namespace LojaApi.Application.Financeiro;

// Acesso aos dados das faturas do cartão, dos pagamentos antecipados e das parcelas de financiamento. Os handlers decidem as regras;
// aqui só se lê e grava. Implementado em Infrastructure/Financeiro (EF Core), na mesma unidade de trabalho dos outros repositórios do Financeiro.
public interface IFaturasRepository
{
    // ── Compras do ciclo ──
    Task<List<FaturaItemDto>> ListarItensDoCicloAsync(Guid cartaoId, DateTime inicio, DateTime fim);   // por data da compra
    Task<decimal> SomarComprasDoCicloAsync(Guid cartaoId, DateTime inicio, DateTime fim);

    // ── Fatura ──
    Task<FaturaCartao?> ObterFaturaDoMesAsync(Guid cartaoId, int ano, int mes);                         // pelo ano e mês da referência
    Task<FaturaCartao?> ObterFaturaPorReferenciaAsync(Guid cartaoId, DateTime mesReferencia);           // pela data exata (1º dia do mês)
    Task<List<FaturaCartao>> ListarFaturasPorIdsAsync(List<Guid> ids);
    void Adicionar(FaturaCartao fatura);

    // ── Pagamentos antecipados ──
    Task<List<AntecipadoDto>> ListarAntecipadosAsync(Guid faturaId);                                    // por data
    Task<decimal> SomarAntecipadosAsync(Guid faturaId);
    Task<PagamentoAntecipadoFatura?> ObterAntecipadoAsync(Guid id, Guid lojaId);
    void Adicionar(PagamentoAntecipadoFatura antecipado);
    void Remover(PagamentoAntecipadoFatura antecipado);

    // ── Parcelas de financiamento (lançamentos ligados a um cartão) ──
    Task<List<LancamentoFinanceiro>> ListarParcelasDoMesAsync(Guid cartaoId, int ano, int mes);         // por número da parcela
    Task<List<LancamentoFinanceiro>> ListarParcelasPendentesDoMesAsync(Guid cartaoId, int ano, int mes);
    Task<List<LancamentoFinanceiro>> ListarParcelasPagasDoMesAsync(Guid cartaoId, int ano, int mes);
    Task<List<LancamentoFinanceiro>> ListarParcelasPendentesDaFaturaAsync(Guid faturaId);

    // ── Autocorreção das parcelas de financiamento ──
    Task<List<FaturaParcialDto>> ListarFaturasParciaisAsync(Guid lojaId);
    Task<List<LancamentoFinanceiro>> ListarPendentesDeCartoesAsync(Guid lojaId, List<Guid?> cartaoIds);
    Task<List<Guid?>> ListarGruposDeFinanciamentoPendentesAsync(Guid lojaId);
    Task<List<LancamentoFinanceiro>> ListarPendentesDosGruposAsync(List<Guid?> grupoIds);
    Task<Dictionary<Guid, CartaoCredito>> ObterCartoesPorIdsAsync(List<Guid> ids);
    Task SalvarSeHouverAlteracoesAsync();

    Task SalvarAsync();
}
