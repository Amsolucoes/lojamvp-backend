using LojaApi.Application.Financeiro;
using LojaApi.Models;
using LojaApi.Services;

namespace LojaApi.Infrastructure.Financeiro;

// Liga a porta do Application ao FinanceiroService (que também serve os serviços em segundo plano e os fixos de cartão).
public class GeradorDeLancamentosFixosAdapter(FinanceiroService financeiro) : IGeradorDeLancamentosFixos
{
    public Task GerarLoteAsync(LancamentoFixo fixo, DateTime desde) => financeiro.GerarLoteFixoAsync(fixo, desde);

    public Task LimparFuturosAsync(Guid lancamentoFixoId) => financeiro.LimparFuturosAsync(lancamentoFixoId);
}
