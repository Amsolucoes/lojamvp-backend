using System.Security.Claims;

namespace LojaApi.Shared.Application;

// Descobre a loja do usuário logado. Antes cada controller tinha a sua cópia deste código.
public interface ILojaAtual
{
    Task<Guid?> ObterIdAsync(ClaimsPrincipal usuario);
}
