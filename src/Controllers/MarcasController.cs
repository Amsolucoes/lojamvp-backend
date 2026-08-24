using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using LojaApi.Data;
using LojaApi.src.Models;

namespace LojaApi.Controllers;

[ApiController]
[Route("api/marcas")]
[Authorize]
public class MarcasController(AppDbContext db) : ControllerBase
{
    private Guid UsuarioId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private async Task<Guid?> GetLojaId()
    {
        var vinculo = await db.UsuariosLoja
            .FirstOrDefaultAsync(ul => ul.UsuarioId == UsuarioId && ul.Ativo);
        return vinculo?.LojaId;
    }

    // ── Listar marcas da loja ────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> Listar()
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return Ok(Array.Empty<object>());

        var marcas = await db.Marcas
            .Where(m => m.LojaId == lojaId && m.Ativo)
            .OrderBy(m => m.Nome)
            .Select(m => new { m.Id, m.Nome })
            .ToListAsync();

        return Ok(marcas);
    }

    public record CriarMarcaRequest(string Nome);

    // ── Criar marca ──────────────────────────────────────────────
    [HttpPost]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> Criar([FromBody] CriarMarcaRequest req)
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return BadRequest(new { erro = "Loja não encontrada." });

        if (string.IsNullOrWhiteSpace(req.Nome))
            return BadRequest(new { erro = "Nome da marca é obrigatório." });

        var nome = req.Nome.Trim();

        var existe = await db.Marcas
            .AnyAsync(m => m.LojaId == lojaId && m.Nome.ToLower() == nome.ToLower() && m.Ativo);
        if (existe)
            return Conflict(new { erro = "Já existe uma marca com esse nome." });

        var marca = new Marca
        {
            LojaId = lojaId.Value,
            Nome = nome,
        };
        db.Marcas.Add(marca);
        await db.SaveChangesAsync();

        return Ok(new { marca.Id, marca.Nome });
    }

    // ── Editar marca ─────────────────────────────────────────────
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> Atualizar(Guid id, [FromBody] CriarMarcaRequest req)
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return BadRequest(new { erro = "Loja não encontrada." });

        var marca = await db.Marcas.FirstOrDefaultAsync(m => m.Id == id && m.LojaId == lojaId);
        if (marca is null) return NotFound();

        if (string.IsNullOrWhiteSpace(req.Nome))
            return BadRequest(new { erro = "Nome da marca é obrigatório." });

        var nomeNovo = req.Nome.Trim();
        var duplicada = await db.Marcas
            .AnyAsync(m => m.LojaId == lojaId && m.Id != id && m.Nome.ToLower() == nomeNovo.ToLower() && m.Ativo);
        if (duplicada)
            return Conflict(new { erro = "Já existe uma marca com esse nome." });

        marca.Nome = nomeNovo;
        await db.SaveChangesAsync();

        return Ok(new { marca.Id, marca.Nome });
    }

    // ── Excluir marca ────────────────────────────────────────────
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> Excluir(Guid id)
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return BadRequest(new { erro = "Loja não encontrada." });

        var marca = await db.Marcas.FirstOrDefaultAsync(m => m.Id == id && m.LojaId == lojaId);
        if (marca is null) return NotFound();

        var qtdProdutos = await db.Produtos.CountAsync(p => p.MarcaId == id && p.Ativo);
        if (qtdProdutos > 0)
            return BadRequest(new { erro = $"Não é possível excluir: {qtdProdutos} produto(s) usam esta marca." });

        marca.Ativo = false;
        await db.SaveChangesAsync();
        return Ok(new { mensagem = "Marca excluída." });
    }
}