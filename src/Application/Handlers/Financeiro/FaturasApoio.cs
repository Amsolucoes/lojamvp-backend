using LojaApi.Domain.Financeiro;
using LojaApi.Models;

namespace LojaApi.Application.Financeiro;

// Pedaços compartilhados pelos handlers e serviços das faturas do cartão.
internal static class FaturasApoio
{
    // Parcela de financiamento antigo que vence no mesmo mês de uma fatura paga parcialmente faz parte do total dessa fatura:
    // some de "A Pagar" (vira paga/absorvida). NaoAfetaSaldo evita descontar duas vezes da conta (o dinheiro que saiu é o ValorPago da fatura).
    public static void AbsorverParcela(LancamentoFinanceiro l, DateTime data, string motivo)
    {
        l.Status = "pago";
        l.PagoEm = data;
        l.NaoAfetaSaldo = true;
        l.Descricao = FaturaRegras.DescricaoDaParcelaAbsorvida(l.Descricao, data, motivo);
    }
}
