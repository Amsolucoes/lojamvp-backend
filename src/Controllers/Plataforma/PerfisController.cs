using LojaApi.Application.Plataforma;
using LojaApi.Shared.Api;
using LojaApi.Shared.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LojaApi.Controllers.Plataforma;

// Perfis de loja (modelos de categorias e campos) e as categorias da loja.
[ApiController]
[Route("api/perfis")]
[Authorize]
public class PerfisController(ILojaAtual lojaAtual) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Listar([FromServices] IQueryHandler<ListarPerfisQuery> handler) =>
        (await handler.HandleAsync(new())).ParaResposta(this);

    [HttpPost("aplicar")]
    public async Task<IActionResult> Aplicar([FromBody] AplicarPerfilRequest req, [FromServices] ICommandHandler<AplicarPerfilCommand> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), req))).ParaResposta(this);

    [HttpGet("loja/categorias")]
    public async Task<IActionResult> Categorias([FromServices] IQueryHandler<ListarCategoriasDaLojaQuery> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User)))).ParaResposta(this);

    [HttpGet("loja/campos-extras")]
    public async Task<IActionResult> CamposExtras([FromServices] IQueryHandler<ListarCamposExtrasDaLojaQuery> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User)))).ParaResposta(this);

    [HttpPost("loja/categorias")]
    public async Task<IActionResult> AdicionarCategoria([FromBody] SalvarCategoriaRequest req, [FromServices] ICommandHandler<AdicionarCategoriaCommand> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), req))).ParaResposta(this);
}
