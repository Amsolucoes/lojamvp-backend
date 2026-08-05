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
public class PublicoChacaraController(AppDbContext db, LojaApi.src.Services.ReservaChacaraNotificacaoService notificacao) : ControllerBase
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
            (r.Status == "confirmada" || (r.Status == "pendente_pagamento" && (r.ExpiraEm == null || r.ExpiraEm > DateTime.UtcNow))) &&
            r.DataInicio <= fim && r.DataFim >= ini);

        return Ok(new { disponivel = !conflita });
    }

    [HttpGet("datas-ocupadas")]
    public async Task<IActionResult> DatasOcupadas(string slug, [FromQuery] int meses = 3)
    {
        var loja = await db.Lojas.FirstOrDefaultAsync(l => l.Slug == slug);
        if (loja is null) return NotFound(new { erro = "Página não encontrada." });

        var agora = DateTime.UtcNow;
        var limite = agora.AddMonths(meses);

        var ocupadas = await db.Reservas
            .Where(r => r.LojaId == loja.Id &&
                (r.Status == "confirmada" || (r.Status == "pendente_pagamento" && (r.ExpiraEm == null || r.ExpiraEm > agora))) &&
                r.DataFim >= agora && r.DataInicio <= limite)
            .OrderBy(r => r.DataInicio)
            .Select(r => new { r.DataInicio, r.DataFim })
            .ToListAsync();

        return Ok(ocupadas);
    }

    [HttpGet("valor")]
    public async Task<IActionResult> Valor(string slug, [FromQuery] DateTime dataInicio, [FromQuery] DateTime dataFim, [FromQuery] int pessoas)
    {
        var loja = await db.Lojas.FirstOrDefaultAsync(l => l.Slug == slug);
        if (loja is null) return NotFound(new { erro = "Página não encontrada." });

        var cfg = await db.ConfiguracoesPrecoChacara.FirstOrDefaultAsync(c => c.LojaId == loja.Id)
            ?? new ConfiguracaoPrecoChacara { LojaId = loja.Id };

        var faixas = await db.FaixasPrecoChacara.Where(f => f.LojaId == loja.Id).ToListAsync();
        if (faixas.Count == 0)
            return BadRequest(new { erro = "Esta chácara ainda não configurou os valores de reserva." });

        if (pessoas < cfg.MinimoPessoas)
            return BadRequest(new { erro = $"O mínimo é de {cfg.MinimoPessoas} pessoas." });

        var diasSolicitados = (int)Math.Round((dataFim.Date - dataInicio.Date).TotalDays) + 1;
        if (diasSolicitados > 30)
            return BadRequest(new { erro = "O período máximo por reserva é de 30 dias." });

        var ini = DateTime.SpecifyKind(dataInicio.Date, DateTimeKind.Utc).AddHours(12);
        var fim = DateTime.SpecifyKind(dataFim.Date, DateTimeKind.Utc).AddHours(12);

        var periodosEspeciais = await db.PeriodosEspeciaisChacara.Where(p => p.LojaId == loja.Id).ToListAsync();

        var resultado = CalculadoraPrecoChacara.Calcular(ini, fim, pessoas, cfg, faixas, periodosEspeciais);
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

        var diasSolicitadosReserva = (int)Math.Round((req.DataFim.Date - req.DataInicio.Date).TotalDays) + 1;
        if (diasSolicitadosReserva > 30)
            return BadRequest(new { erro = "O período máximo por reserva é de 30 dias." });

        var cfgValidacao = await db.ConfiguracoesPrecoChacara.FirstOrDefaultAsync(c => c.LojaId == loja.Id)
            ?? new ConfiguracaoPrecoChacara { LojaId = loja.Id };

        if (req.Pessoas < cfgValidacao.MinimoPessoas)
            return BadRequest(new { erro = $"O mínimo é de {cfgValidacao.MinimoPessoas} pessoas." });

        var ini = DateTime.SpecifyKind(req.DataInicio.Date, DateTimeKind.Utc).AddHours(12);
        var fim = DateTime.SpecifyKind(req.DataFim.Date, DateTimeKind.Utc).AddHours(12);

        // Revalida disponibilidade (evita corrida entre duas pessoas reservando ao mesmo tempo)
        var conflita = await db.Reservas.AnyAsync(r =>
            r.LojaId == loja.Id &&
            (r.Status == "confirmada" || (r.Status == "pendente_pagamento" && (r.ExpiraEm == null || r.ExpiraEm > DateTime.UtcNow))) &&
            r.DataInicio <= fim && r.DataFim >= ini);

        if (conflita)
            return Conflict(new { erro = "Essas datas acabaram de ser reservadas. Escolha outro período." });

        var cfg = await db.ConfiguracoesPrecoChacara.FirstOrDefaultAsync(c => c.LojaId == loja.Id)
            ?? new ConfiguracaoPrecoChacara { LojaId = loja.Id };

        var faixas = await db.FaixasPrecoChacara.Where(f => f.LojaId == loja.Id).ToListAsync();
        if (faixas.Count == 0)
            return BadRequest(new { erro = "Esta chácara ainda não configurou os valores de reserva." });

        var periodosEspeciais = await db.PeriodosEspeciaisChacara.Where(p => p.LojaId == loja.Id).ToListAsync();

        var resultado = CalculadoraPrecoChacara.Calcular(ini, fim, req.Pessoas, cfg, faixas, periodosEspeciais);

        var reserva = new Reserva
        {
            LojaId = loja.Id,
            DataInicio = ini,
            DataFim = fim,
            Pessoas = req.Pessoas,
            ClienteNome = req.ClienteNome.Trim(),
            ClienteEmail = req.ClienteEmail.Trim(),
            ClienteTelefone = new string(req.ClienteTelefone.Where(char.IsDigit).ToArray()),
            ClienteDocumento = string.IsNullOrWhiteSpace(req.ClienteDocumento) ? null : req.ClienteDocumento.Trim(),
            ClienteCep = string.IsNullOrWhiteSpace(req.ClienteCep) ? null : req.ClienteCep.Trim(),
            ClienteEndereco = string.IsNullOrWhiteSpace(req.ClienteEndereco) ? null : req.ClienteEndereco.Trim(),
            Valor = resultado.ValorTotal,
            Status = "pendente_pagamento",
            ExpiraEm = DateTime.UtcNow.AddMinutes(15),
        };

        db.Reservas.Add(reserva);
        await db.SaveChangesAsync();

        await notificacao.NotificarPendenteAsync(reserva);

        return Ok(new
        {
            reserva.Id,
            reserva.Valor,
            reserva.ExpiraEm,
            mensagem = "Reserva criada. Prossiga para o pagamento.",
        });
    }

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

    [HttpGet("avaliacao/{reservaId:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> BuscarParaAvaliar(string slug, int reservaId)
    {
        var loja = await db.Lojas.FirstOrDefaultAsync(l => l.Slug == slug);
        if (loja is null) return NotFound(new { erro = "Página não encontrada." });

        var reserva = await db.Reservas.FirstOrDefaultAsync(r => r.Id == reservaId && r.LojaId == loja.Id);
        if (reserva is null) return NotFound(new { erro = "Reserva não encontrada." });

        if (reserva.Status != "confirmada")
            return BadRequest(new { erro = "Essa reserva ainda não foi confirmada." });

        if (reserva.DataFim > DateTime.UtcNow)
            return BadRequest(new { erro = "Sua estadia ainda não terminou — volte aqui depois do check-out para avaliar." });

        var existente = await db.AvaliacoesChacara.FirstOrDefaultAsync(a => a.ReservaId == reservaId);

        return Ok(new
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

    public record EnviarAvaliacaoChacaraRequest(int Nota, string? Comentario);

    [HttpPost("avaliacao/{reservaId:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> EnviarAvaliacao(string slug, int reservaId, [FromBody] EnviarAvaliacaoChacaraRequest req)
    {
        var loja = await db.Lojas.FirstOrDefaultAsync(l => l.Slug == slug);
        if (loja is null) return NotFound(new { erro = "Página não encontrada." });

        var reserva = await db.Reservas.FirstOrDefaultAsync(r => r.Id == reservaId && r.LojaId == loja.Id);
        if (reserva is null) return NotFound(new { erro = "Reserva não encontrada." });

        if (reserva.Status != "confirmada")
            return BadRequest(new { erro = "Essa reserva ainda não foi confirmada." });

        if (reserva.DataFim > DateTime.UtcNow)
            return BadRequest(new { erro = "Sua estadia ainda não terminou." });

        if (req.Nota < 1 || req.Nota > 5)
            return BadRequest(new { erro = "Escolha uma nota de 1 a 5 estrelas." });

        var existente = await db.AvaliacoesChacara.FirstOrDefaultAsync(a => a.ReservaId == reservaId);
        if (existente != null)
        {
            existente.Nota = req.Nota;
            existente.Comentario = string.IsNullOrWhiteSpace(req.Comentario) ? null : req.Comentario.Trim();
        }
        else
        {
            db.AvaliacoesChacara.Add(new AvaliacaoChacara
            {
                ReservaId = reservaId,
                Nota = req.Nota,
                Comentario = string.IsNullOrWhiteSpace(req.Comentario) ? null : req.Comentario.Trim(),
            });
        }
        await db.SaveChangesAsync();

        return Ok(new { mensagem = "Avaliação enviada, obrigado!" });
    }

    [HttpGet("dados")]
    public async Task<IActionResult> Dados(string slug)
    {
        var loja = await db.Lojas.FirstOrDefaultAsync(l => l.Slug == slug);
        if (loja is null) return NotFound(new { erro = "Página não encontrada." });

        var modulosAtivos = (loja.ModulosAtivos ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (!modulosAtivos.Contains("chacara_reservas"))
            return NotFound(new { erro = "Página não encontrada." });

        var info = await db.InfosChacara.FirstOrDefaultAsync(i => i.LojaId == loja.Id);
        var fotos = await db.FotosChacara
            .Where(f => f.LojaId == loja.Id)
            .OrderBy(f => f.Ordem)
            .Select(f => f.Url)
            .ToListAsync();

        var cfg = await db.ConfiguracoesPrecoChacara.FirstOrDefaultAsync(c => c.LojaId == loja.Id)
            ?? new ConfiguracaoPrecoChacara { LojaId = loja.Id };

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

        return Ok(new
        {
            nome = loja.Nome,
            logoUrl = loja.LogoUrl,
            corPrimaria = loja.CorPrimaria,
            descricao = info?.Descricao ?? "",
            endereco = info?.Endereco ?? "",
            mapaEmbedUrl = info?.MapaEmbedUrl,
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

public record ReservarPublicoRequest(
    DateTime DataInicio,
    DateTime DataFim,
    int Pessoas,
    string ClienteNome,
    string ClienteEmail,
    string ClienteTelefone,
    string? ClienteDocumento,
    string? ClienteCep,
    string? ClienteEndereco
);