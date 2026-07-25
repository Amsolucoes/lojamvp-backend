using LojaApi.Data;
using LojaApi.Models;
using LojaApi.src.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LojaApi.src.Controllers;

[ApiController]
[Route("api/comissoes")]
[Authorize]
public class ComissoesController(AppDbContext db) : ControllerBase
{
    private Guid UsuarioId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private async Task<Guid?> GetLojaId()
    {
        var vinculo = await db.UsuariosLoja.FirstOrDefaultAsync(ul => ul.UsuarioId == UsuarioId && ul.Ativo);
        return vinculo?.LojaId;
    }

    // ── Resumo por profissional: quanto cada um tem pendente ───────
    [HttpGet("resumo")]
    public async Task<IActionResult> Resumo([FromQuery] DateTime? de, [FromQuery] DateTime? ate)
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return Ok(Array.Empty<object>());

        var q = db.ComissoesFuncionario.Where(c => c.LojaId == lojaId && c.Status == "pendente");

        if (de.HasValue) q = q.Where(c => c.CriadoEm >= de.Value.Date);
        if (ate.HasValue) q = q.Where(c => c.CriadoEm <= ate.Value.Date.AddDays(1));

        var comissoes = await q.ToListAsync();
        var profissionalIds = comissoes.Select(c => c.ProfissionalId).Distinct().ToList();
        var profissionais = await db.Profissionais.Where(p => profissionalIds.Contains(p.Id)).ToListAsync();

        var resumo = profissionalIds.Select(pid =>
        {
            var doProfissional = comissoes.Where(c => c.ProfissionalId == pid).ToList();
            var prof = profissionais.FirstOrDefault(p => p.Id == pid);
            return new
            {
                profissionalId = pid,
                profissionalNome = prof?.Nome ?? "—",
                qtdAtendimentos = doProfissional.Count,
                valorTotal = doProfissional.Sum(c => c.ValorComissao),
            };
        }).OrderByDescending(r => r.valorTotal).ToList();

        return Ok(resumo);
    }

    // ── Detalhe das comissões pendentes de um profissional ─────────
    [HttpGet("profissional/{profissionalId:guid}")]
    public async Task<IActionResult> DetalheProfissional(Guid profissionalId, [FromQuery] string status = "pendente")
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return Ok(Array.Empty<object>());

        var lista = await db.ComissoesFuncionario
            .Where(c => c.LojaId == lojaId && c.ProfissionalId == profissionalId && c.Status == status)
            .Include(c => c.Agendamento)
            .OrderByDescending(c => c.CriadoEm)
            .Select(c => new
            {
                c.Id,
                c.ValorServico,
                c.ComissaoPercentual,
                c.ValorComissao,
                c.Status,
                c.PagoEm,
                c.CriadoEm,
                nomeServico = c.Agendamento != null ? c.Agendamento.NomeServico : null,
                nomeCliente = c.Agendamento != null ? c.Agendamento.NomeCliente : null,
                dataAtendimento = c.Agendamento != null ? c.Agendamento.DataHora : (DateTime?)null,
            })
            .ToListAsync();

        return Ok(lista);
    }

    // ── Fechar e pagar as comissões pendentes de um profissional num período ──
    public record FecharComissaoRequest(Guid ProfissionalId, DateTime PeriodoInicio, DateTime PeriodoFim, Guid? ContaBancariaId, DateTime? Vencimento);

    [HttpPost("fechar")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> Fechar([FromBody] FecharComissaoRequest req)
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return BadRequest(new { erro = "Loja não encontrada." });

        var profissional = await db.Profissionais.FirstOrDefaultAsync(p => p.Id == req.ProfissionalId && p.LojaId == lojaId);
        if (profissional is null) return NotFound(new { erro = "Profissional não encontrado." });

        var inicio = DateTime.SpecifyKind(req.PeriodoInicio.Date, DateTimeKind.Utc);
        var fim = DateTime.SpecifyKind(req.PeriodoFim.Date, DateTimeKind.Utc).AddDays(1);

        var comissoesPendentes = await db.ComissoesFuncionario
            .Where(c => c.LojaId == lojaId && c.ProfissionalId == req.ProfissionalId
                && c.Status == "pendente" && c.CriadoEm >= inicio && c.CriadoEm < fim)
            .ToListAsync();

        if (comissoesPendentes.Count == 0)
            return BadRequest(new { erro = "Nenhuma comissão pendente encontrada nesse período." });

        var valorTotal = comissoesPendentes.Sum(c => c.ValorComissao);

        var fechamento = new FechamentoComissao
        {
            LojaId = lojaId.Value,
            ProfissionalId = profissional.Id,
            PeriodoInicio = inicio,
            PeriodoFim = DateTime.SpecifyKind(req.PeriodoFim.Date, DateTimeKind.Utc),
            ValorTotal = valorTotal,
            QtdAtendimentos = comissoesPendentes.Count,
        };
        db.FechamentosComissao.Add(fechamento);
        await db.SaveChangesAsync();

        foreach (var c in comissoesPendentes)
        {
            c.Status = "pago";
            c.PagoEm = DateTime.UtcNow;
            c.FechamentoId = fechamento.Id;
        }

        // Integração opcional com o Financeiro: só cria a conta a pagar se veio conta bancária escolhida
        if (req.ContaBancariaId.HasValue)
        {
            var categoriaId = await ObterOuCriarCategoriaComissaoAsync(lojaId.Value);
            var vencimento = req.Vencimento.HasValue
                ? DateTime.SpecifyKind(req.Vencimento.Value.Date, DateTimeKind.Utc).AddHours(12)
                : DateTime.SpecifyKind(req.PeriodoFim.Date, DateTimeKind.Utc).AddDays(5).AddHours(12);

            var lancamento = new LancamentoFinanceiro
            {
                LojaId = lojaId.Value,
                ContaBancariaId = req.ContaBancariaId.Value,
                Tipo = "pagar",
                Modo = "avulsa",
                Descricao = $"Comissão — {profissional.Nome} ({req.PeriodoInicio:dd/MM} a {req.PeriodoFim:dd/MM})",
                CategoriaId = categoriaId,
                Valor = valorTotal,
                Vencimento = vencimento,
                Status = "pendente",
            };
            db.LancamentosFinanceiros.Add(lancamento);
            await db.SaveChangesAsync();

            fechamento.LancamentoFinanceiroId = lancamento.Id;
        }

        await db.SaveChangesAsync();

        return Ok(new
        {
            fechamento.Id,
            fechamento.ValorTotal,
            fechamento.QtdAtendimentos,
            profissionalNome = profissional.Nome,
        });
    }

    // ── Histórico de fechamentos já pagos ──────────────────────────
    [HttpGet("fechamentos")]
    public async Task<IActionResult> ListarFechamentos([FromQuery] Guid? profissionalId)
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return Ok(Array.Empty<object>());

        var q = db.FechamentosComissao.Where(f => f.LojaId == lojaId);
        if (profissionalId.HasValue) q = q.Where(f => f.ProfissionalId == profissionalId.Value);

        var lista = await q
            .Include(f => f.Profissional)
            .OrderByDescending(f => f.PagoEm)
            .Select(f => new
            {
                f.Id,
                f.ProfissionalId,
                profissionalNome = f.Profissional != null ? f.Profissional.Nome : "—",
                f.PeriodoInicio,
                f.PeriodoFim,
                f.ValorTotal,
                f.QtdAtendimentos,
                f.PagoEm,
            })
            .ToListAsync();

        return Ok(lista);
    }

    // ── Desfazer um fechamento (volta as comissões pra pendente) ───
    [HttpPost("fechamentos/{id:guid}/desfazer")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> DesfazerFechamento(Guid id)
    {
        var lojaId = await GetLojaId();
        var fechamento = await db.FechamentosComissao.FirstOrDefaultAsync(f => f.Id == id && f.LojaId == lojaId);
        if (fechamento is null) return NotFound();

        if (fechamento.LancamentoFinanceiroId.HasValue)
        {
            var lancamento = await db.LancamentosFinanceiros.FindAsync(fechamento.LancamentoFinanceiroId.Value);
            if (lancamento != null)
            {
                if (lancamento.Status == "pago")
                    return BadRequest(new { erro = "Essa comissão já foi paga no Financeiro. Estorne o lançamento lá antes de desfazer o fechamento." });

                db.LancamentosFinanceiros.Remove(lancamento);
            }
        }

        var comissoes = await db.ComissoesFuncionario.Where(c => c.FechamentoId == id).ToListAsync();
        foreach (var c in comissoes)
        {
            c.Status = "pendente";
            c.PagoEm = null;
            c.FechamentoId = null;
        }

        db.FechamentosComissao.Remove(fechamento);
        await db.SaveChangesAsync();

        return Ok(new { mensagem = "Fechamento desfeito. As comissões voltaram para pendente." });
    }

    private async Task<Guid> ObterOuCriarCategoriaComissaoAsync(Guid lojaId)
    {
        var categoria = await db.CategoriasFinanceiras
            .FirstOrDefaultAsync(c => c.LojaId == lojaId && c.Nome == "Comissões de Funcionários");
        if (categoria != null) return categoria.Id;

        categoria = new CategoriaFinanceira
        {
            LojaId = lojaId,
            Nome = "Comissões de Funcionários",
            Tipo = "pagar",
            Icone = "👤",
        };
        db.CategoriasFinanceiras.Add(categoria);
        await db.SaveChangesAsync();
        return categoria.Id;
    }
}