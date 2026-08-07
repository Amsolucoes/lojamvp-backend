using System.ComponentModel.DataAnnotations;

namespace LojaApi.src.Models.Etiquetas;

public class ConfiguracaoEtiqueta
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid LojaId { get; set; }

    public bool IncluirLogo { get; set; } = true;
    public bool UsarLogoPropria { get; set; } = false; // false = usa a logo já cadastrada da loja
    [MaxLength(500)]
    public string? LogoEtiquetaUrl { get; set; } // só usada se UsarLogoPropria = true

    public bool IncluirNomeMarca { get; set; } = true;
    public bool IncluirNomeProduto { get; set; } = true;
    public bool IncluirPreco { get; set; } = true;
    public bool IncluirCodigoBarras { get; set; } = true;

    // Tamanho da etiqueta em milímetros — usado tanto pra calcular o grid na folha A4
    // quanto como referência pra impressora térmica futura.
    public decimal LarguraMm { get; set; } = 40;
    public decimal AlturaMm { get; set; } = 30;

    public DateTime AtualizadoEm { get; set; } = DateTime.UtcNow;
}