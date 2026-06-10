using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using LojaApi.Data;
using LojaApi.DTOs;
using LojaApi.Models;
using LojaApi.Services;

namespace LojaApi.Controllers;

[ApiController]
[Route("api/pagamentos")]
public class PagamentosController(
    AppDbContext db,
    TenantService tenantService,
    MercadoPagoService mpService,
    ILogger<PagamentosController> logger) : ControllerBase
{
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

            // Mercado Pago envia tipo "payment"
            if (!root.TryGetProperty("type", out var tipo) || tipo.GetString() != "payment")
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
