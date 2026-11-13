using LojaApi.Application.Chacara;
using LojaApi.Domain.Chacara;
using LojaApi.Models;
using LojaApi.Services;

namespace LojaApi.Infrastructure.Chacara;

// Implementações das portas do Application (ver Application/Chacara/Portas.cs).

public class PagamentoOnlineChacaraMercadoPago(MercadoPagoService mp) : IPagamentoOnlineChacara
{
    private static ResultadoPagamentoOnline Converter(MpPaymentResult r) =>
        new(r.MpPaymentId, r.Status, r.QrCode, r.QrCodeBase64, r.Erro);

    public async Task<ResultadoPagamentoOnline> CriarPix(decimal valor, string descricao, string emailPagador, string cpfPagador,
        string nomePagador, Guid pagamentoId, DateTime? expiraEm, string? accessToken) =>
        Converter(await mp.CriarPix(valor, descricao, emailPagador, cpfPagador, nomePagador, pagamentoId, expiraEm, accessToken));

    public async Task<ResultadoPagamentoOnline> CriarCartao(decimal valor, string descricao, string cardToken, int parcelas,
        string emailPagador, string cpfPagador, string nomePagador, Guid pagamentoId, string? accessToken) =>
        Converter(await mp.CriarCartao(valor, descricao, cardToken, parcelas, emailPagador, cpfPagador, nomePagador, pagamentoId, accessToken));

    public Task<(bool ok, string? email)> ValidarToken(string accessToken) => mp.ValidarToken(accessToken);

    public Task<string?> VerificarStatus(string mpPaymentId, string? accessToken) => mp.VerificarStatus(mpPaymentId, accessToken);
}

public class ProtetorDeSegredosConfiguracao(IConfiguration config) : IProtetorDeSegredos
{
    public string Cifrar(string texto) => SegredoService.Cifrar(texto, config);
    public string? Decifrar(string? cifrado) => SegredoService.Decifrar(cifrado, config);
}

public class GeradorContratoChacaraQuestPdf : IGeradorContratoChacara
{
    public byte[] Gerar(Reserva reserva, Loja loja, InfoChacara? info, ConfiguracaoPrecoChacara cfg) =>
        ContratoChacaraService.GerarContratoPdf(reserva, loja, info, cfg);
}
