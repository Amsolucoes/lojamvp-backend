using LojaApi.Data;
using LojaApi.src.Models;
using Microsoft.EntityFrameworkCore;
using Resend;

namespace LojaApi.src.Services;

public class ReservaChacaraNotificacaoService(AppDbContext db, IResend resend, ILogger<ReservaChacaraNotificacaoService> logger)
{
    public async Task NotificarConfirmacaoAsync(Reserva reserva)
    {
        var loja = await db.Lojas.FindAsync(reserva.LojaId);
        if (loja is null) return;

        var info = await db.InfosChacara.FirstOrDefaultAsync(i => i.LojaId == reserva.LojaId);
        var cfg = await db.ConfiguracoesPrecoChacara.FirstOrDefaultAsync(c => c.LojaId == reserva.LojaId)
            ?? new ConfiguracaoPrecoChacara { LojaId = reserva.LojaId };

        // 1) E-mail pro dono da loja
        if (!string.IsNullOrWhiteSpace(loja.Email))
        {
            try
            {
                var htmlDono = $@"
                    <div style='font-family:sans-serif;max-width:480px;margin:0 auto'>
                        <h2 style='color:#2f7d4f'>Nova reserva confirmada — {loja.Nome}</h2>
                        <p><strong>{reserva.ClienteNome}</strong> confirmou o pagamento da reserva.</p>
                        <table style='width:100%;border-collapse:collapse;margin-top:12px'>
                            <tr><td style='padding:6px 10px'>Período</td><td style='padding:6px 10px;text-align:right'>{reserva.DataInicio:dd/MM/yyyy} — {reserva.DataFim:dd/MM/yyyy}</td></tr>
                            <tr><td style='padding:6px 10px'>Pessoas</td><td style='padding:6px 10px;text-align:right'>{reserva.Pessoas}</td></tr>
                            <tr><td style='padding:6px 10px'>Telefone</td><td style='padding:6px 10px;text-align:right'>{reserva.ClienteTelefone}</td></tr>
                            <tr><td style='padding:6px 10px'>Valor</td><td style='padding:6px 10px;text-align:right'><strong>R$ {reserva.Valor:N2}</strong></td></tr>
                        </table>
                    </div>";

                var msgDono = new EmailMessage
                {
                    From = "AldevSoftware <reservas@aldevsoftware.com.br>",
                    Subject = $"✅ Reserva confirmada — {reserva.ClienteNome} ({reserva.DataInicio:dd/MM})",
                    HtmlBody = htmlDono,
                };
                msgDono.To.Add(loja.Email);
                await resend.EmailSendAsync(msgDono);
                logger.LogInformation("E-mail de confirmação enviado ao dono da loja {LojaId}.", loja.Id);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro ao enviar e-mail de confirmação pro dono da loja {LojaId}.", loja.Id);
            }
        }

        // 2) Gera o contrato em PDF
        byte[] pdfBytes;
        try
        {
            pdfBytes = ContratoChacaraService.GerarContratoPdf(reserva, loja, info, cfg);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao gerar contrato PDF da reserva {ReservaId}.", reserva.Id);
            return;
        }

        // 3) E-mail pro cliente, com contrato anexado
        if (!string.IsNullOrWhiteSpace(reserva.ClienteEmail))
        {
            try
            {
                var htmlCliente = $@"
                    <div style='font-family:sans-serif;max-width:480px;margin:0 auto'>
                        <h2 style='color:#2f7d4f'>Reserva confirmada!</h2>
                        <p>Olá, {reserva.ClienteNome}! Sua reserva na {loja.Nome} foi confirmada.</p>
                        <table style='width:100%;border-collapse:collapse;margin-top:12px'>
                            <tr><td style='padding:6px 10px'>Período</td><td style='padding:6px 10px;text-align:right'>{reserva.DataInicio:dd/MM/yyyy} — {reserva.DataFim:dd/MM/yyyy}</td></tr>
                            <tr><td style='padding:6px 10px'>Valor pago</td><td style='padding:6px 10px;text-align:right'><strong>R$ {reserva.Valor:N2}</strong></td></tr>
                        </table>
                        <p style='margin-top:16px'>Segue em anexo o contrato de locação. Guarde este e-mail.</p>
                    </div>";

                var msgCliente = new EmailMessage
                {
                    From = "AldevSoftware <reservas@aldevsoftware.com.br>",
                    Subject = $"Reserva confirmada — {loja.Nome}",
                    HtmlBody = htmlCliente,
                };
                msgCliente.To.Add(reserva.ClienteEmail);
                msgCliente.Attachments.Add(new EmailAttachment
                {
                    Filename = $"contrato-reserva-{reserva.Id}.pdf",
                    Content = pdfBytes,
                });

                await resend.EmailSendAsync(msgCliente);

                reserva.ContratoEnviadoEm = DateTime.UtcNow;
                await db.SaveChangesAsync();

                logger.LogInformation("Contrato enviado ao cliente da reserva {ReservaId}.", reserva.Id);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro ao enviar e-mail com contrato pro cliente da reserva {ReservaId}.", reserva.Id);
            }
        }
    }
}