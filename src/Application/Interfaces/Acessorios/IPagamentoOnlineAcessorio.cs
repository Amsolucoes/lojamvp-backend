namespace LojaApi.Application.Acessorios;

// PORTA: o pagamento online (Mercado Pago) da loja de acessórios, descrito como interface. Quem implementa mora na API
// (Infrastructure/Acessorios) e os testes trocam por um falso.
public interface IPagamentoOnlineAcessorio
{
    Task<ResultadoPagamentoAcessorioDto> CriarPix(decimal valor, string descricao, string emailPagador, string cpfPagador, string nomePagador, Guid pagamentoId);

    Task<ResultadoPagamentoAcessorioDto> CriarCartao(decimal valor, string descricao, string cardToken, int parcelas,
        string emailPagador, string cpfPagador, string nomePagador, Guid pagamentoId);

    Task<string?> VerificarStatus(string mpPaymentId);
}
