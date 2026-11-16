using System.Text.Json;
using LojaApi.Shared.Application;
using Microsoft.Extensions.Logging;

namespace LojaApi.Application.Plataforma;

// Webhook do Mercado Pago (rota pública): pagamento avulso aprovado, cobrança recorrente da assinatura e mudança de status da assinatura.
// Sempre devolve sucesso (o Mercado Pago reenviaria se não); qualquer erro é só registrado em log.
public class ProcessarWebhookMpCommandHandler(IPlataformaRepository repositorio, IGatewayDePagamentoPlataforma gateway, ILogger<ProcessarWebhookMpCommandHandler> logger) : ICommandHandler<ProcessarWebhookMpCommand>
{
    public async Task<Resultado<object>> HandleAsync(ProcessarWebhookMpCommand command)
    {
        logger.LogInformation("Webhook MP recebido: {Body}", command.Corpo);

        try
        {
            var doc = JsonDocument.Parse(command.Corpo);
            var root = doc.RootElement;

            var tipoStr = root.TryGetProperty("type", out var tipo) ? tipo.GetString() : null;

            if (tipoStr == "subscription_authorized_payment")
            {
                await TratarCobrancaAssinaturaAsync(root);
                return Resultado.Ok(new object());
            }

            if (tipoStr == "subscription_preapproval")
            {
                await TratarStatusAssinaturaAsync(root);
                return Resultado.Ok(new object());
            }

            if (tipoStr != "payment") return Resultado.Ok(new object());

            if (!root.TryGetProperty("data", out var data) || !data.TryGetProperty("id", out var idProp))
                return Resultado.Ok(new object());

            var mpPaymentId = idProp.GetRawText().Trim('"');

            var pagamento = await repositorio.ObterPagamentoDaCobrancaAsync(mpPaymentId);
            if (pagamento is null)
            {
                logger.LogWarning("Pagamento MP {Id} não encontrado.", mpPaymentId);
                return Resultado.Ok(new object());
            }

            var status = await gateway.VerificarStatusAsync(mpPaymentId);
            logger.LogInformation("Status MP {Id}: {Status}", mpPaymentId, status);

            if (status == "approved" && pagamento.Status != "pago")
            {
                await PlataformaApoio.RegistrarPagamentoAsync(repositorio, logger,
                    pagamento.LojaId, pagamento.Valor, pagamento.Vencimento, DateTime.UtcNow,
                    pagamento.CobrancaMpPaymentId == mpPaymentId ? "pix" : (pagamento.FormaPagamento ?? "pix"),
                    "Pagamento confirmado via Mercado Pago", null, mpPaymentId);

                logger.LogInformation("Loja {Id} reativada após pagamento {MpId}.", pagamento.LojaId, mpPaymentId);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao processar webhook MP.");
        }

        return Resultado.Ok(new object());
    }

    // Cada cobrança mensal recorrente da assinatura
    private async Task TratarCobrancaAssinaturaAsync(JsonElement root)
    {
        try
        {
            if (!root.TryGetProperty("data", out var data) || !data.TryGetProperty("id", out var idProp)) return;

            var authPaymentId = idProp.GetRawText().Trim('"');

            var (preapprovalId, status) = await gateway.VerificarCobrancaAssinaturaAsync(authPaymentId);
            logger.LogInformation("Cobrança assinatura {Id}: preapproval={Pre} status={St}", authPaymentId, preapprovalId, status);

            if (preapprovalId is null) return;

            var loja = await repositorio.ObterLojaPorAssinaturaAsync(preapprovalId);
            if (loja is null)
            {
                logger.LogWarning("Loja não encontrada para preapproval {Id}", preapprovalId);
                return;
            }

            if (status == "processed" || status == "approved")
            {
                await PlataformaApoio.RegistrarPagamentoAsync(repositorio, logger,
                    loja.Id, loja.MensalidadeValor, loja.ProximoVencimento ?? DateTime.UtcNow,
                    DateTime.UtcNow, "cartao", "Cobrança recorrente automática (assinatura)", null, authPaymentId);
                logger.LogInformation("Loja {Id} renovada via assinatura.", loja.Id);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao tratar cobrança de assinatura.");
        }
    }

    // Mudança de status da assinatura (cancelada, pausada, etc.)
    private async Task TratarStatusAssinaturaAsync(JsonElement root)
    {
        try
        {
            if (!root.TryGetProperty("data", out var data) || !data.TryGetProperty("id", out var idProp)) return;

            var preapprovalId = idProp.GetRawText().Trim('"');
            var status = await gateway.VerificarAssinaturaAsync(preapprovalId);
            if (status is null) return;

            var loja = await repositorio.ObterLojaPorAssinaturaAsync(preapprovalId);
            if (loja is null) return;

            loja.AssinaturaStatus = status;
            loja.AtualizadoEm = DateTime.UtcNow;
            await repositorio.SalvarAsync();
            logger.LogInformation("Assinatura da loja {Id} agora está {St}.", loja.Id, status);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao tratar status de assinatura.");
        }
    }
}
