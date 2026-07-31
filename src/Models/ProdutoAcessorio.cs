using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LojaApi.src.Models;

// ── Produto físico vendido pela AlDevSoftware (leitor de código de barras, impressora, etc.) ──
public class ProdutoAcessorio
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(150)]
    public string Nome { get; set; } = "";

    [MaxLength(2000)]
    public string? Descricao { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal Preco { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal? PrecoPromocional { get; set; }

    public int Estoque { get; set; } = 0;

    [MaxLength(30)]
    public string Categoria { get; set; } = "outro"; // leitor_codigo_barras | impressora_fiscal | impressora_etiquetas | outro

    // URLs de imagem separadas por vírgula (a primeira é a capa)
    [MaxLength(1000)]
    public string? ImagensUrls { get; set; }

    [Column(TypeName = "decimal(10,3)")]
    public decimal? PesoKg { get; set; } // usado no cálculo de frete real, quando implementado

    public bool Ativo { get; set; } = true;
    public int Ordem { get; set; } = 0;
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public DateTime AtualizadoEm { get; set; } = DateTime.UtcNow;
}

// ── Pedido de compra de acessório (venda avulsa, não recorrente) ──
public class PedidoAcessorio
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(150)]
    public string ClienteNome { get; set; } = "";

    [Required, MaxLength(150)]
    public string ClienteEmail { get; set; } = "";

    [Required, MaxLength(20)]
    public string ClienteTelefone { get; set; } = "";

    [MaxLength(14)]
    public string? ClienteCpfCnpj { get; set; }

    // Endereço de entrega
    [Required, MaxLength(9)]
    public string Cep { get; set; } = "";
    [Required, MaxLength(200)]
    public string Endereco { get; set; } = "";
    [MaxLength(20)]
    public string? Numero { get; set; }
    [MaxLength(100)]
    public string? Complemento { get; set; }
    [MaxLength(100)]
    public string? Bairro { get; set; }
    [Required, MaxLength(100)]
    public string Cidade { get; set; } = "";
    [Required, MaxLength(2)]
    public string Uf { get; set; } = "";

    [Column(TypeName = "decimal(10,2)")]
    public decimal Subtotal { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal ValorFrete { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal Total { get; set; }

    [MaxLength(20)]
    public string Status { get; set; } = "aguardando_pagamento"; // aguardando_pagamento | pago | enviado | entregue | cancelado

    [MaxLength(50)]
    public string? CodigoRastreio { get; set; }

    // Integração Mercado Pago (checkout pagamento único)
    [MaxLength(100)]
    public string? MpPaymentId { get; set; }
    [MaxLength(50)]
    public string? MpStatus { get; set; }

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public DateTime? PagoEm { get; set; }
    public DateTime? EnviadoEm { get; set; }

    // Navegação
    public ICollection<ItemPedidoAcessorio> Itens { get; set; } = [];
}

public class ItemPedidoAcessorio
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid PedidoId { get; set; }
    public PedidoAcessorio Pedido { get; set; } = null!;

    public Guid ProdutoId { get; set; }
    public ProdutoAcessorio? Produto { get; set; }

    [Required, MaxLength(150)]
    public string NomeProduto { get; set; } = ""; // snapshot

    public int Quantidade { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal PrecoUnitario { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal Subtotal { get; set; }
}