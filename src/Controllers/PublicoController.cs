using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LojaApi.Data;
using LojaApi.Models;

namespace LojaApi.Controllers;

[ApiController]
[Route("api/publico")]
[AllowAnonymous]
public class PublicoController(AppDbContext db) : ControllerBase
{
    // ── Dados da loja + serviços (pelo slug) ──────────────────────
    [HttpGet("{slug}")]
    public async Task<IActionResult> DadosLoja(string slug)
    {
        var loja = await db.Lojas.FirstOrDefaultAsync(l => l.Slug == slug);
        if (loja is null || !loja.AgendamentoOnlineAtivo)
            return NotFound(new { erro = "Página de agendamento não encontrada." });

        var pausado = loja.PausaAte.HasValue && loja.PausaAte.Value > DateTime.UtcNow;

        var servicos = pausado
            ? new List<object>()
            : await db.Servicos
                .Where(s => s.LojaId == loja.Id && s.Ativo)
                .OrderBy(s => s.Categoria).ThenBy(s => s.Nome)
                .Select(s => (object)new { s.Id, s.Nome, s.Categoria, s.Preco, s.DuracaoMin })
                .ToListAsync();

        return Ok(new
        {
            nome = loja.Nome,
            logoUrl = loja.LogoUrl,
            corPrimaria = loja.CorPrimaria,
            confirmacao = string.IsNullOrEmpty(loja.AgendamentoOnlineConfirmacao) ? "aprovacao" : loja.AgendamentoOnlineConfirmacao,
            servicos,
            pausado,
            pausaAte = pausado ? loja.PausaAte : null,
            pausaMensagem = pausado ? (loja.PausaMensagem ?? "Estamos temporariamente fechados. Voltamos em breve!") : null,
        });
    }

    // ── Horários livres de um dia para um serviço ─────────────────
    [HttpGet("{slug}/horarios")]
    public async Task<IActionResult> Horarios(string slug, [FromQuery] DateTime data, [FromQuery] Guid servicoId)
    {
        var loja = await db.Lojas.FirstOrDefaultAsync(l => l.Slug == slug);
        if (loja is null || !loja.AgendamentoOnlineAtivo)
            return NotFound(new { erro = "Página não encontrada." });

        if (loja.PausaAte.HasValue && loja.PausaAte.Value > DateTime.UtcNow)
            return BadRequest(new { erro = loja.PausaMensagem ?? "Agendamentos temporariamente pausados." });

        var servico = await db.Servicos.FirstOrDefaultAsync(s => s.Id == servicoId && s.LojaId == loja.Id);
        if (servico is null) return BadRequest(new { erro = "Serviço não encontrado." });

        var dia = DateTime.SpecifyKind(data.Date, DateTimeKind.Unspecified);
        var fim = dia.AddDays(1);

        // Agendamentos que ocupam horário nesse dia (agendado, concluido, pendente — não os cancelados)
        var ocupados = await db.Agendamentos
            .Where(a => a.LojaId == loja.Id
                && a.DataHora >= dia && a.DataHora < fim
                && a.Status != "cancelado")
            .Select(a => new { a.DataHora, a.DuracaoMin })
            .ToListAsync();

        // Gera slots de 30min dentro da faixa da loja
        var horaInicio = loja.AgendaHoraInicio;
        var horaFim = loja.AgendaHoraFim;
        var duracao = servico.DuracaoMin > 0 ? servico.DuracaoMin : 30;

        var slots = new List<string>();
        var agora = DateTime.UtcNow.AddHours(-3); // aprox. horário Brasília p/ não oferecer horário passado

        for (int h = horaInicio; h < horaFim; h++)
        {
            foreach (var m in new[] { 0, 30 })
            {
                var inicioSlot = dia.AddHours(h).AddMinutes(m);
                var fimSlot = inicioSlot.AddMinutes(duracao);

                // Não pode passar da faixa de funcionamento
                if (fimSlot > dia.AddHours(horaFim)) continue;

                // Não oferecer horários no passado (se for hoje)
                if (inicioSlot <= agora) continue;

                // Verifica conflito com algum agendamento ocupado
                bool conflita = ocupados.Any(o =>
                {
                    var oInicio = o.DataHora;
                    var oFim = o.DataHora.AddMinutes(o.DuracaoMin);
                    return inicioSlot < oFim && fimSlot > oInicio; // sobreposição
                });

                if (!conflita)
                    slots.Add(inicioSlot.ToString("HH:mm"));
            }
        }

        return Ok(new { horarios = slots });
    }

    // ── Criar agendamento (cliente final) ─────────────────────────
    [HttpPost("{slug}/agendar")]
    public async Task<IActionResult> Agendar(string slug, [FromBody] AgendarPublicoRequest req)
    {
        var loja = await db.Lojas.FirstOrDefaultAsync(l => l.Slug == slug);
        if (loja is null || !loja.AgendamentoOnlineAtivo)
            return NotFound(new { erro = "Página não encontrada." });

        if (loja.PausaAte.HasValue && loja.PausaAte.Value > DateTime.UtcNow)
            return BadRequest(new { erro = loja.PausaMensagem ?? "Agendamentos temporariamente pausados." });

        if (string.IsNullOrWhiteSpace(req.NomeCliente) || string.IsNullOrWhiteSpace(req.Telefone))
            return BadRequest(new { erro = "Informe nome e telefone." });

        var servico = await db.Servicos.FirstOrDefaultAsync(s => s.Id == req.ServicoId && s.LojaId == loja.Id);
        if (servico is null) return BadRequest(new { erro = "Serviço não encontrado." });

        // Cria ou reusa cliente pelo telefone
        var telefone = new string(req.Telefone.Where(char.IsDigit).ToArray());

        var clientesLoja = await db.Clientes
            .Where(c => c.LojaId == loja.Id)
            .ToListAsync();

        var cliente = clientesLoja.FirstOrDefault(c =>
            new string((c.Telefone ?? "").Where(char.IsDigit).ToArray()) == telefone);

        if (cliente is null)
        {
            cliente = new Cliente
            {
                LojaId = loja.Id,
                Nome = req.NomeCliente.Trim(),
                Telefone = telefone,  // sempre só dígitos
            };
            db.Clientes.Add(cliente);
        }

        // Monta a data/hora (sem UTC — hora local, padrão da agenda)
        var dataHora = DateTime.SpecifyKind(req.DataHora, DateTimeKind.Unspecified);

        // Revalida que o horário ainda está livre (evita corrida)
        var fimNovo = dataHora.AddMinutes(servico.DuracaoMin);
        var ocupados = await db.Agendamentos
            .Where(a => a.LojaId == loja.Id && a.Status != "cancelado"
                && a.DataHora >= dataHora.Date && a.DataHora < dataHora.Date.AddDays(1))
            .Select(a => new { a.DataHora, a.DuracaoMin })
            .ToListAsync();

        bool conflita = ocupados.Any(o =>
            dataHora < o.DataHora.AddMinutes(o.DuracaoMin) && fimNovo > o.DataHora);

        if (conflita)
            return Conflict(new { erro = "Este horário acabou de ser reservado. Escolha outro." });

        // Status conforme config da loja
        var confirmacao = string.IsNullOrEmpty(loja.AgendamentoOnlineConfirmacao) ? "aprovacao" : loja.AgendamentoOnlineConfirmacao;
        var status = confirmacao == "automatico" ? "agendado" : "pendente";

        var ag = new Agendamento
        {
            LojaId = loja.Id,
            ServicoId = servico.Id,
            NomeServico = servico.Nome,
            ClienteId = cliente.Id,
            NomeCliente = cliente.Nome,
            Preco = servico.Preco,
            DataHora = dataHora,
            DuracaoMin = servico.DuracaoMin,
            Status = status,
            Origem = "online",
        };
        db.Agendamentos.Add(ag);
        await db.SaveChangesAsync();

        return Ok(new
        {
            mensagem = status == "pendente"
                ? "Solicitação enviada! A loja vai confirmar seu horário em breve."
                : "Agendamento confirmado!",
            status,
        });
    }

    // ── Verificar se cliente já existe (pelo telefone) ────────────
    [HttpGet("{slug}/cliente")]
    public async Task<IActionResult> VerificarCliente(string slug, [FromQuery] string telefone)
    {
        var loja = await db.Lojas.FirstOrDefaultAsync(l => l.Slug == slug);
        if (loja is null || !loja.AgendamentoOnlineAtivo)
            return NotFound();

        var tel = new string((telefone ?? "").Where(char.IsDigit).ToArray());
        if (tel.Length < 8) return Ok(new { existe = false });

        var clientesDaLoja = await db.Clientes
            .Where(c => c.LojaId == loja.Id && c.Telefone != null)
            .Select(c => new { c.Nome, c.Telefone })
            .ToListAsync();

        var cliente = clientesDaLoja.FirstOrDefault(c =>
            new string((c.Telefone ?? "").Where(char.IsDigit).ToArray()) == tel);

        if (cliente is null) return Ok(new { existe = false });

        // Retorna só o primeiro nome (privacidade)
        var primeiroNome = cliente.Nome.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";

        return Ok(new { existe = true, primeiroNome });
    }
}

public record AgendarPublicoRequest(
    Guid ServicoId,
    string NomeCliente,
    string Telefone,
    DateTime DataHora
);