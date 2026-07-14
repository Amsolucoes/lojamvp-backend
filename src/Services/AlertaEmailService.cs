using LojaApi.Data;
using Microsoft.EntityFrameworkCore;
using Resend;
using System.Net.Mail;

namespace LojaApi.Services;

public class AlertaEmailService(AppDbContext db, IResend resend, ILogger<AlertaEmailService> logger)
{
    public async Task EnviarAlertasDoDiaAsync()
    {
        var hoje = DateTime.UtcNow.Date;
        var amanha = hoje.AddDays(1);

        // Todos os lançamentos "a pagar", pendentes, vencendo HOJE, com aviso ligado
        var vencendoHoje = await db.LancamentosFinanceiros
            .Where(l => l.Tipo == "pagar" && l.Status == "pendente" && l.Avisar &&
                        l.Vencimento >= hoje && l.Vencimento < amanha)
            .ToListAsync();

        if (vencendoHoje.Count == 0) return;

        var porLoja = vencendoHoje.GroupBy(l => l.LojaId);

        foreach (var grupo in porLoja)
        {
            var loja = await db.Lojas.FindAsync(grupo.Key);
            if (loja is null || string.IsNullOrWhiteSpace(loja.Email)) continue;

            var total = grupo.Sum(l => l.Valor);
            var linhas = string.Join("", grupo.Select(l =>
                $"<tr><td style='padding:6px 10px;border-bottom:1px solid #eee'>{l.Descricao}</td>" +
                $"<td style='padding:6px 10px;border-bottom:1px solid #eee;text-align:right'>R$ {l.Valor:N2}</td></tr>"));

            var html = $@"
                <div style='font-family:sans-serif;max-width:480px;margin:0 auto'>
                    <h2 style='color:#c38228'>Contas vencendo hoje — {loja.Nome}</h2>
                    <p>Você tem {grupo.Count()} conta(s) a pagar vencendo hoje, totalizando <strong>R$ {total:N2}</strong>.</p>
                    <table style='width:100%;border-collapse:collapse;margin-top:12px'>{linhas}</table>
                    <p style='margin-top:20px;font-size:13px;color:#888'>Acesse o sistema para marcar como pago.</p>
                </div>";

            try
            {
                var msg = new EmailMessage
                {
                    From = "AldevSoftware <onboarding@resend.dev>", // TEMPORÁRIO: trocar para financeiro@aldevsoftware.com.br quando o domínio estiver verificado
                    Subject = $"💰 {grupo.Count()} conta(s) vencendo hoje — R$ {total:N2}",
                    HtmlBody = html,
                };
                msg.To.Add(loja.Email);

                await resend.EmailSendAsync(msg);
                logger.LogInformation("E-mail de alerta enviado para {Email} ({Loja}).", loja.Email, loja.Nome);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro ao enviar e-mail de alerta para {Email}.", loja.Email);
            }
        }
    }
}