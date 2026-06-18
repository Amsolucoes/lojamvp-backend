using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using LojaApi.Data;
using LojaApi.Models;

namespace LojaApi.Controllers;

[ApiController]
[Route("api/perfis")]
[Authorize]
public class PerfisController(AppDbContext db) : ControllerBase
{
    private Guid UsuarioId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private async Task<Guid?> GetLojaId()
    {
        var v = await db.UsuariosLoja.FirstOrDefaultAsync(ul => ul.UsuarioId == UsuarioId && ul.Ativo);
        return v?.LojaId;
    }

    [HttpGet]
    public async Task<IActionResult> Listar()
    {
        var perfis = await db.PerfisLoja
            .Include(p => p.Categorias.OrderBy(c => c.Ordem))
            .Include(p => p.CamposExtras.OrderBy(c => c.Ordem))
            .Where(p => p.Ativo)
            .ToListAsync();

        return Ok(perfis.Select(p => new {
            p.Id,
            p.Nome,
            p.Descricao,
            p.Icone,
            categorias = p.Categorias.Select(c => new { c.Id, c.Nome, c.Ordem, c.TipoTamanho }),
            camposExtras = p.CamposExtras.Select(c => new { c.Id, c.Chave, c.Label, c.Tipo, c.Opcoes, c.Obrigatorio, c.Ordem }),
        }));
    }

    [HttpPost("aplicar")]
    public async Task<IActionResult> Aplicar([FromBody] AplicarPerfilRequest req)
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return NotFound(new { erro = "Loja não encontrada." });

        var perfil = await db.PerfisLoja
            .Include(p => p.Categorias)
            .Include(p => p.CamposExtras)
            .FirstOrDefaultAsync(p => p.Id == req.PerfilLojaId);

        if (perfil is null) return NotFound(new { erro = "Perfil não encontrado." });

        // Remove existentes
        db.CategoriasLoja.RemoveRange(await db.CategoriasLoja.Where(c => c.LojaId == lojaId).ToListAsync());
        db.CamposExtrasLoja.RemoveRange(await db.CamposExtrasLoja.Where(c => c.LojaId == lojaId).ToListAsync());

        // Adiciona do perfil
        foreach (var cat in perfil.Categorias)
            db.CategoriasLoja.Add(new CategoriaLoja { LojaId = lojaId.Value, Nome = cat.Nome, Ordem = cat.Ordem });

        foreach (var campo in perfil.CamposExtras)
            db.CamposExtrasLoja.Add(new CampoExtraLoja
            {
                LojaId = lojaId.Value,
                Chave = campo.Chave,
                Label = campo.Label,
                Tipo = campo.Tipo,
                Opcoes = campo.Opcoes,
                Obrigatorio = campo.Obrigatorio,
                Ordem = campo.Ordem,
            });

        await db.SaveChangesAsync();
        return Ok(new { mensagem = $"Perfil '{perfil.Nome}' aplicado!" });
    }

    [HttpGet("loja/categorias")]
    public async Task<IActionResult> Categorias()
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return Ok(new List<object>());

        var cats = await db.CategoriasLoja
            .Where(c => c.LojaId == lojaId && c.Ativo)
            .OrderBy(c => c.Ordem)
            .Select(c => new { c.Id, c.Nome, c.Ativo, c.Ordem, c.TipoTamanho })
            .ToListAsync();

        return Ok(cats);
    }

    [HttpGet("loja/campos-extras")]
    public async Task<IActionResult> CamposExtras()
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return Ok(new List<object>());

        var campos = await db.CamposExtrasLoja
            .Where(c => c.LojaId == lojaId && c.Ativo)
            .OrderBy(c => c.Ordem)
            .Select(c => new { c.Id, c.Chave, c.Label, c.Tipo, c.Opcoes, c.Obrigatorio, c.Ativo, c.Ordem })
            .ToListAsync();

        return Ok(campos);
    }

    [HttpPost("loja/categorias")]
    public async Task<IActionResult> AdicionarCategoria([FromBody] SalvarCategoriaRequest req)
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return NotFound();

        var cat = new CategoriaLoja { LojaId = lojaId.Value, Nome = req.Nome, Ordem = req.Ordem };
        db.CategoriasLoja.Add(cat);
        await db.SaveChangesAsync();
        return Ok(new { cat.Id, cat.Nome, cat.Ativo, cat.Ordem });
    }
}

public record AplicarPerfilRequest(Guid PerfilLojaId);
public record SalvarCategoriaRequest(string Nome, int Ordem);