using System.ComponentModel.DataAnnotations;
using LojaApi.Models;

namespace LojaApi.src.Models.Fiscal;

// Registro de cada tentativa de emissão fiscal de uma venda (histórico/auditoria).
public class EmissaoFiscal
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid LojaId { get; set; }
    public Loja? Loja { get; set; }

    public Guid VendaId { get; set; }
    public Venda Venda { get; set; } = null!;

    [Required, MaxLength(20)]
    public string Tipo { get; set; } = "nfce";

    [Required, MaxLength(20)]
    public string Status { get; set; } = "pendente"; // pendente | processando | autorizada | rejeitada | cancelada | erro

    [MaxLength(30)]
    public string? Provedor { get; set; } // snapshot do provedor usado nesta tentativa

    [MaxLength(44)]
    public string? ChaveAcesso { get; set; }

    [MaxLength(20)]
    public string? NumeroNota { get; set; }

    [MaxLength(3)]
    public string? SerieNota { get; set; }

    [MaxLength(50)]
    public string? Protocolo { get; set; }

    [MaxLength(500)]
    public string? UrlDanfe { get; set; }

    [MaxLength(500)]
    public string? UrlXml { get; set; }

    [MaxLength(500)]
    public string? MotivoErro { get; set; }

    public Guid? SolicitadaPorUsuarioId { get; set; }

    public DateTime SolicitadaEm { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessadaEm { get; set; }
}
