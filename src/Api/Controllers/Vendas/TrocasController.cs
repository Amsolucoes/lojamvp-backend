using LojaApi.Application.Vendas;
using LojaApi.Shared.Api;
using LojaApi.Shared.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LojaApi.Controllers.Vendas;

// Trocas de produtos (devolução + novos itens, com crédito ao cliente)
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TrocasController(ILojaAtual lojaAtual) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Registrar([FromBody] CriarTrocaRequest req, [FromServices] ICommandHandler<RegistrarTrocaCommand> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), req))).ParaResposta(this);

    [HttpGet]
    public async Task<IActionResult> Listar([FromServices] IQueryHandler<ListarTrocasQuery> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User)))).ParaResposta(this);
}
