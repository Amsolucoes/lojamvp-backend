using LojaApi.Models;

namespace LojaApi.src.Models;

public class HorarioChacara
{
    public int Id { get; set; }
    public Guid LojaId { get; set; }

    public string Tipo { get; set; } = "entrada"; // entrada | saida
    public string Hora { get; set; } = "08:00";     // "HH:mm"
    public decimal Ajuste { get; set; } = 0;         // soma ao valor calculado da diária — pode ser negativo (desconto)
    public int Ordem { get; set; }

    public Loja? Loja { get; set; }
}
