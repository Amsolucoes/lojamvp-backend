using System.ComponentModel.DataAnnotations;

namespace LojaApi.src.Models;

// ── Categoria dos vídeos da Central de Ajuda (gerenciável pelo admin) ──
public class CategoriaVideoAjuda
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(60)]
    public string Nome { get; set; } = "";

    public int Ordem { get; set; } = 0;
    public bool Ativa { get; set; } = true;
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}