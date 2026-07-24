using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using LojaApi.Data;

namespace LojaApi.src.Controllers;

[ApiController]
[Route("api/chacara")]
[Authorize]
public class ChacaraDashboardController(AppDbContext db) : ControllerBase
{
    private Guid UsuarioId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private async Task<Guid?> GetLojaId()
    {
        var vinculo = await db.UsuariosLoja
            .FirstOrDefaultAsync(ul => ul.UsuarioId == UsuarioId && ul.Ativo);
        return vinculo?.LojaId;
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard([FromQuery] int dias = 15)
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return Ok(new { });

        var agora = DateTime.UtcNow;
        var fimPeriodo = agora.AddDays(dias);

        var confirmadas = await db.Reservas
            .Where(r => r.LojaId == lojaId && r.Status == "confirmada")
            .ToListAsync();

        var proximas = confirmadas
            .Where(r => r.DataFim >= agora)
            .OrderBy(r => r.DataInicio)
            .Take(10)
            .Select(r => new { r.Id, r.ClienteNome, r.DataInicio, r.DataFim, r.Pessoas, r.Valor, r.ValorPago, saldoPendente = r.Valor - r.ValorPago })
            .ToList();

        var noPeriodo = confirmadas
            .Where(r => r.DataInicio >= agora && r.DataInicio <= fimPeriodo)
            .ToList();

        var receitaPeriodo = noPeriodo.Sum(r => r.Valor);

        var diasReservados = confirmadas
            .Where(r => r.DataInicio < fimPeriodo && r.DataFim >= agora)
            .Sum(r => (int)Math.Round((r.DataFim - r.DataInicio).TotalDays) + 1);

        var pendentes = await db.Reservas
            .Where(r => r.LojaId == lojaId && r.Status == "pendente_pagamento" && r.ExpiraEm > agora)
            .OrderBy(r => r.CriadoEm)
            .Select(r => new { r.Id, r.ClienteNome, r.DataInicio, r.DataFim, r.Valor, r.ExpiraEm })
            .ToListAsync();

        return Ok(new
        {
            proximasReservas = proximas,
            receitaPeriodo,
            reservasNoPeriodo = noPeriodo.Count,
            diasReservados,
            diasPeriodo = dias,
            pendentes,
        });
    }
}