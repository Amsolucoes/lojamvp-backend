using System.ComponentModel.DataAnnotations;

namespace LojaApi.src.Models;

// ── Marca do produto (ex: Ipiranga, Bosch, NeuPar) — reaproveitável, com filtro ──
public class Marca
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid LojaId { get; set; }

    [Required, MaxLength(80)]
    public string Nome { get; set; } = "";

    public bool Ativo { get; set; } = true;
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}