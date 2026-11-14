using LojaApi.Application.Vendas;
using LojaApi.Shared.Api;
using LojaApi.Shared.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LojaApi.Controllers.Vendas;

// Reforços e sangrias do caixa
[ApiController]
[Route("api/movimentos-caixa")]
[Authorize]
public class MovimentosCaixaController(ILojaAtual lojaAtual) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listar([FromServices] IQueryHandler<ListarMovimentosCaixaQuery> handler,
        [FromQuery] DateTime? de, [FromQuery] DateTime? ate) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), de, ate))).ParaResposta(this);

    [HttpPost]
    public async Task<IActionResult> Criar([FromBody] CriarMovimentoRequest req, [FromServices] ICommandHandler<CriarMovimentoCaixaCommand> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), req))).ParaResposta(this);

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Editar(Guid id, [FromBody] EditarMovimentoRequest req, [FromServices] ICommandHandler<EditarMovimentoCaixaCommand> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), id, req))).ParaResposta(this);

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Excluir(Guid id, [FromServices] ICommandHandler<ExcluirMovimentoCaixaCommand> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), id))).ParaResposta(this);
}
