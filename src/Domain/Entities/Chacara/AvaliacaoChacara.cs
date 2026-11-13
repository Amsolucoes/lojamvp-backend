using System.ComponentModel.DataAnnotations;

namespace LojaApi.Domain.Chacara;

public class AvaliacaoChacara
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public int ReservaId { get; set; }
    public Reserva? Reserva { get; set; }

    [Range(1, 5)]
    public int Nota { get; set; }

    [MaxLength(1000)]
    public string? Comentario { get; set; }

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    public static bool NotaValida(int nota) => nota >= 1 && nota <= 5;

    private static string? Limpar(string? texto) => string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();

    public static AvaliacaoChacara Criar(int reservaId, int nota, string? comentario) => new()
    {
        ReservaId = reservaId, Nota = nota, Comentario = Limpar(comentario),
    };

    public void Atualizar(int nota, string? comentario)
    {
        Nota = nota;
        Comentario = Limpar(comentario);
    }
}