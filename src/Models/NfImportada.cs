using LojaApi.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LojaApi.src.Models;

// Registra cada NF-e já importada (via XML) ou lançada manualmente, pra
// evitar duplicar entrada de estoque e manter um histórico único das duas.
public class NfImportada
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid LojaId { get; set; }
    [MaxLength(44)]
    public string? ChaveAcesso { get; set; } // 44 dígitos, único por NF-e — só existe na importação por XML
    [MaxLength(20)]
    public string NumeroNf { get; set; } = "";
    [MaxLength(150)]
    public string NomeFornecedor { get; set; } = "";
    public int QtdItens { get; set; }
    public DateTime ImportadoEm { get; set; } = DateTime.UtcNow;
    public bool Desfeita { get; set; } = false;
    // JSON com o detalhe de cada item processado, pra permitir desfazer depois
    [Column(TypeName = "jsonb")]
    public string ItensJson { get; set; } = "[]";

    // xml (importação de NF-e) | manual (lançamento manual) — usado pra saber
    // como desfazer (tag diferente no MovimentoEstoque.Observacao de cada origem).
    [MaxLength(10)]
    public string Origem { get; set; } = "xml";

    public Guid? FornecedorId { get; set; }
    public Fornecedor? Fornecedor { get; set; }

    public DateTime? DataEmissao { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal? ValorTotal { get; set; }
}