using System.ComponentModel.DataAnnotations;
using LojaApi.Models;

namespace LojaApi.src.Models.Fiscal;

// Configuração de emissão fiscal (NFC-e) por loja. Fica pronta desde já — mas só passa a
// funcionar de verdade quando um provedor terceirizado (Focus NFe, PlugNotas etc.) for
// contratado e suas credenciais preenchidas aqui.
public class ConfiguracaoFiscal
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid LojaId { get; set; }
    public Loja? Loja { get; set; }

    public bool Ativo { get; set; } = false;

    [MaxLength(30)]
    public string? Provedor { get; set; } // ex.: "focus_nfe" — null enquanto nenhum provedor for contratado

    [Required, MaxLength(20)]
    public string Ambiente { get; set; } = "homologacao"; // homologacao | producao

    [MaxLength(300)]
    public string? CredencialToken { get; set; }

    [MaxLength(14)]
    public string? InscricaoEstadual { get; set; }

    [MaxLength(20)]
    public string? RegimeTributario { get; set; } // simples_nacional | lucro_presumido | lucro_real | mei

    [Required, MaxLength(3)]
    public string SerieNfce { get; set; } = "1";

    public int ProximoNumeroNfce { get; set; } = 1;

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public DateTime AtualizadoEm { get; set; } = DateTime.UtcNow;
}
