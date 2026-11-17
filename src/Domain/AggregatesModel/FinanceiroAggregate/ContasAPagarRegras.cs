namespace LojaApi.Domain.Financeiro;

// Regras puras de Contas a Pagar (sem banco): o período consultado, os meses de fatura a checar e o que conta como pendente na fatura.
public static class ContasAPagarRegras
{
    private const int LimiteDeMeses = 36;

    // Período consultado: se vierem "de" e "até", vale o intervalo (o dia "até" inclusive); senão, o mês (o informado ou o atual)
    public static (DateTime Inicio, DateTime Fim) PeriodoConsultado(int? ano, int? mes, DateTime? de, DateTime? ate, DateTime hoje)
    {
        if (de.HasValue && ate.HasValue)
            return (DateTime.SpecifyKind(de.Value.Date, DateTimeKind.Utc), DateTime.SpecifyKind(ate.Value.Date, DateTimeKind.Utc).AddDays(1));

        var inicio = new DateTime(ano ?? hoje.Year, mes ?? hoje.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        return (inicio, inicio.AddMonths(1));
    }

    // Meses (no máximo 36) cujas faturas podem vencer dentro do período
    public static List<(int Ano, int Mes)> MesesDoPeriodo(DateTime inicio, DateTime fim)
    {
        var meses = new List<(int Ano, int Mes)>();
        var cursor = new DateTime(inicio.Year, inicio.Month, 1);
        var limite = new DateTime(fim.AddDays(-1).Year, fim.AddDays(-1).Month, 1);
        while (cursor <= limite && meses.Count < LimiteDeMeses)
        {
            meses.Add((cursor.Year, cursor.Month));
            cursor = cursor.AddMonths(1);
        }
        return meses;
    }

    // Fatura paga, financiada ou paga em parte: as compras do ciclo dela já foram resolvidas e não contam mais como pendência
    public static bool ComprasResolvidas(string? statusDaFatura) => statusDaFatura is "pago" or "financiada" or "parcial";

    // Compras ainda pendentes: o total do ciclo menos o que já foi antecipado (nunca negativo); zero se já resolvidas
    public static decimal ComprasPendentes(decimal totalDoCiclo, decimal antecipado, bool resolvidas) =>
        resolvidas ? 0 : Math.Max(0, totalDoCiclo - antecipado);
}
