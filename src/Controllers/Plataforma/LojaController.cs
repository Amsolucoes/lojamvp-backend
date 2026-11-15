using LojaApi.Application.Plataforma;
using LojaApi.Shared.Api;
using LojaApi.Shared.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LojaApi.Controllers.Plataforma;

// Configurações da loja: situação da assinatura, agenda, agendamento online, módulos, pausa e link.
[ApiController]
[Route("api/loja")]
[Authorize]
public class LojaController(ILojaAtual lojaAtual) : ControllerBase
{
    [HttpGet("situacao")]
    public async Task<IActionResult> Situacao([FromServices] IQueryHandler<SituacaoLojaQuery> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User)))).ParaResposta(this);

    [HttpPatch("agenda-horario")]
    public async Task<IActionResult> AtualizarAgendaHorario([FromBody] AgendaHorarioRequest req, [FromServices] ICommandHandler<AtualizarAgendaHorarioCommand> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), req))).ParaResposta(this);

    [HttpPatch("agendamento-online")]
    public async Task<IActionResult> ConfigAgendamentoOnline([FromBody] AgendamentoOnlineConfigRequest req, [FromServices] ICommandHandler<ConfigurarAgendamentoOnlineCommand> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), req))).ParaResposta(this);

    // Cliente ativa/desativa módulo sozinho
    [HttpPatch("modulos")]
    public async Task<IActionResult> AlternarModulo([FromBody] AlternarModuloRequest req, [FromServices] ICommandHandler<AlternarModuloCommand> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), req))).ParaResposta(this);

    [HttpPatch("pausa-agendamento")]
    public async Task<IActionResult> ConfigurarPausa([FromBody] PausaAgendamentoRequest req, [FromServices] ICommandHandler<ConfigurarPausaCommand> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), req))).ParaResposta(this);

    [HttpPatch("slug")]
    public async Task<IActionResult> DefinirSlug([FromBody] DefinirSlugRequest req, [FromServices] ICommandHandler<DefinirSlugCommand> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), req))).ParaResposta(this);
}
