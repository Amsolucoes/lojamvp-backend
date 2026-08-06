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

        // 1) Gera o contrato em PDF primeiro — usado nos dois e-mails abaixo
        byte[]? pdfBytes = null;
        try
        {
            pdfBytes = ContratoChacaraService.GerarContratoPdf(reserva, loja, info, cfg);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao gerar contrato PDF da reserva {ReservaId}.", reserva.Id);
        }

        // Linha de valor pago/pendente reaproveitada nos dois e-mails abaixo
        var saldoPendente = reserva.Valor - reserva.ValorPago;
        var linhaValor = saldoPendente > 0.01m
            ? $@"<tr><td style='padding:6px 10px'>Valor pago (sinal)</td><td style='padding:6px 10px;text-align:right'><strong>R$ {reserva.ValorPago:N2}</strong></td></tr>
                 <tr><td style='padding:6px 10px'>Saldo a receber</td><td style='padding:6px 10px;text-align:right;color:#c38228'><strong>R$ {saldoPendente:N2}</strong></td></tr>"
            : $@"<tr><td style='padding:6px 10px'>Valor pago</td><td style='padding:6px 10px;text-align:right'><strong>R$ {reserva.ValorPago:N2}</strong></td></tr>";

        // 2) E-mail pro dono da loja, com o contrato anexado (se foi gerado com sucesso)
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
                            <tr><td style='padding:6px 10px'>Valor da reserva</td><td style='padding:6px 10px;text-align:right'>R$ {reserva.Valor:N2}</td></tr>
                            {linhaValor}
                        </table>
                    </div>";

                var msgDono = new EmailMessage
                {
                    From = "AldevSoftware <reservas@aldevsoftware.com.br>",
                    Subject = $"✅ Reserva confirmada — {reserva.ClienteNome} ({reserva.DataInicio:dd/MM})",
                    HtmlBody = htmlDono,
                };
                msgDono.To.Add(loja.Email);

                if (pdfBytes != null)
                {
                    msgDono.Attachments = new List<EmailAttachment>
                    {
                        new EmailAttachment
                        {
                            Filename = $"contrato-reserva-{reserva.Id}.pdf",
                            Content = pdfBytes,
                        },
                    };
                }

                await resend.EmailSendAsync(msgDono);
                logger.LogInformation("E-mail de confirmação enviado ao dono da loja {LojaId}.", loja.Id);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro ao enviar e-mail de confirmação pro dono da loja {LojaId}.", loja.Id);
            }
        }

        if (pdfBytes is null) return; // sem PDF gerado, não dá pra mandar o do cliente com anexo

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
                            <tr><td style='padding:6px 10px'>Valor da reserva</td><td style='padding:6px 10px;text-align:right'>R$ {reserva.Valor:N2}</td></tr>
                            {linhaValor}
                        </table>
                        {(saldoPendente > 0.01m ? "<p style='margin-top:12px;font-size:13px;color:#c38228'>O saldo restante é acertado na chegada.</p>" : "")}
                        <p style='margin-top:16px'>Segue em anexo o contrato de locação. Guarde este e-mail.</p>
                    </div>";

                var msgCliente = new EmailMessage
                {
                    From = "AldevSoftware <reservas@aldevsoftware.com.br>",
                    Subject = $"Reserva confirmada — {loja.Nome}",
                    HtmlBody = htmlCliente,
                    Attachments = new List<EmailAttachment>
                    {
                        new EmailAttachment
                        {
                            Filename = $"contrato-reserva-{reserva.Id}.pdf",
                            Content = pdfBytes,
                        },
                    },
                };
                msgCliente.To.Add(reserva.ClienteEmail);
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

    public async Task<int> ExpirarReservasVencidasAsync()
    {
        var agora = DateTime.UtcNow;
        var vencidas = await db.Reservas
            .Where(r => r.Status == "pendente_pagamento" && r.ExpiraEm != null && r.ExpiraEm < agora)
            .ToListAsync();
        foreach (var r in vencidas) r.Status = "expirada";
        if (vencidas.Count > 0) await db.SaveChangesAsync();
        return vencidas.Count;
    }

    public async Task<int> EnviarLembretesAvaliacaoDoDiaAsync()
    {
        var ontemInicio = DateTime.UtcNow.Date.AddDays(-1);
        var ontemFim = ontemInicio.AddDays(1);

        var reservas = await db.Reservas
            .Where(r => r.Status == "confirmada" && !r.AvisoAvaliacaoEnviado
                     && r.DataFim >= ontemInicio && r.DataFim < ontemFim
                     && r.ClienteEmail != "")
            .ToListAsync();

        var enviados = 0;
        foreach (var reserva in reservas)
        {
            var loja = await db.Lojas.FindAsync(reserva.LojaId);
            if (loja is null || string.IsNullOrWhiteSpace(loja.Slug)) continue;

            var link = $"https://app.aldevsoftware.com.br/chacara-site/{loja.Slug}/avaliar/{reserva.Id}";

            try
            {
                var html = $@"
                    <div style='font-family:sans-serif;max-width:480px;margin:0 auto'>
                        <h2 style='color:#2f7d4f'>Como foi sua estadia?</h2>
                        <p>Olá, {reserva.ClienteNome}! Esperamos que tenha aproveitado a {loja.Nome}.</p>
                        <p>Sua opinião é muito importante — conta pra gente como foi, em menos de 1 minuto:</p>
                        <div style='margin-top:20px;text-align:center'>
                            <a href='{link}' style='background:#2f7d4f;color:#fff;padding:10px 24px;border-radius:8px;text-decoration:none;font-weight:600'>
                                Avaliar minha estadia
                            </a>
                        </div>
                    </div>";

                var msg = new EmailMessage
                {
                    From = "AldevSoftware <reservas@aldevsoftware.com.br>",
                    Subject = $"Como foi sua estadia na {loja.Nome}?",
                    HtmlBody = html,
                };
                msg.To.Add(reserva.ClienteEmail);
                await resend.EmailSendAsync(msg);

                reserva.AvisoAvaliacaoEnviado = true;
                await db.SaveChangesAsync();
                enviados++;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro ao enviar lembrete de avaliação da reserva {ReservaId}.", reserva.Id);
            }
        }

        return enviados;
    }

    public async Task NotificarPendenteAsync(Reserva reserva)
    {
        var loja = await db.Lojas.FindAsync(reserva.LojaId);
        if (loja is null || string.IsNullOrWhiteSpace(loja.Email)) return;

        try
        {
            var html = $@"
                <div style='font-family:sans-serif;max-width:480px;margin:0 auto'>
                    <h2 style='color:#c38228'>Nova reserva pendente — {loja.Nome}</h2>
                    <p><strong>{reserva.ClienteNome}</strong> criou uma reserva e está indo para o pagamento.</p>
                    <table style='width:100%;border-collapse:collapse;margin-top:12px'>
                        <tr><td style='padding:6px 10px'>Período</td><td style='padding:6px 10px;text-align:right'>{reserva.DataInicio:dd/MM/yyyy} — {reserva.DataFim:dd/MM/yyyy}</td></tr>
                        <tr><td style='padding:6px 10px'>Pessoas</td><td style='padding:6px 10px;text-align:right'>{reserva.Pessoas}</td></tr>
                        <tr><td style='padding:6px 10px'>Telefone</td><td style='padding:6px 10px;text-align:right'>{reserva.ClienteTelefone}</td></tr>
                        <tr><td style='padding:6px 10px'>Valor</td><td style='padding:6px 10px;text-align:right'><strong>R$ {reserva.Valor:N2}</strong></td></tr>
                    </table>
                    <p style='margin-top:16px;font-size:13px;color:#888'>
                        Esta reserva expira em {reserva.ExpiraEm:HH:mm} se o pagamento não for concluído.
                        Se o cliente pagar por fora (Pix direto), você pode confirmar manualmente no sistema.
                    </p>
                </div>";

            var msg = new EmailMessage
            {
                From = "AldevSoftware <reservas@aldevsoftware.com.br>",
                Subject = $"⏳ Nova reserva pendente — {reserva.ClienteNome} ({reserva.DataInicio:dd/MM})",
                HtmlBody = html,
            };
            msg.To.Add(loja.Email);
            await resend.EmailSendAsync(msg);
            logger.LogInformation("E-mail de reserva pendente enviado ao dono da loja {LojaId}.", loja.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao enviar e-mail de reserva pendente pro dono da loja {LojaId}.", loja.Id);
        }
    }

    public async Task ReenviarContratoAtualizadoAsync(Reserva reserva)
    {
        if (string.IsNullOrWhiteSpace(reserva.ClienteEmail)) return;

        var loja = await db.Lojas.FindAsync(reserva.LojaId);
        if (loja is null) return;

        var info = await db.InfosChacara.FirstOrDefaultAsync(i => i.LojaId == reserva.LojaId);
        var cfg = await db.ConfiguracoesPrecoChacara.FirstOrDefaultAsync(c => c.LojaId == reserva.LojaId)
            ?? new ConfiguracaoPrecoChacara { LojaId = reserva.LojaId };

        byte[] pdfBytes;
        try
        {
            pdfBytes = ContratoChacaraService.GerarContratoPdf(reserva, loja, info, cfg);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao gerar contrato PDF atualizado da reserva {ReservaId}.", reserva.Id);
            return;
        }

        try
        {
            var html = $@"
                <div style='font-family:sans-serif;max-width:480px;margin:0 auto'>
                    <h2 style='color:#2f7d4f'>Reserva atualizada</h2>
                    <p>Olá, {reserva.ClienteNome}! Os dados da sua reserva na {loja.Nome} foram atualizados.</p>
                    <table style='width:100%;border-collapse:collapse;margin-top:12px'>
                        <tr><td style='padding:6px 10px'>Novo período</td><td style='padding:6px 10px;text-align:right'>{reserva.DataInicio:dd/MM/yyyy} — {reserva.DataFim:dd/MM/yyyy}</td></tr>
                        <tr><td style='padding:6px 10px'>Novo valor</td><td style='padding:6px 10px;text-align:right'><strong>R$ {reserva.Valor:N2}</strong></td></tr>
                    </table>
                    <p style='margin-top:16px'>Segue em anexo o contrato atualizado. Este documento substitui qualquer contrato anterior enviado.</p>
                </div>";

            var msg = new EmailMessage
            {
                From = "AldevSoftware <reservas@aldevsoftware.com.br>",
                Subject = $"Reserva atualizada — {loja.Nome}",
                HtmlBody = html,
                Attachments = new List<EmailAttachment>
                {
                    new EmailAttachment
                    {
                        Filename = $"contrato-reserva-{reserva.Id}-atualizado.pdf",
                        Content = pdfBytes,
                    },
                },
            };
            msg.To.Add(reserva.ClienteEmail);
            await resend.EmailSendAsync(msg);

            reserva.ContratoEnviadoEm = DateTime.UtcNow;
            await db.SaveChangesAsync();

            logger.LogInformation("Contrato atualizado reenviado ao cliente da reserva {ReservaId}.", reserva.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao reenviar contrato atualizado pro cliente da reserva {ReservaId}.", reserva.Id);
        }
    }

    public async Task<bool> EnviarContratoManualAsync(Reserva reserva)
    {
        if (string.IsNullOrWhiteSpace(reserva.ClienteEmail)) return false;

        var loja = await db.Lojas.FindAsync(reserva.LojaId);
        if (loja is null) return false;

        var info = await db.InfosChacara.FirstOrDefaultAsync(i => i.LojaId == reserva.LojaId);
        var cfg = await db.ConfiguracoesPrecoChacara.FirstOrDefaultAsync(c => c.LojaId == reserva.LojaId)
            ?? new ConfiguracaoPrecoChacara { LojaId = reserva.LojaId };

        byte[] pdfBytes;
        try
        {
            pdfBytes = ContratoChacaraService.GerarContratoPdf(reserva, loja, info, cfg);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao gerar contrato PDF (envio manual) da reserva {ReservaId}.", reserva.Id);
            return false;
        }

        try
        {
            var html = $@"
                <div style='font-family:sans-serif;max-width:480px;margin:0 auto'>
                    <h2 style='color:#2f7d4f'>Contrato da sua reserva</h2>
                    <p>Olá, {reserva.ClienteNome}! Segue em anexo o contrato de locação referente à sua reserva na {loja.Nome}.</p>
                    <table style='width:100%;border-collapse:collapse;margin-top:12px'>
                        <tr><td style='padding:6px 10px'>Período</td><td style='padding:6px 10px;text-align:right'>{reserva.DataInicio:dd/MM/yyyy} — {reserva.DataFim:dd/MM/yyyy}</td></tr>
                        <tr><td style='padding:6px 10px'>Valor total</td><td style='padding:6px 10px;text-align:right'><strong>R$ {reserva.Valor:N2}</strong></td></tr>
                    </table>
                </div>";

            var msg = new EmailMessage
            {
                From = "AldevSoftware <reservas@aldevsoftware.com.br>",
                Subject = $"Contrato — {loja.Nome}",
                HtmlBody = html,
                Attachments = new List<EmailAttachment>
                {
                    new EmailAttachment
                    {
                        Filename = $"contrato-reserva-{reserva.Id}.pdf",
                        Content = pdfBytes,
                    },
                },
            };
            msg.To.Add(reserva.ClienteEmail);
            await resend.EmailSendAsync(msg);

            reserva.ContratoEnviadoEm = DateTime.UtcNow;
            await db.SaveChangesAsync();

            logger.LogInformation("Contrato enviado manualmente ao cliente da reserva {ReservaId}.", reserva.Id);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao enviar contrato manualmente pro cliente da reserva {ReservaId}.", reserva.Id);
            return false;
        }
    }
}