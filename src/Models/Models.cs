using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LojaApi.Models;

// ── Usuario ───────────────────────────────────────────────────────
public class Usuario
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(100)]
    public string Nome { get; set; } = "";

    [Required, MaxLength(150)]
    public string Email { get; set; } = "";

    [Required]
    public string SenhaHash { get; set; } = "";

    [Required, MaxLength(20)]
    public string Role { get; set; } = "operador"; // admin | operador

    public bool Ativo { get; set; } = true;
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}

// ── Produto ───────────────────────────────────────────────────────
public class Produto
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(150)]
    public string Nome { get; set; } = "";

    [MaxLength(200)]
    public string? Descricao { get; set; }

    [Required, MaxLength(30)]
    public string Categoria { get; set; } = "outro"; // semi-joias | maquiagem | acessorios | outro

    [Column(TypeName = "decimal(10,2)")]
    public decimal PrecoCusto { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal PrecoVenda { get; set; }

    public int Estoque { get; set; }
    public int EstoqueMinimo { get; set; } = 3;

    [MaxLength(50)]
    public string? CodigoBarras { get; set; }

    public bool Ativo { get; set; } = true;
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public DateTime AtualizadoEm { get; set; } = DateTime.UtcNow;

    // Navegação
    public ICollection<MovimentoEstoque> Movimentos { get; set; } = [];
    public ICollection<ItemVenda> ItensVenda { get; set; } = [];
}

// ── Cliente ───────────────────────────────────────────────────────
public class Cliente
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(150)]
    public string Nome { get; set; } = "";

    [Required, MaxLength(20)]
    public string Telefone { get; set; } = "";

    [MaxLength(14)]
    public string? Cpf { get; set; }

    [MaxLength(150)]
    public string? Email { get; set; }

    [MaxLength(300)]
    public string? Endereco { get; set; }

    [MaxLength(500)]
    public string? Observacoes { get; set; }

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    // Navegação
    public ICollection<Venda> Vendas { get; set; } = [];
}

// ── Venda ─────────────────────────────────────────────────────────
public class Venda
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid? ClienteId { get; set; }
    public Cliente? Cliente { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal Total { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal Desconto { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal TotalFinal { get; set; }

    [Required, MaxLength(20)]
    public string FormaPagamento { get; set; } = "pix"; // dinheiro | pix | credito | debito

    [Column(TypeName = "decimal(10,2)")]
    public decimal? Troco { get; set; }

    public DateTime CriadaEm { get; set; } = DateTime.UtcNow;

    // Navegação
    public ICollection<ItemVenda> Itens { get; set; } = [];
}

// ── ItemVenda ─────────────────────────────────────────────────────
public class ItemVenda
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid VendaId { get; set; }
    public Venda Venda { get; set; } = null!;

    public Guid ProdutoId { get; set; }
    public Produto Produto { get; set; } = null!;

    public int Quantidade { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal PrecoUnitario { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal Subtotal { get; set; }
}

// ── MovimentoEstoque ──────────────────────────────────────────────
public class MovimentoEstoque
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ProdutoId { get; set; }
    public Produto Produto { get; set; } = null!;

    [Required, MaxLength(20)]
    public string Tipo { get; set; } = "entrada"; // entrada | saida | ajuste

    public int Quantidade { get; set; }

    [MaxLength(300)]
    public string? Observacao { get; set; }

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}
