using LojaApi.Data;
using LojaApi.DTOs;
using LojaApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;

namespace LojaApi.Controllers;

[ApiController]
[Route("api/pagamentos")]
public class PagamentosController(
    AppDbContext db,
    TenantService tenantService,
    MercadoPagoService mpService,
    ILogger<PagamentosController> logger) : ControllerBase
{

    private Guid UsuarioId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private async Task<Guid?> GetLojaIdDoUsuario()
    {
        var vinculo = await db.UsuariosLoja
            .FirstOrDefaultAsync(ul => ul.UsuarioId == UsuarioId && ul.Ativo);
        return vinculo?.LojaId;
    }

    // ── Criar cobrança no Mercado Pago ────────────────────────────
    [HttpPost("criar")]
    [Authorize]
    public async Task<IActionResult> CriarPagamento([FromBody] CriarPagamentoMpRequest req)
    {
        var pagamento = await db.Pagamentos
            .Include(p => p.Loja)
            .FirstOrDefaultAsync(p => p.Id == req.PagamentoId);

        if (pagamento is null)
            return NotFound(new { erro = "Fatura não encontrada." });

        if (pagamento.Status == "pago")
            return BadRequest(new { erro = "Esta fatura já foi paga." });

        var loja      = pagamento.Loja;
        var cpf       = req.CpfPagador ?? loja.Cpf ?? loja.Cnpj ?? "00000000000";
        var nome      = req.NomePagador ?? loja.Nome;
        var email     = req.EmailPagador ?? loja.Email;
        var descricao = $"Mensalidade {loja.Nome} - {pagamento.Vencimento:MM/yyyy}";

        MpPaymentResult result;

        result = req.FormaPagamento switch
        {
            "pix"    => await mpService.CriarPix(pagamento.Valor, descricao, email, cpf, nome, pagamento.Id),
            "boleto" => await mpService.CriarBoleto(pagamento.Valor, descricao, email, cpf, nome, pagamento.Id),
            "cartao" => await mpService.CriarCartao(
                pagamento.Valor, descricao,
                req.CardToken!, req.Parcelas ?? 1,
                email, cpf, nome, pagamento.Id),
            _ => new MpPaymentResult { Erro = "Forma de pagamento inválida." }
        };

        if (!result.Sucesso)
            return BadRequest(new { erro = result.Erro });

        // Salva dados do MP no pagamento
        pagamento.MpPaymentId     = result.MpPaymentId;
        pagamento.MpQrCode        = result.QrCode;
        pagamento.MpQrCodeBase64  = result.QrCodeBase64;
        pagamento.MpBoletoUrl     = result.BoletoUrl;
        pagamento.MpBoletoBarcode = result.BoletoBarcode;
        pagamento.FormaPagamento  = req.FormaPagamento;

        // Cartão aprovado na hora
        if (result.Status == "approved")
        {
            await tenantService.RegistrarPagamentoAsync(
                loja.Id, pagamento.Valor, pagamento.Vencimento,
                DateTime.UtcNow, req.FormaPagamento, null, null,
                result.MpPaymentId);
        }

        await db.SaveChangesAsync();

        return Ok(new PagamentoMpResponse(
            result.Status ?? "",
            result.QrCode,
            result.QrCodeBase64,
            result.BoletoUrl,
            result.BoletoBarcode,
            result.MpPaymentId
        ));
    }

    // ── Criar assinatura recorrente (cartão) ──────────────────────
    [HttpPost("assinatura")]
    [Authorize]
    public async Task<IActionResult> CriarAssinatura([FromBody] CriarAssinaturaRequest req)
    {
        var lojaId = await GetLojaIdDoUsuario();
        if (lojaId is null) return BadRequest(new { erro = "Loja não encontrada." });

        var loja = await db.Lojas.FindAsync(lojaId.Value);
        if (loja is null) return NotFound(new { erro = "Loja não encontrada." });

        if (loja.AssinaturaStatus == "authorized")
            return BadRequest(new { erro = "Esta loja já tem uma assinatura ativa." });

        var email = req.EmailPagador ?? loja.Email;
        var motivo = "Assinatura AL Dev Software";

        // Próxima ocorrência do dia de vencimento
        var inicio = ProximoDiaVencimento(loja.MensalidadeDia);
        var backUrl = "https://app.aldevsoftware.com.br/pagamento";

        var result = await mpService.CriarAssinatura(
            loja.MensalidadeValor, motivo, req.CardToken, email, inicio, backUrl);

        if (!result.Sucesso)
            return BadRequest(new { erro = result.Erro });

        loja.MpPreapprovalId = result.PreapprovalId;
        loja.AssinaturaStatus = result.Status ?? "authorized";
        loja.AssinaturaCartaoFinal = req.CartaoFinal;
        loja.AtualizadoEm = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return Ok(new AssinaturaResponse(
            loja.AssinaturaStatus ?? "",
            loja.MpPreapprovalId,
            loja.AssinaturaCartaoFinal));
    }

    // ── Cancelar assinatura ───────────────────────────────────────
    [HttpPost("assinatura/cancelar")]
    [Authorize]
    public async Task<IActionResult> CancelarAssinatura()
    {
        var lojaId = await GetLojaIdDoUsuario();
        if (lojaId is null) return BadRequest(new { erro = "Loja não encontrada." });

        var loja = await db.Lojas.FindAsync(lojaId.Value);
        if (loja is null || loja.MpPreapprovalId is null)
            return BadRequest(new { erro = "Nenhuma assinatura ativa." });

        var ok = await mpService.CancelarAssinatura(loja.MpPreapprovalId);
        if (!ok) return BadRequest(new { erro = "Erro ao cancelar no Mercado Pago." });

        loja.AssinaturaStatus = "cancelled";
        loja.AtualizadoEm = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return Ok(new { status = "cancelled" });
    }

    // Calcula a próxima ocorrência do dia de vencimento
    private static DateTime ProximoDiaVencimento(int dia)
    {
        var hoje = DateTime.UtcNow;
        var diaValido = Math.Clamp(dia, 1, 28);
        var candidato = new DateTime(hoje.Year, hoje.Month, diaValido, 12, 0, 0, DateTimeKind.Utc);
        if (candidato <= hoje.AddHours(1)) // se já passou (ou é agora), joga pro mês seguinte
            candidato = candidato.AddMonths(1);
        return candidato;
    }

    // ── Webhook do Mercado Pago ───────────────────────────────────
    [HttpPost("webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> Webhook()
    {
        string body;
        using (var reader = new System.IO.StreamReader(Request.Body))
            body = await reader.ReadToEndAsync();

        logger.LogInformation("Webhook MP recebido: {Body}", body);

        try
        {
            var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            var tipoStr = root.TryGetProperty("type", out var tipo) ? tipo.GetString() : null;

            // ── Cobrança recorrente de assinatura ──────────────────
            if (tipoStr == "subscription_authorized_payment")
            {
                await TratarCobrancaAssinatura(root);
                return Ok();
            }

            // ── Mudança de status da assinatura ────────────────────
            if (tipoStr == "subscription_preapproval")
            {
                await TratarStatusAssinatura(root);
                return Ok();
            }

            // ── Pagamento avulso (pix/boleto/cartão) ───────────────
            if (tipoStr != "payment")
                return Ok();

            if (!root.TryGetProperty("data", out var data) ||
                !data.TryGetProperty("id", out var idProp))
                return Ok();

            var mpPaymentId = idProp.GetRawText().Trim('"');

            // Busca pagamento no banco pelo ID do MP
            var pagamento = await db.Pagamentos
                .Include(p => p.Loja)
                .FirstOrDefaultAsync(p => p.MpPaymentId == mpPaymentId);

            if (pagamento is null)
            {
                logger.LogWarning("Pagamento MP {Id} não encontrado.", mpPaymentId);
                return Ok();
            }

            // Verifica status no MP
            var status = await mpService.VerificarStatus(mpPaymentId);
            logger.LogInformation("Status MP {Id}: {Status}", mpPaymentId, status);

            if (status == "approved" && pagamento.Status != "pago")
            {
                await tenantService.RegistrarPagamentoAsync(
                    pagamento.LojaId,
                    pagamento.Valor,
                    pagamento.Vencimento,
                    DateTime.UtcNow,
                    pagamento.FormaPagamento ?? "pix",
                    "Pagamento confirmado via Mercado Pago",
                    null,
                    mpPaymentId
                );

                logger.LogInformation("Loja {Id} reativada após pagamento {MpId}.",
                    pagamento.LojaId, mpPaymentId);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao processar webhook MP.");
        }

        return Ok();
    }

    // Trata cada cobrança mensal recorrente da assinatura
    private async Task TratarCobrancaAssinatura(JsonElement root)
    {
        try
        {
            if (!root.TryGetProperty("data", out var data) ||
                !data.TryGetProperty("id", out var idProp))
                return;

            var authPaymentId = idProp.GetRawText().Trim('"');

            // Busca o authorized_payment no MP para saber o preapproval e o status
            var (preapprovalId, status) = await mpService.VerificarCobrancaAssinatura(authPaymentId);
            logger.LogInformation("Cobrança assinatura {Id}: preapproval={Pre} status={St}", authPaymentId, preapprovalId, status);

            if (preapprovalId is null) return;

            var loja = await db.Lojas.FirstOrDefaultAsync(l => l.MpPreapprovalId == preapprovalId);
            if (loja is null)
            {
                logger.LogWarning("Loja não encontrada para preapproval {Id}", preapprovalId);
                return;
            }

            if (status == "processed" || status == "approved")
            {
                await tenantService.RegistrarPagamentoAsync(
                    loja.Id, loja.MensalidadeValor, loja.ProximoVencimento ?? DateTime.UtcNow,
                    DateTime.UtcNow, "cartao",
                    "Cobrança recorrente automática (assinatura)", null, authPaymentId);
                logger.LogInformation("Loja {Id} renovada via assinatura.", loja.Id);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao tratar cobrança de assinatura.");
        }
    }

    // Trata mudança de status da assinatura (cancelada, pausada, etc.)
    private async Task TratarStatusAssinatura(JsonElement root)
    {
        try
        {
            if (!root.TryGetProperty("data", out var data) ||
                !data.TryGetProperty("id", out var idProp))
                return;

            var preapprovalId = idProp.GetRawText().Trim('"');
            var status = await mpService.VerificarAssinatura(preapprovalId);
            if (status is null) return;

            var loja = await db.Lojas.FirstOrDefaultAsync(l => l.MpPreapprovalId == preapprovalId);
            if (loja is null) return;

            loja.AssinaturaStatus = status;
            loja.AtualizadoEm = DateTime.UtcNow;
            await db.SaveChangesAsync();
            logger.LogInformation("Assinatura da loja {Id} agora está {St}.", loja.Id, status);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao tratar status de assinatura.");
        }
    }

    // ── Verificar status de um pagamento ──────────────────────────
    [HttpGet("{id:guid}/status")]
    [Authorize]
    public async Task<IActionResult> VerificarStatus(Guid id)
    {
        var pagamento = await db.Pagamentos.FindAsync(id);
        if (pagamento is null) return NotFound();

        if (pagamento.MpPaymentId != null && pagamento.Status != "pago")
        {
            var status = await mpService.VerificarStatus(pagamento.MpPaymentId);
            if (status == "approved")
            {
                await tenantService.RegistrarPagamentoAsync(
                    pagamento.LojaId, pagamento.Valor, pagamento.Vencimento,
                    DateTime.UtcNow, pagamento.FormaPagamento ?? "pix",
                    null, null, pagamento.MpPaymentId);
            }
        }

        var atualizado = await db.Pagamentos.FindAsync(id);
        return Ok(new { status = atualizado?.Status });
    }
}
