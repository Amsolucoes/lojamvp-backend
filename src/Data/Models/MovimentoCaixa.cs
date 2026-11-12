using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LojaApi.Models;

// ── Movimento manual de caixa: reforço (entrada) ou sangria (saída) ──
public class MovimentoCaixa
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? LojaId { get; set; }
    public Loja? Loja { get; set; }

    [Required, MaxLength(10)]
    public string Tipo { get; set; } = "entrada"; // entrada | saida (sangria)

    [Column(TypeName = "decimal(10,2)")]
    public decimal Valor { get; set; }

    public DateTime Data { get; set; } // data do movimento (aceita passada, não futura)

    public Guid? OrigemVendaId { get; set; }
    public OrigemVenda? OrigemVenda { get; set; }

    [MaxLength(50)]
    public string? OrigemNome { get; set; } // snapshot — não quebra se a origem for excluída depois

    [MaxLength(300)]
    public string? Observacao { get; set; }

    // Se a loja tem módulo Financeiro ativo e o dono escolheu uma conta, espelha o lançamento lá
    public Guid? ContaBancariaId { get; set; }
    public Guid? AjusteContaBancariaId { get; set; }

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}