using System.ComponentModel.DataAnnotations;

namespace LojaApi.src.Models;

// Registra cada NF-e já importada, pra evitar duplicar entrada de estoque
// se a mesma nota for enviada de novo.
public class NfImportada
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid LojaId { get; set; }
    [MaxLength(44)]
    public string ChaveAcesso { get; set; } = ""; // 44 dígitos, único por NF-e
    [MaxLength(20)]
    public string NumeroNf { get; set; } = "";
    [MaxLength(150)]
    public string NomeFornecedor { get; set; } = "";
    public int QtdItens { get; set; }
    public DateTime ImportadoEm { get; set; } = DateTime.UtcNow;
}