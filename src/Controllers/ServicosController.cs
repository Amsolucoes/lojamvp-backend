using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using LojaApi.Data;
using LojaApi.Models;

namespace LojaApi.Controllers;

[ApiController]
[Route("api/servicos")]
[Authorize]
public class ServicosController(AppDbContext db) : ControllerBase
{
    private Guid UsuarioId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private async Task<Guid?> GetLojaId()
    {
        var vinculo = await db.UsuariosLoja
            .FirstOrDefaultAsync(ul => ul.UsuarioId == UsuarioId && ul.Ativo);
        return vinculo?.LojaId;
    }

    // ── Listar ────────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> Listar()
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return Ok(Array.Empty<object>());

        var servicos = await db.Servicos
            .Where(s => s.LojaId == lojaId)
            .OrderBy(s => s.Categoria).ThenBy(s => s.Nome)
            .Select(s => new {
                s.Id,
                s.Nome,
                s.Categoria,
                s.Preco,
                s.DuracaoMin,
                s.Ativo,
                s.CriadoEm
            })
            .ToListAsync();

        return Ok(servicos);
    }

    // ── Criar ─────────────────────────────────────────────────────
    [HttpPost]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> Criar([FromBody] SalvarServicoRequest req)
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return BadRequest(new { erro = "Loja não encontrada." });

        if (string.IsNullOrWhiteSpace(req.Nome))
            return BadRequest(new { erro = "Nome do serviço é obrigatório." });

        var servico = new Servico
        {
            LojaId = lojaId.Value,
            Nome = req.Nome.Trim(),
            Categoria = string.IsNullOrWhiteSpace(req.Categoria) ? "Geral" : req.Categoria.Trim(),
            Preco = req.Preco,
            DuracaoMin = req.DuracaoMin,
            Ativo = req.Ativo,
        };
        db.Servicos.Add(servico);
        await db.SaveChangesAsync();

        return Ok(new { servico.Id, servico.Nome, servico.Categoria, servico.Preco, servico.DuracaoMin, servico.Ativo, servico.CriadoEm });
    }

    // ── Editar ────────────────────────────────────────────────────
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> Atualizar(Guid id, [FromBody] SalvarServicoRequest req)
    {
        var lojaId = await GetLojaId();
        var servico = await db.Servicos.FirstOrDefaultAsync(s => s.Id == id && s.LojaId == lojaId);
        if (servico is null) return NotFound();

        servico.Nome = req.Nome.Trim();
        servico.Categoria = string.IsNullOrWhiteSpace(req.Categoria) ? "Geral" : req.Categoria.Trim();
        servico.Preco = req.Preco;
        servico.DuracaoMin = req.DuracaoMin;
        servico.Ativo = req.Ativo;
        await db.SaveChangesAsync();

        return Ok(new { servico.Id, servico.Nome, servico.Categoria, servico.Preco, servico.DuracaoMin, servico.Ativo, servico.CriadoEm });
    }

    // ── Excluir ───────────────────────────────────────────────────
    // ── Excluir ───────────────────────────────────────────────────
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> Excluir(Guid id)
    {
        var lojaId = await GetLojaId();
        var servico = await db.Servicos.FirstOrDefaultAsync(s => s.Id == id && s.LojaId == lojaId);
        if (servico is null) return NotFound();

        // Impede exclusão se houver agendamentos vinculados
        var qtdAgendamentos = await db.Agendamentos.CountAsync(a => a.ServicoId == id);
        if (qtdAgendamentos > 0)
            return BadRequest(new { erro = $"Não é possível excluir: este serviço tem {qtdAgendamentos} agendamento(s) vinculado(s). Você pode desativá-lo em vez de excluir." });

        db.Servicos.Remove(servico);
        await db.SaveChangesAsync();
        return Ok(new { mensagem = "Serviço excluído." });
    }
}

public record SalvarServicoRequest(
    string Nome, string Categoria,
    decimal Preco, int DuracaoMin, bool Ativo
);