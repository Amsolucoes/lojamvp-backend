using LojaApi.Application.Turmas;
using LojaApi.Shared.Api;
using LojaApi.Shared.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LojaApi.Controllers.Turmas;

[ApiController]
[Route("api/turmas")]
[Authorize]
public class TurmasController(TurmasService servico, ILojaAtual lojaAtual) : ControllerBase
{
    // ══════════════════ TURMAS (CRUD) ══════════════════

    [HttpGet]
    public async Task<IActionResult> Listar() =>
        (await servico.ListarAsync(await lojaAtual.ObterIdAsync(User))).ParaResposta(this);

    [HttpPost]
    public async Task<IActionResult> Criar([FromBody] SalvarTurmaRequest req) =>
        (await servico.CriarAsync(await lojaAtual.ObterIdAsync(User), req)).ParaResposta(this);

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Atualizar(Guid id, [FromBody] SalvarTurmaRequest req) =>
        (await servico.AtualizarAsync(await lojaAtual.ObterIdAsync(User), id, req)).ParaResposta(this);

    [HttpPatch("{id:guid}/ativo")]
    public async Task<IActionResult> AlternarAtiva(Guid id) =>
        (await servico.AlternarAtivaAsync(await lojaAtual.ObterIdAsync(User), id)).ParaResposta(this);

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Excluir(Guid id) =>
        (await servico.ExcluirAsync(await lojaAtual.ObterIdAsync(User), id)).ParaResposta(this);

    // ══════════════════ MATRÍCULA ══════════════════

    [HttpGet("{id:guid}/alunos")]
    public async Task<IActionResult> ListarAlunos(Guid id) =>
        (await servico.ListarAlunosAsync(await lojaAtual.ObterIdAsync(User), id)).ParaResposta(this);

    [HttpPost("{id:guid}/matricular")]
    public async Task<IActionResult> Matricular(Guid id, [FromBody] MatricularRequest req) =>
        (await servico.MatricularAsync(await lojaAtual.ObterIdAsync(User), id, req)).ParaResposta(this);

    [HttpDelete("matricula/{matriculaId:guid}")]
    public async Task<IActionResult> Desmatricular(Guid matriculaId) =>
        (await servico.DesmatricularAsync(await lojaAtual.ObterIdAsync(User), matriculaId)).ParaResposta(this);

    // ══════════════════ SESSÕES (agenda semanal) ══════════════════

    [HttpGet("sessoes")]
    public async Task<IActionResult> ListarSessoes([FromQuery] DateTime de, [FromQuery] DateTime ate) =>
        (await servico.ListarSessoesAsync(await lojaAtual.ObterIdAsync(User), de, ate)).ParaResposta(this);

    // Vagas disponíveis noutras sessões (pra remarcar)
    [HttpGet("sessoes-com-vaga")]
    public async Task<IActionResult> SessoesComVaga([FromQuery] DateTime de, [FromQuery] DateTime ate) =>
        (await servico.SessoesComVagaAsync(await lojaAtual.ObterIdAsync(User), de, ate)).ParaResposta(this);

    // ══════════════════ FALTA / REMARCAÇÃO ══════════════════

    [HttpPost("inscricoes/{id:guid}/falta")]
    public async Task<IActionResult> MarcarFaltaAvisada(Guid id, [FromBody] RemarcarRequest req) =>
        (await servico.MarcarFaltaAvisadaAsync(await lojaAtual.ObterIdAsync(User), id, req)).ParaResposta(this);

    // ══════════════════ CHAMADA (presença real) ══════════════════

    [HttpPost("inscricoes/{id:guid}/presenca")]
    public async Task<IActionResult> MarcarPresenca(Guid id, [FromBody] MarcarPresencaRequest req) =>
        (await servico.MarcarPresencaAsync(await lojaAtual.ObterIdAsync(User), id, req)).ParaResposta(this);

    // ══════════════════ PROFISSIONAIS ══════════════════

    [HttpGet("profissionais")]
    public async Task<IActionResult> ListarProfissionais() =>
        (await servico.ListarProfissionaisAsync(await lojaAtual.ObterIdAsync(User))).ParaResposta(this);

    [HttpPost("profissionais")]
    public async Task<IActionResult> CriarProfissional([FromBody] SalvarProfissionalRequest req) =>
        (await servico.CriarProfissionalAsync(await lojaAtual.ObterIdAsync(User), req)).ParaResposta(this);

    [HttpPatch("profissionais/{id:guid}/ativo")]
    public async Task<IActionResult> AlternarProfissional(Guid id) =>
        (await servico.AlternarProfissionalAsync(await lojaAtual.ObterIdAsync(User), id)).ParaResposta(this);

    // Define/troca o profissional de um aluno NESSA sessão
    [HttpPatch("inscricoes/{id:guid}/profissional")]
    public async Task<IActionResult> DefinirProfissional(Guid id, [FromBody] DefinirProfissionalRequest req) =>
        (await servico.DefinirProfissionalAsync(await lojaAtual.ObterIdAsync(User), id, req)).ParaResposta(this);
}
