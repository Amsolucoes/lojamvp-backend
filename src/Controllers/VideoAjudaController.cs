using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LojaApi.Data;
using LojaApi.src.Models;

namespace LojaApi.src.Controllers;

[ApiController]
[Route("api/videos-ajuda")]
public class VideoAjudaController(AppDbContext db) : ControllerBase
{
    // ── Endpoint público — consumido pela Central de Ajuda dentro da loja ──
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Listar()
    {
        var lista = await db.VideosAjuda
            .Where(v => v.Ativo)
            .OrderBy(v => v.Categoria).ThenBy(v => v.Ordem)
            .Select(v => new { v.Id, v.Titulo, v.Categoria, v.YoutubeId, v.Ordem })
            .ToListAsync();

        return Ok(lista);
    }

    // ── Gestão (superadmin) ──────────────────────────────────────────
    [HttpGet("todos")]
    [Authorize(Roles = "superadmin")]
    public async Task<IActionResult> ListarTodos()
    {
        var lista = await db.VideosAjuda
            .OrderBy(v => v.Categoria).ThenBy(v => v.Ordem)
            .ToListAsync();
        return Ok(lista);
    }

    public record SalvarVideoRequest(string Titulo, string Categoria, string YoutubeId, int Ordem, bool Ativo);

    [HttpPost]
    [Authorize(Roles = "superadmin")]
    public async Task<IActionResult> Criar([FromBody] SalvarVideoRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Titulo) || string.IsNullOrWhiteSpace(req.Categoria) || string.IsNullOrWhiteSpace(req.YoutubeId))
            return BadRequest(new { erro = "Preencha título, categoria e ID do YouTube." });

        var video = new VideoAjuda
        {
            Titulo = req.Titulo.Trim(),
            Categoria = req.Categoria.Trim(),
            YoutubeId = ExtrairYoutubeId(req.YoutubeId.Trim()),
            Ordem = req.Ordem,
            Ativo = req.Ativo,
        };
        db.VideosAjuda.Add(video);
        await db.SaveChangesAsync();

        return Ok(video);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "superadmin")]
    public async Task<IActionResult> Atualizar(Guid id, [FromBody] SalvarVideoRequest req)
    {
        var video = await db.VideosAjuda.FindAsync(id);
        if (video is null) return NotFound();

        video.Titulo = req.Titulo.Trim();
        video.Categoria = req.Categoria.Trim();
        video.YoutubeId = ExtrairYoutubeId(req.YoutubeId.Trim());
        video.Ordem = req.Ordem;
        video.Ativo = req.Ativo;

        await db.SaveChangesAsync();
        return Ok(video);
    }

    [HttpPatch("{id:guid}/ativo")]
    [Authorize(Roles = "superadmin")]
    public async Task<IActionResult> AlternarAtivo(Guid id)
    {
        var video = await db.VideosAjuda.FindAsync(id);
        if (video is null) return NotFound();

        video.Ativo = !video.Ativo;
        await db.SaveChangesAsync();
        return Ok(new { video.Id, video.Ativo });
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "superadmin")]
    public async Task<IActionResult> Excluir(Guid id)
    {
        var video = await db.VideosAjuda.FindAsync(id);
        if (video is null) return NotFound();

        db.VideosAjuda.Remove(video);
        await db.SaveChangesAsync();
        return Ok(new { mensagem = "Vídeo excluído." });
    }

    // Aceita tanto o ID puro quanto um link colado (youtube.com/watch?v=X ou youtu.be/X)
    private static string ExtrairYoutubeId(string entrada)
    {
        if (!entrada.Contains("youtube.com") && !entrada.Contains("youtu.be"))
            return entrada; // já é o ID puro

        try
        {
            var uri = new Uri(entrada);
            if (entrada.Contains("youtu.be"))
                return uri.AbsolutePath.Trim('/');

            var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
            return query["v"] ?? entrada;
        }
        catch
        {
            return entrada;
        }
    }
}