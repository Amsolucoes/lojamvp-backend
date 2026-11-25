using LojaApi.Application.Plataforma;
using LojaApi.Shared.Api;
using LojaApi.Shared.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LojaApi.Controllers.Plataforma;

// Cobrança da mensalidade pelo Mercado Pago: cobrança avulsa (Pix, boleto, cartão), assinatura recorrente, webhook e status da fatura.
[ApiController]
[Route("api/pagamentos")]
public class PagamentosController(ILojaAtual lojaAtual) : ControllerBase
{
    // Criar cobrança no Mercado Pago
    [HttpPost("criar")]
    [Authorize]
    public async Task<IActionResult> CriarPagamento([FromBody] CriarPagamentoMpRequest req, [FromServices] ICommandHandler<CriarCobrancaCommand> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), User.IsInRole("superadmin"), req))).ParaResposta(this);

    // Criar assinatura recorrente (cartão)
    [HttpPost("assinatura")]
    [Authorize]
    public async Task<IActionResult> CriarAssinatura([FromBody] CriarAssinaturaRequest req, [FromServices] ICommandHandler<CriarAssinaturaCommand> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), req))).ParaResposta(this);

    // Cancelar assinatura
    [HttpPost("assinatura/cancelar")]
    [Authorize]
    public async Task<IActionResult> CancelarAssinatura([FromServices] ICommandHandler<CancelarAssinaturaCommand> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User)))).ParaResposta(this);

    // Webhook do Mercado Pago (sempre responde 200)
    [HttpPost("webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> Webhook([FromServices] ICommandHandler<ProcessarWebhookMpCommand> handler)
    {
        string corpo;
        using (var reader = new StreamReader(Request.Body))
            corpo = await reader.ReadToEndAsync();

        await handler.HandleAsync(new(corpo));
        return Ok();
    }

    // Verificar status de um pagamento
    [HttpGet("{id:guid}/status")]
    [Authorize]
    public async Task<IActionResult> VerificarStatus(Guid id, [FromServices] ICommandHandler<ConsultarStatusPagamentoCommand> handler) =>
        (await handler.HandleAsync(new(id, await lojaAtual.ObterIdAsync(User), User.IsInRole("superadmin")))).ParaResposta(this);
}
