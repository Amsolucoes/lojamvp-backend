using LojaApi.Application.Servicos;
using LojaApi.Shared.Api;
using LojaApi.Shared.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LojaApi.Controllers.Servicos;

// Agenda da loja
[ApiController]
[Route("api/agendamentos")]
[Authorize]
public class AgendamentosController(AgendamentosService servico, ILojaAtual lojaAtual) : ControllerBase
{
    // GET /api/agendamentos?data=2026-06-30  (ou ?de=...&ate=...)
    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] DateTime? data, [FromQuery] DateTime? de, [FromQuery] DateTime? ate) =>
        (await servico.ListarAsync(await lojaAtual.ObterIdAsync(User), data, de, ate)).ParaResposta(this);

    [HttpGet("pendentes-pagamento")]
    public async Task<IActionResult> PendentesPagamento() =>
        (await servico.PendentesPagamentoAsync(await lojaAtual.ObterIdAsync(User))).ParaResposta(this);

    // Agendamentos online esperando aprovação
    [HttpGet("pendentes")]
    public async Task<IActionResult> Pendentes() =>
        (await servico.PendentesAsync(await lojaAtual.ObterIdAsync(User))).ParaResposta(this);

    [HttpPost]
    public async Task<IActionResult> Criar([FromBody] SalvarAgendamentoRequest req) =>
        (await servico.CriarAsync(await lojaAtual.ObterIdAsync(User), req)).ParaResposta(this);

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Atualizar(Guid id, [FromBody] SalvarAgendamentoRequest req) =>
        (await servico.AtualizarAsync(await lojaAtual.ObterIdAsync(User), id, req)).ParaResposta(this);

    // Concluir / cancelar / reabrir
    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> MudarStatus(Guid id, [FromBody] StatusAgendamentoRequest req) =>
        (await servico.MudarStatusAsync(await lojaAtual.ObterIdAsync(User), id, req)).ParaResposta(this);

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Excluir(Guid id) =>
        (await servico.ExcluirAsync(await lojaAtual.ObterIdAsync(User), id)).ParaResposta(this);

    // Histórico de agendamentos de um cliente
    [HttpGet("cliente/{clienteId:guid}")]
    public async Task<IActionResult> PorCliente(Guid clienteId) =>
        (await servico.PorClienteAsync(await lojaAtual.ObterIdAsync(User), clienteId)).ParaResposta(this);

    // Resumo de serviços por cliente (para os cards)
    [HttpGet("resumo-clientes")]
    public async Task<IActionResult> ResumoClientes() =>
        (await servico.ResumoClientesAsync(await lojaAtual.ObterIdAsync(User))).ParaResposta(this);
}
