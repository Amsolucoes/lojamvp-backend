using LojaApi.Application.Produtos;
using LojaApi.Shared.Api;
using LojaApi.Shared.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LojaApi.Controllers.Produtos;

// Marcas dos produtos (ex.: Bosch, Ipiranga).
[ApiController]
[Route("api/marcas")]
[Authorize]
public class MarcasController(ILojaAtual lojaAtual) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listar([FromServices] IQueryHandler<ListarMarcasQuery> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User)))).ParaResposta(this);

    [HttpPost]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> Criar([FromBody] CriarMarcaRequest req, [FromServices] ICommandHandler<CriarMarcaCommand> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), req))).ParaResposta(this);

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> Atualizar(Guid id, [FromBody] CriarMarcaRequest req, [FromServices] ICommandHandler<AtualizarMarcaCommand> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), id, req))).ParaResposta(this);

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> Excluir(Guid id, [FromServices] ICommandHandler<ExcluirMarcaCommand> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), id))).ParaResposta(this);
}
