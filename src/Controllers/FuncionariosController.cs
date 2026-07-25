using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using LojaApi.Data;
using LojaApi.src.Models;

namespace LojaApi.src.Controllers;

[ApiController]
[Route("api/funcionarios")]
[Authorize]
public class FuncionariosController(AppDbContext db) : ControllerBase
{
    private Guid UsuarioId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private async Task<Guid?> GetLojaId()
    {
        var vinculo = await db.UsuariosLoja.FirstOrDefaultAsync(ul => ul.UsuarioId == UsuarioId && ul.Ativo);
        return vinculo?.LojaId;
    }

    // ── Listar profissionais (com comissão padrão e exceções) ──────
    [HttpGet]
    public async Task<IActionResult> Listar()
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return Ok(Array.Empty<object>());

        var lista = await db.Profissionais
            .Where(p => p.LojaId == lojaId)
            .Include(p => p.ComissoesPorServico)
            .OrderBy(p => p.Nome)
            .Select(p => new
            {
                p.Id,
                p.Nome,
                p.Ativo,
                p.ComissaoPadraoPercentual,
                comissoesPorServico = p.ComissoesPorServico.Select(c => new { c.Id, c.ServicoId, c.ComissaoPercentual }),
            })
            .ToListAsync();

        return Ok(lista);
    }

    public record SalvarProfissionalRequest(string Nome, decimal? ComissaoPadraoPercentual, bool Ativo);

    [HttpPost]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> Criar([FromBody] SalvarProfissionalRequest req)
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return BadRequest(new { erro = "Loja não encontrada." });

        if (string.IsNullOrWhiteSpace(req.Nome))
            return BadRequest(new { erro = "Nome é obrigatório." });

        var profissional = new Profissional
        {
            LojaId = lojaId.Value,
            Nome = req.Nome.Trim(),
            ComissaoPadraoPercentual = req.ComissaoPadraoPercentual,
            Ativo = req.Ativo,
        };
        db.Profissionais.Add(profissional);
        await db.SaveChangesAsync();

        return Ok(new { profissional.Id, profissional.Nome, profissional.Ativo, profissional.ComissaoPadraoPercentual });
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> Atualizar(Guid id, [FromBody] SalvarProfissionalRequest req)
    {
        var lojaId = await GetLojaId();
        var profissional = await db.Profissionais.FirstOrDefaultAsync(p => p.Id == id && p.LojaId == lojaId);
        if (profissional is null) return NotFound();

        profissional.Nome = req.Nome.Trim();
        profissional.ComissaoPadraoPercentual = req.ComissaoPadraoPercentual;
        profissional.Ativo = req.Ativo;
        await db.SaveChangesAsync();

        return Ok(new { profissional.Id, profissional.Nome, profissional.Ativo, profissional.ComissaoPadraoPercentual });
    }

    [HttpPatch("{id:guid}/ativo")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> AlternarAtivo(Guid id)
    {
        var lojaId = await GetLojaId();
        var profissional = await db.Profissionais.FirstOrDefaultAsync(p => p.Id == id && p.LojaId == lojaId);
        if (profissional is null) return NotFound();

        profissional.Ativo = !profissional.Ativo;
        await db.SaveChangesAsync();
        return Ok(new { profissional.Id, profissional.Ativo });
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> Excluir(Guid id)
    {
        var lojaId = await GetLojaId();
        var profissional = await db.Profissionais.FirstOrDefaultAsync(p => p.Id == id && p.LojaId == lojaId);
        if (profissional is null) return NotFound();

        var temAgendamentos = await db.Agendamentos.AnyAsync(a => a.ProfissionalId == id);
        var temComissoes = await db.ComissoesFuncionario.AnyAsync(c => c.ProfissionalId == id);
        if (temAgendamentos || temComissoes)
        {
            profissional.Ativo = false; // desativa em vez de excluir, se já usado
            await db.SaveChangesAsync();
            return Ok(new { mensagem = "Profissional em uso — foi desativado em vez de excluído." });
        }

        db.Profissionais.Remove(profissional);
        await db.SaveChangesAsync();
        return Ok(new { mensagem = "Profissional excluído." });
    }

    // ── Comissão por serviço (exceção ao padrão) ───────────────────
    public record SalvarComissaoServicoRequest(Guid ServicoId, decimal ComissaoPercentual);

    [HttpPost("{id:guid}/comissoes-servico")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> DefinirComissaoServico(Guid id, [FromBody] SalvarComissaoServicoRequest req)
    {
        var lojaId = await GetLojaId();
        var profissional = await db.Profissionais.FirstOrDefaultAsync(p => p.Id == id && p.LojaId == lojaId);
        if (profissional is null) return NotFound();

        var servico = await db.Servicos.FirstOrDefaultAsync(s => s.Id == req.ServicoId && s.LojaId == lojaId);
        if (servico is null) return BadRequest(new { erro = "Serviço não encontrado." });

        var existente = await db.ComissoesServicoProfissional
            .FirstOrDefaultAsync(c => c.ProfissionalId == id && c.ServicoId == req.ServicoId);

        if (existente != null)
        {
            existente.ComissaoPercentual = req.ComissaoPercentual;
        }
        else
        {
            db.ComissoesServicoProfissional.Add(new ComissaoServicoProfissional
            {
                LojaId = lojaId!.Value,
                ProfissionalId = id,
                ServicoId = req.ServicoId,
                ComissaoPercentual = req.ComissaoPercentual,
            });
        }

        await db.SaveChangesAsync();
        return Ok(new { mensagem = "Comissão do serviço definida." });
    }

    [HttpDelete("comissoes-servico/{comissaoId:guid}")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> RemoverComissaoServico(Guid comissaoId)
    {
        var lojaId = await GetLojaId();
        var comissao = await db.ComissoesServicoProfissional.FirstOrDefaultAsync(c => c.Id == comissaoId && c.LojaId == lojaId);
        if (comissao is null) return NotFound();

        db.ComissoesServicoProfissional.Remove(comissao);
        await db.SaveChangesAsync();
        return Ok(new { mensagem = "Exceção removida — volta a usar a comissão padrão do profissional." });
    }

    // ── Lista simplificada (id + nome), usada em seletores de outras telas ──
    [HttpGet("ativos")]
    public async Task<IActionResult> ListarAtivos()
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return Ok(Array.Empty<object>());

        var lista = await db.Profissionais
            .Where(p => p.LojaId == lojaId && p.Ativo)
            .OrderBy(p => p.Nome)
            .Select(p => new { p.Id, p.Nome })
            .ToListAsync();

        return Ok(lista);
    }
}