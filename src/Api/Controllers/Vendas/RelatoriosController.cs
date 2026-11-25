using LojaApi.Application.Vendas;
using LojaApi.Shared.Api;
using LojaApi.Shared.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LojaApi.Controllers.Vendas;

// Relatórios de vendas da loja do usuário
[ApiController]
[Route("api/relatorios")]
[Authorize]
public class RelatoriosController(ILojaAtual lojaAtual) : ControllerBase
{
    // GET /api/relatorios/resumo?de=2024-01-01&ate=2024-01-31
    [HttpGet("resumo")]
    public async Task<IActionResult> Resumo([FromServices] IQueryHandler<ResumoVendasQuery> handler,
        [FromQuery] DateTime? de, [FromQuery] DateTime? ate) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), de, ate))).ParaResposta(this);

    // GET /api/relatorios/produtos-ranking?de=...&ate=...&top=10
    [HttpGet("produtos-ranking")]
    public async Task<IActionResult> ProdutosRanking([FromServices] IQueryHandler<ProdutosRankingQuery> handler,
        [FromQuery] DateTime? de, [FromQuery] DateTime? ate, [FromQuery] int top = 10) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), de, ate, top))).ParaResposta(this);

    // GET /api/relatorios/fluxo-diario?mes=6&ano=2025
    [HttpGet("fluxo-diario")]
    public async Task<IActionResult> FluxoDiario([FromServices] IQueryHandler<FluxoDiarioQuery> handler,
        [FromQuery] int mes, [FromQuery] int ano) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), mes, ano))).ParaResposta(this);

    // GET /api/relatorios/fluxo-mensal?ano=2025
    [HttpGet("fluxo-mensal")]
    public async Task<IActionResult> FluxoMensal([FromServices] IQueryHandler<FluxoMensalQuery> handler, [FromQuery] int ano) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), ano))).ParaResposta(this);
}
