using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using LojaApi.Data;
using LojaApi.Models;

namespace LojaApi.src.Controllers;

[ApiController]
[Route("api/origens-venda")]
[Authorize]
public class OrigensVendaController(AppDbContext db) : ControllerBase
{
    private Guid UsuarioId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private async Task<Guid?> GetLojaId()
    {
        var vinculo = await db.UsuariosLoja.FirstOrDefaultAsync(ul => ul.UsuarioId == UsuarioId && ul.Ativo);
        return vinculo?.LojaId;
    }

    [HttpGet]
    public async Task<IActionResult> Listar()
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return Ok(Array.Empty<object>());

        var lista = await db.OrigensVenda
            .Where(o => o.LojaId == lojaId && o.Ativa)
            .OrderBy(o => o.Ordem).ThenBy(o => o.Nome)
            .Select(o => new { o.Id, o.Nome, o.Ordem })
            .ToListAsync();

        return Ok(lista);
    }

    [HttpGet("todas")]
    public async Task<IActionResult> ListarTodas()
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return Ok(Array.Empty<object>());

        var lista = await db.OrigensVenda
            .Where(o => o.LojaId == lojaId)
            .OrderBy(o => o.Ordem).ThenBy(o => o.Nome)
            .ToListAsync();

        return Ok(lista);
    }

    public record SalvarOrigemRequest(string Nome, int Ordem, bool Ativa);

    [HttpPost]
    public async Task<IActionResult> Criar([FromBody] SalvarOrigemRequest req)
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return BadRequest(new { erro = "Loja não encontrada." });

        if (string.IsNullOrWhiteSpace(req.Nome))
            return BadRequest(new { erro = "Informe o nome da origem." });

        var existente = await db.OrigensVenda.AnyAsync(o => o.LojaId == lojaId && o.Nome == req.Nome.Trim());
        if (existente)
            return BadRequest(new { erro = "Já existe uma origem com esse nome." });

        var origem = new OrigemVenda
        {
            LojaId = lojaId.Value,
            Nome = req.Nome.Trim(),
            Ordem = req.Ordem,
            Ativa = req.Ativa,
        };
        db.OrigensVenda.Add(origem);
        await db.SaveChangesAsync();

        return Ok(origem);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Atualizar(Guid id, [FromBody] SalvarOrigemRequest req)
    {
        var lojaId = await GetLojaId();
        var origem = await db.OrigensVenda.FirstOrDefaultAsync(o => o.Id == id && o.LojaId == lojaId);
        if (origem is null) return NotFound();

        origem.Nome = req.Nome.Trim();
        origem.Ordem = req.Ordem;
        origem.Ativa = req.Ativa;
        await db.SaveChangesAsync();

        return Ok(origem);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Excluir(Guid id)
    {
        var lojaId = await GetLojaId();
        var origem = await db.OrigensVenda.FirstOrDefaultAsync(o => o.Id == id && o.LojaId == lojaId);
        if (origem is null) return NotFound();

        var emUso = await db.Vendas.AnyAsync(v => v.OrigemVendaId == id);
        if (emUso)
        {
            origem.Ativa = false; // desativa em vez de excluir, se já usada em vendas
            await db.SaveChangesAsync();
            return Ok(new { mensagem = "Origem em uso — foi desativada em vez de excluída." });
        }

        db.OrigensVenda.Remove(origem);
        await db.SaveChangesAsync();
        return Ok(new { mensagem = "Origem excluída." });
    }

    [HttpPost("seed-padrao")]
    public async Task<IActionResult> SeedPadrao()
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return BadRequest(new { erro = "Loja não encontrada." });

        var jaTem = await db.OrigensVenda.AnyAsync(o => o.LojaId == lojaId);
        if (jaTem)
            return BadRequest(new { erro = "Você já tem origens cadastradas." });

        var padrao = new[] { "Loja física", "Site", "WhatsApp" };
        for (int i = 0; i < padrao.Length; i++)
        {
            db.OrigensVenda.Add(new OrigemVenda
            {
                LojaId = lojaId.Value,
                Nome = padrao[i],
                Ordem = i,
            });
        }

        await db.SaveChangesAsync();
        return Ok(new { mensagem = "Origens padrão criadas." });
    }
}