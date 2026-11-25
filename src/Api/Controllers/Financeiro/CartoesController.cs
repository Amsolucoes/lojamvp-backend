using LojaApi.Application.Financeiro;
using LojaApi.Shared.Api;
using LojaApi.Shared.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LojaApi.Controllers.Financeiro;

// Cartões de crédito da loja: cadastro, compras (avulsa e parcelada), compras fixas e edição/exclusão de compras.
[ApiController]
[Route("api/financeiro")]
[Authorize]
public class CartoesController(ILojaAtual lojaAtual) : ControllerBase
{
    [HttpGet("cartoes")]
    public async Task<IActionResult> ListarCartoes([FromServices] IQueryHandler<ListarCartoesQuery> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User)))).ParaResposta(this);

    [HttpPost("cartoes")]
    public async Task<IActionResult> CriarCartao([FromBody] SalvarCartaoRequest req, [FromServices] ICommandHandler<CriarCartaoCommand> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), req))).ParaResposta(this);

    [HttpPut("cartoes/{id:guid}")]
    public async Task<IActionResult> AtualizarCartao(Guid id, [FromBody] SalvarCartaoRequest req, [FromServices] ICommandHandler<AtualizarCartaoCommand> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), id, req))).ParaResposta(this);

    // Lançar uma compra no cartão
    [HttpPost("cartoes/{id:guid}/lancamentos")]
    public async Task<IActionResult> LancarCompra(Guid id, [FromBody] SalvarLancamentoCartaoRequest req, [FromServices] ICommandHandler<LancarCompraCommand> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), id, req))).ParaResposta(this);

    // Compra parcelada no cartão (ex: TV em 10x)
    [HttpPost("cartoes/{id:guid}/lancamentos/parcelado")]
    public async Task<IActionResult> LancarCompraParcelada(Guid id, [FromBody] CompraParceladaCartaoRequest req, [FromServices] ICommandHandler<LancarCompraParceladaCommand> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), id, req))).ParaResposta(this);

    // Lançamento fixo/recorrente no cartão (ex: Netflix)
    [HttpPost("cartoes/{id:guid}/fixos")]
    public async Task<IActionResult> CriarCartaoFixo(Guid id, [FromBody] CartaoFixoRequest req, [FromServices] ICommandHandler<CriarCartaoFixoCommand> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), id, req))).ParaResposta(this);

    [HttpGet("cartoes/{id:guid}/fixos")]
    public async Task<IActionResult> ListarCartaoFixos(Guid id, [FromServices] IQueryHandler<ListarCartaoFixosQuery> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), id))).ParaResposta(this);

    [HttpPatch("cartoes/fixos/{id:guid}/ativo")]
    public async Task<IActionResult> AlternarCartaoFixo(Guid id, [FromServices] ICommandHandler<AlternarCartaoFixoCommand> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), id))).ParaResposta(this);

    [HttpGet("cartoes/lancamentos/descricoes")]
    public async Task<IActionResult> ListarDescricoesRecentesCartao([FromServices] IQueryHandler<ListarDescricoesRecentesCartaoQuery> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User)))).ParaResposta(this);

    // Editar item de compra do cartão
    [HttpPut("cartoes/lancamentos/{id:guid}")]
    public async Task<IActionResult> EditarLancamentoCartao(Guid id, [FromQuery] string modo, [FromBody] EditarLancamentoCartaoRequest req, [FromServices] ICommandHandler<EditarLancamentoCartaoCommand> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), id, modo, req))).ParaResposta(this);

    // Excluir item de compra do cartão
    [HttpDelete("cartoes/lancamentos/{id:guid}")]
    public async Task<IActionResult> ExcluirLancamentoCartao(Guid id, [FromServices] ICommandHandler<ExcluirLancamentoCartaoCommand> handler, [FromQuery] string modo = "unica") =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), id, modo))).ParaResposta(this);
}
