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

        // Reservas "em negociação" — pendentes de pagamento que você marcou pra não expirar
        // sozinha (ExpiraEm == null). Ficam separadas dos confirmados até você de fato confirmar.
        var emNegociacao = await db.Reservas
            .Where(r => r.LojaId == lojaId && r.Status == "pendente_pagamento" && r.ExpiraEm == null)
            .ToListAsync();

        // Totais gerais (todo o histórico de reservas confirmadas, sem filtro de data)
        var totalReservas = confirmadas.Count;
        var totalPago = confirmadas.Sum(r => r.ValorPago);
        var totalPendente = confirmadas.Sum(r => r.Valor - r.ValorPago);
        var totalNegociacao = emNegociacao.Count;
        var totalPendenteNegociacao = emNegociacao.Sum(r => r.Valor - r.ValorPago);

        // Quebra mês a mês, a partir do mês atual
        var meses = new List<object>();
        var mesBase = new DateTime(agora.Year, agora.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        // Garante que a janela sempre cubra a reserva confirmada (ou em negociação) mais distante
        // no futuro, mesmo que isso ultrapasse os "mesesAFrente" pedidos (ex: reserva de Ano Novo
        // que atravessa a virada do ano, feita com bastante antecedência).
        var totalMeses = mesesAFrente;
        var todasParaJanela = confirmadas.Concat(emNegociacao).ToList();
        if (todasParaJanela.Count > 0)
        {
            var maxDataFim = todasParaJanela.Max(r => r.DataFim);
            var mesesNecessarios = (maxDataFim.Year - mesBase.Year) * 12 + (maxDataFim.Month - mesBase.Month) + 1;
            totalMeses = Math.Max(totalMeses, Math.Min(mesesNecessarios, 24)); // limite de segurança: 24 meses
        }

        for (int i = 0; i < totalMeses; i++)
        {
            var primeiroDia = mesBase.AddMonths(i);
            var ultimoDia = primeiroDia.AddMonths(1).AddDays(-1);
            var diasNoMes = ultimoDia.Day;

            var reservasDoMes = confirmadas
                .Where(r => r.DataInicio <= ultimoDia && r.DataFim >= primeiroDia)
                .ToList();

            int diasOcupados = 0;
            decimal receita = 0;
            decimal pagoMes = 0;
            decimal pendenteMes = 0;
            foreach (var r in reservasDoMes)
            {
                var inicioInterv = r.DataInicio > primeiroDia ? r.DataInicio : primeiroDia;
                var fimInterv = r.DataFim < ultimoDia ? r.DataFim : ultimoDia;
                var diasNesseMes = (int)Math.Round((fimInterv - inicioInterv).TotalDays) + 1;
                var totalDiasReserva = (int)Math.Round((r.DataFim - r.DataInicio).TotalDays) + 1;
                var proporcao = totalDiasReserva > 0 ? (decimal)diasNesseMes / totalDiasReserva : 0;

                diasOcupados += diasNesseMes;
                // Receita, pago e pendente alocados proporcionalmente quando a reserva atravessa a virada do mês
                receita += r.Valor * proporcao;
                pagoMes += r.ValorPago * proporcao;
                pendenteMes += (r.Valor - r.ValorPago) * proporcao;
            }

            var percentualOcupado = diasNoMes > 0 ? Math.Round((decimal)diasOcupados / diasNoMes * 100, 1) : 0;

            // Mesma lógica de proporção, agora pras reservas em negociação desse mês
            var negociacaoDoMes = emNegociacao
                .Where(r => r.DataInicio <= ultimoDia && r.DataFim >= primeiroDia)
                .ToList();

            int diasOcupadosNegociacao = 0;
            decimal valorNegociacaoMes = 0;
            foreach (var r in negociacaoDoMes)
            {
                var inicioInterv = r.DataInicio > primeiroDia ? r.DataInicio : primeiroDia;
                var fimInterv = r.DataFim < ultimoDia ? r.DataFim : ultimoDia;
                var diasNesseMes = (int)Math.Round((fimInterv - inicioInterv).TotalDays) + 1;
                var totalDiasReserva = (int)Math.Round((r.DataFim - r.DataInicio).TotalDays) + 1;
                var proporcao = totalDiasReserva > 0 ? (decimal)diasNesseMes / totalDiasReserva : 0;

                diasOcupadosNegociacao += diasNesseMes;
                valorNegociacaoMes += (r.Valor - r.ValorPago) * proporcao;
            }

            var percentualNegociacao = diasNoMes > 0 ? Math.Round((decimal)diasOcupadosNegociacao / diasNoMes * 100, 1) : 0;

            meses.Add(new
            {
                ano = primeiroDia.Year,
                mes = primeiroDia.Month,
                qtdReservas = reservasDoMes.Count,
                diasOcupados,
                diasNoMes,
                percentualOcupado,
                percentualLivre = Math.Round(100 - percentualOcupado - percentualNegociacao, 1),
                receita,
                pago = pagoMes,
                pendente = pendenteMes,
                qtdNegociacao = negociacaoDoMes.Count,
                diasOcupadosNegociacao,
                percentualNegociacao,
                valorNegociacao = valorNegociacaoMes,
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

        var reservasEmNegociacao = emNegociacao
            .OrderBy(r => r.DataInicio)
            .Take(10)
            .Select(r => new { r.Id, r.ClienteNome, r.DataInicio, r.DataFim, r.Pessoas, r.Valor, r.ValorPago, saldoPendente = r.Valor - r.ValorPago })
            .ToList();

        return Ok(new
        {
            totalReservas,
            totalPago,
            totalPendente,
            totalNegociacao,
            totalPendenteNegociacao,
            meses,
            proximasReservas = proximas,
            pendentes,
            reservasEmNegociacao,
        });
    }
}