using LojaApi.Domain.Plataforma;
using LojaApi.Shared.Application;

namespace LojaApi.Application.Plataforma;

// Liga/desliga o agendamento online, define o link (slug) e se a confirmação é automática ou por aprovação.
public class ConfigurarAgendamentoOnlineCommandHandler(IPlataformaRepository repositorio) : ICommandHandler<ConfigurarAgendamentoOnlineCommand>
{
    public async Task<Resultado<object>> HandleAsync(ConfigurarAgendamentoOnlineCommand command)
    {
        var (lojaId, req) = command;
        if (lojaId is null) return Resultado.NaoEncontrado();

        var loja = await repositorio.ObterLojaAsync(lojaId.Value);
        if (loja is null) return Resultado.NaoEncontrado();

        // Normaliza e valida o link (se enviado)
        if (!string.IsNullOrWhiteSpace(req.Slug))
        {
            var slug = PlataformaRegras.GerarSlug(req.Slug);
            if (!PlataformaRegras.SlugValido(slug))
                return Resultado.Invalido("O link deve ter ao menos 3 caracteres válidos.");

            if (await repositorio.SlugEmUsoAsync(slug, loja.Id))
                return Resultado.Conflito("Este link já está em uso. Escolha outro.");

            loja.Slug = slug;
        }

        loja.AgendamentoOnlineAtivo = req.Ativo;
        if (!string.IsNullOrWhiteSpace(req.Confirmacao))
            loja.AgendamentoOnlineConfirmacao = PlataformaRegras.ConfirmacaoDoAgendamentoOnline(req.Confirmacao);

        // Para ativar precisa ter link
        if (loja.AgendamentoOnlineAtivo && string.IsNullOrWhiteSpace(loja.Slug))
            return Resultado.Invalido("Defina um link (slug) antes de ativar o agendamento online.");

        loja.AtualizadoEm = DateTime.UtcNow;
        await repositorio.SalvarAsync();

        return Resultado.Ok(new AgendamentoOnlineDto(loja.AgendamentoOnlineAtivo, loja.AgendamentoOnlineConfirmacao, loja.Slug));
    }
}
