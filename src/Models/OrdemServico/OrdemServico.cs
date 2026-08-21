using LojaApi.Models;
using LojaApi.src.Models.Funcionarios;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LojaApi.src.Models.OrdemServico;

// ── Categoria de checklist configurável (ex: "Suspensão", "Freios") ──
public class ChecklistCategoria
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid LojaId { get; set; }

    [Required, MaxLength(100)]
    public string Nome { get; set; } = "";

    public int Ordem { get; set; } = 0;
    public bool Ativa { get; set; } = true;

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    public ICollection<ChecklistItem> Itens { get; set; } = [];
}

// ── Item dentro de uma categoria (ex: "Bandeja", "Pivô", "Bieleta") ──
public class ChecklistItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid LojaId { get; set; }
    public Guid CategoriaId { get; set; }
    public ChecklistCategoria? Categoria { get; set; }

    [Required, MaxLength(100)]
    public string Nome { get; set; } = "";

    public int Ordem { get; set; } = 0;
    public bool Ativo { get; set; } = true;

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}

// ── Orçamento — vira Ordem de Serviço quando aprovado ──────────────
public class OrcamentoServico
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid LojaId { get; set; }
    public Guid ClienteId { get; set; }

    [MaxLength(200)]
    public string? VeiculoDescricao { get; set; } // ex: "Gol 2015 - placa ABC1D23"

    [MaxLength(20)]
    public string Status { get; set; } = "pendente"; // pendente | aprovado | reprovado | em_andamento | concluido | cancelado

    [MaxLength(1000)]
    public string? Observacoes { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal ValorTotal { get; set; } // soma dos itens, recalculado ao salvar

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public DateTime? AprovadoEm { get; set; }
    public DateTime? ConcluidoEm { get; set; }

    // Vínculo com o lançamento gerado no Financeiro ao concluir (a receber)
    public Guid? LancamentoFinanceiroId { get; set; }

    // Navegação
    public ICollection<ItemOrcamentoServico> Itens { get; set; } = [];
    public ICollection<MecanicoOrcamento> Mecanicos { get; set; } = [];
    public ICollection<ChecklistRespostaItem> ChecklistRespostas { get; set; } = [];
}

// ── Item de cobrança do orçamento: peça (estoque ou avulsa) ou serviço (mão de obra) ──
public class ItemOrcamentoServico
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid LojaId { get; set; }
    public Guid OrcamentoId { get; set; }
    public OrcamentoServico? Orcamento { get; set; }

    [MaxLength(20)]
    public string Tipo { get; set; } = "peca"; // peca | servico

    // Se a peça vier do estoque, aponta pro Produto; se for avulsa (não é do estoque da oficina), fica null
    public Guid? ProdutoId { get; set; }
    public Produto? Produto { get; set; }

    [Required, MaxLength(150)]
    public string Descricao { get; set; } = ""; // nome da peça/serviço no momento (avulso ou copiado do produto)

    public int Quantidade { get; set; } = 1;

    [Column(TypeName = "decimal(10,2)")]
    public decimal ValorUnitario { get; set; } // preço do estoque OU digitado na hora (avulso)

    [Column(TypeName = "decimal(10,2)")]
    public decimal ValorTotal { get; set; } // Quantidade * ValorUnitario, gravado no momento

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}

// ── Mecânico vinculado ao orçamento, com sua comissão própria ──────
public class MecanicoOrcamento
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid LojaId { get; set; }
    public Guid OrcamentoId { get; set; }
    public OrcamentoServico? Orcamento { get; set; }

    public Guid ProfissionalId { get; set; }
    public Profissional? Profissional { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal ComissaoPercentual { get; set; } // sugerido do Profissional, editável por orçamento

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}

// ── Resposta do checklist de inspeção pra um orçamento específico ──
public class ChecklistRespostaItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid LojaId { get; set; }
    public Guid OrcamentoId { get; set; }
    public OrcamentoServico? Orcamento { get; set; }

    public Guid ChecklistItemId { get; set; }
    public ChecklistItem? ChecklistItem { get; set; }

    [MaxLength(20)]
    public string Estado { get; set; } = "bom"; // bom | regular | ruim | precisa_trocar

    [MaxLength(300)]
    public string? Observacao { get; set; }

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}