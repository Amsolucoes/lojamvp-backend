using LojaApi.Data;
using Microsoft.EntityFrameworkCore;
using Resend;

namespace LojaApi.Services;

public class ComunicadoEmailService(AppDbContext db, IResend resend, ILogger<ComunicadoEmailService> logger)
{
    public record ResultadoEnvio(int TotalEnviados, int TotalFalhas, List<string> Falhas);

    public async Task<ResultadoEnvio> EnviarAsync(List<Guid>? lojaIds, bool todasLojas, string assunto, string corpoHtml, List<string>? emailsExtras = null)
    {
        var lojas = new List<Models.Loja>();
        if (todasLojas || (lojaIds != null && lojaIds.Count > 0))
        {
            var q = db.Lojas.Where(l => !l.EhTeste && l.Status != Models.StatusLoja.Cancelado);
            if (!todasLojas) q = q.Where(l => lojaIds!.Contains(l.Id));
            lojas = await q.ToListAsync();
        }

        var enviados = 0;
        var falhas = new List<string>();

        foreach (var loja in lojas)
        {
            if (string.IsNullOrWhiteSpace(loja.Email)) continue;

            var html = $@"
                <div style='font-family:sans-serif;max-width:520px;margin:0 auto'>
                    <div style='text-align:center;margin-bottom:20px'>
                        <strong style='font-size:18px;color:#c38228'>AldevSoftware</strong>
                    </div>
                    {corpoHtml}
                    <div style='margin-top:28px;text-align:center'>
                        <a href='https://app.aldevsoftware.com.br' style='background:#c38228;color:#fff;padding:10px 24px;border-radius:8px;text-decoration:none;font-weight:600'>
                            Acessar o sistema
                        </a>
                    </div>
                    <p style='margin-top:24px;font-size:12px;color:#999;text-align:center'>
                        Você recebeu este e-mail porque é cliente AldevSoftware — {loja.Nome}.
                    </p>
                </div>";

            try
            {
                var msg = new EmailMessage
                {
                    From = "AldevSoftware <novidades@aldevsoftware.com.br>",
                    Subject = assunto,
                    HtmlBody = html,
                };
                msg.To.Add(loja.Email);
                await resend.EmailSendAsync(msg);
                enviados++;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro ao enviar comunicado para {Email}.", loja.Email);
                falhas.Add(loja.Email);
            }
        }

        if (emailsExtras != null)
        {
            foreach (var email in emailsExtras.Where(e => !string.IsNullOrWhiteSpace(e)))
            {
                var html = $@"
                    <div style='font-family:sans-serif;max-width:520px;margin:0 auto'>
                        <div style='text-align:center;margin-bottom:20px'>
                            <strong style='font-size:18px;color:#c38228'>AldevSoftware</strong>
                        </div>
                        {corpoHtml}
                        <div style='margin-top:28px;text-align:center'>
                            <a href='https://app.aldevsoftware.com.br' style='background:#c38228;color:#fff;padding:10px 24px;border-radius:8px;text-decoration:none;font-weight:600'>
                                Acessar o sistema
                            </a>
                        </div>
                        <p style='margin-top:24px;font-size:12px;color:#999;text-align:center'>
                            E-mail de teste — envio manual do painel AldevSoftware.
                        </p>
                    </div>";

                try
                {
                    var msg = new EmailMessage
                    {
                        From = "AldevSoftware <novidades@aldevsoftware.com.br>",
                        Subject = assunto,
                        HtmlBody = html,
                    };
                    msg.To.Add(email.Trim());
                    await resend.EmailSendAsync(msg);
                    enviados++;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Erro ao enviar comunicado (extra) para {Email}.", email);
                    falhas.Add(email);
                }
            }
        }

        return new ResultadoEnvio(enviados, falhas.Count, falhas);
    }
}