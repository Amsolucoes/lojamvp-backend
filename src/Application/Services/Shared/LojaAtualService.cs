using System.Security.Claims;
using LojaApi.Data;
using Microsoft.EntityFrameworkCore;

namespace LojaApi.Shared.Application;

public class LojaAtualService(AppDbContext db) : ILojaAtual
{
    public async Task<Guid?> ObterIdAsync(ClaimsPrincipal usuario)
    {
        var usuarioId = Guid.Parse(usuario.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
        var vinculo = await db.UsuariosLoja
            .FirstOrDefaultAsync(ul => ul.UsuarioId == usuarioId && ul.Ativo);
        return vinculo?.LojaId;
    }
}
