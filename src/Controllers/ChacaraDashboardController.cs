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
    public async Task<IActionResult> Dashboard([FromQuery] int mesesAFrente = 6)
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return Ok(new { });

        var agora = DateTime.UtcNow;

        var confirmadas = await db.Reservas
            .Where(r => r.LojaId == lojaId && r.Status == "confirmada")
            .ToListAsync();

        // Totais gerais (todo o histórico de reservas confirmadas, sem filtro de data)
        var totalReservas = confirmadas.Count;
        var totalPago = confirmadas.Sum(r => r.ValorPago);
        var totalPendente = confirmadas.Sum(r => r.Valor - r.ValorPago);

        // Quebra mês a mês, a partir do mês atual
        var meses = new List<object>();
        var mesBase = new DateTime(agora.Year, agora.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        for (int i = 0; i < mesesAFrente; i++)
        {
            var primeiroDia = mesBase.AddMonths(i);
            var ultimoDia = primeiroDia.AddMonths(1).AddDays(-1);
            var diasNoMes = ultimoDia.Day;

            var reservasDoMes = confirmadas
                .Where(r => r.DataInicio <= ultimoDia && r.DataFim >= primeiroDia)
                .ToList();

            int diasOcupados = 0;
            decimal receita = 0;
            foreach (var r in reservasDoMes)
            {
                var inicioInterv = r.DataInicio > primeiroDia ? r.DataInicio : primeiroDia;
                var fimInterv = r.DataFim < ultimoDia ? r.DataFim : ultimoDia;
                var diasNesseMes = (int)Math.Round((fimInterv - inicioInterv).TotalDays) + 1;
                var totalDiasReserva = (int)Math.Round((r.DataFim - r.DataInicio).TotalDays) + 1;

                diasOcupados += diasNesseMes;
                // Receita alocada proporcionalmente quando a reserva atravessa a virada do mês
                receita += totalDiasReserva > 0 ? r.Valor * diasNesseMes / totalDiasReserva : 0;
            }

            var percentualOcupado = diasNoMes > 0 ? Math.Round((decimal)diasOcupados / diasNoMes * 100, 1) : 0;

            meses.Add(new
            {
                ano = primeiroDia.Year,
                mes = primeiroDia.Month,
                qtdReservas = reservasDoMes.Count,
                diasOcupados,
                diasNoMes,
                percentualOcupado,
                percentualLivre = Math.Round(100 - percentualOcupado, 1),
                receita,
            });
        }

        var proximas = confirmadas
            .Where(r => r.DataFim >= agora)
            .OrderBy(r => r.DataInicio)
            .Take(10)
            .Select(r => new { r.Id, r.ClienteNome, r.DataInicio, r.DataFim, r.Pessoas, r.Valor, r.ValorPago, saldoPendente = r.Valor - r.ValorPago })
            .ToList();

        var pendentes = await db.Reservas
            .Where(r => r.LojaId == lojaId && r.Status == "pendente_pagamento" && r.ExpiraEm > agora)
            .OrderBy(r => r.CriadoEm)
            .Select(r => new { r.Id, r.ClienteNome, r.DataInicio, r.DataFim, r.Valor, r.ExpiraEm })
            .ToListAsync();

        return Ok(new
        {
            totalReservas,
            totalPago,
            totalPendente,
            meses,
            proximasReservas = proximas,
            pendentes,
        });
    }
}