using LojaApi.Data;
using LojaApi.Services;
using LojaApi.src.Models;
using LojaApi.src.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LojaApi.Controllers;

[ApiController]
[Route("api/publico/{slug}/chacara")]
[AllowAnonymous]
public class PublicoChacaraController(AppDbContext db, LojaApi.src.Services.ReservaChacaraNotificacaoService notificacao, MercadoPagoService mpService) : ControllerBase
{
    // Cartão parcelado (pagando o total): até R$800 → 2x; acima disso → trava em 3x
    private static int ParcelasMaximas(decimal valor) => valor <= 800m ? 2 : 3;

    // Repassa a taxa fixa de processamento do Mercado Pago (~4,98% no modo Parcelado Cliente)
    // — não confundir com juros de parcelamento, que o próprio MP já cobra do cliente à parte.
    private const decimal MARKUP_CARTAO = 1.05m;
    private static decimal ComMarkupCartao(decimal valor) => Math.Round(valor * MARKUP_CARTAO, 2);

    [HttpGet("disponibilidade")]
    public async Task<IActionResult> Disponibilidade(string slug, [FromQuery] DateTime dataInicio, [FromQuery] DateTime dataFim)
    {
        var loja = await db.Lojas.FirstOrDefaultAsync(l => l.Slug == slug);
        if (loja is null) return NotFound(new { erro = "Página não encontrada." });

        var ini = DateTime.SpecifyKind(dataInicio.Date, DateTimeKind.Utc).AddHours(12);
        var fim = DateTime.SpecifyKind(dataFim.Date, DateTimeKind.Utc).AddHours(12);

        var conflita = await db.Reservas.AnyAsync(r =>
            r.LojaId == loja.Id &&
            (r.Status == "confirmada" || r.Status == "confirmada_parcial" || (r.Status == "pendente_pagamento" && (r.ExpiraEm == null || r.ExpiraEm > DateTime.UtcNow))) &&
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
                (r.Status == "confirmada" || r.Status == "confirmada_parcial" || (r.Status == "pendente_pagamento" && (r.ExpiraEm == null || r.ExpiraEm > agora))) &&
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
            (r.Status == "confirmada" || r.Status == "confirmada_parcial" || (r.Status == "pendente_pagamento" && (r.ExpiraEm == null || r.ExpiraEm > DateTime.UtcNow))) &&
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

    // ── Escolhe a forma de pagamento (define o que os endpoints seguintes vão cobrar) ──
    public record EscolherPagamentoRequest(string FormaPagamento); // pix | cartao | combinado

    [HttpPost("reservas/{id:int}/pagamento/escolher")]
    [AllowAnonymous]
    public async Task<IActionResult> EscolherPagamento(string slug, int id, [FromBody] EscolherPagamentoRequest req)
    {
        var loja = await db.Lojas.FirstOrDefaultAsync(l => l.Slug == slug);
        if (loja is null) return NotFound(new { erro = "Página não encontrada." });

        var reserva = await db.Reservas.FirstOrDefaultAsync(r => r.Id == id && r.LojaId == loja.Id);
        if (reserva is null) return NotFound(new { erro = "Reserva não encontrada." });

        if (reserva.Status != "pendente_pagamento")
            return BadRequest(new { erro = "Essa reserva não está mais aguardando pagamento." });

        if (req.FormaPagamento != "pix" && req.FormaPagamento != "cartao" && req.FormaPagamento != "combinado")
            return BadRequest(new { erro = "Forma de pagamento inválida." });

        reserva.FormaPagamento = req.FormaPagamento;
        await db.SaveChangesAsync();

        var valorPix = req.FormaPagamento is "pix" or "combinado" ? reserva.Valor / 2 : 0;
        var parcelasMax = req.FormaPagamento == "cartao" ? ParcelasMaximas(reserva.Valor) : 1;
        var valorCartao = req.FormaPagamento == "cartao" ? ComMarkupCartao(reserva.Valor)
                         : req.FormaPagamento == "combinado" ? ComMarkupCartao(reserva.Valor / 2) : 0;

        return Ok(new { valorPix, valorCartao, parcelasMax });
    }

    // ── Gera o Pix (sinal de 50%, ou metade do combinado) ──────────
    [HttpPost("reservas/{id:int}/pagamento/pix")]
    [AllowAnonymous]
    public async Task<IActionResult> PagarPix(string slug, int id)
    {
        var loja = await db.Lojas.FirstOrDefaultAsync(l => l.Slug == slug);
        if (loja is null) return NotFound(new { erro = "Página não encontrada." });

        var reserva = await db.Reservas.FirstOrDefaultAsync(r => r.Id == id && r.LojaId == loja.Id);
        if (reserva is null) return NotFound(new { erro = "Reserva não encontrada." });

        if (reserva.FormaPagamento != "pix" && reserva.FormaPagamento != "combinado")
            return BadRequest(new { erro = "Escolha a forma de pagamento antes de gerar o Pix." });

        if (string.IsNullOrWhiteSpace(reserva.ClienteDocumento))
            return BadRequest(new { erro = "CPF é obrigatório para gerar o Pix." });

        var valor = reserva.Valor / 2;
        var descricao = $"Reserva {loja.Nome} — {reserva.DataInicio:dd/MM} a {reserva.DataFim:dd/MM}";

        var resultado = await mpService.CriarPix(valor, descricao, reserva.ClienteEmail, reserva.ClienteDocumento, reserva.ClienteNome, Guid.NewGuid(), reserva.ExpiraEm);
        if (!resultado.Sucesso)
            return BadRequest(new { erro = resultado.Erro });

        reserva.MpPaymentId = resultado.MpPaymentId;
        reserva.MpStatusPix = resultado.Status;
        await db.SaveChangesAsync();

        return Ok(new { valor, qrCode = resultado.QrCode, qrCodeBase64 = resultado.QrCodeBase64 });
    }

    // ── Cobra no cartão (total parcelado, ou metade à vista no combinado) ──
    public record PagarCartaoRequest(string Token, int Parcelas);

    [HttpPost("reservas/{id:int}/pagamento/cartao")]
    [AllowAnonymous]
    public async Task<IActionResult> PagarCartao(string slug, int id, [FromBody] PagarCartaoRequest req)
    {
        var loja = await db.Lojas.FirstOrDefaultAsync(l => l.Slug == slug);
        if (loja is null) return NotFound(new { erro = "Página não encontrada." });

        var reserva = await db.Reservas.FirstOrDefaultAsync(r => r.Id == id && r.LojaId == loja.Id);
        if (reserva is null) return NotFound(new { erro = "Reserva não encontrada." });

        if (reserva.FormaPagamento != "cartao" && reserva.FormaPagamento != "combinado")
            return BadRequest(new { erro = "Escolha a forma de pagamento antes de cobrar o cartão." });

        if (string.IsNullOrWhiteSpace(reserva.ClienteDocumento))
            return BadRequest(new { erro = "CPF é obrigatório para pagar no cartão." });

        var ehCombinado = reserva.FormaPagamento == "combinado";
        var valorBase = ehCombinado ? reserva.Valor / 2 : reserva.Valor;
        var valor = ComMarkupCartao(valorBase);
        var parcelasPermitidas = ehCombinado ? 1 : ParcelasMaximas(reserva.Valor); // faixa baseada no valor real, sem o markup

        if (req.Parcelas < 1 || req.Parcelas > parcelasPermitidas)
            return BadRequest(new { erro = $"Para esse valor, o máximo é {parcelasPermitidas}x." });

        var descricao = $"Reserva {loja.Nome} — {reserva.DataInicio:dd/MM} a {reserva.DataFim:dd/MM}";

        var resultado = await mpService.CriarCartao(valor, descricao, req.Token, req.Parcelas, reserva.ClienteEmail, reserva.ClienteDocumento, reserva.ClienteNome, Guid.NewGuid());
        if (!resultado.Sucesso)
            return BadRequest(new { erro = resultado.Erro });

        reserva.MpPaymentIdCartao = resultado.MpPaymentId;
        reserva.MpStatusCartao = resultado.Status;
        await AtualizarConfirmacaoSeCompleto(reserva);
        await db.SaveChangesAsync();

        return Ok(new { status = resultado.Status, valor });
    }

    // ── Consulta status (polling) — confirma a reserva quando tudo que era necessário aprovar já aprovou ──
    [HttpGet("reservas/{id:int}/pagamento/status")]
    [AllowAnonymous]
    public async Task<IActionResult> StatusPagamento(string slug, int id)
    {
        var loja = await db.Lojas.FirstOrDefaultAsync(l => l.Slug == slug);
        if (loja is null) return NotFound(new { erro = "Página não encontrada." });

        var reserva = await db.Reservas.FirstOrDefaultAsync(r => r.Id == id && r.LojaId == loja.Id);
        if (reserva is null) return NotFound(new { erro = "Reserva não encontrada." });

        if (reserva.Status == "pendente_pagamento" && reserva.MpPaymentId != null && reserva.MpStatusPix != "approved")
        {
            var statusPix = await mpService.VerificarStatus(reserva.MpPaymentId);
            if (statusPix != null) reserva.MpStatusPix = statusPix;
        }
        if (reserva.Status == "pendente_pagamento" && reserva.MpPaymentIdCartao != null && reserva.MpStatusCartao != "approved")
        {
            var statusCartao = await mpService.VerificarStatus(reserva.MpPaymentIdCartao);
            if (statusCartao != null) reserva.MpStatusCartao = statusCartao;
        }

        var confirmouAgora = await AtualizarConfirmacaoSeCompleto(reserva);
        await db.SaveChangesAsync();

        if (confirmouAgora)
        {
            try { await notificacao.NotificarConfirmacaoAsync(reserva); } catch { /* já confirmado, e-mail é best-effort */ }
        }

        return Ok(new { reserva.Status, reserva.MpStatusPix, reserva.MpStatusCartao });
    }

    [HttpPost("reservas/{id:int}/cancelar")]
    [AllowAnonymous]
    public async Task<IActionResult> CancelarPeloCliente(string slug, int id)
    {
        var loja = await db.Lojas.FirstOrDefaultAsync(l => l.Slug == slug);
        if (loja is null) return NotFound(new { erro = "Página não encontrada." });

        var reserva = await db.Reservas.FirstOrDefaultAsync(r => r.Id == id && r.LojaId == loja.Id);
        if (reserva is null) return NotFound(new { erro = "Reserva não encontrada." });

        if (reserva.Status != "pendente_pagamento")
            return BadRequest(new { erro = "Essa reserva não pode mais ser cancelada." });

        reserva.Status = "cancelada";
        reserva.MotivoCancelamento = "Cliente desistiu";
        reserva.ExpiraEm = null;
        await db.SaveChangesAsync();

        return Ok(new { mensagem = "Reserva cancelada." });
    }

    // Confirma a reserva se as condições da forma de pagamento escolhida foram satisfeitas.
    // Retorna true só quando a confirmação acabou de acontecer agora (pra saber se manda e-mail).
    private async Task<bool> AtualizarConfirmacaoSeCompleto(Reserva reserva)
    {
        if (reserva.Status != "pendente_pagamento") return false;

        var pixOk = reserva.MpStatusPix == "approved";
        var cartaoOk = reserva.MpStatusCartao == "approved";

        var completo = reserva.FormaPagamento switch
        {
            "pix" => pixOk,
            "cartao" => cartaoOk,
            "combinado" => pixOk && cartaoOk,
            _ => false,
        };
        if (!completo) return false;

        // Pagamento chegou depois do prazo de 15min — a data pode já ter sido pega por outra
        // reserva nesse meio-tempo. Não confirma automaticamente nesse caso; fica pendente pra
        // você resolver manualmente (reagendar o cliente ou estornar o pagamento no Mercado Pago).
        if (reserva.ExpiraEm.HasValue && reserva.ExpiraEm.Value < DateTime.UtcNow)
        {
            var conflitaAgora = await db.Reservas.AnyAsync(r =>
                r.Id != reserva.Id && r.LojaId == reserva.LojaId &&
                (r.Status == "confirmada" || r.Status == "confirmada_parcial" || (r.Status == "pendente_pagamento" && (r.ExpiraEm == null || r.ExpiraEm > DateTime.UtcNow))) &&
                r.DataInicio <= reserva.DataFim && r.DataFim >= reserva.DataInicio);

            if (conflitaAgora) return false; // fica pendente — pagamento recebido, mas sem data livre
        }

        var ehSoPix = reserva.FormaPagamento == "pix";
        reserva.ValorPago = ehSoPix ? reserva.Valor / 2 : reserva.Valor;
        reserva.Status = ehSoPix ? "confirmada_parcial" : "confirmada"; // só o sinal ainda não é "confirmada" plena
        reserva.DataConfirmacao = DateTime.UtcNow;
        reserva.ExpiraEm = null;
        return true;
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