using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using LojaApi.Data;
using LojaApi.src.Models;

namespace LojaApi.src.Controllers;

[ApiController]
[Route("api/chacara/periodos-especiais")]
[Authorize]
public class PeriodoEspecialChacaraController(AppDbContext db) : ControllerBase
{
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

        var lista = await db.PeriodosEspeciaisChacara
            .Where(p => p.LojaId == lojaId)
            .OrderBy(p => p.DataInicio)
            .ToListAsync();

        return Ok(lista);
    }

    public record SalvarPeriodoRequest(string Nome, DateTime DataInicio, DateTime DataFim, decimal ValorTotal);

    [HttpPost]
    public async Task<IActionResult> Criar([FromBody] SalvarPeriodoRequest req)
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return BadRequest(new { erro = "Loja não encontrada." });

        if (string.IsNullOrWhiteSpace(req.Nome))
            return BadRequest(new { erro = "Informe um nome para o período." });

        if (req.DataFim.Date < req.DataInicio.Date)
            return BadRequest(new { erro = "Data final não pode ser antes da data inicial." });

        var periodo = new PeriodoEspecialChacara
        {
            LojaId = lojaId.Value,
            Nome = req.Nome.Trim(),
            DataInicio = DateTime.SpecifyKind(req.DataInicio.Date, DateTimeKind.Utc).AddHours(12),
            DataFim = DateTime.SpecifyKind(req.DataFim.Date, DateTimeKind.Utc).AddHours(12),
            ValorTotal = req.ValorTotal,
        };
        db.PeriodosEspeciaisChacara.Add(periodo);
        await db.SaveChangesAsync();

        return Ok(periodo);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Atualizar(int id, [FromBody] SalvarPeriodoRequest req)
    {
        var lojaId = await GetLojaId();
        var periodo = await db.PeriodosEspeciaisChacara.FirstOrDefaultAsync(p => p.Id == id && p.LojaId == lojaId);
        if (periodo is null) return NotFound();

        if (req.DataFim.Date < req.DataInicio.Date)
            return BadRequest(new { erro = "Data final não pode ser antes da data inicial." });

        periodo.Nome = req.Nome.Trim();
        periodo.DataInicio = DateTime.SpecifyKind(req.DataInicio.Date, DateTimeKind.Utc).AddHours(12);
        periodo.DataFim = DateTime.SpecifyKind(req.DataFim.Date, DateTimeKind.Utc).AddHours(12);
        periodo.ValorTotal = req.ValorTotal;

        await db.SaveChangesAsync();
        return Ok(periodo);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Excluir(int id)
    {
        var lojaId = await GetLojaId();
        var periodo = await db.PeriodosEspeciaisChacara.FirstOrDefaultAsync(p => p.Id == id && p.LojaId == lojaId);
        if (periodo is null) return NotFound();

        db.PeriodosEspeciaisChacara.Remove(periodo);
        await db.SaveChangesAsync();
        return Ok(new { mensagem = "Período excluído." });
    }
}