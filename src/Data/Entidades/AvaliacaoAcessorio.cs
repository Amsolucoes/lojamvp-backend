using System.ComponentModel.DataAnnotations;

namespace LojaApi.src.Models;

public class AvaliacaoAcessorio
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ProdutoId { get; set; }
    public ProdutoAcessorio? Produto { get; set; }

    public Guid PedidoId { get; set; }
    public PedidoAcessorio? Pedido { get; set; }

    [Required, MaxLength(150)]
    public string ClienteNome { get; set; } = "";

    [Range(1, 5)]
    public int Nota { get; set; }

    [MaxLength(1000)]
    public string? Comentario { get; set; }

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}