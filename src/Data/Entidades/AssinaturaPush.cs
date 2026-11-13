using System.ComponentModel.DataAnnotations;

namespace LojaApi.Models;

// Aparelho (navegador/PWA) de um usuário que ativou as notificações push.
public class AssinaturaPush
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UsuarioId { get; set; }
    public Guid LojaId { get; set; }

    [Required, MaxLength(1000)]
    public string Endpoint { get; set; } = "";

    [Required, MaxLength(200)]
    public string ChavePublica { get; set; } = "";

    [Required, MaxLength(100)]
    public string ChaveAuth { get; set; } = "";

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    // Última vez que o aviso diário de vencimentos foi processado para este aparelho
    public DateTime? UltimoAvisoEm { get; set; }
}
