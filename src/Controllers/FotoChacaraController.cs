using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using LojaApi.Data;
using LojaApi.src.Models;

namespace LojaApi.src.Controllers;

[ApiController]
[Route("api/chacara/fotos")]
[Authorize]
public class FotoChacaraController(AppDbContext db) : ControllerBase
{
    private const int LimiteFotos = 30;

    private Guid UsuarioId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private async Task<Guid?> GetLojaId()
    {
        var vinculo = await db.UsuariosLoja
            .FirstOrDefaultAsync(ul => ul.UsuarioId == UsuarioId && ul.Ativo);
        return vinculo?.LojaId;
    }

    [HttpGet]
    public async Task<IActionResult> Listar()
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return Ok(Array.Empty<object>());

        var fotos = await db.FotosChacara
            .Where(f => f.LojaId == lojaId)
            .OrderBy(f => f.Ordem)
            .ToListAsync();

        return Ok(fotos);
    }

    public record AdicionarFotoRequest(string Url);

    [HttpPost]
    public async Task<IActionResult> Adicionar([FromBody] AdicionarFotoRequest req)
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return BadRequest(new { erro = "Loja não encontrada." });

        if (string.IsNullOrWhiteSpace(req.Url))
            return BadRequest(new { erro = "URL inválida." });

        var total = await db.FotosChacara.CountAsync(f => f.LojaId == lojaId);
        if (total >= LimiteFotos)
            return BadRequest(new { erro = $"Limite de {LimiteFotos} fotos atingido." });

        var proximaOrdem = await db.FotosChacara
            .Where(f => f.LojaId == lojaId)
            .Select(f => (int?)f.Ordem)
            .MaxAsync() ?? -1;

        var foto = new FotoChacara
        {
            LojaId = lojaId.Value,
            Url = req.Url.Trim(),
            Ordem = proximaOrdem + 1,
        };
        db.FotosChacara.Add(foto);
        await db.SaveChangesAsync();

        return Ok(foto);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Remover(int id)
    {
        var lojaId = await GetLojaId();
        var foto = await db.FotosChacara.FirstOrDefaultAsync(f => f.Id == id && f.LojaId == lojaId);
        if (foto is null) return NotFound();

        db.FotosChacara.Remove(foto);
        await db.SaveChangesAsync();
        return Ok(new { mensagem = "Foto removida." });
    }

    public record ReordenarRequest(List<int> Ids); // ids na nova ordem desejada

    [HttpPut("ordem")]
    public async Task<IActionResult> Reordenar([FromBody] ReordenarRequest req)
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return BadRequest(new { erro = "Loja não encontrada." });

        var fotos = await db.FotosChacara.Where(f => f.LojaId == lojaId).ToListAsync();

        for (int i = 0; i < req.Ids.Count; i++)
        {
            var foto = fotos.FirstOrDefault(f => f.Id == req.Ids[i]);
            if (foto != null) foto.Ordem = i;
        }

        await db.SaveChangesAsync();
        return Ok(new { mensagem = "Ordem atualizada." });
    }
}