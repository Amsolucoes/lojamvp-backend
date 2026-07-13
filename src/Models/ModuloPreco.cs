using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LojaApi.src.Models;

public class ModuloPreco
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(30)]
    public string Chave { get; set; } = ""; // servicos | financeiro | turmas | etiquetas | nf

    [Required, MaxLength(80)]
    public string Nome { get; set; } = "";

    [Column(TypeName = "decimal(10,2)")]
    public decimal Valor { get; set; }

    public bool DisponivelParaAtivar { get; set; } = true; // "em breve" = false
    public DateTime AtualizadoEm { get; set; } = DateTime.UtcNow;
}