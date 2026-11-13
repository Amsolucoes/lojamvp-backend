using LojaApi.Application.Equipe;
using LojaApi.Models;
using LojaApi.Services;

namespace LojaApi.Infrastructure.Equipe;

// Liga a porta do Application ao FinanceiroService (o Financeiro ainda é um serviço da API, não migrado).
// Usa o mesmo AppDbContext da requisição, então o que ele gera é gravado junto pelo repositório da Equipe.
public class LancamentoFixoFinanceiroAdapter(FinanceiroService financeiro) : ILancamentoFixoFinanceiro
{
    public Task GerarLoteAsync(LancamentoFixo fixo, DateTime desde) => financeiro.GerarLoteFixoAsync(fixo, desde);

    public Task LimparFuturosAsync(Guid lancamentoFixoId) => financeiro.LimparFuturosAsync(lancamentoFixoId);
}
