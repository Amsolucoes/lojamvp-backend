using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LojaApi.Models;

// ── Conta bancária (Itaú, Santander, Caixa da loja, etc.) ──────────
public class ContaBancaria
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid LojaId { get; set; }
    public Loja? Loja { get; set; }

    [Required, MaxLength(80)]
    public string Nome { get; set; } = "";

    [Column(TypeName = "decimal(12,2)")]
    public decimal SaldoInicial { get; set; } = 0;

    public bool Ativa { get; set; } = true;
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}

// ── Lançamento fixo/recorrente (ex: aluguel — repete todo mês) ─────
public class LancamentoFixo
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid LojaId { get; set; }

    public Guid ContaBancariaId { get; set; }
    public ContaBancaria? ContaBancaria { get; set; }

    [MaxLength(20)]
    public string Tipo { get; set; } = "pagar"; // pagar | receber

    [Required, MaxLength(150)]
    public string Descricao { get; set; } = "";

    public Guid? CategoriaId { get; set; }
    public CategoriaFinanceira? Categoria { get; set; }

    [Column(TypeName = "decimal(12,2)")]
    public decimal Valor { get; set; }

    public int DiaVencimento { get; set; } = 10;
    public bool Ativa { get; set; } = true;
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}

// ── Lançamento concreto — o que aparece na lista de Pagar/Receber ──
public class LancamentoFinanceiro
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid LojaId { get; set; }

    public Guid ContaBancariaId { get; set; }
    public ContaBancaria? ContaBancaria { get; set; }

    [MaxLength(20)]
    public string Tipo { get; set; } = "pagar"; // pagar | receber

    [MaxLength(20)]
    public string Modo { get; set; } = "avulsa"; // avulsa | parcelada | fixa

    [Required, MaxLength(150)]
    public string Descricao { get; set; } = "";

    public Guid? CategoriaId { get; set; }
    public CategoriaFinanceira? Categoria { get; set; }

    [Column(TypeName = "decimal(12,2)")]
    public decimal Valor { get; set; }

    public DateTime Vencimento { get; set; }

    [MaxLength(20)]
    public string Status { get; set; } = "pendente"; // pendente | pago
    public DateTime? PagoEm { get; set; }

    // Parcelamento
    public Guid? GrupoParcelamentoId { get; set; }
    public int? NumeroParcela { get; set; }
    public int? TotalParcelas { get; set; }

    // Origem, se veio de um lançamento fixo (aluguel, etc.)
    public Guid? LancamentoFixoId { get; set; }

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}

// ── Ajuste manual de saldo (auditável, igual MovimentoEstoque) ─────
public class AjusteContaBancaria
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid LojaId { get; set; }

    public Guid ContaBancariaId { get; set; }
    public ContaBancaria? ContaBancaria { get; set; }

    [MaxLength(20)]
    public string Tipo { get; set; } = "ajuste"; // entrada | saida | ajuste

    [Column(TypeName = "decimal(12,2)")]
    public decimal Valor { get; set; } // para "ajuste", é a diferença já calculada

    [MaxLength(200)]
    public string? Observacao { get; set; }

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}

// ── Categoria financeira (Aluguel, Luz, Fornecedor, Mensalidade, etc.) ──
public class CategoriaFinanceira
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid LojaId { get; set; }

    [Required, MaxLength(60)]
    public string Nome { get; set; } = "";

    [MaxLength(20)]
    public string Tipo { get; set; } = "ambos"; // pagar | receber | ambos

    [MaxLength(4)]
    public string? Icone { get; set; }

    public bool Ativa { get; set; } = true;
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}

// ── Cartão de crédito (configuração) ───────────────────────────────
public class CartaoCredito
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid LojaId { get; set; }

    [Required, MaxLength(60)]
    public string Nome { get; set; } = "";

    [Column(TypeName = "decimal(12,2)")]
    public decimal Limite { get; set; }

    public int DiaFechamento { get; set; } = 10;
    public int DiaVencimento { get; set; } = 15;

    public Guid ContaBancariaId { get; set; }
    public ContaBancaria? ContaBancaria { get; set; }

    public bool Ativo { get; set; } = true;
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}

// ── Compra individual no cartão ────────────────────────────────────
public class LancamentoCartao
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid LojaId { get; set; }

    public Guid CartaoCreditoId { get; set; }
    public CartaoCredito? CartaoCredito { get; set; }

    [Required, MaxLength(150)]
    public string Descricao { get; set; } = "";

    [Column(TypeName = "decimal(12,2)")]
    public decimal Valor { get; set; }

    public DateTime DataCompra { get; set; }

    public Guid? CategoriaId { get; set; }
    public CategoriaFinanceira? Categoria { get; set; }

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}

// ── Fatura do cartão (agregado do ciclo, calculado) ────────────────
public class FaturaCartao
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid LojaId { get; set; }

    public Guid CartaoCreditoId { get; set; }
    public CartaoCredito? CartaoCredito { get; set; }

    public DateTime MesReferencia { get; set; } // primeiro dia do mês de vencimento
    public DateTime Vencimento { get; set; }

    [Column(TypeName = "decimal(12,2)")]
    public decimal Total { get; set; }

    [MaxLength(20)]
    public string Status { get; set; } = "pendente"; // pendente | pago
    public DateTime? PagoEm { get; set; }

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}