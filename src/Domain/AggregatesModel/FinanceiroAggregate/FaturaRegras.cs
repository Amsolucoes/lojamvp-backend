using System.Globalization;
using System.Text.RegularExpressions;

namespace LojaApi.Domain.Financeiro;

// Regras puras da fatura do cartão (sem banco): texto das parcelas absorvidas, juros do saldo rotativo e do financiamento,
// validações do pagamento parcial e do parcelamento, primeiro mês das parcelas e a correção de vencimento das parcelas antigas.
public static class FaturaRegras
{
    public const int MinParcelasDoFinanciamento = 2;
    public const int MaxParcelasDoFinanciamento = 24;
    private const int LimiteDaDescricao = 150;

    private static readonly string[] Meses = ["Jan", "Fev", "Mar", "Abr", "Mai", "Jun", "Jul", "Ago", "Set", "Out", "Nov", "Dez"];
    private static readonly Regex SufixoDeAbsorcao = new(@"\s*\(absorvida [^)]*\)$");
    private static readonly Regex SufixoDeRefinanciamento = new(@"\s*\(absorvida no refinanciamento de \d{2}/\d{2}/\d{4}\)$");

    public static string AbreviacaoDoMes(int mes) => Meses[mes - 1];

    // "Descrição (absorvida no <motivo> de dd/MM/yyyy)": troca um sufixo de absorção anterior e respeita o limite da coluna (150)
    public static string DescricaoDaParcelaAbsorvida(string descricao, DateTime data, string motivo) =>
        ComSufixo(SufixoDeAbsorcao.Replace(descricao, ""), $" (absorvida no {motivo} de {data.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)})");

    // Igual, mas só troca o sufixo de um refinanciamento anterior (ex.: tentativa desfeita), para não empilhar texto sem limite
    public static string DescricaoDaParcelaRefinanciada(string descricao, DateTime data) =>
        ComSufixo(SufixoDeRefinanciamento.Replace(descricao, ""), $" (absorvida no refinanciamento de {data.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)})");

    public static bool FoiAbsorvidaNoPagamentoParcial(string descricao) => descricao.Contains("(absorvida no pagamento parcial");

    public static string RemoverSufixoDeAbsorcao(string descricao) => SufixoDeAbsorcao.Replace(descricao, "");

    private static string ComSufixo(string descricaoBase, string sufixo)
    {
        var espacoDisponivel = LimiteDaDescricao - sufixo.Length;
        if (descricaoBase.Length > espacoDisponivel)
            descricaoBase = descricaoBase.Substring(0, Math.Max(0, espacoDisponivel));
        return $"{descricaoBase}{sufixo}";
    }

    // Pagamento parcial: maior que zero e menor que o total devido
    public static bool PagamentoParcialValido(decimal pago, decimal totalDevido) => pago > 0 && pago < totalDevido;

    // Entrada do parcelamento: de zero até menos que o total a financiar
    public static bool EntradaValida(decimal entrada, decimal totalParaFinanciar) => entrada >= 0 && entrada < totalParaFinanciar;

    public static bool TotalDeParcelasDoFinanciamentoValido(int parcelas) => parcelas is >= MinParcelasDoFinanciamento and <= MaxParcelasDoFinanciamento;

    // O que sobrou da fatura vira um item na próxima, com os juros mensais do cartão
    public static decimal SaldoRotativoComJuros(decimal restante, decimal taxaJurosMensal) => restante * (1 + (taxaJurosMensal / 100m));

    // Parcela do financiamento da fatura: juros simples de (taxa x parcelas), dividido pelas parcelas, arredondado em centavos
    public static decimal ValorDaParcelaFinanciada(decimal valorFinanciar, decimal taxaJurosMensal, int parcelas)
    {
        var comJuros = valorFinanciar * (1 + (taxaJurosMensal / 100m) * parcelas);
        return Math.Round(comJuros / parcelas, 2);
    }

    // A 1ª parcela precisa cair num ciclo que ainda não fechou: avança mês a mês (até 12) enquanto o ciclo do mês já fechou antes de hoje
    public static DateTime PrimeiroMesDasParcelas(int diaFechamento, int diaVencimento, DateTime mesInicio, DateTime hoje)
    {
        for (int tentativa = 0; tentativa < 12; tentativa++)
        {
            var vencimentoTeste = CartaoRegras.VencimentoDaFatura(diaVencimento, mesInicio.Year, mesInicio.Month);
            var (_, fimTeste) = CartaoRegras.CicloDaFatura(diaFechamento, vencimentoTeste);
            if (hoje.Date <= fimTeste.Date) break; // ciclo ainda não fechou — pode usar
            mesInicio = mesInicio.AddMonths(1);
        }
        return mesInicio;
    }

    // Mês de vencimento da 1ª parcela de um financiamento, recalculado SEMPRE do zero a partir da criação dela (idempotente):
    // o do mês da criação, ou o seguinte se o ciclo desse mês já tinha fechado antes de ela nascer.
    public static DateTime MesBaseDoFinanciamento(int diaFechamento, int diaVencimento, DateTime criadoEm)
    {
        var mesBase = new DateTime(criadoEm.Year, criadoEm.Month, 1);
        for (int tentativa = 0; tentativa < 12; tentativa++)
        {
            var vencimentoTeste = CartaoRegras.VencimentoDaFatura(diaVencimento, mesBase.Year, mesBase.Month);
            var (_, fimTeste) = CartaoRegras.CicloDaFatura(diaFechamento, vencimentoTeste);
            if (criadoEm.Date <= fimTeste.Date) break; // ciclo ainda não tinha fechado quando a parcela nasceu — usa esse
            mesBase = mesBase.AddMonths(1);
        }
        return mesBase;
    }

    // Vencimento certo da parcela de número N: N-1 meses depois do mês base, no dia de vencimento do cartão
    public static DateTime VencimentoCorrigidoDaParcela(int diaVencimento, DateTime mesBase, int? numeroParcela)
    {
        var mesParcela = mesBase.AddMonths((numeroParcela ?? 1) - 1);
        return CartaoRegras.VencimentoDaFatura(diaVencimento, mesParcela.Year, mesParcela.Month);
    }
}
