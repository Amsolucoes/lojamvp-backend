using LojaApi.Application.Plataforma;
using LojaApi.Shared.Api;
using LojaApi.Shared.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LojaApi.Controllers.Plataforma;

// Preço de cada módulo (todos veem; só o superadmin altera).
[ApiController]
[Route("api/modulos-preco")]
[Authorize]
public class ModulosPrecoController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listar([FromServices] IQueryHandler<ListarModulosPrecoQuery> handler) =>
        (await handler.HandleAsync(new())).ParaResposta(this);

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "superadmin")]
    public async Task<IActionResult> Atualizar(Guid id, [FromBody] AtualizarModuloPrecoRequest req, [FromServices] ICommandHandler<AtualizarModuloPrecoCommand> handler) =>
        (await handler.HandleAsync(new(id, req))).ParaResposta(this);
}
