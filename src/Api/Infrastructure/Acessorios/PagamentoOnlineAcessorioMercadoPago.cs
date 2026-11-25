using LojaApi.Application.Acessorios;
using LojaApi.Services;

namespace LojaApi.Infrastructure.Acessorios;

// Implementa a porta do pagamento online da loja de acessórios com o Mercado Pago da plataforma.
public class PagamentoOnlineAcessorioMercadoPago(MercadoPagoService mp) : IPagamentoOnlineAcessorio
{
    private static ResultadoPagamentoAcessorioDto Converter(MpPaymentResult r) =>
        new(r.MpPaymentId, r.Status, r.QrCode, r.QrCodeBase64, r.Erro);

    public async Task<ResultadoPagamentoAcessorioDto> CriarPix(decimal valor, string descricao, string emailPagador, string cpfPagador, string nomePagador, Guid pagamentoId) =>
        Converter(await mp.CriarPix(valor: valor, descricao: descricao, emailPagador: emailPagador, cpfPagador: cpfPagador, nomePagador: nomePagador, pagamentoId: pagamentoId));

    public async Task<ResultadoPagamentoAcessorioDto> CriarCartao(decimal valor, string descricao, string cardToken, int parcelas,
        string emailPagador, string cpfPagador, string nomePagador, Guid pagamentoId) =>
        Converter(await mp.CriarCartao(valor: valor, descricao: descricao, cardToken: cardToken, parcelas: parcelas, emailPagador: emailPagador,
            cpfPagador: cpfPagador, nomePagador: nomePagador, pagamentoId: pagamentoId));

    public Task<string?> VerificarStatus(string mpPaymentId) => mp.VerificarStatus(mpPaymentId);
}
