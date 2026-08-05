using System.ComponentModel.DataAnnotations;

namespace LojaApi.src.Models;

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
}