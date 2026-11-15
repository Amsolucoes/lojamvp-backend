using LojaApi.Shared.Application;

namespace LojaApi.Application.Plataforma;

// Quem é o usuário logado. Retomar a sessão conta como último acesso, menos o acesso de suporte
// (superadmin entrando como o cliente).
public class RegistrarSessaoCommandHandler(IPlataformaRepository repositorio) : ICommandHandler<RegistrarSessaoCommand>
{
    public async Task<Resultado<object>> HandleAsync(RegistrarSessaoCommand command)
    {
        var (id, nome, email, role, acessoSuporte) = command;

        if (!acessoSuporte && Guid.TryParse(id, out var usuarioId))
        {
            var usuario = await repositorio.ObterUsuarioAsync(usuarioId);
            if (usuario != null)
            {
                usuario.UltimoLoginEm = DateTime.UtcNow;
                await repositorio.SalvarAsync();
            }
        }

        return Resultado.Ok(new SessaoDto(id, nome, email, role));
    }
}
