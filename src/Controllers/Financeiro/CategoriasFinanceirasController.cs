using LojaApi.Application.Financeiro;
using LojaApi.Shared.Api;
using LojaApi.Shared.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LojaApi.Controllers.Financeiro;

// Categorias dos lançamentos financeiros (aluguel, água, vendas...).
[ApiController]
[Route("api/financeiro")]
[Authorize]
public class CategoriasFinanceirasController(ILojaAtual lojaAtual) : ControllerBase
{
    [HttpGet("categorias")]
    public async Task<IActionResult> ListarCategorias([FromServices] IQueryHandler<ListarCategoriasFinanceirasQuery> handler, [FromQuery] string? tipo) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), tipo))).ParaResposta(this);

    [HttpPost("categorias")]
    public async Task<IActionResult> CriarCategoria([FromBody] SalvarCategoriaFinanceiraRequest req, [FromServices] ICommandHandler<CriarCategoriaFinanceiraCommand> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), req))).ParaResposta(this);

    [HttpPut("categorias/{id:guid}")]
    public async Task<IActionResult> AtualizarCategoria(Guid id, [FromBody] SalvarCategoriaFinanceiraRequest req, [FromServices] ICommandHandler<AtualizarCategoriaFinanceiraCommand> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), id, req))).ParaResposta(this);

    [HttpDelete("categorias/{id:guid}")]
    public async Task<IActionResult> ExcluirCategoria(Guid id, [FromServices] ICommandHandler<ExcluirCategoriaFinanceiraCommand> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), id))).ParaResposta(this);

    [HttpPost("categorias/seed-padrao")]
    public async Task<IActionResult> SeedCategoriasPadrao([FromServices] ICommandHandler<SeedCategoriasPadraoCommand> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User)))).ParaResposta(this);
}
