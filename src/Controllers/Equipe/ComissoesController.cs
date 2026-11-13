using LojaApi.Application.Equipe;
using LojaApi.Shared.Api;
using LojaApi.Shared.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LojaApi.Controllers.Equipe;

// Comissões dos funcionários
[ApiController]
[Route("api/comissoes")]
[Authorize]
public class ComissoesController(ILojaAtual lojaAtual) : ControllerBase
{
    // Resumo por profissional: quanto cada um tem pendente
    [HttpGet("resumo")]
    public async Task<IActionResult> Resumo([FromQuery] DateTime? de, [FromQuery] DateTime? ate, [FromServices] IQueryHandler<ResumoComissoesQuery> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), de, ate))).ParaResposta(this);

    // Detalhe das comissões de um profissional (por padrão, as pendentes)
    [HttpGet("profissional/{profissionalId:guid}")]
    public async Task<IActionResult> DetalheProfissional(Guid profissionalId, [FromServices] IQueryHandler<DetalheComissoesProfissionalQuery> handler, [FromQuery] string status = "pendente") =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), profissionalId, status))).ParaResposta(this);

    // Fechar e pagar as comissões (+ diárias, se houver) de um profissional num período
    [HttpPost("fechar")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> Fechar([FromBody] FecharComissaoRequest req, [FromServices] ICommandHandler<FecharComissoesCommand> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), req))).ParaResposta(this);

    // Histórico de fechamentos já pagos
    [HttpGet("fechamentos")]
    public async Task<IActionResult> ListarFechamentos([FromQuery] Guid? profissionalId, [FromServices] IQueryHandler<ListarFechamentosQuery> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), profissionalId))).ParaResposta(this);

    // Desfazer um fechamento (volta as comissões para pendente)
    [HttpPost("fechamentos/{id:guid}/desfazer")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> DesfazerFechamento(Guid id, [FromServices] ICommandHandler<DesfazerFechamentoCommand> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), id))).ParaResposta(this);
}
