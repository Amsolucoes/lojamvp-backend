using System.Security.Claims;
using LojaApi.Application.Plataforma;
using LojaApi.Shared.Api;
using LojaApi.Shared.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LojaApi.Controllers.Plataforma;

// Login, sessão, cadastro público de loja e troca de senha.
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req, [FromServices] ICommandHandler<LoginCommand> handler) =>
        (await handler.HandleAsync(new(req))).ParaResposta(this);

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me([FromServices] ICommandHandler<RegistrarSessaoCommand> handler) =>
        (await handler.HandleAsync(new(
            User.FindFirstValue(ClaimTypes.NameIdentifier), User.FindFirstValue(ClaimTypes.Name),
            User.FindFirstValue(ClaimTypes.Email), User.FindFirstValue(ClaimTypes.Role),
            User.FindFirstValue("acesso_suporte") == "true"))).ParaResposta(this);

    [HttpGet("gerar-hash/{senha}")]
    [AllowAnonymous]
    public async Task<IActionResult> GerarHash(string senha, [FromServices] IQueryHandler<GerarHashQuery> handler) =>
        (await handler.HandleAsync(new(senha))).ParaResposta(this);

    // Auto-cadastro (signup público)
    [HttpPost("signup")]
    [AllowAnonymous]
    public async Task<IActionResult> Signup([FromBody] SignupRequest req, [FromServices] ICommandHandler<CadastrarLojaCommand> handler) =>
        (await handler.HandleAsync(new(req))).ParaResposta(this);

    // Trocar senha (usuário logado)
    [HttpPost("trocar-senha")]
    [Authorize]
    public async Task<IActionResult> TrocarSenha([FromBody] TrocarSenhaRequest req, [FromServices] ICommandHandler<TrocarSenhaCommand> handler) =>
        (await handler.HandleAsync(new(Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!), req))).ParaResposta(this);
}
