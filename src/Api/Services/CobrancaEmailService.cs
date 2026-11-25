using System.Globalization;
using System.Net;
using LojaApi.Data;
using LojaApi.Models;
using Microsoft.EntityFrameworkCore;
using Resend;

namespace LojaApi.Services;

// Cobrança automática da mensalidade das lojas: no dia do vencimento e todos os dias enquanto estiver
// em atraso, envia um e-mail com o Pix copia e cola (gerado na hora) e o link do painel de pagamento.
// Quando o Pix é pago o webhook do Mercado Pago registra o pagamento e a loja é reativada.
public class CobrancaEmailService(
    AppDbContext db,
    IResend resend,
    MercadoPagoService mp,
    ILogger<CobrancaEmailService> logger)
{
    private static readonly TimeSpan OffsetBrasilia = TimeSpan.FromHours(-3); // sem horário de verão desde 2019
    private static readonly CultureInfo PtBr = new("pt-BR");
    private const int MAX_DIAS_AVISO = 30;      // depois de 30 dias de atraso para de enviar (evita spam)
    private const int HORAS_VALIDADE_PIX = 23;  // menor que 24h: o Pix do dia seguinte substitui o anterior
    private const string URL_PAINEL = "https://admin.aldevsoftware.com.br/cliente";

    public async Task<int> EnviarCobrancasDoDiaAsync(DateTime agoraUtc, CancellationToken ct = default)
    {
        var hojeBr = (agoraUtc + OffsetBrasilia).Date;
        var inicioDiaUtc = DateTime.SpecifyKind(hojeBr - OffsetBrasilia, DateTimeKind.Utc);
        var fimDiaUtc = inicioDiaUtc.AddDays(1);

        var abertos = await db.Pagamentos
            .Include(p => p.Loja)
            .Where(p => (p.Status == "pendente" || p.Status == "atrasado")
                        && p.Vencimento < fimDiaUtc
                        && !p.Loja.EhTeste
                        && p.Loja.Status != StatusLoja.Cancelado)
            .ToListAsync(ct);

        var enviados = 0;

        // Uma cobrança por loja: a fatura aberta mais antiga
        foreach (var pagamento in abertos.GroupBy(p => p.LojaId).Select(g => g.OrderBy(p => p.Vencimento).First()))
        {
            if (ct.IsCancellationRequested) break;

            var loja = pagamento.Loja;
            if (string.IsNullOrWhiteSpace(loja.Email)) continue;
            if (pagamento.UltimoAvisoCobrancaEm.HasValue && pagamento.UltimoAvisoCobrancaEm.Value >= inicioDiaUtc) continue;

            var dias = TenantService.DiasDeAtraso(pagamento.Vencimento, agoraUtc);
            if (dias < 0 || dias > MAX_DIAS_AVISO) continue;
            // Quem paga no cartão recorrente é cobrado sozinho no vencimento; só avisa se atrasar
            if (dias == 0 && loja.AssinaturaStatus == "authorized") continue;

            try
            {
                var pix = await GerarPixAsync(pagamento, loja, agoraUtc);

                var msg = new EmailMessage
                {
                    From = "AldevSoftware <financeiro@aldevsoftware.com.br>",
                    Subject = dias == 0
                        ? "Sua mensalidade AldevSoftware vence hoje — " + pagamento.Valor.ToString("C", PtBr)
                        : "Mensalidade em atraso (" + dias + (dias == 1 ? " dia" : " dias") + ") — " + pagamento.Valor.ToString("C", PtBr),
                    HtmlBody = MontarHtml(loja, pagamento, dias, pix),
                };
                msg.To.Add(loja.Email);
                await resend.EmailSendAsync(msg);

                pagamento.UltimoAvisoCobrancaEm = agoraUtc;
                enviados++;
                logger.LogInformation("E-mail de cobrança enviado para {Email} ({Loja}), {Dias} dia(s) de atraso.", loja.Email, loja.Nome, dias);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro ao enviar e-mail de cobrança para {Email} ({Loja}).", loja.Email, loja.Nome);
            }

            // Salva por loja: o id do Pix gerado precisa ficar gravado para o webhook reconhecer o pagamento
            await db.SaveChangesAsync(ct);
        }

        return enviados;
    }

    private async Task<string?> GerarPixAsync(Pagamento pagamento, Loja loja, DateTime agoraUtc)
    {
        var documento = new string((loja.Cpf ?? loja.Cnpj ?? "").Where(char.IsDigit).ToArray());
        if (documento.Length != 11 && documento.Length != 14) return null; // sem CPF/CNPJ o Mercado Pago recusa: e-mail vai só com o link

        try
        {
            var r = await mp.CriarPix(
                pagamento.Valor,
                $"Mensalidade {loja.Nome} - {pagamento.Vencimento:MM/yyyy}",
                loja.Email, documento, loja.Nome, pagamento.Id,
                agoraUtc.AddHours(HORAS_VALIDADE_PIX));

            if (r.Sucesso && !string.IsNullOrWhiteSpace(r.QrCode))
            {
                pagamento.CobrancaMpPaymentId = r.MpPaymentId;
                return r.QrCode;
            }
            logger.LogWarning("Pix de cobrança não gerado para {Loja}: {Erro}", loja.Nome, r.Erro);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Falha ao gerar Pix de cobrança para {Loja}.", loja.Nome);
        }
        return null;
    }

    private static string MontarHtml(Loja loja, Pagamento pagamento, int dias, string? pix)
    {
        var nome = WebUtility.HtmlEncode(loja.Nome);
        var valor = pagamento.Valor.ToString("C", PtBr);
        var venc = (pagamento.Vencimento + OffsetBrasilia).ToString("dd/MM/yyyy");

        var frase = dias > 0
            ? "A mensalidade de <strong>" + valor + "</strong>, que venceu em <strong>" + venc + "</strong>, está em atraso há " + dias + (dias == 1 ? " dia" : " dias") + "."
            : "A mensalidade de <strong>" + valor + "</strong> vence hoje (<strong>" + venc + "</strong>).";

        string titulo, aviso;
        if (loja.Status == StatusLoja.Bloqueado)
        {
            titulo = "Mensalidade em atraso";
            aviso = "O acesso ao sistema está <strong>bloqueado</strong>. Assim que o pagamento for confirmado, o acesso é liberado automaticamente.";
        }
        else if (dias == 0)
        {
            titulo = "Sua mensalidade vence hoje";
            aviso = "Pague hoje para evitar o bloqueio do acesso ao sistema.";
        }
        else
        {
            titulo = "Mensalidade em atraso";
            aviso = "Regularize o pagamento para evitar o bloqueio do acesso ao sistema.";
        }

        var blocoPix = pix is null ? "" : $@"
            <p style='margin:18px 0 6px;font-weight:600'>Pague agora com Pix (copia e cola):</p>
            <div style='background:#f6f6f6;border:1px solid #e2e2e2;border-radius:8px;padding:12px;font-family:monospace;font-size:12px;word-break:break-all'>{WebUtility.HtmlEncode(pix)}</div>
            <p style='font-size:12px;color:#888;margin-top:6px'>No app do seu banco, escolha Pix &gt; Pix Copia e Cola. Este código vale por {HORAS_VALIDADE_PIX} horas; amanhã enviamos um novo se ainda estiver em aberto. O acesso é liberado automaticamente após a confirmação.</p>";

        return $@"
            <div style='font-family:sans-serif;max-width:520px;margin:0 auto'>
                <div style='text-align:center;margin-bottom:20px'>
                    <strong style='font-size:18px;color:#c38228'>AldevSoftware</strong>
                </div>
                <h2 style='color:#c38228;margin-bottom:8px'>{titulo}</h2>
                <p>Olá, <strong>{nome}</strong>.</p>
                <p>{frase}</p>
                <p>{aviso}</p>
                {blocoPix}
                <div style='margin-top:24px;text-align:center'>
                    <a href='{URL_PAINEL}' style='background:#c38228;color:#fff;padding:10px 24px;border-radius:8px;text-decoration:none;font-weight:600'>
                        Pagar pelo painel (Pix, boleto ou cartão)
                    </a>
                </div>
                <p style='margin-top:24px;font-size:12px;color:#999;text-align:center'>
                    Se você já pagou, desconsidere este e-mail — a baixa é automática.
                </p>
            </div>";
    }
}
