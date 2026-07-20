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
    public DateTime? UltimoLoginEm { get; set; }
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

    [Column(TypeName = "decimal(10,3)")]
    public decimal Estoque { get; set; }

    [Column(TypeName = "decimal(10,3)")]
    public decimal EstoqueMinimo { get; set; } = 3;

    [MaxLength(15)]
    public string TipoVenda { get; set; } = "unidade"; // unidade | fracionado

    [MaxLength(5)]
    public string UnidadeMedida { get; set; } = "un";  // un | kg | g | L | ml

    [MaxLength(50)]
    public string? CodigoBarras { get; set; }

    public bool Ativo { get; set; } = true;

    public Guid? LojaId { get; set; }
    public Loja? Loja { get; set; }

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public DateTime AtualizadoEm { get; set; } = DateTime.UtcNow;

    // Navegação
    public ICollection<MovimentoEstoque> Movimentos { get; set; } = [];
    public ICollection<ItemVenda> ItensVenda { get; set; } = [];
    public ICollection<ProdutoVariacao> Variacoes { get; set; } = [];
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

    public DateTime? DataNascimento { get; set; }

    [MaxLength(300)]
    public string? Endereco { get; set; }

    [MaxLength(500)]
    public string? Observacoes { get; set; }

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    public Guid? LojaId { get; set; }

    public Loja? Loja { get; set; }

    public decimal CreditoLoja { get; set; } = 0;

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

    public Guid? LojaId { get; set; }
    public Loja? Loja { get; set; }

    [MaxLength(500)]
    public string? FormasPagamento { get; set; }

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

    public Guid? ProdutoId { get; set; }
    public Produto? Produto { get; set; }

    public Guid? ServicoId { get; set; }
    public Servico? Servico { get; set; }

    [Required, MaxLength(200)]
    public string NomeProduto { get; set; } = "";

    [Column(TypeName = "decimal(10,3)")]
    public decimal Quantidade { get; set; }

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

    [Column(TypeName = "decimal(10,3)")]
    public decimal Quantidade { get; set; }

    [MaxLength(300)]
    public string? Observacao { get; set; }

    public Guid? LojaId { get; set; }
    public Loja? Loja { get; set; }

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}

// ── Perfil de loja (template) ─────────────────────────────────────
public class PerfilLoja
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(100)]
    public string Nome { get; set; } = "";

    [MaxLength(300)]
    public string? Descricao { get; set; }

    [MaxLength(50)]
    public string Icone { get; set; } = "🏪";

    public bool Ativo { get; set; } = true;

    [MaxLength(20)]
    public string TipoPlanoAplica { get; set; } = "loja";
    public ICollection<ServicoPerfilLoja> Servicos { get; set; } = [];

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    public ICollection<CategoriaPerfilLoja> Categorias { get; set; } = [];
    public ICollection<CampoExtraPerfil> CamposExtras { get; set; } = [];
}

public class CategoriaPerfilLoja
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PerfilLojaId { get; set; }
    public PerfilLoja PerfilLoja { get; set; } = null!;

    [Required, MaxLength(100)]
    public string Nome { get; set; } = "";
    public int Ordem { get; set; } = 0;

    [MaxLength(20)]
    public string TipoTamanho { get; set; } = "letra"; 
}

public class ServicoPerfilLoja
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PerfilLojaId { get; set; }
    public PerfilLoja? PerfilLoja { get; set; }

    [Required, MaxLength(150)]
    public string Nome { get; set; } = "";

    [MaxLength(50)]
    public string Categoria { get; set; } = "Geral";

    [Column(TypeName = "decimal(10,2)")]
    public decimal Preco { get; set; }

    public int DuracaoMin { get; set; } = 30;
    public int Ordem { get; set; }
}

public class CampoExtraPerfil
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PerfilLojaId { get; set; }
    public PerfilLoja PerfilLoja { get; set; } = null!;

    [Required, MaxLength(50)]
    public string Chave { get; set; } = "";

    [Required, MaxLength(100)]
    public string Label { get; set; } = "";

    [MaxLength(20)]
    public string Tipo { get; set; } = "texto";

    [MaxLength(500)]
    public string? Opcoes { get; set; }

    public bool Obrigatorio { get; set; } = false;
    public int Ordem { get; set; } = 0;
}

public class CategoriaLoja
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid LojaId { get; set; }
    public Loja Loja { get; set; } = null!;

    [Required, MaxLength(100)]
    public string Nome { get; set; } = "";
    public bool Ativo { get; set; } = true;
    public int Ordem { get; set; } = 0;

    [MaxLength(20)]
    public string TipoTamanho { get; set; } = "letra"; // letra | numero | personalizado

    // Configuração da grade
    public bool UsaTamanho { get; set; } = true;
    public bool UsaCor { get; set; } = true;

    [MaxLength(300)]
    public string? TamanhosPersonalizados { get; set; } // ex: "Único,Bebê,Infantil"

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}

public class CampoExtraLoja
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid LojaId { get; set; }
    public Loja Loja { get; set; } = null!;

    [Required, MaxLength(50)]
    public string Chave { get; set; } = "";

    [Required, MaxLength(100)]
    public string Label { get; set; } = "";

    [MaxLength(20)]
    public string Tipo { get; set; } = "texto";

    [MaxLength(500)]
    public string? Opcoes { get; set; }

    public bool Obrigatorio { get; set; } = false;
    public bool Ativo { get; set; } = true;
    public int Ordem { get; set; } = 0;
}

// ── Variação de produto ───────────────────────────────────────────
public class ProdutoVariacao
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ProdutoId { get; set; }
    public Produto Produto { get; set; } = null!;

    [MaxLength(100)]
    public string? Tamanho { get; set; }

    [MaxLength(100)]
    public string? Cor { get; set; }

    [MaxLength(100)]
    public string? OutroCampo { get; set; } // para campos extras futuros

    public int Estoque { get; set; } = 0;
    public int EstoqueMinimo { get; set; } = 1;
    public bool Ativo { get; set; } = true;
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public DateTime AtualizadoEm { get; set; } = DateTime.UtcNow;
}

// ── Troca ─────────────────────────────────────────────────────────
public class Troca
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ClienteId { get; set; }
    public Cliente Cliente { get; set; } = null!;

    public decimal TotalDevolvido { get; set; }  // soma dos produtos devolvidos
    public decimal TotalNovo { get; set; }  // soma dos produtos novos
    public decimal Diferenca { get; set; }  // novo - devolvido (positivo = cliente paga, negativo = crédito)
    public decimal CreditoGerado { get; set; }  // crédito gerado se sobrou
    public string? FormaPagamento { get; set; }  // se pagou diferença
    public Guid? LojaId { get; set; }
    public DateTime CriadaEm { get; set; } = DateTime.UtcNow;

    public ICollection<ItemTroca> Itens { get; set; } = [];
}

public class ItemTroca
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TrocaId { get; set; }
    public Troca Troca { get; set; } = null!;

    public Guid ProdutoId { get; set; }

    [MaxLength(200)]
    public string NomeProduto { get; set; } = "";

    public Guid? VariacaoId { get; set; }
    public int Quantidade { get; set; }
    public decimal PrecoUnitario { get; set; }

    [MaxLength(20)]
    public string Tipo { get; set; } = "devolvido"; // devolvido | novo

    public bool VoltaEstoque { get; set; } = true; // só para devolvidos
}

// ── Plano oferecido pela loja (ex: "Cabelo + Barba") ──────────────
public class Plano
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid LojaId { get; set; }

    [MaxLength(100)]
    public string Nome { get; set; } = "";

    public decimal Valor { get; set; }

    // IDs dos serviços incluídos, separados por vírgula (ex: "guid1,guid2")
    public string? ServicosIds { get; set; }

    public bool Ativo { get; set; } = true;

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}

// ── Cliente vinculado a um plano ──────────────────────────────────
public class AssinaturaCliente
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid LojaId { get; set; }
    public Guid ClienteId { get; set; }
    public Guid PlanoId { get; set; }
    public int DiaVencimento { get; set; } = 10;
    public DateTime DataInicio { get; set; } = DateTime.UtcNow;
    // Primeiro dia do mês em que a cobrança deve começar (ex: 2026-08-01)
    public DateTime MesInicioCobranca { get; set; } = DateTime.UtcNow;
    [MaxLength(20)]
    public string Status { get; set; } = "ativa"; // ativa | cancelada
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}

// ── Controle de pagamento mensal do plano ─────────────────────────
public class PagamentoPlano
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid AssinaturaId { get; set; }
    public Guid LojaId { get; set; }

    // Mês de referência (ex: 2026-07-01 representa julho/2026)
    public DateTime MesReferencia { get; set; }

    public decimal Valor { get; set; }

    public Guid? VendaId { get; set; }

    [MaxLength(20)]
    public string Status { get; set; } = "pendente"; // pago | pendente

    public DateTime? PagoEm { get; set; }

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}

// ── Registro de uso de serviço incluso no plano ───────────────────
public class ConsumoPlano
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AssinaturaId { get; set; }
    public Guid LojaId { get; set; }
    public Guid ServicoId { get; set; }
    [MaxLength(150)]
    public string NomeServico { get; set; } = "";
    public Guid? VendaId { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}