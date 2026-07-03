using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LojaApi.Models;

// ── Usuario (já existe — adicionar campo LojaId opcional) ─────────
// Não alterar o modelo existente, apenas adicionar via UsuarioLoja

// ── Status da loja ────────────────────────────────────────────────
public enum StatusLoja
{
    Trial,
    Ativo,
    Bloqueado,
    Cancelado,
}

// ── Loja ──────────────────────────────────────────────────────────
public class Loja
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(150)]
    public string Nome { get; set; } = "";

    [MaxLength(18)]
    public string? Cnpj { get; set; }

    [MaxLength(14)]
    public string? Cpf { get; set; }

    [Required, MaxLength(150)]
    public string Email { get; set; } = "";

    [MaxLength(20)]
    public string? Telefone { get; set; }

    [MaxLength(200)]
    public string? Endereco { get; set; }

    // Personalização visual
    [MaxLength(7)]
    public string CorPrimaria { get; set; } = "#e8945a";

    [MaxLength(500)]
    public string? LogoUrl { get; set; }

    // Controle de acesso
    public StatusLoja Status { get; set; } = StatusLoja.Trial;
    public DateTime TrialAte { get; set; } = DateTime.UtcNow.AddDays(7);
    public int MensalidadeDia { get; set; } = 10;

    [Column(TypeName = "decimal(10,2)")]
    public decimal MensalidadeValor { get; set; } = 120.00m;

    // ── Promoção / desconto ───────────────────────────────────────
    public bool Promocional { get; set; } = false;

    [Column(TypeName = "decimal(10,2)")]
    public decimal? ValorPromocional { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal? ValorPosPromocional { get; set; }

    public int MesesPromocional { get; set; } = 0;

    // ── Módulos / Plano ───────────────────────────────────────────
    [MaxLength(20)]
    public string TipoPlano { get; set; } = "loja";

    public bool EhTeste { get; set; } = false;

    public int AgendaHoraInicio { get; set; } = 8;
    public int AgendaHoraFim { get; set; } = 18;

    [MaxLength(200)]
    public string ModulosAtivos { get; set; } = "";

    public DateTime? UltimaCobranca { get; set; }
    public DateTime? ProximoVencimento { get; set; }

    // ── Assinatura recorrente (Mercado Pago) ──────────────────────
    [MaxLength(100)]
    public string? MpPreapprovalId { get; set; }

    [MaxLength(20)]
    public string? AssinaturaStatus { get; set; } // authorized | paused | cancelled

    [MaxLength(4)]
    public string? AssinaturaCartaoFinal { get; set; } // últimos 4 dígitos (exibição)

    [MaxLength(50)]
    public string SchemaNome { get; set; } = "";

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public DateTime AtualizadoEm { get; set; } = DateTime.UtcNow;

    // Navegação
    public ICollection<UsuarioLoja> Usuarios { get; set; } = [];
    public ICollection<Pagamento> Pagamentos { get; set; } = [];
}

// ── Vínculo usuário ↔ loja ────────────────────────────────────────
public class UsuarioLoja
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid LojaId { get; set; }
    public Loja Loja { get; set; } = null!;

    public Guid UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;

    [MaxLength(20)]
    public string Role { get; set; } = "operador";

    public bool Ativo { get; set; } = true;
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}

// ── Pagamento ─────────────────────────────────────────────────────
public class Pagamento
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid LojaId { get; set; }
    public Loja Loja { get; set; } = null!;

    [Column(TypeName = "decimal(10,2)")]
    public decimal Valor { get; set; }

    [MaxLength(20)]
    public string Status { get; set; } = "pendente"; // pendente | pago | atrasado

    public DateTime Vencimento { get; set; }
    public DateTime? PagoEm { get; set; }

    [MaxLength(20)]
    public string? FormaPagamento { get; set; } // pix | boleto | cartao

    [MaxLength(300)]
    public string? Observacao { get; set; }

    // Mercado Pago
    [MaxLength(100)]
    public string? MpPaymentId { get; set; }

    [MaxLength(5000)]
    public string? MpQrCode { get; set; }       // QR Code do Pix

    [MaxLength(10000)]
    public string? MpQrCodeBase64 { get; set; } // QR Code imagem

    [MaxLength(200)]
    public string? MpBoletoUrl { get; set; }    // URL do boleto

    [MaxLength(200)]
    public string? MpBoletoBarcode { get; set; } // Código de barras

    public Guid? RegistradoPorId { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}

public class Servico
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? LojaId { get; set; }
    public Loja? Loja { get; set; }

    [Required, MaxLength(150)]
    public string Nome { get; set; } = "";

    [MaxLength(50)]
    public string Categoria { get; set; } = "Geral";

    [Column(TypeName = "decimal(10,2)")]
    public decimal Preco { get; set; }

    public int DuracaoMin { get; set; } = 30;

    public bool Ativo { get; set; } = true;
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}

public class Agendamento
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? LojaId { get; set; }
    public Loja? Loja { get; set; }

    public Guid ServicoId { get; set; }
    public Servico? Servico { get; set; }

    public Guid? ClienteId { get; set; }
    public Cliente? Cliente { get; set; }

    [MaxLength(150)]
    public string NomeServico { get; set; } = "";   // snapshot
    [MaxLength(150)]
    public string? NomeCliente { get; set; }         // cadastrado ou avulso

    [Column(TypeName = "decimal(10,2)")]
    public decimal Preco { get; set; }

    public DateTime DataHora { get; set; }
    public int DuracaoMin { get; set; } = 30;

    [MaxLength(20)]
    public string Status { get; set; } = "agendado";

    public bool Pago { get; set; } = false;
    public Guid? VendaId { get; set; }
    public Venda? Venda { get; set; }

    [MaxLength(300)]
    public string? Observacao { get; set; }

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}
