using LojaApi.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LojaApi.src.Models.OrdemServico;

// ── Item de patrimônio (ferramenta, equipamento — não é produto pra venda) ──
public class ItemPatrimonio
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid LojaId { get; set; }

    [Required, MaxLength(150)]
    public string Nome { get; set; } = "";

    [MaxLength(80)]
    public string? Categoria { get; set; } // ex: "Ferramentas manuais", "Elevação"

    // Quantidade que deveria existir hoje, segundo o cadastro — é o valor "esperado"
    // usado como referência na próxima contagem
    public int QuantidadeEsperada { get; set; } = 0;

    [Column(TypeName = "decimal(10,2)")]
    public decimal ValorUnitario { get; set; } = 0;

    [MaxLength(300)]
    public string? Observacao { get; set; }

    public bool Ativo { get; set; } = true;
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public DateTime AtualizadoEm { get; set; } = DateTime.UtcNow;
}

// ── Sessão de contagem — uma "conferência" feita numa data específica ──
public class ContagemPatrimonio
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid LojaId { get; set; }

    public DateTime DataContagem { get; set; } = DateTime.UtcNow;

    [MaxLength(100)]
    public string? Responsavel { get; set; } // nome de quem fez a contagem, texto livre

    [MaxLength(300)]
    public string? Observacao { get; set; }

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    // Navegação
    public ICollection<ItemContagemPatrimonio> Itens { get; set; } = [];
}

// ── Item dentro de uma contagem — compara esperado (na hora) vs contado ──
public class ItemContagemPatrimonio
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid LojaId { get; set; }
    public Guid ContagemId { get; set; }
    public ContagemPatrimonio? Contagem { get; set; }

    public Guid ItemPatrimonioId { get; set; }
    public ItemPatrimonio? ItemPatrimonio { get; set; }

    // Snapshot do que era esperado NO MOMENTO dessa contagem — assim, se o cadastro
    // mudar depois, o histórico dessa contagem continua mostrando o comparativo real da época
    public int QuantidadeEsperadaNoMomento { get; set; }

    public int QuantidadeContada { get; set; }

    [MaxLength(200)]
    public string? Observacao { get; set; } // ex: "2 chaves emprestadas pro Anderson"
}