using System.ComponentModel.DataAnnotations;

namespace LojaApi.Models;

public class ComunicadoEnviado
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(200)]
    public string Assunto { get; set; } = "";

    [Required]
    public string CorpoHtml { get; set; } = "";

    public bool TodasLojas { get; set; }

    public int TotalEnviados { get; set; }
    public int TotalFalhas { get; set; }

    // E-mails separados por vírgula — simples o suficiente pro volume esperado aqui
    public string DestinatariosSucesso { get; set; } = "";
    public string DestinatariosFalha { get; set; } = "";

    public DateTime EnviadoEm { get; set; } = DateTime.UtcNow;
}