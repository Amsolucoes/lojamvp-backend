using LojaApi.Domain.Plataforma;
using LojaApi.Shared.Application;

namespace LojaApi.Application.Plataforma;

// Pausa o agendamento online até uma data de retorno (hoje ou futura), com mensagem; ou tira a pausa.
public class ConfigurarPausaCommandHandler(IPlataformaRepository repositorio) : ICommandHandler<ConfigurarPausaCommand>
{
    public async Task<Resultado<object>> HandleAsync(ConfigurarPausaCommand command)
    {
        var (lojaId, req) = command;
        if (lojaId is null) return Resultado.NaoEncontrado();

        var loja = await repositorio.ObterLojaAsync(lojaId.Value);
        if (loja is null) return Resultado.NaoEncontrado();

        if (req.Ativar)
        {
            if (!PlataformaRegras.PausaValida(req.PausaAte, DateTime.UtcNow))
                return Resultado.Invalido("Informe uma data de retorno válida (hoje ou futura).");

            loja.PausaAte = PlataformaRegras.FimDaPausa(req.PausaAte!.Value);
            loja.PausaMensagem = PlataformaRegras.MensagemDaPausa(req.Mensagem);
        }
        else
        {
            loja.PausaAte = null;
            loja.PausaMensagem = null;
        }

        loja.AtualizadoEm = DateTime.UtcNow;
        await repositorio.SalvarAsync();

        return Resultado.Ok(new PausaDto(loja.PausaAte, loja.PausaMensagem));
    }
}
