using System.Security.Claims;
using LojaApi.Data;
using Microsoft.EntityFrameworkCore;

namespace LojaApi.Shared.Application;

// Descobre a loja do usuário logado. Antes cada controller tinha a sua cópia deste código.
public interface ILojaAtual
{
    Task<Guid?> ObterIdAsync(ClaimsPrincipal usuario);
}

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
