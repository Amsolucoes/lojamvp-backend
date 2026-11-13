namespace LojaApi.Domain.Chacara;

// Padrão do projeto para datas de reserva/período: sempre UTC, ao meio-dia (evita virar o dia por fuso horário).
public static class DiaDeReserva
{
    public static DateTime MeioDiaUtc(DateTime data) =>
        DateTime.SpecifyKind(data.Date, DateTimeKind.Utc).AddHours(12);
}
