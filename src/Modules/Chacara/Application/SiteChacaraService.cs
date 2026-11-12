using LojaApi.Data;
using LojaApi.Modules.Chacara.Domain;
using LojaApi.Shared.Application;
using Microsoft.EntityFrameworkCore;

namespace LojaApi.Modules.Chacara.Application;

public record ReservarPublicoRequest(
    DateTime DataInicio,
    DateTime DataFim,
    int Pessoas,
    string ClienteNome,
    string ClienteEmail,
    string ClienteTelefone,
    string? ClienteDocumento,
    string? ClienteCep,
    string? ClienteEndereco,
    string? HoraEntrada,
    string? HoraSaida
);

public record EnviarAvaliacaoChacaraRequest(int Nota, string? Comentario);

// O que o cliente vê e faz no site público da chácara: informações, disponibilidade, preço, reserva e avaliação.
public class SiteChacaraService(
    AppDbContext db,
    CredencialMercadoPagoChacaraService credencialMp,
    ReservaChacaraNotificacaoService notificacao)
{
    private const string PaginaNaoEncontrada = "Página não encontrada.";
    private const string ReservaNaoEncontrada = "Reserva não encontrada.";

    private static readonly Dictionary<string, string> ComodidadesLabels = new()
    {
        ["piscina"] = "Piscina",
        ["churrasqueira"] = "Churrasqueira",
        ["wifi"] = "Wi-Fi",
        ["estacionamento"] = "Estacionamento",
        ["area_coberta"] = "Área coberta",
        ["playground"] = "Playground infantil",
        ["campo_futebol"] = "Campo de futebol",
        ["salao_festas"] = "Salão de festas",
        ["gerador"] = "Gerador de energia",
        ["ar_condicionado"] = "Ar-condicionado",
    };

    private Task<LojaApi.Models.Loja?> BuscarLojaAsync(string slug) =>
        db.Lojas.FirstOrDefaultAsync(l => l.Slug == slug);

    // ── Disponibilidade e preço ──────────────────────────────────────────────

    public async Task<Resultado<object>> DisponibilidadeAsync(string slug, DateTime dataInicio, DateTime dataFim)
    {
        var loja = await BuscarLojaAsync(slug);
        if (loja is null) return Resultado.NaoEncontrado(PaginaNaoEncontrada);

        var ini = DiaDeReserva.MeioDiaUtc(dataInicio);
        var fim = DiaDeReserva.MeioDiaUtc(dataFim);

        var conflita = await db.Reservas
            .Where(r => r.LojaId == loja.Id)
            .Where(RegrasAgenda.OcupaAAgenda(DateTime.UtcNow))
            .Where(RegrasAgenda.SobrepoeOPeriodo(ini, fim))
            .AnyAsync();

        return Resultado.Ok(new { disponivel = !conflita });
    }

    public async Task<Resultado<object>> DatasOcupadasAsync(string slug, int meses)
    {
        var loja = await BuscarLojaAsync(slug);
        if (loja is null) return Resultado.NaoEncontrado(PaginaNaoEncontrada);

        var agora = DateTime.UtcNow;
        var limite = agora.AddMonths(meses);

        var ocupadas = await db.Reservas
            .Where(r => r.LojaId == loja.Id && r.DataFim >= agora && r.DataInicio <= limite)
            .Where(RegrasAgenda.OcupaAAgenda(agora))
            .OrderBy(r => r.DataInicio)
            .Select(r => new { r.DataInicio, r.DataFim })
            .ToListAsync();

        return Resultado.Ok(ocupadas);
    }

    public async Task<Resultado<object>> ValorAsync(
        string slug, DateTime dataInicio, DateTime dataFim, int pessoas, string? horaEntrada, string? horaSaida)
    {
        var loja = await BuscarLojaAsync(slug);
        if (loja is null) return Resultado.NaoEncontrado(PaginaNaoEncontrada);

        var cfg = await db.ConfiguracoesPrecoChacara.FirstOrDefaultAsync(c => c.LojaId == loja.Id)
            ?? new ConfiguracaoPrecoChacara { LojaId = loja.Id };

        var faixas = await db.FaixasPrecoChacara.Where(f => f.LojaId == loja.Id).ToListAsync();
        if (faixas.Count == 0)
            return Resultado.Invalido("Esta chácara ainda não configurou os valores de reserva.");

        if (pessoas < cfg.MinimoPessoas)
            return Resultado.Invalido($"O mínimo é de {cfg.MinimoPessoas} pessoas.");

        var diasSolicitados = (int)Math.Round((dataFim.Date - dataInicio.Date).TotalDays) + 1;
        if (diasSolicitados > 30)
            return Resultado.Invalido("O período máximo por reserva é de 30 dias.");

        var ini = DiaDeReserva.MeioDiaUtc(dataInicio);
        var fim = DiaDeReserva.MeioDiaUtc(dataFim);

        var periodosEspeciais = await db.PeriodosEspeciaisChacara.Where(p => p.LojaId == loja.Id).ToListAsync();
        var resultado = CalculadoraPrecoChacara.Calcular(ini, fim, pessoas, cfg, faixas, periodosEspeciais);

        return Resultado.Ok(await ComAjusteHorarioAsync(loja.Id, resultado, horaEntrada, horaSaida));
    }

    private async Task<CalculadoraPrecoChacara.ResultadoCalculo> ComAjusteHorarioAsync(
        Guid lojaId, CalculadoraPrecoChacara.ResultadoCalculo resultado, string? horaEntrada, string? horaSaida)
    {
        // Datas especiais (pacote fechado) têm preço fixo — horário de entrada/saída não altera o valor.
        if (resultado.PacoteFechado) return resultado;
        if (string.IsNullOrWhiteSpace(horaEntrada) && string.IsNullOrWhiteSpace(horaSaida)) return resultado;

        var horarios = await db.HorariosChacara.Where(h => h.LojaId == lojaId).ToListAsync();
        return CalculadoraPrecoChacara.AplicarAjustesDeHorario(resultado, horarios, horaEntrada, horaSaida);
    }

    // ── Reserva ──────────────────────────────────────────────────────────────

    public async Task<Resultado<object>> ReservarAsync(string slug, ReservarPublicoRequest req)
    {
        var loja = await BuscarLojaAsync(slug);
        if (loja is null) return Resultado.NaoEncontrado(PaginaNaoEncontrada);

        if (string.IsNullOrWhiteSpace(req.ClienteNome) || string.IsNullOrWhiteSpace(req.ClienteTelefone) || string.IsNullOrWhiteSpace(req.ClienteEmail))
            return Resultado.Invalido("Informe nome, e-mail e telefone.");

        if (req.DataFim.Date < req.DataInicio.Date)
            return Resultado.Invalido("Data final não pode ser antes da data inicial.");

        var diasSolicitadosReserva = (int)Math.Round((req.DataFim.Date - req.DataInicio.Date).TotalDays) + 1;
        if (diasSolicitadosReserva > 30)
            return Resultado.Invalido("O período máximo por reserva é de 30 dias.");

        var infoLocador = await db.InfosChacara.AsNoTracking().FirstOrDefaultAsync(i => i.LojaId == loja.Id);
        if (infoLocador is null || !infoLocador.LocadorCompleto())
            return Resultado.Invalido("Esta chácara ainda não está habilitada para reservas online. Entre em contato com o responsável.");

        var cfgValidacao = await db.ConfiguracoesPrecoChacara.FirstOrDefaultAsync(c => c.LojaId == loja.Id)
            ?? new ConfiguracaoPrecoChacara { LojaId = loja.Id };
        if (req.Pessoas < cfgValidacao.MinimoPessoas)
            return Resultado.Invalido($"O mínimo é de {cfgValidacao.MinimoPessoas} pessoas.");

        var ini = DiaDeReserva.MeioDiaUtc(req.DataInicio);
        var fim = DiaDeReserva.MeioDiaUtc(req.DataFim);

        // Revalida disponibilidade (evita corrida entre duas pessoas reservando ao mesmo tempo)
        var conflita = await db.Reservas
            .Where(r => r.LojaId == loja.Id)
            .Where(RegrasAgenda.OcupaAAgenda(DateTime.UtcNow))
            .Where(RegrasAgenda.SobrepoeOPeriodo(ini, fim))
            .AnyAsync();
        if (conflita)
            return Resultado.Conflito("Essas datas acabaram de ser reservadas. Escolha outro período.");

        var cfg = await db.ConfiguracoesPrecoChacara.FirstOrDefaultAsync(c => c.LojaId == loja.Id)
            ?? new ConfiguracaoPrecoChacara { LojaId = loja.Id };
        var faixas = await db.FaixasPrecoChacara.Where(f => f.LojaId == loja.Id).ToListAsync();
        if (faixas.Count == 0)
            return Resultado.Invalido("Esta chácara ainda não configurou os valores de reserva.");

        var periodosEspeciais = await db.PeriodosEspeciaisChacara.Where(p => p.LojaId == loja.Id).ToListAsync();

        var resultado = CalculadoraPrecoChacara.Calcular(ini, fim, req.Pessoas, cfg, faixas, periodosEspeciais);
        resultado = await ComAjusteHorarioAsync(loja.Id, resultado, req.HoraEntrada, req.HoraSaida);

        var semPagamentoOnline = !(await credencialMp.ObterAsync(loja.Id)).Ok;

        var reserva = Reserva.CriarPeloSite(
            loja.Id, req.DataInicio, req.DataFim, req.Pessoas,
            req.ClienteNome, req.ClienteEmail, req.ClienteTelefone,
            req.ClienteDocumento, req.ClienteCep, req.ClienteEndereco,
            req.HoraEntrada, req.HoraSaida, resultado.ValorTotal, semPagamentoOnline, DateTime.UtcNow);

        db.Reservas.Add(reserva);
        await db.SaveChangesAsync();

        await notificacao.NotificarPendenteAsync(reserva);

        return Resultado.Ok(new
        {
            reserva.Id,
            reserva.Valor,
            reserva.ExpiraEm,
            semPagamentoOnline,
            mensagem = semPagamentoOnline
                ? "Solicitação enviada. A chácara vai confirmar sua reserva e entrar em contato."
                : "Reserva criada. Prossiga para o pagamento.",
        });
    }

    // ── Avaliação da estadia ─────────────────────────────────────────────────

    public async Task<Resultado<object>> BuscarParaAvaliarAsync(string slug, int reservaId)
    {
        var loja = await BuscarLojaAsync(slug);
        if (loja is null) return Resultado.NaoEncontrado(PaginaNaoEncontrada);

        var reserva = await db.Reservas.FirstOrDefaultAsync(r => r.Id == reservaId && r.LojaId == loja.Id);
        if (reserva is null) return Resultado.NaoEncontrado(ReservaNaoEncontrada);

        if (reserva.Status != StatusReserva.Confirmada)
            return Resultado.Invalido("Essa reserva ainda não foi confirmada.");

        if (!reserva.EstadiaTerminou(DateTime.UtcNow))
            return Resultado.Invalido("Sua estadia ainda não terminou — volte aqui depois do check-out para avaliar.");

        var existente = await db.AvaliacoesChacara.FirstOrDefaultAsync(a => a.ReservaId == reservaId);

        return Resultado.Ok(new
        {
            reserva.ClienteNome,
            reserva.DataInicio,
            reserva.DataFim,
            nomeLoja = loja.Nome,
            jaAvaliado = existente != null,
            notaAtual = existente?.Nota,
            comentarioAtual = existente?.Comentario,
        });
    }

    public async Task<Resultado<object>> EnviarAvaliacaoAsync(string slug, int reservaId, EnviarAvaliacaoChacaraRequest req)
    {
        var loja = await BuscarLojaAsync(slug);
        if (loja is null) return Resultado.NaoEncontrado(PaginaNaoEncontrada);

        var reserva = await db.Reservas.FirstOrDefaultAsync(r => r.Id == reservaId && r.LojaId == loja.Id);
        if (reserva is null) return Resultado.NaoEncontrado(ReservaNaoEncontrada);

        if (reserva.Status != StatusReserva.Confirmada)
            return Resultado.Invalido("Essa reserva ainda não foi confirmada.");

        if (!reserva.EstadiaTerminou(DateTime.UtcNow))
            return Resultado.Invalido("Sua estadia ainda não terminou.");

        if (!AvaliacaoChacara.NotaValida(req.Nota))
            return Resultado.Invalido("Escolha uma nota de 1 a 5 estrelas.");

        var existente = await db.AvaliacoesChacara.FirstOrDefaultAsync(a => a.ReservaId == reservaId);
        if (existente != null)
            existente.Atualizar(req.Nota, req.Comentario);
        else
            db.AvaliacoesChacara.Add(AvaliacaoChacara.Criar(reservaId, req.Nota, req.Comentario));

        await db.SaveChangesAsync();
        return Resultado.Ok(new { mensagem = "Avaliação enviada, obrigado!" });
    }

    // ── Página da chácara ────────────────────────────────────────────────────

    public async Task<Resultado<object>> DadosAsync(string slug)
    {
        var loja = await BuscarLojaAsync(slug);
        if (loja is null) return Resultado.NaoEncontrado(PaginaNaoEncontrada);

        var modulosAtivos = (loja.ModulosAtivos ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (!modulosAtivos.Contains("chacara_reservas"))
            return Resultado.NaoEncontrado(PaginaNaoEncontrada);

        var info = await db.InfosChacara.FirstOrDefaultAsync(i => i.LojaId == loja.Id);

        var fotos = await db.FotosChacara
            .Where(f => f.LojaId == loja.Id)
            .OrderBy(f => f.Ordem)
            .Select(f => f.Url)
            .ToListAsync();

        var cfg = await db.ConfiguracoesPrecoChacara.FirstOrDefaultAsync(c => c.LojaId == loja.Id)
            ?? new ConfiguracaoPrecoChacara { LojaId = loja.Id };

        var horariosConfig = await db.HorariosChacara
            .Where(h => h.LojaId == loja.Id)
            .OrderBy(h => h.Ordem).ThenBy(h => h.Hora)
            .ToListAsync();

        var comodidadesChaves = (info?.Comodidades ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var comodidades = comodidadesChaves
            .Where(c => ComodidadesLabels.ContainsKey(c))
            .Select(c => new { chave = c, label = ComodidadesLabels[c] })
            .ToList();

        var comodidadesExtras = (info?.ComodidadesExtras ?? "")
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim())
            .Where(l => l.Length > 0)
            .ToList();

        var avisos = (info?.AvisosUso ?? "")
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim())
            .Where(l => l.Length > 0)
            .ToList();

        return Resultado.Ok(new
        {
            nome = loja.Nome,
            logoUrl = loja.LogoUrl,
            corPrimaria = loja.CorPrimaria,
            descricao = info?.Descricao ?? "",
            endereco = info?.Endereco ?? "",
            mapaEmbedUrl = info?.MapaEmbedUrl,
            horaEntrada = info?.HoraEntrada ?? "08:00",
            horaSaida = info?.HoraSaida ?? "22:00",
            horarios = new
            {
                entrada = horariosConfig.Where(h => h.Tipo == "entrada").Select(h => new { h.Id, h.Hora, h.Ajuste }),
                saida = horariosConfig.Where(h => h.Tipo == "saida").Select(h => new { h.Id, h.Hora, h.Ajuste }),
            },
            avisosUso = avisos,
            pagamentoOnline = (await credencialMp.ObterAsync(loja.Id)).Ok,
            reservasDisponiveis = info != null && info.LocadorCompleto(),
            fotos,
            comodidades,
            comodidadesExtras,
            precificacao = new
            {
                cfg.MinimoPessoas,
                cfg.LimitePessoasParaTaxaLimpeza,
                cfg.ValorTaxaLimpeza,
                cfg.ValorMultaNaoLimpeza,
            },
        });
    }
}
