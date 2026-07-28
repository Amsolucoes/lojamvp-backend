using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LojaApi.Data;
using LojaApi.src.Models;

namespace LojaApi.src.Controllers;

[ApiController]
[Route("api/categorias-video-ajuda")]
public class CategoriasVideoAjudaController(AppDbContext db) : ControllerBase
{
    // ── Endpoint público — usado no formulário de vídeo e na Central de Ajuda ──
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Listar()
    {
        var lista = await db.CategoriasVideoAjuda
            .Where(c => c.Ativa)
            .OrderBy(c => c.Ordem).ThenBy(c => c.Nome)
            .Select(c => new { c.Id, c.Nome, c.Ordem })
            .ToListAsync();

        return Ok(lista);
    }

    [HttpGet("todas")]
    [Authorize(Roles = "superadmin")]
    public async Task<IActionResult> ListarTodas()
    {
        var lista = await db.CategoriasVideoAjuda
            .OrderBy(c => c.Ordem).ThenBy(c => c.Nome)
            .ToListAsync();
        return Ok(lista);
    }

    public record SalvarCategoriaRequest(string Nome, int Ordem, bool Ativa);

    [HttpPost]
    [Authorize(Roles = "superadmin")]
    public async Task<IActionResult> Criar([FromBody] SalvarCategoriaRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Nome))
            return BadRequest(new { erro = "Informe o nome da categoria." });

        var existente = await db.CategoriasVideoAjuda.AnyAsync(c => c.Nome == req.Nome.Trim());
        if (existente)
            return BadRequest(new { erro = "Já existe uma categoria com esse nome." });

        var categoria = new CategoriaVideoAjuda
        {
            Nome = req.Nome.Trim(),
            Ordem = req.Ordem,
            Ativa = req.Ativa,
        };
        db.CategoriasVideoAjuda.Add(categoria);
        await db.SaveChangesAsync();

        return Ok(categoria);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "superadmin")]
    public async Task<IActionResult> Atualizar(Guid id, [FromBody] SalvarCategoriaRequest req)
    {
        var categoria = await db.CategoriasVideoAjuda.FindAsync(id);
        if (categoria is null) return NotFound();

        var nomeAnterior = categoria.Nome;

        categoria.Nome = req.Nome.Trim();
        categoria.Ordem = req.Ordem;
        categoria.Ativa = req.Ativa;
        await db.SaveChangesAsync();

        // Mantém os vídeos já cadastrados apontando pro nome novo da categoria
        if (nomeAnterior != categoria.Nome)
        {
            var videosDaCategoria = await db.VideosAjuda.Where(v => v.Categoria == nomeAnterior).ToListAsync();
            foreach (var v in videosDaCategoria) v.Categoria = categoria.Nome;
            await db.SaveChangesAsync();
        }

        return Ok(categoria);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "superadmin")]
    public async Task<IActionResult> Excluir(Guid id)
    {
        var categoria = await db.CategoriasVideoAjuda.FindAsync(id);
        if (categoria is null) return NotFound();

        var emUso = await db.VideosAjuda.AnyAsync(v => v.Categoria == categoria.Nome);
        if (emUso)
        {
            categoria.Ativa = false; // desativa em vez de excluir, se já tiver vídeo usando
            await db.SaveChangesAsync();
            return Ok(new { mensagem = "Categoria em uso por vídeo(s) — foi desativada em vez de excluída." });
        }

        db.CategoriasVideoAjuda.Remove(categoria);
        await db.SaveChangesAsync();
        return Ok(new { mensagem = "Categoria excluída." });
    }
}