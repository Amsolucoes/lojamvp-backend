using LojaApi.Data;
using Microsoft.EntityFrameworkCore;
using Resend;

namespace LojaApi.Services;

public class ComunicadoEmailService(AppDbContext db, IResend resend, ILogger<ComunicadoEmailService> logger)
{
    public record ResultadoEnvio(int TotalEnviados, int TotalFalhas, List<string> Falhas);

    public async Task<ResultadoEnvio> EnviarAsync(List<Guid>? lojaIds, bool todasLojas, string assunto, string corpoHtml)
    {
        var q = db.Lojas.Where(l => !l.EhTeste && l.Status != Models.StatusLoja.Cancelado);
        if (!todasLojas && lojaIds != null && lojaIds.Count > 0)
            q = q.Where(l => lojaIds.Contains(l.Id));

        var lojas = await q.ToListAsync();

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

        return new ResultadoEnvio(enviados, falhas.Count, falhas);
    }
}