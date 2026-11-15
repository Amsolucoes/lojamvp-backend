using System.Security.Claims;
using LojaApi.Application.Plataforma;
using LojaApi.Shared.Api;
using LojaApi.Shared.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LojaApi.Controllers.Plataforma;

// Avisos push no navegador: inscrever o aparelho, ver o estado, cancelar e mandar um teste.
[ApiController]
[Route("api/push")]
[Authorize]
public class PushController(ILojaAtual lojaAtual) : ControllerBase
{
    private Guid UsuarioId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // Chave pública que o navegador precisa para se inscrever
    [HttpGet("chave-publica")]
    public async Task<IActionResult> ChavePublica([FromServices] IQueryHandler<ChavePublicaPushQuery> handler) =>
        (await handler.HandleAsync(new())).ParaResposta(this);

    // Quantos aparelhos deste usuário estão ativos (para a tela mostrar o estado)
    [HttpGet("status")]
    public async Task<IActionResult> Status([FromServices] IQueryHandler<StatusPushQuery> handler, [FromQuery] string? endpoint) =>
        (await handler.HandleAsync(new(UsuarioId, endpoint))).ParaResposta(this);

    [HttpPost("assinar")]
    public async Task<IActionResult> Assinar([FromBody] AssinarRequest req, [FromServices] ICommandHandler<AssinarPushCommand> handler) =>
        (await handler.HandleAsync(new(UsuarioId, await lojaAtual.ObterIdAsync(User), req))).ParaResposta(this);

    [HttpPost("cancelar")]
    public async Task<IActionResult> Cancelar([FromBody] CancelarRequest req, [FromServices] ICommandHandler<CancelarPushCommand> handler) =>
        (await handler.HandleAsync(new(UsuarioId, req))).ParaResposta(this);

    // Envia uma notificação de teste para os aparelhos do usuário logado
    [HttpPost("testar")]
    public async Task<IActionResult> Testar([FromServices] ICommandHandler<TestarPushCommand> handler) =>
        (await handler.HandleAsync(new(UsuarioId))).ParaResposta(this);
}
