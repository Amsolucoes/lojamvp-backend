using LojaApi.Application.Vendas;
using LojaApi.Shared.Api;
using LojaApi.Shared.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LojaApi.Controllers.Vendas;

// Origens de venda (Loja física, Site, WhatsApp...)
[ApiController]
[Route("api/origens-venda")]
[Authorize]
public class OrigensVendaController(ILojaAtual lojaAtual) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listar([FromServices] IQueryHandler<ListarOrigensVendaQuery> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User)))).ParaResposta(this);

    [HttpGet("todas")]
    public async Task<IActionResult> ListarTodas([FromServices] IQueryHandler<ListarTodasOrigensVendaQuery> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User)))).ParaResposta(this);

    [HttpPost]
    public async Task<IActionResult> Criar([FromBody] SalvarOrigemRequest req, [FromServices] ICommandHandler<CriarOrigemVendaCommand> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), req))).ParaResposta(this);

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Atualizar(Guid id, [FromBody] SalvarOrigemRequest req, [FromServices] ICommandHandler<AtualizarOrigemVendaCommand> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), id, req))).ParaResposta(this);

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Excluir(Guid id, [FromServices] ICommandHandler<ExcluirOrigemVendaCommand> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), id))).ParaResposta(this);

    [HttpPost("seed-padrao")]
    public async Task<IActionResult> SeedPadrao([FromServices] ICommandHandler<CriarOrigensPadraoCommand> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User)))).ParaResposta(this);
}
