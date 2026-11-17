using LojaApi.Application.Financeiro;
using LojaApi.Shared.Api;
using LojaApi.Shared.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LojaApi.Controllers.Financeiro;

// Contas a Pagar: lançamentos por tipo, visão unificada (lançamentos + faturas de cartão) e faturas dos cartões no mês.
[ApiController]
[Route("api/financeiro")]
[Authorize]
public class ContasAPagarController(ILojaAtual lojaAtual) : ControllerBase
{
    [HttpGet("lancamentos")]
    public async Task<IActionResult> ListarLancamentos([FromQuery] string tipo, [FromQuery] int? ano, [FromQuery] int? mes, [FromServices] IQueryHandler<ListarLancamentosQuery> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), tipo, ano, mes))).ParaResposta(this);

    // Contas a Pagar unificado (lançamentos + cartões)
    [HttpGet("pagar-unificado")]
    public async Task<IActionResult> PagarUnificado([FromServices] IQueryHandler<PagarUnificadoQuery> handler, [FromQuery] int? ano, [FromQuery] int? mes,
        [FromQuery] string modo = "agrupado", [FromQuery] DateTime? de = null, [FromQuery] DateTime? ate = null) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), ano, mes, modo, de, ate))).ParaResposta(this);

    // Faturas de cartão de um mês — com o que já foi pago, parcial e parcelamento
    [HttpGet("faturas-do-mes")]
    public async Task<IActionResult> FaturasDoMes([FromQuery] int ano, [FromQuery] int mes, [FromServices] IQueryHandler<FaturasDoMesQuery> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), ano, mes))).ParaResposta(this);
}
