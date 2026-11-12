using LojaApi.Data;
using LojaApi.Domain.Chacara;
using LojaApi.Infrastructure.Chacara;
using LojaApi.Shared.Application;
using Microsoft.EntityFrameworkCore;

namespace LojaApi.Application.Chacara;

public record ConfirmarComPagamentoRequest(decimal? ValorPago);
public record RegistrarPrejuizoRequest(decimal? Valor, string? Observacao);
public record AvaliarClienteRequest(int? Nota, string? Comentario);
public record RegistrarPagamentoRequest(decimal Valor);

public record CriarReservaManualRequest(
    DateTime DataInicio, DateTime DataFim, int Pessoas,
    string ClienteNome, string? ClienteEmail, string? ClienteTelefone,
    string? ClienteDocumento, string? ClienteCep, string? ClienteEndereco, string? ClienteNumero, string? ClienteCidade,
    string? HoraEntrada, string? HoraSaida,
    decimal Valor, decimal? ValorPago
);

public record EditarReservaRequest(
    DateTime DataInicio, DateTime DataFim, int Pessoas, string ClienteNome, string ClienteEmail, string ClienteTelefone,
    string? ClienteDocumento, string? ClienteCep, string? ClienteEndereco, string? ClienteNumero, string? ClienteCidade,
    string? HoraEntrada, string? HoraSaida, decimal? ValorManual
);

public record ArquivoContrato(byte[] Pdf, string NomeArquivo);

// Casos de uso das reservas, do ponto de vista do dono da chácara (painel).
public class ReservasChacaraService(AppDbContext db, ReservaChacaraNotificacaoService notificacao)
{
    // Marca como "expirada" as reservas pendentes cujo prazo já passou — chamado sob demanda
    // (mesmo padrão usado na Loja de Acessórios), sem precisar de job em segundo plano dedicado.
    private async Task ExpirarReservasVencidasAsync(Guid lojaId)
    {
        var agora = DateTime.UtcNow;
        var vencidas = await db.Reservas
            .Where(r => r.LojaId == lojaId && r.Status == StatusReserva.PendentePagamento && r.ExpiraEm != null && r.ExpiraEm < agora)
            .ToListAsync();
        if (vencidas.Count == 0) return;

        foreach (var r in vencidas) r.Status = StatusReserva.Expirada;
        await db.SaveChangesAsync();
    }

    private Task<Reserva?> BuscarAsync(Guid? lojaId, int id) =>
        db.Reservas.FirstOrDefaultAsync(r => r.Id == id && r.LojaId == lojaId);

    // ── Consultas ────────────────────────────────────────────────────────────

    public async Task<Resultado<object>> ListarAsync(Guid? lojaId)
    {
        if (lojaId is null) return Resultado.Ok(Array.Empty<object>());

        await ExpirarReservasVencidasAsync(lojaId.Value);

        var lista = await db.Reservas
            .Where(r => r.LojaId == lojaId)
            .OrderByDescending(r => r.CriadoEm)
            .ToListAsync();
        return Resultado.Ok(lista);
    }

    // Reservas perdidas (expiradas) — paginado, com filtro de mês/ano ou período livre
    public async Task<Resultado<object>> ListarPerdidasAsync(
        Guid? lojaId, int pagina, int porPagina, int? mes, int? ano, DateTime? de, DateTime? ate)
    {
        if (lojaId is null) return Resultado.Ok(new { itens = Array.Empty<object>(), total = 0, pagina, porPagina });

        await ExpirarReservasVencidasAsync(lojaId.Value);

        var q = db.Reservas.Where(r => r.LojaId == lojaId && r.Status == StatusReserva.Expirada);

        if (de.HasValue && ate.HasValue)
        {
            var iniF = DateTime.SpecifyKind(de.Value.Date, DateTimeKind.Utc);
            var fimF = DateTime.SpecifyKind(ate.Value.Date, DateTimeKind.Utc).AddDays(1);
            q = q.Where(r => r.CriadoEm >= iniF && r.CriadoEm < fimF);
        }
        else if (mes.HasValue && ano.HasValue)
        {
            var iniF = new DateTime(ano.Value, mes.Value, 1, 0, 0, 0, DateTimeKind.Utc);
            var fimF = iniF.AddMonths(1);
            q = q.Where(r => r.CriadoEm >= iniF && r.CriadoEm < fimF);
        }

        var total = await q.CountAsync();
        var itens = await q.OrderByDescending(r => r.CriadoEm)
            .Skip((pagina - 1) * porPagina).Take(porPagina)
            .ToListAsync();
        return Resultado.Ok(new { itens, total, pagina, porPagina });
    }

    // ── Ciclo de vida da reserva ─────────────────────────────────────────────

    // Marcar manualmente como expirada (você decide, sem esperar o prazo)
    public async Task<Resultado<object>> MarcarExpiradaAsync(Guid? lojaId, int id)
    {
        var reserva = await BuscarAsync(lojaId, id);
        if (reserva is null) return Resultado.NaoEncontrado();

        var erro = reserva.MarcarComoExpirada();
        if (erro != null) return Resultado.Invalido(erro);

        await db.SaveChangesAsync();
        return Resultado.Ok(new { reserva.Id, reserva.Status });
    }

    public async Task<Resultado<object>> ReativarAsync(Guid? lojaId, int id)
    {
        var reserva = await BuscarAsync(lojaId, id);
        if (reserva is null) return Resultado.NaoEncontrado();

        var erro = reserva.PodeReativar();
        if (erro != null) return Resultado.Invalido(erro);

        // A data pode ter sido ocupada por outra reserva desde que essa expirou — não deixa
        // reativar cegamente se der conflito.
        var agora = DateTime.UtcNow;
        var conflita = await db.Reservas
            .Where(r => r.Id != id && r.LojaId == lojaId)
            .Where(RegrasAgenda.OcupaAAgenda(agora))
            .Where(RegrasAgenda.SobrepoeOPeriodo(reserva.DataInicio, reserva.DataFim))
            .AnyAsync();
        if (conflita)
            return Resultado.Conflito("Essas datas já foram ocupadas por outra reserva. Não é possível reativar.");

        reserva.Reativar(DateTime.UtcNow);
        await db.SaveChangesAsync();
        return Resultado.Ok(new { reserva.Id, reserva.Status, reserva.ExpiraEm });
    }

    public async Task<Resultado<object>> ConfirmarAsync(Guid? lojaId, int id, ConfirmarComPagamentoRequest? req)
    {
        var reserva = await BuscarAsync(lojaId, id);
        if (reserva is null) return Resultado.NaoEncontrado();

        var erro = reserva.Confirmar(req?.ValorPago, DateTime.UtcNow);
        if (erro != null) return Resultado.Invalido(erro);

        await db.SaveChangesAsync();
        await notificacao.NotificarConfirmacaoAsync(reserva);

        return Resultado.Ok(new { reserva.Id, reserva.Status, reserva.ValorPago, saldoPendente = reserva.SaldoPendente() });
    }

    public async Task<Resultado<object>> RegistrarPrejuizoAsync(Guid? lojaId, int id, RegistrarPrejuizoRequest req)
    {
        var reserva = await BuscarAsync(lojaId, id);
        if (reserva is null) return Resultado.NaoEncontrado();

        var erro = reserva.RegistrarPrejuizo(req.Valor, req.Observacao);
        if (erro != null) return Resultado.Invalido(erro);

        await db.SaveChangesAsync();
        return Resultado.Ok(new { reserva.Id, reserva.ValorPrejuizo, reserva.ObservacaoPrejuizo });
    }

    public async Task<Resultado<object>> AvaliarClienteAsync(Guid? lojaId, int id, AvaliarClienteRequest req)
    {
        var reserva = await BuscarAsync(lojaId, id);
        if (reserva is null) return Resultado.NaoEncontrado();

        var erro = reserva.AvaliarCliente(req.Nota, req.Comentario);
        if (erro != null) return Resultado.Invalido(erro);

        await db.SaveChangesAsync();
        return Resultado.Ok(new { reserva.Id, reserva.NotaCliente, reserva.ComentarioCliente });
    }

    public async Task<Resultado<object>> ManterNegociacaoAsync(Guid? lojaId, int id)
    {
        var reserva = await BuscarAsync(lojaId, id);
        if (reserva is null) return Resultado.NaoEncontrado();

        var erro = reserva.ManterEmNegociacao();
        if (erro != null) return Resultado.Invalido(erro);

        await db.SaveChangesAsync();
        return Resultado.Ok(new { reserva.Id, reserva.ExpiraEm });
    }

    public async Task<Resultado<object>> DesfazerNegociacaoAsync(Guid? lojaId, int id)
    {
        var reserva = await BuscarAsync(lojaId, id);
        if (reserva is null) return Resultado.NaoEncontrado();

        var erro = reserva.DesfazerNegociacao(DateTime.UtcNow);
        if (erro != null) return Resultado.Invalido(erro);

        await db.SaveChangesAsync();
        return Resultado.Ok(new { reserva.Id, reserva.ExpiraEm });
    }

    public async Task<Resultado<object>> RegistrarPagamentoAsync(Guid? lojaId, int id, RegistrarPagamentoRequest req)
    {
        var reserva = await BuscarAsync(lojaId, id);
        if (reserva is null) return Resultado.NaoEncontrado();

        var erro = reserva.RegistrarPagamento(req.Valor);
        if (erro != null) return Resultado.Invalido(erro);

        await db.SaveChangesAsync();
        return Resultado.Ok(new
        {
            reserva.Id, reserva.Status, reserva.ValorPago,
            saldoPendente = reserva.SaldoPendente(),
            quitada = reserva.ValorPago >= reserva.Valor,
        });
    }

    // ── Criação, edição e exclusão ───────────────────────────────────────────

    public async Task<Resultado<object>> CriarManualAsync(Guid? lojaId, CriarReservaManualRequest req)
    {
        if (lojaId is null) return Resultado.Invalido("Loja não encontrada.");

        if (string.IsNullOrWhiteSpace(req.ClienteNome))
            return Resultado.Invalido("Informe o nome do cliente.");

        if (req.DataFim.Date < req.DataInicio.Date)
            return Resultado.Invalido("Data final não pode ser antes da data inicial.");

        var ini = DiaDeReserva.MeioDiaUtc(req.DataInicio);
        var fim = DiaDeReserva.MeioDiaUtc(req.DataFim);

        var agora = DateTime.UtcNow;
        var conflita = await db.Reservas
            .Where(r => r.LojaId == lojaId)
            .Where(RegrasAgenda.OcupaAAgenda(agora))
            .Where(RegrasAgenda.SobrepoeOPeriodo(ini, fim))
            .AnyAsync();
        if (conflita)
            return Resultado.Conflito("Essas datas conflitam com outra reserva existente.");

        var reserva = Reserva.CriarManual(
            lojaId.Value, req.DataInicio, req.DataFim, req.Pessoas,
            req.ClienteNome, req.ClienteEmail, req.ClienteTelefone,
            req.ClienteDocumento, req.ClienteCep, req.ClienteEndereco, req.ClienteNumero, req.ClienteCidade,
            req.HoraEntrada, req.HoraSaida, req.Valor, req.ValorPago, DateTime.UtcNow);

        db.Reservas.Add(reserva);
        await db.SaveChangesAsync();
        return Resultado.Ok(reserva);
    }

    public async Task<Resultado<object>> EditarAsync(Guid? lojaId, int id, EditarReservaRequest req)
    {
        var reserva = await BuscarAsync(lojaId, id);
        if (reserva is null) return Resultado.NaoEncontrado();

        // TODO: quando o Mercado Pago estiver integrado de verdade, reavaliar se
        // reserva confirmada (já paga) deveria continuar editável livremente aqui,
        // ou exigir um fluxo à parte (estorno/reajuste de cobrança).
        if (req.DataFim.Date < req.DataInicio.Date)
            return Resultado.Invalido("Data final não pode ser antes da data inicial.");

        var diasSolicitadosEdicao = (int)Math.Round((req.DataFim.Date - req.DataInicio.Date).TotalDays) + 1;
        if (diasSolicitadosEdicao > 30)
            return Resultado.Invalido("O período máximo por reserva é de 30 dias.");

        var cfg = await db.ConfiguracoesPrecoChacara.FirstOrDefaultAsync(c => c.LojaId == lojaId)
            ?? new ConfiguracaoPrecoChacara { LojaId = lojaId!.Value };

        if (req.Pessoas < cfg.MinimoPessoas)
            return Resultado.Invalido($"O mínimo é de {cfg.MinimoPessoas} pessoas.");

        var ini = DiaDeReserva.MeioDiaUtc(req.DataInicio);
        var fim = DiaDeReserva.MeioDiaUtc(req.DataFim);

        // Revalida disponibilidade, excluindo a própria reserva da checagem.
        // (Aqui a reserva "em negociação", sem prazo, não bloqueia — comportamento que já existia.)
        var agora = DateTime.UtcNow;
        var conflita = await db.Reservas.AnyAsync(r =>
            r.LojaId == lojaId && r.Id != id &&
            (r.Status == StatusReserva.Confirmada || r.Status == StatusReserva.ConfirmadaParcial
                || (r.Status == StatusReserva.PendentePagamento && r.ExpiraEm > agora)) &&
            r.DataInicio <= fim && r.DataFim >= ini);
        if (conflita)
            return Resultado.Conflito("Essas datas conflitam com outra reserva existente.");

        decimal valorFinal;
        if (req.ValorManual.HasValue)
        {
            // Valor sobrescrito manualmente (ex: desconto combinado com o cliente) — não recalcula pela regra de preço
            valorFinal = req.ValorManual.Value;
        }
        else
        {
            var faixas = await db.FaixasPrecoChacara.Where(f => f.LojaId == lojaId).ToListAsync();
            if (faixas.Count == 0)
                return Resultado.Invalido("Esta chácara ainda não configurou os valores de reserva. Cadastre as faixas de preço antes de editar esta reserva.");

            var periodosEspeciais = await db.PeriodosEspeciaisChacara.Where(p => p.LojaId == lojaId).ToListAsync();

            try
            {
                var resultado = CalculadoraPrecoChacara.Calcular(ini, fim, req.Pessoas, cfg, faixas, periodosEspeciais);

                if (resultado.PacoteFechado)
                {
                    // Datas especiais (pacote fechado) têm preço fixo — horário de entrada/saída não altera o valor.
                    valorFinal = resultado.ValorTotal;
                }
                else
                {
                    var horarios = await db.HorariosChacara.Where(h => h.LojaId == lojaId).ToListAsync();
                    var ajusteEntrada = AjusteHorarioChacaraService.Obter(horarios, "entrada", req.HoraEntrada);
                    var ajusteSaida = AjusteHorarioChacaraService.Obter(horarios, "saida", req.HoraSaida);
                    valorFinal = resultado.ValorTotal + ajusteEntrada + ajusteSaida;
                }
            }
            catch (InvalidOperationException ex)
            {
                return Resultado.Invalido(ex.Message);
            }
        }

        // Guarda os valores "antes" pra comparar depois — só reenvia contrato se algo
        // que realmente aparece nele mudou (não precisa notificar por corrigir telefone/e-mail, etc.)
        var dataInicioAntes = reserva.DataInicio;
        var dataFimAntes = reserva.DataFim;
        var pessoasAntes = reserva.Pessoas;
        var valorAntes = reserva.Valor;

        reserva.AtualizarDados(
            ini, fim, req.Pessoas, valorFinal,
            req.ClienteNome, req.ClienteEmail, req.ClienteTelefone,
            req.ClienteDocumento, req.ClienteCep, req.ClienteEndereco, req.ClienteNumero, req.ClienteCidade,
            req.HoraEntrada, req.HoraSaida);

        var eraConfirmada = reserva.Status == StatusReserva.Confirmada;
        var mudouTermosDoContrato = dataInicioAntes != reserva.DataInicio
            || dataFimAntes != reserva.DataFim
            || pessoasAntes != reserva.Pessoas
            || valorAntes != reserva.Valor;

        await db.SaveChangesAsync();

        var avisoNotificacao = (string?)null;
        if (eraConfirmada && mudouTermosDoContrato)
        {
            try
            {
                await notificacao.ReenviarContratoAtualizadoAsync(reserva);
            }
            catch (Exception)
            {
                // A edição já foi salva com sucesso — uma falha no reenvio do contrato
                // não deve derrubar a resposta nem fazer parecer que os dados não salvaram.
                avisoNotificacao = "Reserva atualizada, mas houve um problema ao reenviar o contrato por e-mail. Você pode reenviar manualmente.";
            }
        }

        return Resultado.Ok(new { reserva, aviso = avisoNotificacao });
    }

    public async Task<Resultado<object>> ExcluirAsync(Guid? lojaId, int id)
    {
        var reserva = await BuscarAsync(lojaId, id);
        if (reserva is null) return Resultado.NaoEncontrado();

        // TODO: mesma ressalva do Editar — revisar quando o pagamento real (Mercado Pago)
        // estiver funcionando, pra não apagar reserva paga sem tratar estorno.
        db.Reservas.Remove(reserva);
        await db.SaveChangesAsync();
        return Resultado.Ok(new { mensagem = "Reserva excluída." });
    }

    // ── Contrato ─────────────────────────────────────────────────────────────

    // PDF do contrato (para o dono enviar pelo WhatsApp ou imprimir), sem enviar e-mail.
    public async Task<Resultado<ArquivoContrato>> GerarContratoPdfAsync(Guid? lojaId, int id)
    {
        var reserva = await BuscarAsync(lojaId, id);
        if (reserva is null) return Resultado<ArquivoContrato>.NaoEncontrado();

        var loja = await db.Lojas.FindAsync(reserva.LojaId);
        if (loja is null) return Resultado<ArquivoContrato>.NaoEncontrado();

        var info = await db.InfosChacara.FirstOrDefaultAsync(i => i.LojaId == reserva.LojaId);
        var cfg = await db.ConfiguracoesPrecoChacara.FirstOrDefaultAsync(c => c.LojaId == reserva.LojaId)
            ?? new ConfiguracaoPrecoChacara { LojaId = reserva.LojaId };

        byte[] pdf;
        try
        {
            pdf = ContratoChacaraService.GerarContratoPdf(reserva, loja, info, cfg);
        }
        catch (Exception)
        {
            return Resultado<ArquivoContrato>.Falha("Não foi possível gerar o contrato.");
        }

        return Resultado<ArquivoContrato>.Ok(new ArquivoContrato(pdf, $"Contrato-{reserva.DataInicio:yyyy-MM-dd}.pdf"));
    }

    public async Task<Resultado<object>> EnviarContratoAsync(Guid? lojaId, int id)
    {
        var reserva = await BuscarAsync(lojaId, id);
        if (reserva is null) return Resultado.NaoEncontrado();

        if (string.IsNullOrWhiteSpace(reserva.ClienteEmail))
            return Resultado.Invalido("Esta reserva não tem e-mail de cliente cadastrado.");

        var enviado = await notificacao.EnviarContratoManualAsync(reserva);
        if (!enviado)
            return Resultado.Falha("Não foi possível enviar o contrato. Confira os logs do servidor.");

        return Resultado.Ok(new { mensagem = "Contrato enviado com sucesso." });
    }
}
