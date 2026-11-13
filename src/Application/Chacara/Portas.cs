using LojaApi.Domain.Chacara;
using LojaApi.Models;

namespace LojaApi.Application.Chacara;

// PORTAS: o que o Application precisa do mundo externo, descrito como interface. Quem implementa (Mercado Pago, criptografia,
// gerador de PDF) mora no projeto da API (Infrastructure) e é ligado no registro do módulo. Assim o Application não depende
// de nenhum serviço externo e dá para trocar/simular cada um (os testes usam um Mercado Pago de mentira).

public record ResultadoPagamentoOnline(string? MpPaymentId, string? Status, string? QrCode, string? QrCodeBase64, string? Erro)
{
    public bool Sucesso => Erro == null;
}

public interface IPagamentoOnlineChacara
{
    Task<ResultadoPagamentoOnline> CriarPix(decimal valor, string descricao, string emailPagador, string cpfPagador,
        string nomePagador, Guid pagamentoId, DateTime? expiraEm, string? accessToken);

    Task<ResultadoPagamentoOnline> CriarCartao(decimal valor, string descricao, string cardToken, int parcelas,
        string emailPagador, string cpfPagador, string nomePagador, Guid pagamentoId, string? accessToken);

    Task<(bool ok, string? email)> ValidarToken(string accessToken);

    Task<string?> VerificarStatus(string mpPaymentId, string? accessToken);
}

public interface IProtetorDeSegredos
{
    string Cifrar(string texto);
    string? Decifrar(string? cifrado);
}

public interface IGeradorContratoChacara
{
    byte[] Gerar(Reserva reserva, Loja loja, InfoChacara? info, ConfiguracaoPrecoChacara cfg);
}
