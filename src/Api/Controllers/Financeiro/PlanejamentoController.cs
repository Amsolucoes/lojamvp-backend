using LojaApi.Application.Financeiro;
using LojaApi.Shared.Api;
using LojaApi.Shared.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LojaApi.Controllers.Financeiro;

// Planejamento: quanto se quer gastar por categoria no mês, quanto guardar e metas; e a troca de categoria de gastos no detalhe do planejamento.
[ApiController]
[Route("api/financeiro")]
[Authorize]
public class PlanejamentoController(ILojaAtual lojaAtual) : ControllerBase
{
    [HttpPut("planejamento/config")]
    public async Task<IActionResult> SalvarPlanejamentoConfig([FromBody] PlanejamentoConfigRequest req, [FromServices] ICommandHandler<SalvarPlanejamentoConfigCommand> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), req))).ParaResposta(this);

    [HttpPut("planejamento/categorias")]
    public async Task<IActionResult> SalvarPlanejamentoCategorias([FromBody] PlanejamentoCategoriasRequest req, [FromServices] ICommandHandler<SalvarPlanejamentoCategoriasCommand> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), req))).ParaResposta(this);

    [HttpPost("planejamento/metas")]
    public async Task<IActionResult> CriarMetaPlanejamento([FromBody] MetaPlanejamentoRequest req, [FromServices] ICommandHandler<CriarMetaPlanejamentoCommand> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), req))).ParaResposta(this);

    [HttpPut("planejamento/metas/{id:guid}")]
    public async Task<IActionResult> AtualizarMetaPlanejamento(Guid id, [FromBody] MetaPlanejamentoRequest req, [FromServices] ICommandHandler<AtualizarMetaPlanejamentoCommand> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), id, req))).ParaResposta(this);

    // Soma (ou, com valor negativo, retira) dinheiro guardado numa meta
    [HttpPost("planejamento/metas/{id:guid}/aportar")]
    public async Task<IActionResult> AportarMetaPlanejamento(Guid id, [FromBody] AporteMetaRequest req, [FromServices] ICommandHandler<AportarMetaPlanejamentoCommand> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), id, req))).ParaResposta(this);

    [HttpDelete("planejamento/metas/{id:guid}")]
    public async Task<IActionResult> ExcluirMetaPlanejamento(Guid id, [FromServices] ICommandHandler<ExcluirMetaPlanejamentoCommand> handler)
    {
        var resultado = await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), id));
        return resultado.Sucesso ? NoContent() : resultado.ParaResposta(this);
    }

    // Troca só a categoria de um lançamento (avulso, parcelado ou fixo)
    [HttpPatch("lancamentos/{id:guid}/categoria")]
    public async Task<IActionResult> TrocarCategoriaLancamento(Guid id, [FromBody] TrocarCategoriaRequest req, [FromServices] ICommandHandler<TrocarCategoriaLancamentoCommand> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), id, req))).ParaResposta(this);

    // Troca a categoria de uma compra do cartão (opcionalmente de todas as parcelas da mesma compra)
    [HttpPatch("cartoes/lancamentos/{id:guid}/categoria")]
    public async Task<IActionResult> TrocarCategoriaCompraCartao(Guid id, [FromBody] TrocarCategoriaRequest req, [FromServices] ICommandHandler<TrocarCategoriaCompraCartaoCommand> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), id, req))).ParaResposta(this);
}
