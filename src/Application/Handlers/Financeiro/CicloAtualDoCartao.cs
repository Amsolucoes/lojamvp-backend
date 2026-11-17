using LojaApi.Domain.Financeiro;
using LojaApi.Models;

namespace LojaApi.Application.Financeiro;

// A fatura "a pagar agora" de um cartão: a mais antiga já FECHADA e ainda não paga (bate com o que aparece em Contas a Pagar).
// Se não houver nenhuma pendente já fechada, cai no ciclo que ainda está acumulando (informativo).
public class CicloAtualDoCartao(IFaturasRepository faturas)
{
    public async Task<(DateTime Vencimento, decimal Total, string Status)> ObterAsync(CartaoCredito cartao)
    {
        var hoje = DateTime.UtcNow;

        for (int offset = -3; offset <= 0; offset++)
        {
            var refBase = new DateTime(hoje.Year, hoje.Month, 1).AddMonths(offset);
            var vencimentoCheck = CartaoRegras.VencimentoDaFatura(cartao.DiaVencimento, refBase.Year, refBase.Month);
            var cicloCheck = CartaoRegras.CicloDaFatura(cartao.DiaFechamento, vencimentoCheck);

            var cicloJaFechou = hoje.Date > cicloCheck.Fim.Date;
            if (!cicloJaFechou) continue;

            var totalCheck = await faturas.SomarComprasDoCicloAsync(cartao.Id, cicloCheck.Inicio, cicloCheck.Fim);
            if (totalCheck <= 0) continue;

            var faturaCheck = await faturas.ObterFaturaDoMesAsync(cartao.Id, vencimentoCheck.Year, vencimentoCheck.Month);
            if (faturaCheck?.Status == "pago") continue;

            return (vencimentoCheck, totalCheck, faturaCheck?.Status ?? "pendente");
        }

        // Nenhuma fatura fechada pendente — mostra o ciclo em aberto (só informativo)
        var (vencimento, ciclo) = CicloEmAberto(cartao, hoje);

        var total = await faturas.SomarComprasDoCicloAsync(cartao.Id, ciclo.Inicio, ciclo.Fim);
        var fatura = await faturas.ObterFaturaDoMesAsync(cartao.Id, vencimento.Year, vencimento.Month);
        return (vencimento, total, fatura?.Status ?? "pendente");
    }

    // Vencimento e ciclo da fatura em que "hoje" cai (procura até 3 meses à frente)
    public static (DateTime Vencimento, (DateTime Inicio, DateTime Fim) Ciclo) CicloEmAberto(CartaoCredito cartao, DateTime hoje) =>
        CartaoRegras.CicloEmAberto(cartao.DiaFechamento, cartao.DiaVencimento, hoje);
}
