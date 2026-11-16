namespace LojaApi.Application.Plataforma;

public record ResultadoComunicado(int TotalEnviados, int TotalFalhas, List<string> Falhas);

// Porta: manda o comunicado por e-mail para as lojas e e-mails extras (e guarda no histórico). Implementada com o ComunicadoEmailService.
public interface IEnvioDeComunicados
{
    Task<ResultadoComunicado> EnviarAsync(List<Guid>? lojaIds, bool todasLojas, string assunto, string corpoHtml, List<string>? emailsExtras);
}
