using LojaApi.Application.Financeiro;
using LojaApi.Shared.Api;
using LojaApi.Shared.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LojaApi.Controllers.Financeiro;

// Contas bancárias da loja: cadastro, saldo, ajuste manual e transferência entre contas.
[ApiController]
[Route("api/financeiro")]
[Authorize]
public class ContasBancariasController(ILojaAtual lojaAtual) : ControllerBase
{
    [HttpGet("contas")]
    public async Task<IActionResult> ListarContas([FromServices] IQueryHandler<ListarContasQuery> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User)))).ParaResposta(this);

    [HttpPost("contas")]
    public async Task<IActionResult> CriarConta([FromBody] SalvarContaBancariaRequest req, [FromServices] ICommandHandler<CriarContaCommand> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), req))).ParaResposta(this);

    [HttpPut("contas/{id:guid}")]
    public async Task<IActionResult> AtualizarConta(Guid id, [FromBody] SalvarContaBancariaRequest req, [FromServices] ICommandHandler<AtualizarContaCommand> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), id, req))).ParaResposta(this);

    [HttpPatch("contas/{id:guid}/ativo")]
    public async Task<IActionResult> AlternarAtivaConta(Guid id, [FromServices] ICommandHandler<AlternarContaAtivaCommand> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), id))).ParaResposta(this);

    // Ajuste manual de saldo
    [HttpPost("contas/{id:guid}/ajuste")]
    public async Task<IActionResult> AjustarSaldo(Guid id, [FromBody] AjusteSaldoRequest req, [FromServices] ICommandHandler<AjustarSaldoCommand> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), id, req))).ParaResposta(this);

    [HttpPost("contas/transferencia")]
    public async Task<IActionResult> TransferirEntreContas([FromBody] TransferenciaContaRequest req, [FromServices] ICommandHandler<TransferirEntreContasCommand> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), req))).ParaResposta(this);
}
