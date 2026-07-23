using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LojaApi.Data;
using LojaApi.src.Models;
using LojaApi.src.Services;

namespace LojaApi.Controllers;

[ApiController]
[Route("api/publico/{slug}/chacara")]
[AllowAnonymous]
public class PublicoChacaraController(AppDbContext db) : ControllerBase
{
    [HttpGet("disponibilidade")]
    public async Task<IActionResult> Disponibilidade(string slug, [FromQuery] DateTime dataInicio, [FromQuery] DateTime dataFim)
    {
        var loja = await db.Lojas.FirstOrDefaultAsync(l => l.Slug == slug);
        if (loja is null) return NotFound(new { erro = "Página não encontrada." });

        var ini = DateTime.SpecifyKind(dataInicio.Date, DateTimeKind.Utc).AddHours(12);
        var fim = DateTime.SpecifyKind(dataFim.Date, DateTimeKind.Utc).AddHours(12);

        var conflita = await db.Reservas.AnyAsync(r =>
            r.LojaId == loja.Id &&
            (r.Status == "confirmada" || (r.Status == "pendente_pagamento" && r.ExpiraEm > DateTime.UtcNow)) &&
            r.DataInicio <= fim && r.DataFim >= ini);

        return Ok(new { disponivel = !conflita });
    }

    [HttpGet("valor")]
    public async Task<IActionResult> Valor(string slug, [FromQuery] DateTime dataInicio, [FromQuery] DateTime dataFim, [FromQuery] int pessoas)
    {
        var loja = await db.Lojas.FirstOrDefaultAsync(l => l.Slug == slug);
        if (loja is null) return NotFound(new { erro = "Página não encontrada." });

        var cfg = await db.ConfiguracoesPrecoChacara.FirstOrDefaultAsync(c => c.LojaId == loja.Id)
            ?? new ConfiguracaoPrecoChacara { LojaId = loja.Id };

        var ini = DateTime.SpecifyKind(dataInicio.Date, DateTimeKind.Utc).AddHours(12);
        var fim = DateTime.SpecifyKind(dataFim.Date, DateTimeKind.Utc).AddHours(12);

        var resultado = CalculadoraPrecoChacara.Calcular(ini, fim, pessoas, cfg);
        return Ok(resultado);
    }

    [HttpPost("reservar")]
    public async Task<IActionResult> Reservar(string slug, [FromBody] ReservarPublicoRequest req)
    {
        var loja = await db.Lojas.FirstOrDefaultAsync(l => l.Slug == slug);
        if (loja is null) return NotFound(new { erro = "Página não encontrada." });

        if (string.IsNullOrWhiteSpace(req.ClienteNome) || string.IsNullOrWhiteSpace(req.ClienteTelefone) || string.IsNullOrWhiteSpace(req.ClienteEmail))
            return BadRequest(new { erro = "Informe nome, e-mail e telefone." });

        if (req.DataFim.Date < req.DataInicio.Date)
            return BadRequest(new { erro = "Data final não pode ser antes da data inicial." });

        if (req.Pessoas <= 0)
            return BadRequest(new { erro = "Informe a quantidade de pessoas." });

        var ini = DateTime.SpecifyKind(req.DataInicio.Date, DateTimeKind.Utc).AddHours(12);
        var fim = DateTime.SpecifyKind(req.DataFim.Date, DateTimeKind.Utc).AddHours(12);

        // Revalida disponibilidade (evita corrida entre duas pessoas reservando ao mesmo tempo)
        var conflita = await db.Reservas.AnyAsync(r =>
            r.LojaId == loja.Id &&
            (r.Status == "confirmada" || (r.Status == "pendente_pagamento" && r.ExpiraEm > DateTime.UtcNow)) &&
            r.DataInicio <= fim && r.DataFim >= ini);

        if (conflita)
            return Conflict(new { erro = "Essas datas acabaram de ser reservadas. Escolha outro período." });

        var cfg = await db.ConfiguracoesPrecoChacara.FirstOrDefaultAsync(c => c.LojaId == loja.Id)
            ?? new ConfiguracaoPrecoChacara { LojaId = loja.Id };

        var resultado = CalculadoraPrecoChacara.Calcular(ini, fim, req.Pessoas, cfg);

        var reserva = new Reserva
        {
            LojaId = loja.Id,
            DataInicio = ini,
            DataFim = fim,
            Pessoas = req.Pessoas,
            ClienteNome = req.ClienteNome.Trim(),
            ClienteEmail = req.ClienteEmail.Trim(),
            ClienteTelefone = new string(req.ClienteTelefone.Where(char.IsDigit).ToArray()),
            Valor = resultado.ValorTotal,
            Status = "pendente_pagamento",
            ExpiraEm = DateTime.UtcNow.AddMinutes(15),
        };

        db.Reservas.Add(reserva);
        await db.SaveChangesAsync();

        return Ok(new
        {
            reserva.Id,
            reserva.Valor,
            reserva.ExpiraEm,
            mensagem = "Reserva criada. Prossiga para o pagamento.",
        });
    }
}

public record ReservarPublicoRequest(
    DateTime DataInicio,
    DateTime DataFim,
    int Pessoas,
    string ClienteNome,
    string ClienteEmail,
    string ClienteTelefone
);