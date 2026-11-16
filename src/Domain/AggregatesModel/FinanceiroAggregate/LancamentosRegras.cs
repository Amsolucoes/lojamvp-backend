namespace LojaApi.Domain.Financeiro;

// Regras puras dos lançamentos financeiros (sem banco): vencimento, dia dos fixos, parcelamento e divisão em parcelas de cartão.
public static class LancamentosRegras
{
    public const int MinParcelasParcelamento = 2;
    public const int MaxParcelasParcelamento = 120;
    public const int MinTotalAlterado = 1;
    public const int MaxTotalAlterado = 360;
    public const int MaxParcelasNoCartao = 24;
    public const int DiaPadraoDoFixo = 10;

    // Vencimentos e datas de pagamento escolhidas pelo usuário ficam ao meio-dia (UTC), para não "virar o dia" por fuso
    public static DateTime AoMeioDia(DateTime data) => DateTime.SpecifyKind(data.Date, DateTimeKind.Utc).AddHours(12);

    // Dia de vencimento do fixo: de 1 a 28 (para existir em todo mês); fora disso vale 10
    public static int DiaDoFixo(int dia) => dia is >= 1 and <= 28 ? dia : DiaPadraoDoFixo;

    // Total de parcelas do parcelamento: se há data fim, são os meses entre o primeiro vencimento e ela (inclusive); senão, o informado
    public static int TotalDeParcelas(DateTime primeiroVencimento, DateTime? dataFim, int? totalInformado) =>
        dataFim.HasValue
            ? ((dataFim.Value.Year - primeiroVencimento.Year) * 12) + (dataFim.Value.Month - primeiroVencimento.Month) + 1
            : totalInformado ?? 0;

    public static bool TotalDeParcelasValido(int total) => total is >= MinParcelasParcelamento and <= MaxParcelasParcelamento;

    public static bool TotalAlteradoValido(int total) => total is >= MinTotalAlterado and <= MaxTotalAlterado;

    // Mantém o mês e troca o dia do vencimento (limitado ao último dia do mês), ao meio-dia
    public static DateTime ReajustarDia(DateTime vencimento, int novoDia)
    {
        var diasNoMes = DateTime.DaysInMonth(vencimento.Year, vencimento.Month);
        return new DateTime(vencimento.Year, vencimento.Month, Math.Min(novoDia, diasNoMes), 12, 0, 0, DateTimeKind.Utc);
    }

    public static DateTime PrimeiroDiaDoMes(DateTime data) => new(data.Year, data.Month, 1, 0, 0, 0, DateTimeKind.Utc);

    // Divide o valor em N parcelas de cartão: a última absorve os centavos da divisão
    public static decimal[] ValoresDasParcelasDoCartao(decimal valor, int parcelas)
    {
        var valorParcela = Math.Round(valor / parcelas, 2);
        var valores = new decimal[parcelas];
        for (int i = 0; i < parcelas; i++)
            valores[i] = i == parcelas - 1 ? valor - valorParcela * (parcelas - 1) : valorParcela;
        return valores;
    }
}
