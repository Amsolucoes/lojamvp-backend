using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using LojaApi.Data;
using LojaApi.src.Services;

namespace LojaApi.src.Controllers;

[ApiController]
[Route("api/chacara/reservas")]
[Authorize]
public class ReservaChacaraController(AppDbContext db, ReservaChacaraNotificacaoService notificacao) : ControllerBase
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

        var lista = await db.Reservas
            .Where(r => r.LojaId == lojaId)
            .OrderByDescending(r => r.CriadoEm)
            .ToListAsync();

        return Ok(lista);
    }

    [HttpPatch("{id:int}/confirmar")]
    public async Task<IActionResult> Confirmar(int id)
    {
        var lojaId = await GetLojaId();
        var reserva = await db.Reservas.FirstOrDefaultAsync(r => r.Id == id && r.LojaId == lojaId);
        if (reserva is null) return NotFound();

        if (reserva.Status == "confirmada")
            return BadRequest(new { erro = "Esta reserva já está confirmada." });

        reserva.Status = "confirmada";
        await db.SaveChangesAsync();

        await notificacao.NotificarConfirmacaoAsync(reserva);

        return Ok(new { reserva.Id, reserva.Status });
    }
}