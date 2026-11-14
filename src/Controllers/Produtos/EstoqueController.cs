using LojaApi.Application.Produtos;
using LojaApi.Shared.Api;
using LojaApi.Shared.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LojaApi.Controllers.Produtos;

// Estoque: histórico de movimentos, ajustes, alertas de estoque baixo e ajuste por variação.
[ApiController]
[Route("api/estoque")]
[Authorize]
public class EstoqueController(ILojaAtual lojaAtual) : ControllerBase
{
    [HttpGet("movimentos")]
    public async Task<IActionResult> Movimentos([FromServices] IQueryHandler<ListarMovimentosEstoqueQuery> handler,
        [FromQuery] Guid? produtoId, [FromQuery] string? tipo, [FromQuery] DateTime? de, [FromQuery] DateTime? ate) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), produtoId, tipo, de, ate))).ParaResposta(this);

    [HttpPost("ajuste")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> Ajuste([FromBody] AjusteEstoqueRequest req, [FromServices] ICommandHandler<AjusteEstoqueCommand> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), req))).ParaResposta(this);

    [HttpGet("alertas")]
    public async Task<IActionResult> Alertas([FromServices] IQueryHandler<ListarAlertasEstoqueQuery> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User)))).ParaResposta(this);

    [HttpPost("ajuste-variacao")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> AjusteVariacao([FromBody] AjusteVariacaoRequest req, [FromServices] ICommandHandler<AjusteVariacaoCommand> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), req))).ParaResposta(this);
}
