using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LojaApi.src.Models;

// ── Seguradora (cadastro simples, texto livre inicialmente) ────────
public class Seguradora
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid LojaId { get; set; }

    [Required, MaxLength(100)]
    public string Nome { get; set; } = "";

    public bool Ativa { get; set; } = true;
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}

// ── Card do funil de vendas ──────────────────────────────────────────
public class Oportunidade
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid LojaId { get; set; }
    public Guid ClienteId { get; set; }

    public Guid? SeguradoraId { get; set; }
    public Seguradora? Seguradora { get; set; }

    [MaxLength(150)]
    public string? PlanoDesejado { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal? ValorEstimado { get; set; }

    [MaxLength(30)]
    public string Etapa { get; set; } = "lead"; // lead | contato | proposta | negociacao | ganho | perdido

    [MaxLength(200)]
    public string? MotivoPerda { get; set; }

    public int Ordem { get; set; } = 0; // posição dentro da coluna do funil (drag and drop)

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public DateTime AtualizadoEm { get; set; } = DateTime.UtcNow;
}

// ── Apólice — gerada quando uma oportunidade fecha (etapa "ganho") ──
public class Apolice
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid LojaId { get; set; }
    public Guid ClienteId { get; set; }
    public Guid? OportunidadeId { get; set; }

    public Guid SeguradoraId { get; set; }
    public Seguradora? Seguradora { get; set; }

    [Required, MaxLength(150)]
    public string NomePlano { get; set; } = "";

    [MaxLength(50)]
    public string? NumeroApolice { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal ValorPremio { get; set; } // o que o cliente paga (mensal, geralmente)

    [Column(TypeName = "decimal(10,2)")]
    public decimal ValorComissao { get; set; } // o que ela recebe (pode ser fixo ou calculado)

    [Column(TypeName = "decimal(5,2)")]
    public decimal? PercentualComissao { get; set; } // opcional, se preferir calcular por %

    public DateTime VigenciaInicio { get; set; }
    public DateTime VigenciaFim { get; set; }

    [MaxLength(20)]
    public string Status { get; set; } = "ativa"; // ativa | vencida | renovada | cancelada

    public Guid? RenovadaParaApoliceId { get; set; } // se renovou, aponta pra nova apólice

    public Guid? LancamentoFinanceiroId { get; set; } // vínculo com a comissão lançada no Financeiro

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}