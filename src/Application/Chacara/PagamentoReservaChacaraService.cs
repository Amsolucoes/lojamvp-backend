using LojaApi.Data;
using LojaApi.Domain.Chacara;
using LojaApi.Services;
using LojaApi.Shared.Application;
using Microsoft.EntityFrameworkCore;

namespace LojaApi.Application.Chacara;

public record EscolherPagamentoRequest(string FormaPagamento); // pix | cartao | combinado
public record PagarCartaoRequest(string Token, int Parcelas);

// Pagamento online da reserva (Mercado Pago): escolha da forma, Pix, cartão, acompanhamento e cancelamento pelo cliente.
public class PagamentoReservaChacaraService(
    AppDbContext db,
    CredencialMercadoPagoChacaraService credencialMp,
    ReservaChacaraNotificacaoService notificacao,
    MercadoPagoService mpService)
{
    private const string PaginaNaoEncontrada = "Página não encontrada.";
    private const string ReservaNaoEncontrada = "Reserva não encontrada.";

    private Task<LojaApi.Models.Loja?> BuscarLojaAsync(string slug) =>
        db.Lojas.FirstOrDefaultAsync(l => l.Slug == slug);

    private Task<Reserva?> BuscarReservaAsync(Guid lojaId, int id) =>
        db.Reservas.FirstOrDefaultAsync(r => r.Id == id && r.LojaId == lojaId);

    private async Task<ConfiguracaoPrecoChacara> CfgAsync(Guid lojaId) =>
        await db.ConfiguracoesPrecoChacara.AsNoTracking().FirstOrDefaultAsync(c => c.LojaId == lojaId)
        ?? new ConfiguracaoPrecoChacara { LojaId = lojaId };

    // Escolhe a forma de pagamento (define o que os endpoints seguintes vão cobrar)
    public async Task<Resultado<object>> EscolherAsync(string slug, int id, EscolherPagamentoRequest req)
    {
        var loja = await BuscarLojaAsync(slug);
        if (loja is null) return Resultado.NaoEncontrado(PaginaNaoEncontrada);

        var reserva = await BuscarReservaAsync(loja.Id, id);
        if (reserva is null) return Resultado.NaoEncontrado(ReservaNaoEncontrada);

        var erro = reserva.ValidarEscolhaDePagamento(req.FormaPagamento);
        if (erro != null) return Resultado.Invalido(erro);

        var cred = await credencialMp.ObterAsync(loja.Id);
        if (!cred.Ok)
            return Resultado.Invalido("O pagamento online ainda não está configurado para esta chácara. Entre em contato com o responsável.");

        reserva.DefinirFormaDePagamento(req.FormaPagamento);
        await db.SaveChangesAsync();

        var valorPix = req.FormaPagamento is FormaPagamentoReserva.Pix or FormaPagamentoReserva.Combinado ? reserva.Valor / 2 : 0;
        var cfgCartao = await CfgAsync(loja.Id);
        var parcelasMax = req.FormaPagamento == FormaPagamentoReserva.Cartao ? cfgCartao.ParcelasMaximas(reserva.Valor) : 1;
        var valorCartao = req.FormaPagamento == FormaPagamentoReserva.Cartao ? cfgCartao.ComTaxaDoCartao(reserva.Valor)
                         : req.FormaPagamento == FormaPagamentoReserva.Combinado ? cfgCartao.ComTaxaDoCartao(reserva.Valor / 2) : 0;

        return Resultado.Ok(new { valorPix, valorCartao, parcelasMax, mpPublicKey = cred.PublicKey });
    }

    // Gera o Pix (sinal de 50%, ou metade do combinado)
    public async Task<Resultado<object>> PagarPixAsync(string slug, int id)
    {
        var loja = await BuscarLojaAsync(slug);
        if (loja is null) return Resultado.NaoEncontrado(PaginaNaoEncontrada);

        var reserva = await BuscarReservaAsync(loja.Id, id);
        if (reserva is null) return Resultado.NaoEncontrado(ReservaNaoEncontrada);

        if (reserva.FormaPagamento != FormaPagamentoReserva.Pix && reserva.FormaPagamento != FormaPagamentoReserva.Combinado)
            return Resultado.Invalido("Escolha a forma de pagamento antes de gerar o Pix.");

        if (string.IsNullOrWhiteSpace(reserva.ClienteDocumento))
            return Resultado.Invalido("CPF é obrigatório para gerar o Pix.");

        var credPix = await credencialMp.ObterAsync(loja.Id);
        if (!credPix.Ok)
            return Resultado.Invalido("O pagamento online ainda não está configurado para esta chácara.");

        var valor = reserva.Valor / 2;
        var descricao = $"Reserva {loja.Nome} — {reserva.DataInicio:dd/MM} a {reserva.DataFim:dd/MM}";

        var resultado = await mpService.CriarPix(valor, descricao, reserva.ClienteEmail, reserva.ClienteDocumento, reserva.ClienteNome, Guid.NewGuid(), reserva.ExpiraEm, credPix.Token);
        if (!resultado.Sucesso)
            return Resultado.Invalido(resultado.Erro!);

        reserva.MpPaymentId = resultado.MpPaymentId;
        reserva.MpStatusPix = resultado.Status;
        await db.SaveChangesAsync();

        return Resultado.Ok(new { valor, qrCode = resultado.QrCode, qrCodeBase64 = resultado.QrCodeBase64 });
    }

    // Cobra no cartão (total parcelado, ou metade à vista no combinado)
    public async Task<Resultado<object>> PagarCartaoAsync(string slug, int id, PagarCartaoRequest req)
    {
        var loja = await BuscarLojaAsync(slug);
        if (loja is null) return Resultado.NaoEncontrado(PaginaNaoEncontrada);

        var reserva = await BuscarReservaAsync(loja.Id, id);
        if (reserva is null) return Resultado.NaoEncontrado(ReservaNaoEncontrada);

        if (reserva.FormaPagamento != FormaPagamentoReserva.Cartao && reserva.FormaPagamento != FormaPagamentoReserva.Combinado)
            return Resultado.Invalido("Escolha a forma de pagamento antes de cobrar o cartão.");

        if (string.IsNullOrWhiteSpace(reserva.ClienteDocumento))
            return Resultado.Invalido("CPF é obrigatório para pagar no cartão.");

        var ehCombinado = reserva.FormaPagamento == FormaPagamentoReserva.Combinado;
        var valorBase = ehCombinado ? reserva.Valor / 2 : reserva.Valor;
        var cfgCartao = await CfgAsync(loja.Id);
        var valor = cfgCartao.ComTaxaDoCartao(valorBase);
        var parcelasPermitidas = ehCombinado ? 1 : cfgCartao.ParcelasMaximas(reserva.Valor); // faixa baseada no valor real, sem a taxa
        if (req.Parcelas < 1 || req.Parcelas > parcelasPermitidas)
            return Resultado.Invalido($"Para esse valor, o máximo é {parcelasPermitidas}x.");

        var descricao = $"Reserva {loja.Nome} — {reserva.DataInicio:dd/MM} a {reserva.DataFim:dd/MM}";

        var credCartao = await credencialMp.ObterAsync(loja.Id);
        if (!credCartao.Ok)
            return Resultado.Invalido("O pagamento online ainda não está configurado para esta chácara.");

        var resultado = await mpService.CriarCartao(valor, descricao, req.Token, req.Parcelas, reserva.ClienteEmail, reserva.ClienteDocumento, reserva.ClienteNome, Guid.NewGuid(), credCartao.Token);
        if (!resultado.Sucesso)
            return Resultado.Invalido(resultado.Erro!);

        reserva.MpPaymentIdCartao = resultado.MpPaymentId;
        reserva.MpStatusCartao = resultado.Status;

        await ConfirmarSePagamentoCompletoAsync(reserva);
        await db.SaveChangesAsync();

        return Resultado.Ok(new { status = resultado.Status, valor });
    }

    // Consulta o status (polling) — confirma a reserva quando tudo que era necessário aprovar já aprovou
    public async Task<Resultado<object>> StatusAsync(string slug, int id)
    {
        var loja = await BuscarLojaAsync(slug);
        if (loja is null) return Resultado.NaoEncontrado(PaginaNaoEncontrada);

        var reserva = await BuscarReservaAsync(loja.Id, id);
        if (reserva is null) return Resultado.NaoEncontrado(ReservaNaoEncontrada);

        var credStatus = await credencialMp.ObterAsync(loja.Id);

        if (reserva.Status == StatusReserva.PendentePagamento && reserva.MpPaymentId != null && reserva.MpStatusPix != "approved")
        {
            var statusPix = await mpService.VerificarStatus(reserva.MpPaymentId, credStatus.Token);
            if (statusPix != null) reserva.MpStatusPix = statusPix;
        }

        if (reserva.Status == StatusReserva.PendentePagamento && reserva.MpPaymentIdCartao != null && reserva.MpStatusCartao != "approved")
        {
            var statusCartao = await mpService.VerificarStatus(reserva.MpPaymentIdCartao, credStatus.Token);
            if (statusCartao != null) reserva.MpStatusCartao = statusCartao;
        }

        var confirmouAgora = await ConfirmarSePagamentoCompletoAsync(reserva);
        await db.SaveChangesAsync();

        if (confirmouAgora)
        {
            try { await notificacao.NotificarConfirmacaoAsync(reserva); } catch { /* já confirmado, e-mail é best-effort */ }
        }

        return Resultado.Ok(new { reserva.Status, reserva.MpStatusPix, reserva.MpStatusCartao });
    }

    public async Task<Resultado<object>> CancelarPeloClienteAsync(string slug, int id)
    {
        var loja = await BuscarLojaAsync(slug);
        if (loja is null) return Resultado.NaoEncontrado(PaginaNaoEncontrada);

        var reserva = await BuscarReservaAsync(loja.Id, id);
        if (reserva is null) return Resultado.NaoEncontrado(ReservaNaoEncontrada);

        var erro = reserva.CancelarPeloCliente();
        if (erro != null) return Resultado.Invalido(erro);

        await db.SaveChangesAsync();
        return Resultado.Ok(new { mensagem = "Reserva cancelada." });
    }

    // Confirma a reserva se as condições da forma de pagamento escolhida foram satisfeitas.
    // Retorna true só quando a confirmação acabou de acontecer agora (pra saber se manda e-mail).
    private async Task<bool> ConfirmarSePagamentoCompletoAsync(Reserva reserva)
    {
        if (reserva.Status != StatusReserva.PendentePagamento) return false;
        if (!reserva.PagamentoOnlineAprovado()) return false;

        var agora = DateTime.UtcNow;

        // Pagamento chegou depois do prazo de 15min — a data pode já ter sido pega por outra
        // reserva nesse meio-tempo. Não confirma automaticamente nesse caso; fica pendente pra
        // você resolver manualmente (reagendar o cliente ou estornar o pagamento no Mercado Pago).
        if (reserva.PrazoVencido(agora))
        {
            var conflitaAgora = await db.Reservas
                .Where(r => r.Id != reserva.Id && r.LojaId == reserva.LojaId)
                .Where(RegrasAgenda.OcupaAAgenda(agora))
                .Where(RegrasAgenda.SobrepoeOPeriodo(reserva.DataInicio, reserva.DataFim))
                .AnyAsync();
            if (conflitaAgora) return false; // fica pendente — pagamento recebido, mas sem data livre
        }

        reserva.ConfirmarPorPagamentoOnline(agora);
        return true;
    }
}
