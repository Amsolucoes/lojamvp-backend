namespace LojaApi.Domain.Financeiro;

// Regras puras dos cartões de crédito (sem banco): bandeira, dia do fixo, parcelas e o ciclo de cada fatura.
public static class CartaoRegras
{
    public const int MinParcelasCompra = 2;
    public const int MaxParcelasCompra = 24;
    public const int DiaPadraoDaCompraFixa = 1;

    private static readonly HashSet<string> BandeirasValidas = ["visa", "mastercard", "elo", "amex", "hipercard", "diners"];

    // Bandeira só para exibir o logo: reconhece as conhecidas (sem maiúscula nem espaços); qualquer outra vira nula
    public static string? NormalizarBandeira(string? valor)
    {
        var b = valor?.Trim().ToLowerInvariant();
        return b is not null && BandeirasValidas.Contains(b) ? b : null;
    }

    // Dia da compra do fixo do cartão: de 1 a 28 (para existir em todo mês); fora disso vale 1
    public static int DiaDaCompraFixa(int dia) => dia is >= 1 and <= 28 ? dia : DiaPadraoDaCompraFixa;

    public static bool TotalDeParcelasDaCompraValido(int total) => total is >= MinParcelasCompra and <= MaxParcelasCompra;

    // Vencimento da fatura do mês (o dia do vencimento é limitado ao último dia do mês), ao meio-dia
    public static DateTime VencimentoDaFatura(int diaVencimento, int ano, int mes) =>
        new(ano, mes, Math.Min(diaVencimento, DateTime.DaysInMonth(ano, mes)), 12, 0, 0, DateTimeKind.Utc);

    // Ciclo da fatura: do dia seguinte ao fechamento anterior até o dia de fechamento atual, inclusive.
    // O fechamento é 1 mês antes do vencimento, no dia de fechamento.
    public static (DateTime Inicio, DateTime Fim) CicloDaFatura(int diaFechamento, DateTime vencimento)
    {
        var fechamentoAtual = new DateTime(vencimento.Year, vencimento.Month, Math.Min(diaFechamento, DateTime.DaysInMonth(vencimento.Year, vencimento.Month)), 0, 0, 0, DateTimeKind.Utc);
        var fechamentoAnterior = fechamentoAtual.AddMonths(-1);
        return (fechamentoAnterior.AddDays(1), fechamentoAtual);
    }

    // Vencimento e ciclo da fatura em que "hoje" cai (procura até 3 meses à frente)
    public static (DateTime Vencimento, (DateTime Inicio, DateTime Fim) Ciclo) CicloEmAberto(int diaFechamento, int diaVencimento, DateTime hoje)
    {
        int ano = hoje.Year, mes = hoje.Month;
        DateTime vencimento = default;
        (DateTime Inicio, DateTime Fim) ciclo = default;
        for (int i = 0; i < 3; i++)
        {
            vencimento = VencimentoDaFatura(diaVencimento, ano, mes);
            ciclo = CicloDaFatura(diaFechamento, vencimento);
            if (hoje.Date >= ciclo.Inicio.Date && hoje.Date <= ciclo.Fim.Date) break;
            mes++;
            if (mes > 12) { mes = 1; ano++; }
        }
        return (vencimento, ciclo);
    }

    // Vencimento da fatura em que a compra cai: a do mês da compra, ou a do mês seguinte se a compra ficou depois do fechamento
    public static DateTime VencimentoDaFaturaDaCompra(int diaFechamento, int diaVencimento, DateTime dataCompra)
    {
        var vencimento = VencimentoDaFatura(diaVencimento, dataCompra.Year, dataCompra.Month);
        var (inicio, fim) = CicloDaFatura(diaFechamento, vencimento);
        if (dataCompra.Date < inicio.Date || dataCompra.Date > fim.Date)
        {
            var proximo = dataCompra.AddMonths(1);
            vencimento = VencimentoDaFatura(diaVencimento, proximo.Year, proximo.Month);
        }
        return vencimento;
    }
}
