using LojaApi.src.Models;

namespace LojaApi.src.Services;

public static class AjusteHorarioChacaraService
{
    public static decimal Obter(List<HorarioChacara> horarios, string tipo, string? hora)
    {
        if (string.IsNullOrWhiteSpace(hora)) return 0;
        return horarios.FirstOrDefault(h => h.Tipo == tipo && h.Hora == hora)?.Ajuste ?? 0;
    }
}
