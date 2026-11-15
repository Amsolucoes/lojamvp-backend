using LojaApi.Shared.Application;

namespace LojaApi.Application.Plataforma;

// Envia um comunicado por e-mail para as lojas escolhidas (ou todas) e para e-mails extras.
public class EnviarComunicadoCommandHandler(IEnvioDeComunicados envio) : ICommandHandler<EnviarComunicadoCommand>
{
    public async Task<Resultado<object>> HandleAsync(EnviarComunicadoCommand command)
    {
        var req = command.Req;
        if (string.IsNullOrWhiteSpace(req.Assunto) || string.IsNullOrWhiteSpace(req.CorpoHtml))
            return Resultado.Invalido("Preencha assunto e mensagem.");

        var temExtras = req.EmailsExtras != null && req.EmailsExtras.Count > 0;
        if (!req.TodasLojas && (req.LojaIds is null || req.LojaIds.Count == 0) && !temExtras)
            return Resultado.Invalido("Selecione ao menos uma loja, informe um e-mail extra, ou marque \"Todas as lojas\".");

        var r = await envio.EnviarAsync(req.LojaIds, req.TodasLojas, req.Assunto, req.CorpoHtml, req.EmailsExtras);

        return Resultado.Ok(new ComunicadoEnviadoDto(
            $"{r.TotalEnviados} e-mail(s) enviado(s)." + (r.TotalFalhas > 0 ? $" {r.TotalFalhas} falharam." : ""),
            r.TotalEnviados, r.TotalFalhas, r.Falhas));
    }
}
