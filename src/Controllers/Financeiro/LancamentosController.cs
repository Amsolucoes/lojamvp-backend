using LojaApi.Application.Financeiro;
using LojaApi.Shared.Api;
using LojaApi.Shared.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LojaApi.Controllers.Financeiro;

// Lançamentos a pagar e a receber: avulso, parcelado (e suas parcelas), pagamento (um ou em lote), edição, exclusão e aviso.
[ApiController]
[Route("api/financeiro")]
[Authorize]
public class LancamentosController(ILojaAtual lojaAtual) : ControllerBase
{
    // ── Contas a Receber — visão unificada (Lançamentos + Planos) ──
    [HttpGet("receber-unificado")]
    public async Task<IActionResult> ReceberUnificado([FromQuery] DateTime? de, [FromQuery] DateTime? ate, [FromServices] IQueryHandler<ReceberUnificadoQuery> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), de, ate))).ParaResposta(this);

    [HttpPost("lancamentos/avulso")]
    public async Task<IActionResult> CriarAvulso([FromBody] SalvarLancamentoAvulsoRequest req, [FromServices] ICommandHandler<CriarLancamentoAvulsoCommand> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), req))).ParaResposta(this);

    // Gera N parcelas de uma vez
    [HttpPost("lancamentos/parcelado")]
    public async Task<IActionResult> CriarParcelado([FromBody] SalvarLancamentoParceladoRequest req, [FromServices] ICommandHandler<CriarLancamentoParceladoCommand> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), req))).ParaResposta(this);

    // ── Parcelas de um lançamento parcelado (empréstimo, financiamento...) ──
    [HttpGet("lancamentos/{id:guid}/parcelas")]
    public async Task<IActionResult> ListarParcelas(Guid id, [FromServices] IQueryHandler<ListarParcelasQuery> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), id))).ParaResposta(this);

    [HttpPost("lancamentos/{id:guid}/parcelas/excluir")]
    public async Task<IActionResult> ExcluirParcelas(Guid id, [FromBody] ParcelasIdsRequest req, [FromServices] ICommandHandler<ExcluirParcelasCommand> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), id, req))).ParaResposta(this);

    [HttpPost("lancamentos/{id:guid}/parcelas/pagamento")]
    public async Task<IActionResult> MarcarParcelasPagas(Guid id, [FromBody] ParcelasPagamentoRequest req, [FromServices] ICommandHandler<MarcarParcelasPagasCommand> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), id, req))).ParaResposta(this);

    [HttpPost("lancamentos/{id:guid}/parcelas/total")]
    public async Task<IActionResult> AlterarTotalParcelas(Guid id, [FromBody] ParcelasTotalRequest req, [FromServices] ICommandHandler<AlterarTotalParcelasCommand> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), id, req))).ParaResposta(this);

    // ── Pagar várias contas de uma vez (na conta ou no cartão) ──
    [HttpPost("lancamentos/pagar-lote")]
    public async Task<IActionResult> PagarLote([FromBody] PagarLoteRequest req, [FromServices] ICommandHandler<PagarLoteCommand> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), req))).ParaResposta(this);

    // Marcar lançamento como pago/pendente
    [HttpPost("lancamentos/{id:guid}/pagamento")]
    public async Task<IActionResult> MarcarPagamento(Guid id, [FromBody] MarcarPagamentoRequest req, [FromServices] ICommandHandler<MarcarPagamentoCommand> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), id, req))).ParaResposta(this);

    [HttpPut("lancamentos/{id:guid}")]
    public async Task<IActionResult> EditarLancamento(Guid id, [FromQuery] string modo, [FromBody] EditarLancamentoRequest req, [FromServices] ICommandHandler<EditarLancamentoCommand> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), id, modo, req))).ParaResposta(this);

    [HttpDelete("lancamentos/{id:guid}")]
    public async Task<IActionResult> Excluir(Guid id, [FromServices] ICommandHandler<ExcluirLancamentoCommand> handler, [FromQuery] string modo = "unica") =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), id, modo))).ParaResposta(this);

    [HttpPatch("lancamentos/{id:guid}/avisar")]
    public async Task<IActionResult> AlternarAvisar(Guid id, [FromBody] ToggleAvisarRequest req, [FromServices] ICommandHandler<AlternarAvisarCommand> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), id, req))).ParaResposta(this);

    [HttpGet("lancamentos/descricoes")]
    public async Task<IActionResult> ListarDescricoesRecentes([FromQuery] string tipo, [FromServices] IQueryHandler<ListarDescricoesRecentesQuery> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), tipo))).ParaResposta(this);
}
