using LojaApi.Application.Corretora;
using LojaApi.Shared.Api;
using LojaApi.Shared.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LojaApi.Controllers.Corretora;

[ApiController]
[Route("api/corretora")]
[Authorize]
public class CorretoraController(CorretoraService servico, ILojaAtual lojaAtual) : ControllerBase
{
    // ══════════════════ SEGURADORAS ══════════════════

    [HttpGet("seguradoras")]
    public async Task<IActionResult> ListarSeguradoras([FromQuery] bool todas = false) =>
        (await servico.ListarSeguradorasAsync(await lojaAtual.ObterIdAsync(User), todas)).ParaResposta(this);

    [HttpPost("seguradoras")]
    public async Task<IActionResult> CriarSeguradora([FromBody] SalvarSeguradoraRequest req) =>
        (await servico.CriarSeguradoraAsync(await lojaAtual.ObterIdAsync(User), req)).ParaResposta(this);

    [HttpPatch("seguradoras/{id:guid}/ativo")]
    public async Task<IActionResult> AlternarSeguradora(Guid id) =>
        (await servico.AlternarSeguradoraAsync(await lojaAtual.ObterIdAsync(User), id)).ParaResposta(this);

    [HttpPut("seguradoras/{id:guid}")]
    public async Task<IActionResult> AtualizarSeguradora(Guid id, [FromBody] AtualizarSeguradoraRequest req) =>
        (await servico.AtualizarSeguradoraAsync(await lojaAtual.ObterIdAsync(User), id, req)).ParaResposta(this);

    [HttpDelete("seguradoras/{id:guid}")]
    public async Task<IActionResult> ExcluirSeguradora(Guid id) =>
        (await servico.ExcluirSeguradoraAsync(await lojaAtual.ObterIdAsync(User), id)).ParaResposta(this);

    // ══════════════════ FUNIL DE VENDAS ══════════════════

    [HttpGet("oportunidades")]
    public async Task<IActionResult> ListarOportunidades() =>
        (await servico.ListarOportunidadesAsync(await lojaAtual.ObterIdAsync(User))).ParaResposta(this);

    [HttpPost("oportunidades")]
    public async Task<IActionResult> CriarOportunidade([FromBody] SalvarOportunidadeRequest req) =>
        (await servico.CriarOportunidadeAsync(await lojaAtual.ObterIdAsync(User), req)).ParaResposta(this);

    [HttpPut("oportunidades/{id:guid}")]
    public async Task<IActionResult> AtualizarOportunidade(Guid id, [FromBody] SalvarOportunidadeRequest req) =>
        (await servico.AtualizarOportunidadeAsync(await lojaAtual.ObterIdAsync(User), id, req)).ParaResposta(this);

    // Mover card entre etapas (drag and drop)
    [HttpPatch("oportunidades/{id:guid}/etapa")]
    public async Task<IActionResult> MoverEtapa(Guid id, [FromBody] MoverEtapaRequest req) =>
        (await servico.MoverEtapaAsync(await lojaAtual.ObterIdAsync(User), id, req)).ParaResposta(this);

    [HttpDelete("oportunidades/{id:guid}")]
    public async Task<IActionResult> ExcluirOportunidade(Guid id) =>
        (await servico.ExcluirOportunidadeAsync(await lojaAtual.ObterIdAsync(User), id)).ParaResposta(this);

    [HttpGet("oportunidades/verificar-cliente/{clienteId:guid}")]
    public async Task<IActionResult> VerificarClienteJaTemOportunidade(Guid clienteId) =>
        (await servico.VerificarClienteJaTemOportunidadeAsync(await lojaAtual.ObterIdAsync(User), clienteId)).ParaResposta(this);

    // ══════════════════ APÓLICES (VIGÊNCIA) ══════════════════

    [HttpGet("apolices")]
    public async Task<IActionResult> ListarApolices() =>
        (await servico.ListarApolicesAsync(await lojaAtual.ObterIdAsync(User))).ParaResposta(this);

    [HttpPost("apolices")]
    public async Task<IActionResult> CriarApolice([FromBody] SalvarApoliceRequest req) =>
        (await servico.CriarApoliceAsync(await lojaAtual.ObterIdAsync(User), req)).ParaResposta(this);

    [HttpPut("apolices/{id:guid}")]
    public async Task<IActionResult> AtualizarApolice(Guid id, [FromBody] SalvarApoliceRequest req) =>
        (await servico.AtualizarApoliceAsync(await lojaAtual.ObterIdAsync(User), id, req)).ParaResposta(this);

    // O novo status vem no campo "nome" do corpo
    [HttpPatch("apolices/{id:guid}/status")]
    public async Task<IActionResult> AlterarStatusApolice(Guid id, [FromBody] AlterarStatusApoliceRequest req) =>
        (await servico.AlterarStatusApoliceAsync(await lojaAtual.ObterIdAsync(User), id, req)).ParaResposta(this);

    // ══════════════════ RESUMOS ══════════════════

    [HttpGet("resumo")]
    public async Task<IActionResult> Resumo() =>
        (await servico.ResumoAsync(await lojaAtual.ObterIdAsync(User))).ParaResposta(this);

    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard([FromQuery] int dias = 15) =>
        (await servico.DashboardAsync(await lojaAtual.ObterIdAsync(User), dias)).ParaResposta(this);
}
