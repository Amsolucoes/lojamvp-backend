using System.Security.Claims;
using LojaApi.Application.Plataforma;
using LojaApi.Shared.Api;
using LojaApi.Shared.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LojaApi.Controllers.Plataforma;

// Área do assinante: dashboard, configurações da loja, troca do próprio e-mail e faturas.
[ApiController]
[Route("api/cliente")]
[Authorize]
public class ClienteController : ControllerBase
{
    private Guid UsuarioId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard([FromServices] IQueryHandler<DashboardClienteQuery> handler) =>
        (await handler.HandleAsync(new(UsuarioId))).ParaResposta(this);

    [HttpGet("config")]
    public async Task<IActionResult> Config([FromServices] IQueryHandler<ConfigClienteQuery> handler) =>
        (await handler.HandleAsync(new(UsuarioId))).ParaResposta(this);

    // Atualizar configurações visuais
    [HttpPatch("config")]
    public async Task<IActionResult> AtualizarConfig([FromBody] AtualizarConfigRequest req, [FromServices] ICommandHandler<AtualizarConfigClienteCommand> handler) =>
        (await handler.HandleAsync(new(UsuarioId, req))).ParaResposta(this);

    // Cliente troca o próprio e-mail de login
    [HttpPatch("email")]
    public async Task<IActionResult> TrocarMeuEmail([FromBody] TrocarMeuEmailRequest req, [FromServices] ICommandHandler<TrocarMeuEmailCommand> handler) =>
        (await handler.HandleAsync(new(UsuarioId, req))).ParaResposta(this);

    [HttpGet("faturas")]
    public async Task<IActionResult> Faturas([FromServices] IQueryHandler<ListarFaturasClienteQuery> handler) =>
        (await handler.HandleAsync(new(UsuarioId))).ParaResposta(this);
}
