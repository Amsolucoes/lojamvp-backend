using LojaApi.Shared.Application;

namespace LojaApi.Application.Plataforma;

// O usuário logado troca a própria senha (confirma a atual; a nova tem no mínimo 8 caracteres).
public class TrocarSenhaCommandHandler(IPlataformaRepository repositorio) : ICommandHandler<TrocarSenhaCommand>
{
    public async Task<Resultado<object>> HandleAsync(TrocarSenhaCommand command)
    {
        var (usuarioId, req) = command;
        var usuario = await repositorio.ObterUsuarioAsync(usuarioId);
        if (usuario is null) return Resultado.NaoAutorizado();

        if (!BCrypt.Net.BCrypt.Verify(req.SenhaAtual, usuario.SenhaHash))
            return Resultado.Invalido("Senha atual incorreta.");

        if (req.NovaSenha.Length < 8)
            return Resultado.Invalido("A nova senha deve ter pelo menos 8 caracteres.");

        usuario.SenhaHash = BCrypt.Net.BCrypt.HashPassword(req.NovaSenha);
        await repositorio.SalvarAsync();
        return Resultado.Ok(new MensagemDto("Senha alterada com sucesso."));
    }
}
