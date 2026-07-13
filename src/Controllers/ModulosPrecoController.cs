using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LojaApi.Data;

namespace LojaApi.src.Controllers;

[ApiController]
[Route("api/modulos-preco")]
[Authorize]
public class ModulosPrecoController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listar()
    {
        var lista = await db.ModulosPreco.OrderBy(m => m.Nome).ToListAsync();
        return Ok(lista);
    }

    public record AtualizarModuloPrecoRequest(decimal Valor, bool DisponivelParaAtivar);

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "superadmin")]
    public async Task<IActionResult> Atualizar(Guid id, [FromBody] AtualizarModuloPrecoRequest req)
    {
        var modulo = await db.ModulosPreco.FindAsync(id);
        if (modulo is null) return NotFound();

        modulo.Valor = req.Valor;
        modulo.DisponivelParaAtivar = req.DisponivelParaAtivar;
        modulo.AtualizadoEm = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return Ok(modulo);
    }
}