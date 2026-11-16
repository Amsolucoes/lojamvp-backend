using LojaApi.Application.Financeiro;
using LojaApi.Shared.Api;
using LojaApi.Shared.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LojaApi.Controllers.Financeiro;

// Lançamentos fixos (aluguel, internet...): o modelo que gera uma ocorrência por mês.
[ApiController]
[Route("api/financeiro")]
[Authorize]
public class LancamentosFixosController(ILojaAtual lojaAtual) : ControllerBase
{
    [HttpPost("fixos")]
    public async Task<IActionResult> CriarFixo([FromBody] SalvarLancamentoFixoRequest req, [FromServices] ICommandHandler<CriarFixoCommand> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), req))).ParaResposta(this);

    // Editar um fixo (valor, dia, categoria) e regenerar o lote futuro
    [HttpPut("fixos/{id:guid}")]
    public async Task<IActionResult> AtualizarFixo(Guid id, [FromBody] SalvarLancamentoFixoRequest req, [FromServices] ICommandHandler<AtualizarFixoCommand> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), id, req))).ParaResposta(this);

    // "Errei, apaga tudo que criou pra frente e gera de novo"
    [HttpPost("fixos/{id:guid}/regenerar")]
    public async Task<IActionResult> RegenerarFixo(Guid id, [FromServices] ICommandHandler<RegenerarFixoCommand> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), id))).ParaResposta(this);

    [HttpPatch("fixos/{id:guid}/ativo")]
    public async Task<IActionResult> AlternarFixo(Guid id, [FromServices] ICommandHandler<AlternarFixoCommand> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), id))).ParaResposta(this);
}
