using LojaApi.Data;
using LojaApi.Domain.Servicos;
using LojaApi.Models;
using LojaApi.Shared.Application;
using LojaApi.src.Models.Funcionarios;
using Microsoft.EntityFrameworkCore;

namespace LojaApi.Application.Servicos;

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

// Agenda da loja (painel): criar/editar/listar, mudar status e gerar a comissão do profissional.
public class AgendamentosService(AppDbContext db)
{
    // ── Consultas ────────────────────────────────────────────────────────────

    // GET /api/agendamentos?data=2026-06-30  ou  ?de=...&ate=...
    public async Task<Resultado<object>> ListarAsync(Guid? lojaId, DateTime? data, DateTime? de, DateTime? ate)
    {
        if (lojaId is null) return Resultado.Ok(Array.Empty<object>());

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
            .Select(a => new
            {
                a.Id, a.ServicoId, a.NomeServico, a.ClienteId, a.NomeCliente, a.Preco, a.DataHora, a.DuracaoMin,
                a.Status, a.Pago, a.Observacao, a.ProfissionalId,
                NomeProfissional = a.Profissional != null ? a.Profissional.Nome : null,
            })
            .ToListAsync();

        return Resultado.Ok(lista);
    }

    // Concluídos que ainda não foram cobrados no caixa
    public async Task<Resultado<object>> PendentesPagamentoAsync(Guid? lojaId)
    {
        if (lojaId is null) return Resultado.Ok(Array.Empty<object>());

        var lista = await db.Agendamentos
            .Where(a => a.LojaId == lojaId && a.Status == StatusAgendamento.Concluido && !a.Pago)
            .OrderByDescending(a => a.DataHora)
            .Select(a => new { a.Id, a.ServicoId, a.NomeServico, a.ClienteId, a.NomeCliente, a.Preco, a.DataHora })
            .ToListAsync();

        return Resultado.Ok(lista);
    }

    // Pedidos online esperando aprovação
    public async Task<Resultado<object>> PendentesAsync(Guid? lojaId)
    {
        if (lojaId is null) return Resultado.Ok(Array.Empty<object>());

        var lista = await db.Agendamentos
            .Where(a => a.LojaId == lojaId && a.Status == StatusAgendamento.Pendente)
            .OrderBy(a => a.DataHora)
            .Select(a => new { a.Id, a.NomeServico, a.NomeCliente, a.Preco, a.DataHora, a.DuracaoMin, a.Origem })
            .ToListAsync();

        return Resultado.Ok(lista);
    }

    public async Task<Resultado<object>> PorClienteAsync(Guid? lojaId, Guid clienteId)
    {
        if (lojaId is null) return Resultado.Ok(Array.Empty<object>());

        var lista = await db.Agendamentos
            .Where(a => a.LojaId == lojaId && a.ClienteId == clienteId)
            .OrderByDescending(a => a.DataHora)
            .Select(a => new { a.Id, a.NomeServico, a.Preco, a.DataHora, a.Status, a.Pago })
            .ToListAsync();

        return Resultado.Ok(lista);
    }

    // Quantos serviços e quanto cada cliente já gastou (cancelados ficam de fora)
    public async Task<Resultado<object>> ResumoClientesAsync(Guid? lojaId)
    {
        if (lojaId is null) return Resultado.Ok(Array.Empty<object>());

        var resumo = await db.Agendamentos
            .Where(a => a.LojaId == lojaId && a.ClienteId != null && a.Status != StatusAgendamento.Cancelado)
            .GroupBy(a => a.ClienteId!.Value)
            .Select(g => new { clienteId = g.Key, qtd = g.Count(), total = g.Sum(a => a.Preco) })
            .ToListAsync();

        return Resultado.Ok(resumo);
    }

    // ── Criar / editar ───────────────────────────────────────────────────────

    public async Task<Resultado<object>> CriarAsync(Guid? lojaId, SalvarAgendamentoRequest req)
    {
        if (lojaId is null) return Resultado.Invalido("Loja não encontrada.");

        var preparo = await PrepararAsync(lojaId.Value, req, ignorarAgendamentoId: null);
        if (preparo.Erro is not null) return Resultado.Invalido(preparo.Erro);

        var servico = preparo.Servico!;
        var ag = new Agendamento
        {
            LojaId = lojaId.Value,
            ServicoId = servico.Id,
            NomeServico = servico.Nome,
            ClienteId = req.ClienteId,
            NomeCliente = preparo.NomeCliente,
            Preco = AgendamentoRegras.PrecoFinal(req.Preco, servico.Preco),
            DataHora = req.DataHora,
            DuracaoMin = AgendamentoRegras.DuracaoFinal(req.DuracaoMin, servico.DuracaoMin),
            Status = StatusAgendamento.Agendado,
            Observacao = req.Observacao,
            ProfissionalId = req.ProfissionalId,
        };
        db.Agendamentos.Add(ag);
        await db.SaveChangesAsync();

        return Resultado.Ok(new
        {
            ag.Id, ag.ServicoId, ag.NomeServico, ag.ClienteId, ag.NomeCliente, ag.Preco, ag.DataHora, ag.DuracaoMin,
            ag.Status, ag.Pago, ag.Observacao, ag.ProfissionalId,
        });
    }

    public async Task<Resultado<object>> AtualizarAsync(Guid? lojaId, Guid id, SalvarAgendamentoRequest req)
    {
        var ag = await db.Agendamentos.FirstOrDefaultAsync(a => a.Id == id && a.LojaId == lojaId);
        if (ag is null) return Resultado.NaoEncontrado();

        var preparo = await PrepararAsync(lojaId!.Value, req, ignorarAgendamentoId: ag.Id);
        if (preparo.Erro is not null) return Resultado.Invalido(preparo.Erro);

        var servico = preparo.Servico!;
        ag.ServicoId = servico.Id;
        ag.NomeServico = servico.Nome;
        ag.ClienteId = req.ClienteId;
        ag.NomeCliente = preparo.NomeCliente;
        ag.Preco = AgendamentoRegras.PrecoFinal(req.Preco, servico.Preco);
        ag.DataHora = req.DataHora;
        ag.DuracaoMin = AgendamentoRegras.DuracaoFinal(req.DuracaoMin, servico.DuracaoMin);
        ag.Observacao = req.Observacao;
        ag.ProfissionalId = req.ProfissionalId;
        await db.SaveChangesAsync();

        return Resultado.Ok(new { ag.Id, ag.ServicoId, ag.NomeServico, ag.ClienteId, ag.NomeCliente, ag.Preco, ag.DataHora, ag.DuracaoMin, ag.Status, ag.Observacao });
    }

    private record Preparo(string? Erro, Servico? Servico = null, string? NomeCliente = null);

    // Confere tudo o que o pedido referencia (serviço, cliente, profissional — sempre da MESMA loja) e as regras do plano.
    private async Task<Preparo> PrepararAsync(Guid lojaId, SalvarAgendamentoRequest req, Guid? ignorarAgendamentoId)
    {
        var servico = await db.Servicos.FirstOrDefaultAsync(s => s.Id == req.ServicoId && s.LojaId == lojaId);
        if (servico is null) return new("Serviço não encontrado.");

        // Cliente e profissional de outra loja não podem ser vinculados (e o nome do cliente não pode vazar)
        Cliente? cliente = null;
        if (req.ClienteId.HasValue)
        {
            cliente = await db.Clientes.FirstOrDefaultAsync(c => c.Id == req.ClienteId.Value && c.LojaId == lojaId);
            if (cliente is null) return new("Cliente não encontrado.");
        }
        if (req.ProfissionalId.HasValue &&
            !await db.Profissionais.AnyAsync(p => p.Id == req.ProfissionalId.Value && p.LojaId == lojaId))
            return new("Profissional não encontrado.");

        var erroPlano = await ValidarUsoPlanoAsync(lojaId, req.ClienteId, servico.Id, req.DataHora, ignorarAgendamentoId);
        if (erroPlano is not null) return new(erroPlano);

        // Nome do cliente: o do cadastro, ou o digitado (avulso)
        return new(null, servico, cliente?.Nome ?? req.NomeCliente);
    }

    // ── Regras do plano de assinatura ao agendar ─────────────────────────────
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

        if (!PlanoUsoRegras.Inclui(plano.ServicosIds, servicoId)) return null;

        // Mensalidade em atraso: o plano não cobre o serviço (ele é cobrado normalmente), então não restringe o agendamento
        var agora = DateTime.UtcNow;
        var mesAtual = PlanoUsoRegras.PrimeiroDiaDoMesUtc(agora);
        var pendentes = await db.PagamentosPlano
            .Where(p => p.AssinaturaId == assinatura.Id && p.Status == MensalidadeRegras.Pendente)
            .Select(p => p.MesReferencia)
            .ToListAsync();
        if (pendentes.Any(mes => MensalidadeRegras.EmAtraso(MensalidadeRegras.Pendente, mes, mesAtual, assinatura.DiaVencimento, agora)))
            return null;

        var erroDia = PlanoUsoRegras.ValidarDia(plano.Nome, plano.DiasSemanaPermitidos, dataHora);
        if (erroDia is not null) return erroDia;

        if (plano.LimiteUsosMes.HasValue)
        {
            var (inicioUtc, fimUtc) = PlanoUsoRegras.IntervaloDoMesUtcDeHorarioLocal(dataHora);
            var usados = await db.ConsumosPlano.CountAsync(c =>
                c.AssinaturaId == assinatura.Id && c.CriadoEm >= inicioUtc && c.CriadoEm < fimUtc);

            var inicioLocal = new DateTime(dataHora.Year, dataHora.Month, 1);
            var fimLocal = inicioLocal.AddMonths(1);
            var ids = PlanoUsoRegras.IdsDosServicos(plano.ServicosIds);
            var agendados = await db.Agendamentos.CountAsync(a =>
                a.LojaId == lojaId && a.ClienteId == clienteId.Value && a.Status != StatusAgendamento.Cancelado && !a.Pago &&
                ids.Contains(a.ServicoId) && a.DataHora >= inicioLocal && a.DataHora < fimLocal &&
                (!ignorarAgendamentoId.HasValue || a.Id != ignorarAgendamentoId.Value));

            return PlanoUsoRegras.ValidarLimite(plano.Nome, plano.LimiteUsosMes.Value, usados, agendados);
        }

        return null;
    }

    // ── Status ───────────────────────────────────────────────────────────────

    // Concluir / cancelar / reabrir. Concluir pela primeira vez gera a comissão do profissional.
    public async Task<Resultado<object>> MudarStatusAsync(Guid? lojaId, Guid id, StatusAgendamentoRequest req)
    {
        var ag = await db.Agendamentos.FirstOrDefaultAsync(a => a.Id == id && a.LojaId == lojaId);
        if (ag is null) return Resultado.NaoEncontrado();

        if (!AgendamentoRegras.StatusValido(req.Status)) return Resultado.Invalido("Status inválido.");

        var erroCancelamento = AgendamentoRegras.ValidarCancelamento(req.Status, ag.Pago);
        if (erroCancelamento is not null) return Resultado.Invalido(erroCancelamento);

        var statusAnterior = ag.Status;
        ag.Status = req.Status;
        await db.SaveChangesAsync();

        if (AgendamentoRegras.GeraComissao(req.Status, statusAnterior, ag.ProfissionalId.HasValue))
            await GerarComissaoAsync(lojaId!.Value, ag);

        return Resultado.Ok(new { ag.Id, ag.Status });
    }

    // Prioridade: comissão específica do serviço > comissão padrão do profissional. Uma comissão por atendimento.
    private async Task GerarComissaoAsync(Guid lojaId, Agendamento ag)
    {
        var jaTemComissao = await db.ComissoesFuncionario.AnyAsync(c => c.OrigemTipo == "agendamento" && c.OrigemId == ag.Id);
        if (jaTemComissao) return;

        var profissional = await db.Profissionais.FirstOrDefaultAsync(p => p.Id == ag.ProfissionalId!.Value && p.LojaId == lojaId);
        if (profissional is null) return;

        var comissaoServico = await db.ComissoesServicoProfissional
            .FirstOrDefaultAsync(c => c.ProfissionalId == profissional.Id && c.ServicoId == ag.ServicoId);
        var percentual = comissaoServico?.ComissaoPercentual ?? profissional.ComissaoPadraoPercentual;

        var valorComissao = AgendamentoRegras.ValorComissao(ag.Preco, percentual);
        if (valorComissao is null) return;

        db.ComissoesFuncionario.Add(new ComissaoFuncionario
        {
            LojaId = lojaId,
            ProfissionalId = profissional.Id,
            OrigemTipo = "agendamento",
            OrigemId = ag.Id,
            AgendamentoId = ag.Id,
            ValorServico = ag.Preco,
            ComissaoPercentual = percentual!.Value,
            ValorComissao = valorComissao.Value,
        });
        await db.SaveChangesAsync();
    }

    public async Task<Resultado<object>> ExcluirAsync(Guid? lojaId, Guid id)
    {
        var ag = await db.Agendamentos.FirstOrDefaultAsync(a => a.Id == id && a.LojaId == lojaId);
        if (ag is null) return Resultado.NaoEncontrado();

        var erro = AgendamentoRegras.ValidarExclusao(ag.Pago);
        if (erro is not null) return Resultado.Invalido(erro);

        db.Agendamentos.Remove(ag);
        await db.SaveChangesAsync();
        return Resultado.Ok(new { mensagem = "Agendamento excluído." });
    }
}
