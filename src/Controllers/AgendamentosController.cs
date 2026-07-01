using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using LojaApi.Data;
using LojaApi.Models;

namespace LojaApi.Controllers;

[ApiController]
[Route("api/agendamentos")]
[Authorize]
public class AgendamentosController(AppDbContext db) : ControllerBase
{
    private Guid UsuarioId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private async Task<Guid?> GetLojaId()
    {
        var vinculo = await db.UsuariosLoja
            .FirstOrDefaultAsync(ul => ul.UsuarioId == UsuarioId && ul.Ativo);
        return vinculo?.LojaId;
    }

    // ── Listar por dia ────────────────────────────────────────────
    // GET /api/agendamentos?data=2026-06-30
    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] DateTime? data)
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return Ok(Array.Empty<object>());

        var q = db.Agendamentos.Where(a => a.LojaId == lojaId);

        if (data.HasValue)
        {
            var dia = DateTime.SpecifyKind(data.Value.Date, DateTimeKind.Utc);
            var fim = dia.AddDays(1);
            q = q.Where(a => a.DataHora >= dia && a.DataHora < fim);
        }

        var lista = await q
            .OrderBy(a => a.DataHora)
            .Select(a => new {
                a.Id,
                a.ServicoId,
                a.NomeServico,
                a.ClienteId,
                a.NomeCliente,
                a.Preco,
                a.DataHora,
                a.DuracaoMin,
                a.Status,
                a.Observacao,
            })
            .ToListAsync();

        return Ok(lista);
    }

    // ── Criar ─────────────────────────────────────────────────────
    [HttpPost]
    public async Task<IActionResult> Criar([FromBody] SalvarAgendamentoRequest req)
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return BadRequest(new { erro = "Loja não encontrada." });

        var servico = await db.Servicos.FirstOrDefaultAsync(s => s.Id == req.ServicoId && s.LojaId == lojaId);
        if (servico is null) return BadRequest(new { erro = "Serviço não encontrado." });

        // Nome do cliente: cadastrado ou avulso
        string? nomeCliente = req.NomeCliente;
        if (req.ClienteId.HasValue)
        {
            var cliente = await db.Clientes.FindAsync(req.ClienteId.Value);
            if (cliente != null) nomeCliente = cliente.Nome;
        }

        var ag = new Agendamento
        {
            LojaId = lojaId.Value,
            ServicoId = servico.Id,
            NomeServico = servico.Nome,
            ClienteId = req.ClienteId,
            NomeCliente = nomeCliente,
            Preco = req.Preco > 0 ? req.Preco : servico.Preco,
            DataHora = req.DataHora,
            DuracaoMin = req.DuracaoMin > 0 ? req.DuracaoMin : servico.DuracaoMin,
            Status = "agendado",
            Observacao = req.Observacao,
        };
        db.Agendamentos.Add(ag);
        await db.SaveChangesAsync();

        return Ok(new
        {
            ag.Id,
            ag.ServicoId,
            ag.NomeServico,
            ag.ClienteId,
            ag.NomeCliente,
            ag.Preco,
            ag.DataHora,
            ag.DuracaoMin,
            ag.Status,
            ag.Observacao,
        });
    }

    // ── Atualizar (editar dados) ──────────────────────────────────
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Atualizar(Guid id, [FromBody] SalvarAgendamentoRequest req)
    {
        var lojaId = await GetLojaId();
        var ag = await db.Agendamentos.FirstOrDefaultAsync(a => a.Id == id && a.LojaId == lojaId);
        if (ag is null) return NotFound();

        var servico = await db.Servicos.FirstOrDefaultAsync(s => s.Id == req.ServicoId && s.LojaId == lojaId);
        if (servico is null) return BadRequest(new { erro = "Serviço não encontrado." });

        string? nomeCliente = req.NomeCliente;
        if (req.ClienteId.HasValue)
        {
            var cliente = await db.Clientes.FindAsync(req.ClienteId.Value);
            if (cliente != null) nomeCliente = cliente.Nome;
        }

        ag.ServicoId = servico.Id;
        ag.NomeServico = servico.Nome;
        ag.ClienteId = req.ClienteId;
        ag.NomeCliente = nomeCliente;
        ag.Preco = req.Preco > 0 ? req.Preco : servico.Preco;
        ag.DataHora = req.DataHora;
        ag.DuracaoMin = req.DuracaoMin > 0 ? req.DuracaoMin : servico.DuracaoMin;
        ag.Observacao = req.Observacao;
        await db.SaveChangesAsync();

        return Ok(new
        {
            ag.Id,
            ag.ServicoId,
            ag.NomeServico,
            ag.ClienteId,
            ag.NomeCliente,
            ag.Preco,
            ag.DataHora,
            ag.DuracaoMin,
            ag.Status,
            ag.Observacao,
        });
    }

    // ── Mudar status (concluir / cancelar / reabrir) ──────────────
    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> MudarStatus(Guid id, [FromBody] StatusAgendamentoRequest req)
    {
        var lojaId = await GetLojaId();
        var ag = await db.Agendamentos.FirstOrDefaultAsync(a => a.Id == id && a.LojaId == lojaId);
        if (ag is null) return NotFound();

        var validos = new[] { "agendado", "concluido", "cancelado" };
        if (!validos.Contains(req.Status))
            return BadRequest(new { erro = "Status inválido." });

        ag.Status = req.Status;
        await db.SaveChangesAsync();
        return Ok(new { ag.Id, ag.Status });
    }

    // ── Excluir ───────────────────────────────────────────────────
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Excluir(Guid id)
    {
        var lojaId = await GetLojaId();
        var ag = await db.Agendamentos.FirstOrDefaultAsync(a => a.Id == id && a.LojaId == lojaId);
        if (ag is null) return NotFound();

        db.Agendamentos.Remove(ag);
        await db.SaveChangesAsync();
        return Ok(new { mensagem = "Agendamento excluído." });
    }
}

public record SalvarAgendamentoRequest(
    Guid ServicoId,
    Guid? ClienteId,
    string? NomeCliente,
    decimal Preco,
    DateTime DataHora,
    int DuracaoMin,
    string? Observacao
);

public record StatusAgendamentoRequest(string Status);