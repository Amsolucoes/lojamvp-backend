using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using LojaApi.Data;
using LojaApi.Models;
using LojaApi.DTOs;

namespace LojaApi.Controllers;

[ApiController]
[Route("api/categorias")]
[Authorize]
public class CategoriasController(AppDbContext db) : ControllerBase
{
    private Guid UsuarioId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private async Task<Guid?> GetLojaId()
    {
        var vinculo = await db.UsuariosLoja
            .FirstOrDefaultAsync(ul => ul.UsuarioId == UsuarioId && ul.Ativo);
        return vinculo?.LojaId;
    }

    // ── Listar categorias da loja ─────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> Listar()
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return Ok(Array.Empty<object>());

        var cats = await db.CategoriasLoja
            .Where(c => c.LojaId == lojaId && c.Ativo)
            .OrderBy(c => c.Ordem).ThenBy(c => c.Nome)
            .Select(c => new
            {
                c.Id,
                c.Nome,
                c.TipoTamanho,
                c.UsaTamanho,
                c.UsaCor,
                c.TamanhosPersonalizados,
            })
            .ToListAsync();

        return Ok(cats);
    }

    // ── Criar categoria ───────────────────────────────────────────
    [HttpPost]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> Criar([FromBody] CriarCategoriaRequest req)
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return BadRequest(new { erro = "Loja não encontrada." });

        if (string.IsNullOrWhiteSpace(req.Nome))
            return BadRequest(new { erro = "Nome da categoria é obrigatório." });

        // Evita duplicada
        var existe = await db.CategoriasLoja
            .AnyAsync(c => c.LojaId == lojaId && c.Nome.ToLower() == req.Nome.ToLower() && c.Ativo);
        if (existe)
            return Conflict(new { erro = "Já existe uma categoria com esse nome." });

        var maxOrdem = await db.CategoriasLoja
            .Where(c => c.LojaId == lojaId)
            .Select(c => (int?)c.Ordem).MaxAsync() ?? -1;

        var cat = new CategoriaLoja
        {
            LojaId = lojaId.Value,
            Nome = req.Nome.Trim(),
            TipoTamanho = req.TipoTamanho,
            UsaTamanho = req.UsaTamanho,
            UsaCor = req.UsaCor,
            TamanhosPersonalizados = req.TamanhosPersonalizados,
            Ordem = maxOrdem + 1,
        };
        db.CategoriasLoja.Add(cat);
        await db.SaveChangesAsync();

        return Ok(new
        {
            cat.Id,
            cat.Nome,
            cat.TipoTamanho,
            cat.UsaTamanho,
            cat.UsaCor,
            cat.TamanhosPersonalizados,
        });
    }
}