using LojaApi.Domain.Chacara;
using LojaApi.Models;

namespace LojaApi.Application.Chacara;

// PORTA: o que o Application precisa do mundo externo, descrito como interface. Quem implementa mora no projeto da API
// (Infrastructure) e é ligado no registro do módulo. Assim o Application não depende de nenhum serviço externo e dá para
// trocar/simular cada um (os testes usam um Mercado Pago de mentira).

public interface IPagamentoOnlineChacara
{
    Task<ResultadoPagamentoOnlineDto> CriarPix(decimal valor, string descricao, string emailPagador, string cpfPagador,
        string nomePagador, Guid pagamentoId, DateTime? expiraEm, string? accessToken);

    Task<ResultadoPagamentoOnlineDto> CriarCartao(decimal valor, string descricao, string cardToken, int parcelas,
        string emailPagador, string cpfPagador, string nomePagador, Guid pagamentoId, string? accessToken);

    Task<(bool ok, string? email)> ValidarToken(string accessToken);

    Task<string?> VerificarStatus(string mpPaymentId, string? accessToken);
}
