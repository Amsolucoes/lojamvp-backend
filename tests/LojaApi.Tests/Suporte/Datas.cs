namespace LojaApi.Tests.Suporte;

// Datas usadas nos testes. São calculadas a partir de hoje (sempre no futuro), para os testes não "envelhecerem".
public static class Datas
{
    // Primeira sexta-feira daqui a ~1 ano, mais N semanas. Sexta + sábado + domingo = "fim de semana" da chácara.
    public static DateTime Sexta(int semanasAFrente = 0)
    {
        var d = DateTime.UtcNow.Date.AddDays(365);
        while (d.DayOfWeek != DayOfWeek.Friday) d = d.AddDays(1);
        return d.AddDays(7 * semanasAFrente);
    }

    public static DateTime Segunda(int semanasAFrente = 0) => Sexta(semanasAFrente).AddDays(3);
    public static DateTime Sabado(int semanasAFrente = 0) => Sexta(semanasAFrente).AddDays(1);
    public static DateTime Domingo(int semanasAFrente = 0) => Sexta(semanasAFrente).AddDays(2);

    public static string Texto(DateTime d) => d.ToString("yyyy-MM-dd");
}
