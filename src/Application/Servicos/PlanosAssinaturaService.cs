using LojaApi.Data;
using LojaApi.Domain.Servicos;
using LojaApi.Models;
using LojaApi.Shared.Application;
using Microsoft.EntityFrameworkCore;

namespace LojaApi.Application.Servicos;

public record SalvarPlanoRequest(string Nome, decimal Valor, string? ServicosIds, int? LimiteUsosMes = null, List<int>? DiasSemana = null);
public record VincularPlanoRequest(Guid ClienteId, Guid PlanoId, int DiaVencimento, bool IniciarProximoMes = false);
public record AtualizarAssinaturaRequest(Guid? PlanoId, int? DiaVencimento);
public record MarcarPagamentoPlanoRequest(bool Pago);
public record PagarMesItem(Guid PagamentoId, decimal? ValorPago);
public record PagarMesesRequest(List<PagarMesItem> Meses);

// Planos de assinatura (mensalidade com serviços inclusos) e os clientes assinantes.
// Pagar uma mensalidade gera uma venda no caixa (Pix) com o valor realmente recebido.
public class PlanosAssinaturaService(AppDbContext db)
{
    private static object RespostaPlano(Plano p) => new { p.Id, p.Nome, p.Valor, p.ServicosIds, p.Ativo, p.LimiteUsosMes, p.DiasSemanaPermitidos };

    // ── Planos ───────────────────────────────────────────────────────────────

    public async Task<Resultado<object>> ListarAsync(Guid? lojaId)
    {
        if (lojaId is null) return Resultado.Ok(Array.Empty<object>());

        var planos = await db.Planos
            .Where(p => p.LojaId == lojaId)
            .OrderByDescending(p => p.Ativo).ThenBy(p => p.Nome)
            .Select(p => new { p.Id, p.Nome, p.Valor, p.ServicosIds, p.Ativo, p.LimiteUsosMes, p.DiasSemanaPermitidos })
            .ToListAsync();

        return Resultado.Ok(planos);
    }

    public async Task<Resultado<object>> CriarAsync(Guid? lojaId, SalvarPlanoRequest req)
    {
        if (lojaId is null) return Resultado.Invalido("Loja não encontrada.");
        if (string.IsNullOrWhiteSpace(req.Nome) || req.Valor <= 0) return Resultado.Invalido("Informe nome e valor válidos.");

        var plano = new Plano
        {
            LojaId = lojaId.Value,
            Nome = req.Nome.Trim(),
            Valor = req.Valor,
            ServicosIds = req.ServicosIds,
            LimiteUsosMes = req.LimiteUsosMes is > 0 ? req.LimiteUsosMes : null,
            DiasSemanaPermitidos = PlanoUsoRegras.NormalizarDias(req.DiasSemana),
            Ativo = true,
        };
        db.Planos.Add(plano);
        await db.SaveChangesAsync();

        return Resultado.Ok(RespostaPlano(plano));
    }

    public async Task<Resultado<object>> AtualizarAsync(Guid? lojaId, Guid id, SalvarPlanoRequest req)
    {
        var plano = await db.Planos.FirstOrDefaultAsync(p => p.Id == id && p.LojaId == lojaId);
        if (plano is null) return Resultado.NaoEncontrado();

        plano.Nome = req.Nome.Trim();
        plano.Valor = req.Valor;
        plano.ServicosIds = req.ServicosIds;
        plano.LimiteUsosMes = req.LimiteUsosMes is > 0 ? req.LimiteUsosMes : null;
        plano.DiasSemanaPermitidos = PlanoUsoRegras.NormalizarDias(req.DiasSemana);
        await db.SaveChangesAsync();

        return Resultado.Ok(RespostaPlano(plano));
    }

    public async Task<Resultado<object>> AlternarAtivoAsync(Guid? lojaId, Guid id)
    {
        var plano = await db.Planos.FirstOrDefaultAsync(p => p.Id == id && p.LojaId == lojaId);
        if (plano is null) return Resultado.NaoEncontrado();

        plano.Ativo = !plano.Ativo;
        await db.SaveChangesAsync();
        return Resultado.Ok(new { plano.Id, plano.Ativo });
    }

    public async Task<Resultado<object>> ExcluirAsync(Guid? lojaId, Guid id)
    {
        var plano = await db.Planos.FirstOrDefaultAsync(p => p.Id == id && p.LojaId == lojaId);
        if (plano is null) return Resultado.NaoEncontrado();

        // Não deixa excluir se tem clientes vinculados (ativos)
        var temAssinantes = await db.AssinaturasCliente.AnyAsync(a => a.PlanoId == id && a.Status == "ativa");
        if (temAssinantes) return Resultado.Invalido("Este plano tem clientes ativos. Desative-o em vez de excluir.");

        db.Planos.Remove(plano);
        await db.SaveChangesAsync();
        return Resultado.Ok(new { mensagem = "Plano excluído." });
    }

    // ── Assinaturas (cliente vinculado a um plano) ───────────────────────────

    // Clientes com plano ativo: mensalidade do mês, meses em atraso e usos
    public async Task<Resultado<object>> AssinantesAsync(Guid? lojaId)
    {
        if (lojaId is null) return Resultado.Ok(Array.Empty<object>());

        var assinaturas = await db.AssinaturasCliente
            .Where(a => a.LojaId == lojaId && a.Status == "ativa")
            .ToListAsync();

        var clienteIds = assinaturas.Select(a => a.ClienteId).ToList();
        var planoIds = assinaturas.Select(a => a.PlanoId).ToList();
        var assinaturaIds = assinaturas.Select(a => a.Id).ToList();

        var clientes = await db.Clientes
            .Where(c => clienteIds.Contains(c.Id))
            .Select(c => new { c.Id, c.Nome, c.Telefone })
            .ToListAsync();

        var planos = await db.Planos
            .Where(p => planoIds.Contains(p.Id))
            .Select(p => new { p.Id, p.Nome, p.Valor, p.LimiteUsosMes, p.DiasSemanaPermitidos })
            .ToListAsync();

        var agora = DateTime.UtcNow;
        var (inicioMesUsoUtc, fimMesUsoUtc) = PlanoUsoRegras.IntervaloDoMesUtc(agora);
        var usosNoMesPorAssinatura = await db.ConsumosPlano
            .Where(c => assinaturaIds.Contains(c.AssinaturaId) && c.CriadoEm >= inicioMesUsoUtc && c.CriadoEm < fimMesUsoUtc)
            .GroupBy(c => c.AssinaturaId)
            .Select(g => new { AssinaturaId = g.Key, Total = g.Count() })
            .ToListAsync();
        var mesAtual = PlanoUsoRegras.PrimeiroDiaDoMesUtc(agora);

        // Todos os pagamentos dessas assinaturas (não só o mês atual)
        var todosPagamentos = await db.PagamentosPlano
            .Where(pg => assinaturaIds.Contains(pg.AssinaturaId))
            .ToListAsync();

        var contagemConsumos = await db.ConsumosPlano
            .Where(c => assinaturaIds.Contains(c.AssinaturaId))
            .GroupBy(c => c.AssinaturaId)
            .Select(g => new { AssinaturaId = g.Key, Total = g.Count() })
            .ToListAsync();

        var resultado = assinaturas.Select(a =>
        {
            var cli = clientes.FirstOrDefault(c => c.Id == a.ClienteId);
            var pl = planos.FirstOrDefault(p => p.Id == a.PlanoId);
            var doAssinante = todosPagamentos.Where(pg => pg.AssinaturaId == a.Id).ToList();
            var pgMes = doAssinante.FirstOrDefault(pg => pg.MesReferencia == mesAtual);
            var pendentes = doAssinante.Where(pg => MensalidadeRegras.EmAtraso(pg.Status, pg.MesReferencia, mesAtual, a.DiaVencimento, agora)).ToList();

            return new
            {
                assinaturaId = a.Id,
                clienteId = a.ClienteId,
                clienteNome = cli?.Nome ?? "(cliente removido)",
                clienteTelefone = cli?.Telefone,
                planoId = a.PlanoId,
                planoNome = pl?.Nome ?? "(plano removido)",
                valor = pl?.Valor ?? 0,
                diaVencimento = a.DiaVencimento,
                pagoNoMes = pgMes != null && pgMes.Status == MensalidadeRegras.Pago,
                mesesEmAtraso = pendentes.Count,
                valorTotalAtraso = pendentes.Sum(p => p.Valor),
                mesInicioCobranca = a.MesInicioCobranca,
                aindaNaoIniciou = a.MesInicioCobranca > mesAtual,
                totalConsumos = contagemConsumos.FirstOrDefault(x => x.AssinaturaId == a.Id)?.Total ?? 0,
                usosNoMes = usosNoMesPorAssinatura.FirstOrDefault(x => x.AssinaturaId == a.Id)?.Total ?? 0,
                limiteUsosMes = pl?.LimiteUsosMes,
                diasSemanaPermitidos = pl?.DiasSemanaPermitidos,
            };
        })
        .OrderByDescending(x => x.mesesEmAtraso)
        .ThenBy(x => x.clienteNome)
        .ToList();

        return Resultado.Ok(resultado);
    }

    public async Task<Resultado<object>> VincularClienteAsync(Guid? lojaId, VincularPlanoRequest req)
    {
        if (lojaId is null) return Resultado.Invalido("Loja não encontrada.");

        var plano = await db.Planos.FirstOrDefaultAsync(p => p.Id == req.PlanoId && p.LojaId == lojaId);
        if (plano is null) return Resultado.Invalido("Plano não encontrado.");

        var cliente = await db.Clientes.FirstOrDefaultAsync(c => c.Id == req.ClienteId && c.LojaId == lojaId);
        if (cliente is null) return Resultado.Invalido("Cliente não encontrado.");

        // Se já tem assinatura ativa, não duplica (1 plano por cliente)
        var jaTem = await db.AssinaturasCliente.AnyAsync(a => a.ClienteId == req.ClienteId && a.Status == "ativa");
        if (jaTem) return Resultado.Conflito("Este cliente já tem um plano ativo.");

        var agora = DateTime.UtcNow;
        var mesAtual = PlanoUsoRegras.PrimeiroDiaDoMesUtc(agora);

        var assinatura = new AssinaturaCliente
        {
            LojaId = lojaId.Value,
            ClienteId = req.ClienteId,
            PlanoId = req.PlanoId,
            DiaVencimento = req.DiaVencimento is >= 1 and <= 28 ? req.DiaVencimento : 10,
            DataInicio = agora,
            MesInicioCobranca = req.IniciarProximoMes ? mesAtual.AddMonths(1) : mesAtual,
            Status = "ativa",
        };
        db.AssinaturasCliente.Add(assinatura);
        await db.SaveChangesAsync();

        return Resultado.Ok(new { assinatura.Id });
    }

    // Trocar de plano ou de dia de vencimento
    public async Task<Resultado<object>> AtualizarAssinaturaAsync(Guid? lojaId, Guid id, AtualizarAssinaturaRequest req)
    {
        var assinatura = await db.AssinaturasCliente
            .FirstOrDefaultAsync(a => a.Id == id && a.LojaId == lojaId && a.Status == "ativa");
        if (assinatura is null) return Resultado.NaoEncontrado();

        if (req.PlanoId.HasValue)
        {
            var novoPlano = await db.Planos.FirstOrDefaultAsync(p => p.Id == req.PlanoId.Value && p.LojaId == lojaId);
            if (novoPlano is null) return Resultado.Invalido("Plano não encontrado.");
            assinatura.PlanoId = req.PlanoId.Value;
        }

        if (req.DiaVencimento is >= 1 and <= 28)
            assinatura.DiaVencimento = req.DiaVencimento.Value;

        await db.SaveChangesAsync();
        return Resultado.Ok(new { assinatura.Id, assinatura.PlanoId, assinatura.DiaVencimento });
    }

    public async Task<Resultado<object>> CancelarAssinaturaAsync(Guid? lojaId, Guid id)
    {
        var a = await db.AssinaturasCliente.FirstOrDefaultAsync(x => x.Id == id && x.LojaId == lojaId);
        if (a is null) return Resultado.NaoEncontrado();

        a.Status = "cancelada";
        await db.SaveChangesAsync();
        return Resultado.Ok(new { a.Id, a.Status });
    }

    // ── Mensalidades ─────────────────────────────────────────────────────────

    // Quitar todos os meses pendentes de uma assinatura (cada um vira uma venda do valor do plano)
    public async Task<Resultado<object>> QuitarAtrasoAsync(Guid? lojaId, Guid id)
    {
        var assinatura = await db.AssinaturasCliente.FirstOrDefaultAsync(a => a.Id == id && a.LojaId == lojaId);
        if (assinatura is null) return Resultado.NaoEncontrado();

        var plano = await db.Planos.FindAsync(assinatura.PlanoId);
        var valor = plano?.Valor ?? 0;
        var nomePlano = plano?.Nome ?? "Plano";
        var agora = DateTime.UtcNow;

        var pendentes = await db.PagamentosPlano
            .Where(p => p.AssinaturaId == id && p.Status == MensalidadeRegras.Pendente)
            .ToListAsync();

        if (pendentes.Count == 0) return Resultado.Invalido("Não há meses pendentes para essa assinatura.");

        foreach (var pg in pendentes)
        {
            pg.Status = MensalidadeRegras.Pago;
            pg.PagoEm = agora;
            CriarVendaMensalidade(pg, lojaId!.Value, assinatura.ClienteId, nomePlano, valor);
        }

        await db.SaveChangesAsync();
        return Resultado.Ok(new { quitados = pendentes.Count, valorTotal = pendentes.Sum(p => p.Valor) });
    }

    // Pagar os meses escolhidos, com valor pago editável (ex.: juros/multa ou desconto)
    public async Task<Resultado<object>> PagarMesesAsync(Guid? lojaId, Guid id, PagarMesesRequest req)
    {
        var assinatura = await db.AssinaturasCliente.FirstOrDefaultAsync(a => a.Id == id && a.LojaId == lojaId);
        if (assinatura is null) return Resultado.NaoEncontrado();

        if (req.Meses is null || req.Meses.Count == 0) return Resultado.Invalido("Selecione ao menos um mês para pagar.");

        var plano = await db.Planos.FindAsync(assinatura.PlanoId);
        var nomePlano = plano?.Nome ?? "Plano";
        var ids = req.Meses.Select(m => m.PagamentoId).Distinct().ToList();
        var pagamentos = await db.PagamentosPlano
            .Where(p => p.AssinaturaId == id && ids.Contains(p.Id))
            .ToListAsync();

        if (pagamentos.Count != ids.Count) return Resultado.Invalido("Algum mês selecionado não foi encontrado.");
        if (pagamentos.Any(p => p.Status == MensalidadeRegras.Pago)) return Resultado.Invalido("Algum mês selecionado já está pago.");

        var agora = DateTime.UtcNow;
        decimal totalRecebido = 0;
        foreach (var pg in pagamentos)
        {
            var item = req.Meses.First(m => m.PagamentoId == pg.Id);
            var valorPago = item.ValorPago ?? pg.Valor;
            if (valorPago <= 0) return Resultado.Invalido("Informe um valor pago maior que zero.");

            pg.Status = MensalidadeRegras.Pago;
            pg.PagoEm = agora;
            pg.ValorPago = valorPago;
            totalRecebido += valorPago;
            CriarVendaMensalidade(pg, lojaId!.Value, assinatura.ClienteId, nomePlano, pg.Valor, valorPago);
        }

        await db.SaveChangesAsync();
        return Resultado.Ok(new { pagos = pagamentos.Count, valorTotal = totalRecebido });
    }

    // Marcar a mensalidade do mês como paga/pendente (manual)
    public async Task<Resultado<object>> MarcarPagamentoAsync(Guid? lojaId, Guid id, MarcarPagamentoPlanoRequest req)
    {
        var assinatura = await db.AssinaturasCliente.FirstOrDefaultAsync(a => a.Id == id && a.LojaId == lojaId);
        if (assinatura is null) return Resultado.NaoEncontrado();

        var plano = await db.Planos.FindAsync(assinatura.PlanoId);
        var valor = plano?.Valor ?? 0;
        var nomePlano = plano?.Nome ?? "Plano";

        var agora = DateTime.UtcNow;
        var mesRef = PlanoUsoRegras.PrimeiroDiaDoMesUtc(agora);

        var pg = await db.PagamentosPlano.FirstOrDefaultAsync(p => p.AssinaturaId == id && p.MesReferencia == mesRef);

        if (pg is null)
        {
            pg = new PagamentoPlano
            {
                AssinaturaId = id,
                LojaId = lojaId!.Value,
                MesReferencia = mesRef,
                Valor = valor,
                Status = req.Pago ? MensalidadeRegras.Pago : MensalidadeRegras.Pendente,
                PagoEm = req.Pago ? agora : null,
            };
            db.PagamentosPlano.Add(pg);
        }
        else
        {
            pg.Status = req.Pago ? MensalidadeRegras.Pago : MensalidadeRegras.Pendente;
            pg.PagoEm = req.Pago ? agora : null;
        }

        if (req.Pago)
            CriarVendaMensalidade(pg, lojaId!.Value, assinatura.ClienteId, nomePlano, valor);
        else
            await RemoverVendaAsync(pg);

        await db.SaveChangesAsync();
        return Resultado.Ok(new { pg.Id, pg.Status });
    }

    // ── Plano de um cliente (usado no Caixa e na Agenda) ─────────────────────

    public async Task<Resultado<object>> AssinaturaDoClienteAsync(Guid? lojaId, Guid clienteId, DateTime? data)
    {
        var assinatura = await db.AssinaturasCliente
            .FirstOrDefaultAsync(a => a.ClienteId == clienteId && a.LojaId == lojaId && a.Status == "ativa");
        if (assinatura is null) return Resultado.Ok(new { temPlano = false });

        var plano = await db.Planos.FindAsync(assinatura.PlanoId);
        if (plano is null) return Resultado.Ok(new { temPlano = false });

        var agora = DateTime.UtcNow;
        var mesAtual = PlanoUsoRegras.PrimeiroDiaDoMesUtc(agora);

        var pendentes = await db.PagamentosPlano
            .Where(p => p.AssinaturaId == assinatura.Id && p.Status == MensalidadeRegras.Pendente)
            .Select(p => p.MesReferencia)
            .ToListAsync();
        var temAtraso = pendentes.Any(mes => MensalidadeRegras.EmAtraso(MensalidadeRegras.Pendente, mes, mesAtual, assinatura.DiaVencimento, agora));

        var servicosIncluidos = PlanoUsoRegras.ServicosIncluidos(plano.ServicosIds).ToList();

        // Regras de uso: limite por mês e dias da semana permitidos (data de referência: hoje, ou ?data= no formato yyyy-MM-dd)
        var dataRef = data.HasValue ? DateTime.SpecifyKind(data.Value.Date, DateTimeKind.Utc).AddHours(12) : agora;
        var (inicioMesUtc, fimMesUtc) = PlanoUsoRegras.IntervaloDoMesUtc(dataRef);
        var usosNoMes = await db.ConsumosPlano.CountAsync(c =>
            c.AssinaturaId == assinatura.Id && c.CriadoEm >= inicioMesUtc && c.CriadoEm < fimMesUtc);
        var diaPermitido = PlanoUsoRegras.DiaPermitido(plano.DiasSemanaPermitidos, dataRef);

        // Agendamentos ainda não pagos de serviços do plano, no mês da data de referência (a Agenda soma isso aos usos)
        var idsServicosPlano = PlanoUsoRegras.IdsDosServicos(plano.ServicosIds);
        var mesRefLocal = PlanoUsoRegras.ParaHorarioLocal(dataRef);
        var iniMesLocal = new DateTime(mesRefLocal.Year, mesRefLocal.Month, 1);
        var fimMesLocal = iniMesLocal.AddMonths(1);
        var usosAgendadosNoMes = await db.Agendamentos.CountAsync(ag =>
            ag.LojaId == lojaId && ag.ClienteId == clienteId && ag.Status != StatusAgendamento.Cancelado && !ag.Pago &&
            idsServicosPlano.Contains(ag.ServicoId) && ag.DataHora >= iniMesLocal && ag.DataHora < fimMesLocal);

        return Resultado.Ok(new
        {
            temPlano = true,
            assinaturaId = assinatura.Id,
            planoId = plano.Id,
            planoNome = plano.Nome,
            servicosIncluidos,
            emDia = !temAtraso,
            limiteUsosMes = plano.LimiteUsosMes,
            usosNoMes,
            usosRestantes = plano.LimiteUsosMes.HasValue ? Math.Max(0, plano.LimiteUsosMes.Value - usosNoMes) : (int?)null,
            usosAgendados = usosAgendadosNoMes,
            diasSemanaPermitidos = plano.DiasSemanaPermitidos,
            diaPermitido,
            diasPermitidosTexto = PlanoUsoRegras.DescreverDias(plano.DiasSemanaPermitidos),
        });
    }

    public async Task<Resultado<object>> HistoricoPagamentosAsync(Guid? lojaId, Guid id)
    {
        var assinatura = await db.AssinaturasCliente.FirstOrDefaultAsync(a => a.Id == id && a.LojaId == lojaId);
        if (assinatura is null) return Resultado.NaoEncontrado();

        var historico = await db.PagamentosPlano
            .Where(p => p.AssinaturaId == id)
            .OrderByDescending(p => p.MesReferencia)
            .Select(p => new { p.Id, p.MesReferencia, p.Valor, p.ValorPago, p.Status, p.PagoEm })
            .ToListAsync();

        return Resultado.Ok(historico);
    }

    // Serviços já usados que estavam inclusos no plano
    public async Task<Resultado<object>> ConsumosAsync(Guid? lojaId, Guid id)
    {
        var assinatura = await db.AssinaturasCliente.FirstOrDefaultAsync(a => a.Id == id && a.LojaId == lojaId);
        if (assinatura is null) return Resultado.NaoEncontrado();

        var consumos = await db.ConsumosPlano
            .Where(c => c.AssinaturaId == id)
            .OrderByDescending(c => c.CriadoEm)
            .Select(c => new { c.Id, c.ServicoId, c.NomeServico, c.CriadoEm })
            .ToListAsync();

        return Resultado.Ok(consumos);
    }

    // ── Venda da mensalidade (fluxo de caixa) ────────────────────────────────

    private void CriarVendaMensalidade(PagamentoPlano pg, Guid lojaId, Guid clienteId, string nomePlano, decimal valor, decimal? valorPago = null)
    {
        if (pg.VendaId is not null) return;

        // Pagamento com valor diferente da mensalidade (juros/multa ou desconto): a venda registra o valor realmente recebido
        var recebido = MensalidadeRegras.Recebido(valor, valorPago);
        var venda = new Venda
        {
            LojaId = lojaId,
            ClienteId = clienteId,
            Total = recebido,
            Desconto = 0,
            TotalFinal = recebido,
            FormaPagamento = "pix",
            FormasPagamento = null,
            Troco = null,
        };
        db.Vendas.Add(venda);

        foreach (var item in MensalidadeRegras.ItensDaVenda(nomePlano, pg.MesReferencia, valor, valorPago))
        {
            db.ItensVenda.Add(new ItemVenda
            {
                VendaId = venda.Id,
                ProdutoId = null,
                ServicoId = null,
                NomeProduto = item.Nome,
                Quantidade = 1,
                PrecoUnitario = item.Valor,
                Subtotal = item.Valor,
            });
        }

        pg.VendaId = venda.Id;
    }

    private async Task RemoverVendaAsync(PagamentoPlano pg)
    {
        if (pg.VendaId is null) return;

        var vendaAntiga = await db.Vendas
            .Include(v => v.Itens)
            .FirstOrDefaultAsync(v => v.Id == pg.VendaId.Value);
        if (vendaAntiga is not null)
        {
            db.ItensVenda.RemoveRange(vendaAntiga.Itens);
            db.Vendas.Remove(vendaAntiga);
        }
        pg.VendaId = null;
    }
}
