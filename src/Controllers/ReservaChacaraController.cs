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

    public record ConfirmarComPagamentoRequest(decimal? ValorPago);

    [HttpPatch("{id:int}/confirmar")]
    public async Task<IActionResult> Confirmar(int id, [FromBody] ConfirmarComPagamentoRequest? req)
    {
        var lojaId = await GetLojaId();
        var reserva = await db.Reservas.FirstOrDefaultAsync(r => r.Id == id && r.LojaId == lojaId);
        if (reserva is null) return NotFound();

        if (reserva.Status == "confirmada")
            return BadRequest(new { erro = "Esta reserva já está confirmada." });

        // Sem valor informado, assume pagamento integral (compatibilidade com o fluxo antigo)
        reserva.ValorPago = req?.ValorPago ?? reserva.Valor;
        reserva.Status = "confirmada";
        reserva.DataConfirmacao = DateTime.UtcNow;
        await db.SaveChangesAsync();

        await notificacao.NotificarConfirmacaoAsync(reserva);

        return Ok(new { reserva.Id, reserva.Status, reserva.ValorPago, saldoPendente = reserva.Valor - reserva.ValorPago });
    }

    [HttpPatch("{id:int}/manter-negociacao")]
    public async Task<IActionResult> ManterNegociacao(int id)
    {
        var lojaId = await GetLojaId();
        var reserva = await db.Reservas.FirstOrDefaultAsync(r => r.Id == id && r.LojaId == lojaId);
        if (reserva is null) return NotFound();

        if (reserva.Status != "pendente_pagamento")
            return BadRequest(new { erro = "Só é possível fazer isso em reservas pendentes de pagamento." });

        reserva.ExpiraEm = null; // não expira mais sozinha — só sai se você cancelar/excluir
        await db.SaveChangesAsync();

        return Ok(new { reserva.Id, reserva.ExpiraEm });
    }

    public record RegistrarPagamentoRequest(decimal Valor);

    [HttpPatch("{id:int}/registrar-pagamento")]
    public async Task<IActionResult> RegistrarPagamento(int id, [FromBody] RegistrarPagamentoRequest req)
    {
        var lojaId = await GetLojaId();
        var reserva = await db.Reservas.FirstOrDefaultAsync(r => r.Id == id && r.LojaId == lojaId);
        if (reserva is null) return NotFound();

        if (reserva.Status != "confirmada")
            return BadRequest(new { erro = "Só é possível registrar pagamento adicional em reservas já confirmadas." });

        if (req.Valor <= 0)
            return BadRequest(new { erro = "Informe um valor de pagamento maior que zero." });

        reserva.ValorPago = Math.Min(reserva.Valor, reserva.ValorPago + req.Valor);
        await db.SaveChangesAsync();

        return Ok(new { reserva.Id, reserva.ValorPago, saldoPendente = reserva.Valor - reserva.ValorPago, quitada = reserva.ValorPago >= reserva.Valor });
    }

    public record CriarReservaManualRequest(
        DateTime DataInicio, DateTime DataFim, int Pessoas,
        string ClienteNome, string? ClienteEmail, string? ClienteTelefone,
        decimal Valor, decimal? ValorPago
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
            (r.Status == "confirmada" || (r.Status == "pendente_pagamento" && (r.ExpiraEm == null || r.ExpiraEm > DateTime.UtcNow))) &&
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
            ValorPago = req.ValorPago ?? req.Valor, // sem informar, assume que já foi pago integralmente
            Status = "confirmada", // já fechado por fora, entra direto como confirmada, sem notificação
            DataConfirmacao = DateTime.UtcNow,
        };

        db.Reservas.Add(reserva);
        await db.SaveChangesAsync();

        return Ok(reserva);
    }

    public record EditarReservaRequest(
        DateTime DataInicio, DateTime DataFim, int Pessoas, string ClienteNome, string ClienteEmail, string ClienteTelefone,
        string? ClienteDocumento, string? ClienteCep, string? ClienteEndereco, decimal? ValorManual
    );

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
                return BadRequest(new { erro = "Esta chácara ainda não configurou os valores de reserva. Cadastre as faixas de preço antes de editar esta reserva." });

            var periodosEspeciais = await db.PeriodosEspeciaisChacara.Where(p => p.LojaId == lojaId).ToListAsync();

            try
            {
                var resultado = CalculadoraPrecoChacara.Calcular(ini, fim, req.Pessoas, cfg, faixas, periodosEspeciais);
                valorFinal = resultado.ValorTotal;
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { erro = ex.Message });
            }
        }

        // Guarda os valores "antes" pra comparar depois — só reenvia contrato se algo
        // que realmente aparece nele mudou (não precisa notificar por corrigir telefone/e-mail, etc.)
        var dataInicioAntes = reserva.DataInicio;
        var dataFimAntes = reserva.DataFim;
        var pessoasAntes = reserva.Pessoas;
        var valorAntes = reserva.Valor;

        reserva.DataInicio = ini;
        reserva.DataFim = fim;
        reserva.Pessoas = req.Pessoas;
        reserva.ClienteNome = req.ClienteNome.Trim();
        reserva.ClienteEmail = req.ClienteEmail.Trim();
        reserva.ClienteTelefone = new string(req.ClienteTelefone.Where(char.IsDigit).ToArray());
        reserva.ClienteDocumento = string.IsNullOrWhiteSpace(req.ClienteDocumento) ? reserva.ClienteDocumento : req.ClienteDocumento.Trim();
        reserva.ClienteCep = string.IsNullOrWhiteSpace(req.ClienteCep) ? reserva.ClienteCep : req.ClienteCep.Trim();
        reserva.ClienteEndereco = string.IsNullOrWhiteSpace(req.ClienteEndereco) ? reserva.ClienteEndereco : req.ClienteEndereco.Trim();
        reserva.Valor = valorFinal;

        var eraConfirmada = reserva.Status == "confirmada";
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

        return Ok(new { reserva, aviso = avisoNotificacao });
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

    [HttpPost("{id:int}/enviar-contrato")]
    public async Task<IActionResult> EnviarContrato(int id)
    {
        var lojaId = await GetLojaId();
        var reserva = await db.Reservas.FirstOrDefaultAsync(r => r.Id == id && r.LojaId == lojaId);
        if (reserva is null) return NotFound();

        if (string.IsNullOrWhiteSpace(reserva.ClienteEmail))
            return BadRequest(new { erro = "Esta reserva não tem e-mail de cliente cadastrado." });

        var enviado = await notificacao.EnviarContratoManualAsync(reserva);
        if (!enviado)
            return StatusCode(500, new { erro = "Não foi possível enviar o contrato. Confira os logs do servidor." });

        return Ok(new { mensagem = "Contrato enviado com sucesso." });
    }
}