using System.ComponentModel.DataAnnotations;

namespace LojaApi.src.Models.Etiquetas;

public class ConfiguracaoEtiqueta
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid LojaId { get; set; }

    [MaxLength(60)]
    public string Nome { get; set; } = "Padrão";
    public bool Padrao { get; set; } = false; // usado como pré-selecionado na tela de imprimir

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

    [MaxLength(7)]
    public string CorTexto { get; set; } = "#000000";
    [MaxLength(7)]
    public string CorFundo { get; set; } = "#FFFFFF";
    [MaxLength(60)]
    public string FonteFamilia { get; set; } = "Arial, sans-serif";
    public int EscalaFonte { get; set; } = 100; // % — 100 = tamanho padrão, 150 = 50% maior, etc.

    public DateTime AtualizadoEm { get; set; } = DateTime.UtcNow;
}