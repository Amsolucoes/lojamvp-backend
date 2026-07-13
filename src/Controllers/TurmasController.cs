using LojaApi.Data;
using LojaApi.src.Models;
using LojaApi.src.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LojaApi.src.Controllers;

[ApiController]
[Route("api/turmas")]
[Authorize]
public class TurmasController(AppDbContext db, TurmasService turmasService) : ControllerBase
{
    private Guid UsuarioId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private async Task<Guid?> GetLojaId()
    {
        var vinculo = await db.UsuariosLoja.FirstOrDefaultAsync(ul => ul.UsuarioId == UsuarioId && ul.Ativo);
        return vinculo?.LojaId;
    }

    // ══════════════════ TURMAS (CRUD) ══════════════════

    [HttpGet]
    public async Task<IActionResult> Listar()
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return Ok(Array.Empty<object>());

        var turmas = await db.Turmas.Where(t => t.LojaId == lojaId).ToListAsync();
        var resultado = new List<object>();

        foreach (var t in turmas)
        {
            var qtdAlunos = await db.MatriculasTurma.CountAsync(m => m.TurmaId == t.Id && m.Ativa);
            resultado.Add(new
            {
                t.Id,
                t.Nome,
                t.DiaSemana,
                horario = t.Horario.ToString(@"hh\:mm"),
                t.DuracaoMin,
                t.Capacidade,
                t.Ativa,
                qtdAlunos,
            });
        }
        return Ok(resultado);
    }

    public record SalvarTurmaRequest(string Nome, int DiaSemana, string Horario, int DuracaoMin, int Capacidade);

    [HttpPost]
    public async Task<IActionResult> Criar([FromBody] SalvarTurmaRequest req)
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return BadRequest(new { erro = "Loja não encontrada." });

        var turma = new Turma
        {
            LojaId = lojaId.Value,
            Nome = req.Nome.Trim(),
            DiaSemana = req.DiaSemana,
            Horario = TimeSpan.Parse(req.Horario),
            DuracaoMin = req.DuracaoMin,
            Capacidade = req.Capacidade,
        };
        db.Turmas.Add(turma);
        await db.SaveChangesAsync();

        await turmasService.GerarSessoesFuturasAsync();

        return Ok(new { turma.Id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Atualizar(Guid id, [FromBody] SalvarTurmaRequest req)
    {
        var lojaId = await GetLojaId();
        var turma = await db.Turmas.FirstOrDefaultAsync(t => t.Id == id && t.LojaId == lojaId);
        if (turma is null) return NotFound();

        turma.Nome = req.Nome.Trim();
        turma.DiaSemana = req.DiaSemana;
        turma.Horario = TimeSpan.Parse(req.Horario);
        turma.DuracaoMin = req.DuracaoMin;
        turma.Capacidade = req.Capacidade;
        await db.SaveChangesAsync();

        return Ok(new { turma.Id });
    }

    [HttpPatch("{id:guid}/ativo")]
    public async Task<IActionResult> AlternarAtiva(Guid id)
    {
        var lojaId = await GetLojaId();
        var turma = await db.Turmas.FirstOrDefaultAsync(t => t.Id == id && t.LojaId == lojaId);
        if (turma is null) return NotFound();

        turma.Ativa = !turma.Ativa;
        await db.SaveChangesAsync();
        return Ok(new { turma.Id, turma.Ativa });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Excluir(Guid id)
    {
        var lojaId = await GetLojaId();
        var turma = await db.Turmas.FirstOrDefaultAsync(t => t.Id == id && t.LojaId == lojaId);
        if (turma is null) return NotFound();

        var temMatricula = await db.MatriculasTurma.AnyAsync(m => m.TurmaId == id && m.Ativa);
        if (temMatricula)
            return BadRequest(new { erro = "Não é possível excluir: esta turma tem alunos matriculados. Desative em vez de excluir." });

        db.Turmas.Remove(turma);
        await db.SaveChangesAsync();
        return Ok(new { mensagem = "Turma excluída." });
    }

    // ══════════════════ MATRÍCULA ══════════════════

    [HttpGet("{id:guid}/alunos")]
    public async Task<IActionResult> ListarAlunos(Guid id)
    {
        var lojaId = await GetLojaId();
        var turma = await db.Turmas.FirstOrDefaultAsync(t => t.Id == id && t.LojaId == lojaId);
        if (turma is null) return NotFound();

        var alunos = await db.MatriculasTurma
            .Where(m => m.TurmaId == id && m.Ativa)
            .Join(db.Clientes, m => m.ClienteId, c => c.Id, (m, c) => new { m.Id, m.ClienteId, nome = c.Nome, telefone = c.Telefone })
            .ToListAsync();

        return Ok(alunos);
    }

    public record MatricularRequest(Guid ClienteId);

    [HttpPost("{id:guid}/matricular")]
    public async Task<IActionResult> Matricular(Guid id, [FromBody] MatricularRequest req)
    {
        var lojaId = await GetLojaId();
        var turma = await db.Turmas.FirstOrDefaultAsync(t => t.Id == id && t.LojaId == lojaId);
        if (turma is null) return NotFound();

        var jaMatriculado = await db.MatriculasTurma.AnyAsync(m => m.TurmaId == id && m.ClienteId == req.ClienteId && m.Ativa);
        if (jaMatriculado) return BadRequest(new { erro = "Aluno já matriculado nesta turma." });

        var qtdAtual = await db.MatriculasTurma.CountAsync(m => m.TurmaId == id && m.Ativa);
        if (qtdAtual >= turma.Capacidade)
            return BadRequest(new { erro = $"Turma cheia ({turma.Capacidade} vaga(s))." });

        db.MatriculasTurma.Add(new MatriculaTurma
        {
            LojaId = lojaId!.Value,
            TurmaId = id,
            ClienteId = req.ClienteId,
        });
        await db.SaveChangesAsync();

        await turmasService.GerarSessoesFuturasAsync();

        return Ok(new { mensagem = "Aluno matriculado." });
    }

    [HttpDelete("matricula/{matriculaId:guid}")]
    public async Task<IActionResult> Desmatricular(Guid matriculaId)
    {
        var lojaId = await GetLojaId();
        var matricula = await db.MatriculasTurma.FirstOrDefaultAsync(m => m.Id == matriculaId && m.LojaId == lojaId);
        if (matricula is null) return NotFound();

        matricula.Ativa = false;
        await db.SaveChangesAsync();

        // Remove inscrições futuras (aulas que ainda não aconteceram) desse aluno nessa turma
        var hoje = DateTime.UtcNow;
        var futuras = await db.InscricoesSessao
            .Where(i => i.ClienteId == matricula.ClienteId && i.Tipo == "fixo")
            .Join(db.SessoesTurma.Where(s => s.TurmaId == matricula.TurmaId && s.DataHora >= hoje),
                  i => i.SessaoTurmaId, s => s.Id, (i, s) => i)
            .ToListAsync();
        db.InscricoesSessao.RemoveRange(futuras);
        await db.SaveChangesAsync();

        return Ok(new { mensagem = "Aluno desmatriculado." });
    }

    // ══════════════════ SESSÕES (agenda semanal) ══════════════════

    [HttpGet("sessoes")]
    public async Task<IActionResult> ListarSessoes([FromQuery] DateTime de, [FromQuery] DateTime ate)
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return Ok(Array.Empty<object>());

        var deUtc = DateTime.SpecifyKind(de.Date, DateTimeKind.Utc);
        var ateUtc = DateTime.SpecifyKind(ate.Date, DateTimeKind.Utc).AddDays(1);

        var sessoes = await db.SessoesTurma
            .Include(s => s.Turma)
            .Where(s => s.LojaId == lojaId && s.DataHora >= deUtc && s.DataHora < ateUtc)
            .OrderBy(s => s.DataHora)
            .ToListAsync();

        var resultado = new List<object>();
        foreach (var s in sessoes)
        {
            var inscricoes = await db.InscricoesSessao
                .Where(i => i.SessaoTurmaId == s.Id && i.Status != "faltou")
                .Join(db.Clientes, i => i.ClienteId, c => c.Id, (i, c) => new
                {
                    i.Id,
                    i.ClienteId,
                    nome = c.Nome,
                    i.Tipo,
                    i.Status,
                    i.RemarcadoParaSessaoId,
                })
                .ToListAsync();

            resultado.Add(new
            {
                s.Id,
                s.TurmaId,
                nomeTurma = s.Turma!.Nome,
                dataHora = s.DataHora,
                s.Status,
                capacidade = s.Turma.Capacidade,
                vagasOcupadas = inscricoes.Count(i => i.Status != "falta_avisada" && i.RemarcadoParaSessaoId == null),
                alunos = inscricoes,
            });
        }

        return Ok(resultado);
    }

    // ── Vagas disponíveis noutras sessões (pra remarcar) ────────────
    [HttpGet("sessoes-com-vaga")]
    public async Task<IActionResult> SessoesComVaga([FromQuery] DateTime de, [FromQuery] DateTime ate)
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return Ok(Array.Empty<object>());

        var deUtc = DateTime.SpecifyKind(de.Date, DateTimeKind.Utc);
        var ateUtc = DateTime.SpecifyKind(ate.Date, DateTimeKind.Utc).AddDays(1);

        var sessoes = await db.SessoesTurma
            .Include(s => s.Turma)
            .Where(s => s.LojaId == lojaId && s.DataHora >= deUtc && s.DataHora < ateUtc && s.Status == "aberta")
            .OrderBy(s => s.DataHora)
            .ToListAsync();

        var resultado = new List<object>();
        foreach (var s in sessoes)
        {
            var ocupadas = await db.InscricoesSessao.CountAsync(i =>
                i.SessaoTurmaId == s.Id && i.Status != "falta_avisada" && i.RemarcadoParaSessaoId == null);

            var vagas = s.Turma!.Capacidade - ocupadas;
            if (vagas > 0)
                resultado.Add(new { s.Id, nomeTurma = s.Turma.Nome, dataHora = s.DataHora, vagas });
        }

        return Ok(resultado);
    }

    // ══════════════════ FALTA / REMARCAÇÃO ══════════════════

    public record RemarcarRequest(Guid? SessaoDestinoId); // null = só avisa falta, sem remarcar ainda

    [HttpPost("inscricoes/{id:guid}/falta")]
    public async Task<IActionResult> MarcarFaltaAvisada(Guid id, [FromBody] RemarcarRequest req)
    {
        var lojaId = await GetLojaId();
        var inscricao = await db.InscricoesSessao.FirstOrDefaultAsync(i => i.Id == id && i.LojaId == lojaId);
        if (inscricao is null) return NotFound();

        inscricao.Status = "falta_avisada";

        if (req.SessaoDestinoId.HasValue)
        {
            var destino = await db.SessoesTurma.Include(s => s.Turma).FirstOrDefaultAsync(s => s.Id == req.SessaoDestinoId.Value && s.LojaId == lojaId);
            if (destino is null) return NotFound(new { erro = "Sessão de destino não encontrada." });

            var ocupadas = await db.InscricoesSessao.CountAsync(i =>
                i.SessaoTurmaId == destino.Id && i.Status != "falta_avisada" && i.RemarcadoParaSessaoId == null);
            if (ocupadas >= destino.Turma!.Capacidade)
                return BadRequest(new { erro = "Sessão de destino está sem vagas." });

            inscricao.RemarcadoParaSessaoId = destino.Id;

            db.InscricoesSessao.Add(new InscricaoSessao
            {
                LojaId = lojaId!.Value,
                SessaoTurmaId = destino.Id,
                ClienteId = inscricao.ClienteId,
                Tipo = "remarcacao",
                Status = "confirmado",
            });
        }

        await db.SaveChangesAsync();
        return Ok(new { mensagem = req.SessaoDestinoId.HasValue ? "Remarcado com sucesso." : "Falta registrada." });
    }

    // ══════════════════ CHAMADA (presença real) ══════════════════

    public record MarcarPresencaRequest(bool Compareceu);

    [HttpPost("inscricoes/{id:guid}/presenca")]
    public async Task<IActionResult> MarcarPresenca(Guid id, [FromBody] MarcarPresencaRequest req)
    {
        var lojaId = await GetLojaId();
        var inscricao = await db.InscricoesSessao.FirstOrDefaultAsync(i => i.Id == id && i.LojaId == lojaId);
        if (inscricao is null) return NotFound();

        inscricao.Status = req.Compareceu ? "compareceu" : "faltou";
        await db.SaveChangesAsync();

        return Ok(new { inscricao.Id, inscricao.Status });
    }
}