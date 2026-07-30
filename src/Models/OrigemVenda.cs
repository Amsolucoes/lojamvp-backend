using System.ComponentModel.DataAnnotations;

namespace LojaApi.Models;

// ── Origem da venda (Loja física, Site, WhatsApp, etc. — editável pelo lojista) ──
public class OrigemVenda
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid LojaId { get; set; }

    [Required, MaxLength(50)]
    public string Nome { get; set; } = "";

    public int Ordem { get; set; } = 0;
    public bool Ativa { get; set; } = true;
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}