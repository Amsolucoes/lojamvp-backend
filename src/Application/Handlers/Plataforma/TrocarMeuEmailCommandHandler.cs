using LojaApi.Shared.Application;

namespace LojaApi.Application.Plataforma;

// O assinante troca o próprio e-mail de login (confirma a senha; o e-mail fica minúsculo e sem espaços).
public class TrocarMeuEmailCommandHandler(IPlataformaRepository repositorio) : ICommandHandler<TrocarMeuEmailCommand>
{
    public async Task<Resultado<object>> HandleAsync(TrocarMeuEmailCommand command)
    {
        var (usuarioId, req) = command;
        var usuario = await repositorio.ObterUsuarioAsync(usuarioId);
        if (usuario is null) return Resultado.NaoEncontrado("Usuário não encontrado.");

        if (!BCrypt.Net.BCrypt.Verify(req.SenhaAtual, usuario.SenhaHash))
            return Resultado.Invalido("Senha atual incorreta.");

        var novoEmail = req.NovoEmail?.Trim().ToLower();
        if (string.IsNullOrWhiteSpace(novoEmail) || !novoEmail.Contains('@'))
            return Resultado.Invalido("E-mail inválido.");

        if (await repositorio.EmailDeUsuarioExisteAsync(novoEmail, usuario.Id))
            return Resultado.Conflito("Este e-mail já está em uso.");

        usuario.Email = novoEmail;
        await repositorio.SalvarAsync();
        return Resultado.Ok(new MensagemDto("E-mail atualizado. Use o novo e-mail no próximo login."));
    }
}
