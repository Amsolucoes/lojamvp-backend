using LojaApi.Data;
using LojaApi.Models;
using LojaApi.Services;
using LojaApi.src.Models.Funcionarios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

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
    public async Task<IActionResult> Listar([FromQuery] DateTime? data, [FromQuery] DateTime? de, [FromQuery] DateTime? ate)
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return Ok(Array.Empty<object>());

        var q = db.Agendamentos.Where(a => a.LojaId == lojaId);

        if (data.HasValue)
        {
            var dia = DateTime.SpecifyKind(data.Value.Date, DateTimeKind.Unspecified);
            var fim = dia.AddDays(1);
            q = q.Where(a => a.DataHora >= dia && a.DataHora < fim);
        }
        else if (de.HasValue && ate.HasValue)
        {
            var ini = DateTime.SpecifyKind(de.Value.Date, DateTimeKind.Unspecified);
            var fim = DateTime.SpecifyKind(ate.Value.Date, DateTimeKind.Unspecified).AddDays(1);
            q = q.Where(a => a.DataHora >= ini && a.DataHora < fim);
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
                a.Pago,
                a.Observacao,
                a.ProfissionalId,
                NomeProfissional = a.Profissional != null ? a.Profissional.Nome : null,
            })
            .ToListAsync();

        return Ok(lista);
    }

    // GET /api/agendamentos/pendentes-pagamento
    [HttpGet("pendentes-pagamento")]
    public async Task<IActionResult> PendentesPagamento()
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return Ok(Array.Empty<object>());

        var lista = await db.Agendamentos
            .Where(a => a.LojaId == lojaId && a.Status == "concluido" && !a.Pago)
            .OrderByDescending(a => a.DataHora)
            .Select(a => new {
                a.Id,
                a.ServicoId,
                a.NomeServico,
                a.ClienteId,
                a.NomeCliente,
                a.Preco,
                a.DataHora,
            })
            .ToListAsync();

        return Ok(lista);
    }

    // ── Listar agendamentos online pendentes de aprovação ─────────
    [HttpGet("pendentes")]
    public async Task<IActionResult> Pendentes()
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return Ok(Array.Empty<object>());

        var lista = await db.Agendamentos
            .Where(a => a.LojaId == lojaId && a.Status == "pendente")
            .OrderBy(a => a.DataHora)
            .Select(a => new {
                a.Id,
                a.NomeServico,
                a.NomeCliente,
                a.Preco,
                a.DataHora,
                a.DuracaoMin,
                a.Origem,
            })
            .ToListAsync();

        return Ok(lista);
    }

    // ── Regras do plano de assinatura ao agendar ──────────────────
    // Se o cliente tem plano ativo, em dia, que inclui o serviço: o dia da semana precisa ser permitido
    // e o total de usos do mês (já usados + já agendados) não pode passar do limite do plano.
    private async Task<string?> ValidarUsoPlanoAsync(Guid lojaId, Guid? clienteId, Guid servicoId, DateTime dataHora, Guid? ignorarAgendamentoId)
    {
        if (!clienteId.HasValue) return null;

        var assinatura = await db.AssinaturasCliente.FirstOrDefaultAsync(a =>
            a.ClienteId == clienteId.Value && a.LojaId == lojaId && a.Status == "ativa");
        if (assinatura is null) return null;

        var plano = await db.Planos.FindAsync(assinatura.PlanoId);
        if (plano is null) return null;

        var incluidos = (plano.ServicosIds ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries);
        if (!incluidos.Contains(servicoId.ToString(), StringComparer.OrdinalIgnoreCase)) return null;

        // Mensalidade em atraso: o plano não cobre o serviço (ele é cobrado normalmente), então não restringe o agendamento
        var agora = DateTime.UtcNow;
        var mesAtual = new DateTime(agora.Year, agora.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var emAtraso = await db.PagamentosPlano.AnyAsync(p =>
            p.AssinaturaId == assinatura.Id && p.Status == "pendente" &&
            (p.MesReferencia < mesAtual || (p.MesReferencia == mesAtual && agora.Day > assinatura.DiaVencimento)));
        if (emAtraso) return null;

        if (!PlanoUsoRegras.DiaPermitidoLocal(plano.DiasSemanaPermitidos, dataHora))
            return $"O plano '{plano.Nome}' só pode ser usado em: {PlanoUsoRegras.DescreverDias(plano.DiasSemanaPermitidos)}. Escolha outro dia.";

        if (plano.LimiteUsosMes.HasValue)
        {
            var (inicioUtc, fimUtc) = PlanoUsoRegras.IntervaloDoMesUtcDeHorarioLocal(dataHora);
            var usados = await db.ConsumosPlano.CountAsync(c =>
                c.AssinaturaId == assinatura.Id && c.CriadoEm >= inicioUtc && c.CriadoEm < fimUtc);

            var inicioLocal = new DateTime(dataHora.Year, dataHora.Month, 1);
            var fimLocal = inicioLocal.AddMonths(1);
            var ids = incluidos.Select(s => Guid.TryParse(s, out var g) ? g : Guid.Empty).Where(g => g != Guid.Empty).ToList();
            var agendados = await db.Agendamentos.CountAsync(a =>
                a.LojaId == lojaId && a.ClienteId == clienteId.Value && a.Status != "cancelado" && !a.Pago &&
                ids.Contains(a.ServicoId) && a.DataHora >= inicioLocal && a.DataHora < fimLocal &&
                (!ignorarAgendamentoId.HasValue || a.Id != ignorarAgendamentoId.Value));

            if (usados + agendados + 1 > plano.LimiteUsosMes.Value)
                return $"O plano '{plano.Nome}' permite {plano.LimiteUsosMes} uso(s) por mês e já há {usados + agendados} usado(s) ou agendado(s) neste mês.";
        }

        return null;
    }

    // ── Criar ─────────────────────────────────────────────────────
    [HttpPost]
    public async Task<IActionResult> Criar([FromBody] SalvarAgendamentoRequest req)
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return BadRequest(new { erro = "Loja não encontrada." });

        var servico = await db.Servicos.FirstOrDefaultAsync(s => s.Id == req.ServicoId && s.LojaId == lojaId);
        if (servico is null) return BadRequest(new { erro = "Serviço não encontrado." });

        var erroPlano = await ValidarUsoPlanoAsync(lojaId.Value, req.ClienteId, servico.Id, req.DataHora, null);
        if (erroPlano is not null) return BadRequest(new { erro = erroPlano });

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
            ProfissionalId = req.ProfissionalId,
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
            ag.Pago,
            ag.Observacao,
            ag.ProfissionalId,
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

        var erroPlano = await ValidarUsoPlanoAsync(lojaId!.Value, req.ClienteId, servico.Id, req.DataHora, ag.Id);
        if (erroPlano is not null) return BadRequest(new { erro = erroPlano });

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
        ag.ProfissionalId = req.ProfissionalId;
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

        // Não deixa cancelar agendamento já pago (a venda já existe)
        if (req.Status == "cancelado" && ag.Pago)
            return BadRequest(new { erro = "Não é possível cancelar: este agendamento já foi pago. Para estornar, faça o processo pelo caixa/vendas." });

        var statusAnterior = ag.Status;
        ag.Status = req.Status;
        await db.SaveChangesAsync();

        // Gera a comissão do profissional quando o atendimento é concluído
        // (só na primeira vez que vira "concluido", pra não duplicar se reabrir/reconcluir)
        if (req.Status == "concluido" && statusAnterior != "concluido" && ag.ProfissionalId.HasValue)
        {
            var jaTemComissao = await db.ComissoesFuncionario.AnyAsync(c => c.OrigemTipo == "agendamento" && c.OrigemId == ag.Id);
            if (!jaTemComissao)
            {
                var profissional = await db.Profissionais.FindAsync(ag.ProfissionalId.Value);
                if (profissional != null)
                {
                    // Prioridade: comissão específica do serviço > comissão padrão do profissional
                    var comissaoServico = await db.ComissoesServicoProfissional
                        .FirstOrDefaultAsync(c => c.ProfissionalId == profissional.Id && c.ServicoId == ag.ServicoId);

                    var percentual = comissaoServico?.ComissaoPercentual ?? profissional.ComissaoPadraoPercentual;

                    if (percentual.HasValue && percentual.Value > 0)
                    {
                        var valorComissao = Math.Round(ag.Preco * (percentual.Value / 100m), 2);
                        db.ComissoesFuncionario.Add(new ComissaoFuncionario
                        {
                            LojaId = lojaId!.Value,
                            ProfissionalId = profissional.Id,
                            OrigemTipo = "agendamento",
                            OrigemId = ag.Id,
                            AgendamentoId = ag.Id,
                            ValorServico = ag.Preco,
                            ComissaoPercentual = percentual.Value,
                            ValorComissao = valorComissao,
                        });
                        await db.SaveChangesAsync();
                    }
                }
            }
        }

        return Ok(new { ag.Id, ag.Status });
    }

    // ── Excluir ───────────────────────────────────────────────────
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Excluir(Guid id)
    {
        var lojaId = await GetLojaId();
        var ag = await db.Agendamentos.FirstOrDefaultAsync(a => a.Id == id && a.LojaId == lojaId);
        if (ag is null) return NotFound();

        // Impede exclusão se já foi pago (virou venda no caixa)
        if (ag.Pago)
            return BadRequest(new { erro = "Não é possível excluir: este agendamento já foi pago e gerou uma venda. Você pode cancelá-lo em vez de excluir." });

        db.Agendamentos.Remove(ag);
        await db.SaveChangesAsync();
        return Ok(new { mensagem = "Agendamento excluído." });
    }

    // ── Listar agendamentos de um cliente (histórico) ─────────────
    [HttpGet("cliente/{clienteId:guid}")]
    public async Task<IActionResult> PorCliente(Guid clienteId)
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return Ok(Array.Empty<object>());

        var lista = await db.Agendamentos
            .Where(a => a.LojaId == lojaId && a.ClienteId == clienteId)
            .OrderByDescending(a => a.DataHora)
            .Select(a => new {
                a.Id,
                a.NomeServico,
                a.Preco,
                a.DataHora,
                a.Status,
                a.Pago,
            })
            .ToListAsync();

        return Ok(lista);
    }

    // ── Resumo de serviços por cliente (para os cards) ────────────
    [HttpGet("resumo-clientes")]
    public async Task<IActionResult> ResumoClientes()
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return Ok(Array.Empty<object>());

        var resumo = await db.Agendamentos
            .Where(a => a.LojaId == lojaId && a.ClienteId != null && a.Status != "cancelado")
            .GroupBy(a => a.ClienteId!.Value)
            .Select(g => new {
                clienteId = g.Key,
                qtd = g.Count(),
                total = g.Sum(a => a.Preco),
            })
            .ToListAsync();

        return Ok(resumo);
    }
}

public record SalvarAgendamentoRequest(
    Guid ServicoId,
    Guid? ClienteId,
    string? NomeCliente,
    decimal Preco,
    DateTime DataHora,
    int DuracaoMin,
    string? Observacao,
    Guid? ProfissionalId = null
);

public record StatusAgendamentoRequest(string Status);