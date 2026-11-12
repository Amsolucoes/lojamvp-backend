using System.Text.RegularExpressions;
using LojaApi.Models;

namespace LojaApi.Domain.Chacara;

public class HorarioChacara
{
    private static readonly Regex HoraValida = new(@"^([01]\d|2[0-3]):[0-5]\d$");

    public int Id { get; set; }
    public Guid LojaId { get; set; }

    public string Tipo { get; set; } = "entrada"; // entrada | saida
    public string Hora { get; set; } = "08:00";     // "HH:mm"
    public decimal Ajuste { get; set; } = 0;         // soma ao valor calculado da diária — pode ser negativo (desconto)
    public int Ordem { get; set; }

    public Loja? Loja { get; set; }

    // Regra: tipo entrada/saida e hora no formato HH:mm. Devolve a mensagem de erro, ou null se estiver tudo certo.
    public static string? Validar(string tipo, string hora)
    {
        if (tipo != "entrada" && tipo != "saida")
            return "Tipo deve ser 'entrada' ou 'saida'.";
        if (!HoraValida.IsMatch(hora))
            return "Informe um horário válido (HH:mm).";
        return null;
    }

    public static HorarioChacara Criar(Guid lojaId, string tipo, string hora, decimal ajuste, int ordem) => new()
    {
        LojaId = lojaId, Tipo = tipo, Hora = hora, Ajuste = ajuste, Ordem = ordem,
    };

    public void Atualizar(string tipo, string hora, decimal ajuste, int ordem)
    {
        Tipo = tipo; Hora = hora; Ajuste = ajuste; Ordem = ordem;
    }
}
