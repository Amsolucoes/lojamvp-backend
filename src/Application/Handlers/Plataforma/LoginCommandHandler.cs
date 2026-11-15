using LojaApi.Shared.Application;

namespace LojaApi.Application.Plataforma;

// Login por e-mail e senha. A mesma mensagem vale para e-mail inexistente, usuário inativo e senha errada.
// Para quem tem loja, confere a situação dela (e grava o bloqueio se o prazo passou), mas não barra o login aqui.
public class LoginCommandHandler(IPlataformaRepository repositorio, IGeradorDeToken geradorDeToken) : ICommandHandler<LoginCommand>
{
    public async Task<Resultado<object>> HandleAsync(LoginCommand command)
    {
        var req = command.Req;
        var usuario = await repositorio.ObterUsuarioAtivoPorEmailAsync(req.Email);

        if (usuario is null || !BCrypt.Net.BCrypt.Verify(req.Senha, usuario.SenhaHash))
            return Resultado.NaoAutorizado("E-mail ou senha incorretos.");

        if (usuario.Role != "superadmin")
        {
            var vinculo = await repositorio.ObterVinculoAtivoAsync(usuario.Id);
            if (vinculo != null)
            {
                var loja = await repositorio.ObterLojaAsync(vinculo.LojaId);
                if (loja != null)
                {
                    PlataformaApoio.AtualizarStatusDaLoja(loja, DateTime.UtcNow);
                    await repositorio.SalvarAsync();
                }
            }
        }

        usuario.UltimoLoginEm = DateTime.UtcNow;
        await repositorio.SalvarAsync();

        return Resultado.Ok(new LoginResponse(geradorDeToken.Gerar(usuario), usuario.Nome, usuario.Email, usuario.Role));
    }
}
