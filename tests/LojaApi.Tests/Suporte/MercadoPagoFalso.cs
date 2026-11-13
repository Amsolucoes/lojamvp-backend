using System.Collections.Concurrent;
using LojaApi.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace LojaApi.Tests.Suporte;

// Mercado Pago "de mentira" para os testes: não chama a internet e deixa o teste decidir o que o MP responde.
//  - Access Token que começa com "APP_USR" é aceito; qualquer outro é recusado.
//  - Pix nasce "pending" até o teste chamar Aprovar(...).
//  - Cartão é aprovado na hora, a não ser que o token do cartão seja "cartao-recusado".
//  - FalharPix = true faz o Pix voltar com erro.
public sealed class MercadoPagoFalso() : MercadoPagoService(new ConfigurationBuilder().Build(), NullLogger<MercadoPagoService>.Instance)
{
    private readonly ConcurrentDictionary<string, string> _status = new();

    public ConcurrentQueue<(decimal Valor, string TokenDaLoja)> PixCriados { get; } = new();
    public ConcurrentQueue<(decimal Valor, int Parcelas, string TokenDaLoja)> CobrancasDeCartao { get; } = new();
    public ConcurrentQueue<(string PagamentoId, string TokenDaLoja)> StatusConsultados { get; } = new();

    // Quando ligado, o próximo Pix volta com erro (para testar o que a API faz se o Mercado Pago falhar)
    public bool FalharPix { get; set; }

    public void Aprovar(string mpPaymentId) => _status[mpPaymentId] = "approved";

    public override Task<(bool ok, string? email)> ValidarToken(string accessToken) =>
        Task.FromResult(accessToken.StartsWith("APP_USR") ? (true, (string?)"conta@mp.test") : (false, (string?)null));

    public override Task<MpPaymentResult> CriarPix(decimal valor, string descricao, string emailPagador, string cpfPagador,
        string nomePagador, Guid pagamentoId, DateTime? expiraEm = null, string? accessToken = null)
    {
        if (FalharPix) return Task.FromResult(new MpPaymentResult { Erro = "Mercado Pago fora do ar." });

        var id = "pix-" + Guid.NewGuid().ToString("N");
        _status[id] = "pending";
        PixCriados.Enqueue((valor, accessToken ?? ""));
        return Task.FromResult(new MpPaymentResult { MpPaymentId = id, Status = "pending", QrCode = "000201-qr-de-teste", QrCodeBase64 = "QkFTRTY0" });
    }

    public override Task<MpPaymentResult> CriarCartao(decimal valor, string descricao, string cardToken, int parcelas,
        string emailPagador, string cpfPagador, string nomePagador, Guid pagamentoId, string? accessToken = null)
    {
        CobrancasDeCartao.Enqueue((valor, parcelas, accessToken ?? ""));
        if (cardToken == "cartao-recusado")
            return Task.FromResult(new MpPaymentResult { Erro = "Cartão recusado pelo banco emissor." });

        var id = "cartao-" + Guid.NewGuid().ToString("N");
        _status[id] = "approved";
        return Task.FromResult(new MpPaymentResult { MpPaymentId = id, Status = "approved" });
    }

    public override Task<string?> VerificarStatus(string mpPaymentId, string? accessToken = null)
    {
        StatusConsultados.Enqueue((mpPaymentId, accessToken ?? ""));
        return Task.FromResult(_status.TryGetValue(mpPaymentId, out var s) ? s : null);
    }
}
