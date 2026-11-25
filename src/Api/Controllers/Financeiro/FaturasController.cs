using LojaApi.Application.Financeiro;
using LojaApi.Shared.Api;
using LojaApi.Shared.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LojaApi.Controllers.Financeiro;

// Fatura do cartão de crédito: ver, pagamento antecipado, pagar (total, parcial, parcelado ou desfazer) e faturas de referência.
[ApiController]
[Route("api/financeiro")]
[Authorize]
public class FaturasController(ILojaAtual lojaAtual) : ControllerBase
{
    [HttpGet("cartoes/{id:guid}/fatura")]
    public async Task<IActionResult> VerFatura(Guid id, [FromQuery] int ano, [FromQuery] int mes, [FromServices] IQueryHandler<VerFaturaQuery> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), id, ano, mes))).ParaResposta(this);

    [HttpPost("cartoes/{id:guid}/fatura/antecipado")]
    public async Task<IActionResult> AdicionarAntecipadoFatura(Guid id, [FromQuery] int ano, [FromQuery] int mes, [FromBody] AntecipadoFaturaRequest req, [FromServices] ICommandHandler<AdicionarAntecipadoFaturaCommand> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), id, ano, mes, req))).ParaResposta(this);

    [HttpDelete("cartoes/fatura/antecipado/{id:guid}")]
    public async Task<IActionResult> ExcluirAntecipadoFatura(Guid id, [FromServices] ICommandHandler<ExcluirAntecipadoFaturaCommand> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), id))).ParaResposta(this);

    [HttpPost("cartoes/{id:guid}/fatura/pagamento")]
    public async Task<IActionResult> PagarFatura(Guid id, [FromQuery] int ano, [FromQuery] int mes, [FromBody] PagarFaturaRequest req, [FromServices] ICommandHandler<PagarFaturaCommand> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), id, ano, mes, req))).ParaResposta(this);

    [HttpGet("cartoes/{id:guid}/faturas-referencia")]
    public async Task<IActionResult> FaturasReferencia(Guid id, [FromServices] IQueryHandler<FaturasReferenciaQuery> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), id))).ParaResposta(this);
}
