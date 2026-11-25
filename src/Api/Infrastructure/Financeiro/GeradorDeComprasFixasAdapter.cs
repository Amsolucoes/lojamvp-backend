using LojaApi.Application.Financeiro;
using LojaApi.Models;
using LojaApi.Services;

namespace LojaApi.Infrastructure.Financeiro;

// Liga a porta do Application ao FinanceiroService (que também serve os serviços em segundo plano).
public class GeradorDeComprasFixasAdapter(FinanceiroService financeiro) : IGeradorDeComprasFixas
{
    public Task GerarLoteAsync(CartaoLancamentoFixo fixo, CartaoCredito cartao, DateTime desde) => financeiro.GerarLoteFixoCartaoAsync(fixo, cartao, desde);

    public Task LimparFuturosAsync(Guid cartaoFixoId) => financeiro.LimparFuturosCartaoAsync(cartaoFixoId);
}
