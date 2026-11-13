namespace LojaApi.Domain.Servicos;

// Regras de uso de um plano de assinatura: limite de usos por mês e dias da semana permitidos.
// Dias da semana seguem o padrão do .NET: 0 = domingo ... 6 = sábado, guardados como "1,2,3,4,5".
public static class PlanoUsoRegras
{
    private static readonly TimeZoneInfo Fuso = ObterFuso();

    private static TimeZoneInfo ObterFuso()
    {
        foreach (var id in new[] { "America/Sao_Paulo", "E. South America Standard Time" })
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(id); } catch { /* tenta o próximo */ }
        }
        return TimeZoneInfo.CreateCustomTimeZone("BRT", TimeSpan.FromHours(-3), "BRT", "BRT");
    }

    public static DateTime ParaHorarioLocal(DateTime utc)
        => TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Fuso);

    // Início e fim (UTC) do mês em que a data cai, no horário local da loja
    public static (DateTime InicioUtc, DateTime FimUtc) IntervaloDoMesUtc(DateTime utc)
    {
        var local = ParaHorarioLocal(utc);
        var inicioLocal = new DateTime(local.Year, local.Month, 1, 0, 0, 0, DateTimeKind.Unspecified);
        var fimLocal = inicioLocal.AddMonths(1);
        return (TimeZoneInfo.ConvertTimeToUtc(inicioLocal, Fuso), TimeZoneInfo.ConvertTimeToUtc(fimLocal, Fuso));
    }

    // Início e fim (UTC) do mês de um horário que já está no horário local da loja (ex.: data/hora de um agendamento)
    public static (DateTime InicioUtc, DateTime FimUtc) IntervaloDoMesUtcDeHorarioLocal(DateTime local)
    {
        var inicioLocal = new DateTime(local.Year, local.Month, 1, 0, 0, 0, DateTimeKind.Unspecified);
        return (TimeZoneInfo.ConvertTimeToUtc(inicioLocal, Fuso), TimeZoneInfo.ConvertTimeToUtc(inicioLocal.AddMonths(1), Fuso));
    }

    public static bool DiaPermitidoLocal(string? dias, DateTime local)
    {
        var lista = LerDias(dias);
        return lista.Count == 0 || lista.Contains((int)local.DayOfWeek);
    }

    public static List<int> LerDias(string? dias)
        => (dias ?? "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => int.TryParse(s, out var n) ? n : -1)
            .Where(n => n is >= 0 and <= 6)
            .Distinct().OrderBy(n => n).ToList();

    // Normaliza o que veio da tela: sem dias, ou todos os 7 dias, significa "sem restrição" (null)
    public static string? NormalizarDias(IEnumerable<int>? dias)
    {
        var lista = (dias ?? []).Where(n => n is >= 0 and <= 6).Distinct().OrderBy(n => n).ToList();
        return lista.Count is 0 or 7 ? null : string.Join(",", lista);
    }

    public static bool DiaPermitido(string? dias, DateTime utc)
    {
        var lista = LerDias(dias);
        return lista.Count == 0 || lista.Contains((int)ParaHorarioLocal(utc).DayOfWeek);
    }

    public static string DescreverDias(string? dias)
    {
        var nomes = new[] { "domingo", "segunda", "terça", "quarta", "quinta", "sexta", "sábado" };
        return string.Join(", ", LerDias(dias).Select(d => nomes[d]));
    }

    // ── Regras que a Agenda e o Caixa aplicam em cima do plano ───────────────

    // Primeiro dia do mês (UTC) da data: mês de referência das mensalidades
    public static DateTime PrimeiroDiaDoMesUtc(DateTime agora)
        => new(agora.Year, agora.Month, 1, 0, 0, 0, DateTimeKind.Utc);

    // Os serviços do plano ficam guardados como "id1,id2,id3"
    public static string[] ServicosIncluidos(string? servicosIds)
        => (servicosIds ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries);

    public static bool Inclui(string? servicosIds, Guid servicoId)
        => ServicosIncluidos(servicosIds).Contains(servicoId.ToString(), StringComparer.OrdinalIgnoreCase);

    public static List<Guid> IdsDosServicos(string? servicosIds)
        => ServicosIncluidos(servicosIds).Select(s => Guid.TryParse(s, out var g) ? g : Guid.Empty).Where(g => g != Guid.Empty).ToList();

    // Mensagem de erro quando o dia do agendamento (horário local) não é um dos permitidos pelo plano
    public static string? ValidarDia(string nomePlano, string? dias, DateTime dataHoraLocal)
        => DiaPermitidoLocal(dias, dataHoraLocal)
            ? null
            : $"O plano '{nomePlano}' só pode ser usado em: {DescreverDias(dias)}. Escolha outro dia.";

    // Mensagem de erro quando mais um uso estouraria o limite do mês (usados = consumos já feitos; agendados = ainda não pagos)
    public static string? ValidarLimite(string nomePlano, int limiteUsosMes, int usados, int agendados)
        => usados + agendados + 1 > limiteUsosMes
            ? $"O plano '{nomePlano}' permite {limiteUsosMes} uso(s) por mês e já há {usados + agendados} usado(s) ou agendado(s) neste mês."
            : null;
}
