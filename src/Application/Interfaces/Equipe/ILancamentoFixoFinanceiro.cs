using LojaApi.Models;

namespace LojaApi.Application.Equipe;

// PORTA: o salário fixo de um funcionário vira um lançamento fixo no Financeiro. Quem gera os lançamentos mensais
// (hoje o FinanceiroService, no projeto da API) é ligado em Infrastructure/Equipe; o Application não o conhece.
public interface ILancamentoFixoFinanceiro
{
    // Gera o lote de lançamentos mensais do fixo a partir do mês informado
    Task GerarLoteAsync(LancamentoFixo fixo, DateTime desde);

    // Tira os lançamentos futuros ainda pendentes do fixo (antes de gerar de novo)
    Task LimparFuturosAsync(Guid lancamentoFixoId);
}
