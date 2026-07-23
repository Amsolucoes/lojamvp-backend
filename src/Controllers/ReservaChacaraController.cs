using LojaApi.Data;
using LojaApi.src.Models;
using LojaApi.src.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

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

    public record CriarReservaManualRequest(
        DateTime DataInicio, DateTime DataFim, int Pessoas,
        string ClienteNome, string? ClienteEmail, string? ClienteTelefone,
        decimal Valor
    );

    [HttpPost]
    public async Task<IActionResult> CriarManual([FromBody] CriarReservaManualRequest req)
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return BadRequest(new { erro = "Loja não encontrada." });

        if (string.IsNullOrWhiteSpace(req.ClienteNome))
            return BadRequest(new { erro = "Informe o nome do cliente." });

        if (req.DataFim.Date < req.DataInicio.Date)
            return BadRequest(new { erro = "Data final não pode ser antes da data inicial." });

        var ini = DateTime.SpecifyKind(req.DataInicio.Date, DateTimeKind.Utc).AddHours(12);
        var fim = DateTime.SpecifyKind(req.DataFim.Date, DateTimeKind.Utc).AddHours(12);

        var conflita = await db.Reservas.AnyAsync(r =>
            r.LojaId == lojaId &&
            (r.Status == "confirmada" || (r.Status == "pendente_pagamento" && r.ExpiraEm > DateTime.UtcNow)) &&
            r.DataInicio <= fim && r.DataFim >= ini);

        if (conflita)
            return Conflict(new { erro = "Essas datas conflitam com outra reserva existente." });

        var reserva = new Reserva
        {
            LojaId = lojaId.Value,
            DataInicio = ini,
            DataFim = fim,
            Pessoas = req.Pessoas,
            ClienteNome = req.ClienteNome.Trim(),
            ClienteEmail = req.ClienteEmail?.Trim() ?? "",
            ClienteTelefone = new string((req.ClienteTelefone ?? "").Where(char.IsDigit).ToArray()),
            Valor = req.Valor,
            Status = "confirmada", // já fechado por fora, entra direto como confirmada, sem notificação
        };

        db.Reservas.Add(reserva);
        await db.SaveChangesAsync();

        return Ok(reserva);
    }

    public record EditarReservaRequest(DateTime DataInicio, DateTime DataFim, int Pessoas, string ClienteNome, string ClienteEmail, string ClienteTelefone);

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Editar(int id, [FromBody] EditarReservaRequest req)
    {
        var lojaId = await GetLojaId();
        var reserva = await db.Reservas.FirstOrDefaultAsync(r => r.Id == id && r.LojaId == lojaId);
        if (reserva is null) return NotFound();

        // TODO: quando o Mercado Pago estiver integrado de verdade, reavaliar se
        // reserva confirmada (já paga) deveria continuar editável livremente aqui,
        // ou exigir um fluxo à parte (estorno/reajuste de cobrança).
        if (req.DataFim.Date < req.DataInicio.Date)
            return BadRequest(new { erro = "Data final não pode ser antes da data inicial." });

        var diasSolicitadosEdicao = (int)Math.Round((req.DataFim.Date - req.DataInicio.Date).TotalDays) + 1;
        if (diasSolicitadosEdicao > 30)
            return BadRequest(new { erro = "O período máximo por reserva é de 30 dias." });

        var cfg = await db.ConfiguracoesPrecoChacara.FirstOrDefaultAsync(c => c.LojaId == lojaId)
            ?? new ConfiguracaoPrecoChacara { LojaId = lojaId!.Value };

        if (req.Pessoas < cfg.MinimoPessoas)
            return BadRequest(new { erro = $"O mínimo é de {cfg.MinimoPessoas} pessoas." });

        var ini = DateTime.SpecifyKind(req.DataInicio.Date, DateTimeKind.Utc).AddHours(12);
        var fim = DateTime.SpecifyKind(req.DataFim.Date, DateTimeKind.Utc).AddHours(12);

        // Revalida disponibilidade, excluindo a própria reserva da checagem
        var conflita = await db.Reservas.AnyAsync(r =>
            r.LojaId == lojaId && r.Id != id &&
            (r.Status == "confirmada" || (r.Status == "pendente_pagamento" && r.ExpiraEm > DateTime.UtcNow)) &&
            r.DataInicio <= fim && r.DataFim >= ini);

        if (conflita)
            return Conflict(new { erro = "Essas datas conflitam com outra reserva existente." });

        var resultado = CalculadoraPrecoChacara.Calcular(ini, fim, req.Pessoas, cfg);

        reserva.DataInicio = ini;
        reserva.DataFim = fim;
        reserva.Pessoas = req.Pessoas;
        reserva.ClienteNome = req.ClienteNome.Trim();
        reserva.ClienteEmail = req.ClienteEmail.Trim();
        reserva.ClienteTelefone = new string(req.ClienteTelefone.Where(char.IsDigit).ToArray());
        var eraConfirmada = reserva.Status == "confirmada";

        reserva.Valor = resultado.ValorTotal;

        await db.SaveChangesAsync();

        if (eraConfirmada)
        {
            await notificacao.ReenviarContratoAtualizadoAsync(reserva);
        }

        return Ok(reserva);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Excluir(int id)
    {
        var lojaId = await GetLojaId();
        var reserva = await db.Reservas.FirstOrDefaultAsync(r => r.Id == id && r.LojaId == lojaId);
        if (reserva is null) return NotFound();

        // TODO: mesma ressalva do Editar — revisar quando o pagamento real (Mercado Pago)
        // estiver funcionando, pra não apagar reserva paga sem tratar estorno.
        db.Reservas.Remove(reserva);
        await db.SaveChangesAsync();

        return Ok(new { mensagem = "Reserva excluída." });
    }
}