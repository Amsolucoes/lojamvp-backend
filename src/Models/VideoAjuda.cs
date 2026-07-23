using System.ComponentModel.DataAnnotations;

namespace LojaApi.src.Models;

public class VideoAjuda
{
    public Guid Id { get; set; } = Guid.NewGuid();
    [Required, MaxLength(150)]
    public string Titulo { get; set; } = "";
    [Required, MaxLength(50)]
    public string Categoria { get; set; } = ""; // Produtos, Caixa, Estoque, Financeiro, etc.
    [Required, MaxLength(20)]
    public string YoutubeId { get; set; } = "";
    public int Ordem { get; set; } = 0;
    public bool Ativo { get; set; } = true;
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}