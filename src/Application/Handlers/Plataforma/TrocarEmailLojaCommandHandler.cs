using LojaApi.Shared.Application;

namespace LojaApi.Application.Plataforma;

// Troca o e-mail de login do admin da loja e/ou o e-mail de contato da loja (confere duplicidade de cada um).
public class TrocarEmailLojaCommandHandler(IPlataformaRepository plataforma, IAdminRepository admin) : ICommandHandler<TrocarEmailLojaCommand>
{
    public async Task<Resultado<object>> HandleAsync(TrocarEmailLojaCommand command)
    {
        var (id, req) = command;
        var loja = await plataforma.ObterLojaAsync(id);
        if (loja is null) return Resultado.NaoEncontrado("Loja não encontrada.");

        var novoEmail = req.NovoEmail?.Trim().ToLower();
        if (string.IsNullOrWhiteSpace(novoEmail) || !novoEmail.Contains('@'))
            return Resultado.Invalido("E-mail inválido.");

        if (!req.TrocarLogin && !req.TrocarLoja)
            return Resultado.Invalido("Escolha ao menos um e-mail para trocar.");

        var vinculo = await admin.ObterVinculoAdminAsync(id, apenasAtivo: false);

        if (req.TrocarLogin)
        {
            if (vinculo?.Usuario is null)
                return Resultado.Invalido("Usuário admin da loja não encontrado.");

            if (await plataforma.EmailDeUsuarioExisteAsync(novoEmail, vinculo.Usuario.Id))
                return Resultado.Conflito("Este e-mail já está em uso por outro usuário.");
        }

        if (req.TrocarLoja && await plataforma.EmailDeLojaExisteAsync(novoEmail, id))
            return Resultado.Conflito("Este e-mail já está em uso por outra loja.");

        if (req.TrocarLogin && vinculo?.Usuario is not null)
            vinculo.Usuario.Email = novoEmail;

        if (req.TrocarLoja)
            loja.Email = novoEmail;

        loja.AtualizadoEm = DateTime.UtcNow;
        await plataforma.SalvarAsync();

        return Resultado.Ok(new MensagemDto("E-mail atualizado com sucesso."));
    }
}
