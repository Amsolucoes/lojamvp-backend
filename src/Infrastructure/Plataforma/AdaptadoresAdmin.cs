using LojaApi.Application.Plataforma;
using LojaApi.Services;

namespace LojaApi.Infrastructure.Plataforma;

// Liga as portas da cobrança e dos comunicados aos serviços da API (Mercado Pago e e-mail).

public class GatewayDePagamentoPlataformaAdaptador(MercadoPagoService mp) : IGatewayDePagamentoPlataforma
{
    private static CobrancaCriada Converter(MpPaymentResult r) =>
        new(r.MpPaymentId, r.Status, r.QrCode, r.QrCodeBase64, r.BoletoUrl, r.BoletoBarcode, r.Erro);

    public async Task<CobrancaCriada> CriarPixAsync(decimal valor, string descricao, string email, string cpf, string nome, Guid pagamentoId) =>
        Converter(await mp.CriarPix(valor, descricao, email, cpf, nome, pagamentoId));

    public async Task<CobrancaCriada> CriarBoletoAsync(decimal valor, string descricao, string email, string cpf, string nome, Guid pagamentoId) =>
        Converter(await mp.CriarBoleto(valor, descricao, email, cpf, nome, pagamentoId));

    public async Task<CobrancaCriada> CriarCartaoAsync(decimal valor, string descricao, string cardToken, int parcelas, string email, string cpf, string nome, Guid pagamentoId) =>
        Converter(await mp.CriarCartao(valor, descricao, cardToken, parcelas, email, cpf, nome, pagamentoId));

    public Task<string?> VerificarStatusAsync(string mpPaymentId) => mp.VerificarStatus(mpPaymentId);

    public async Task<AssinaturaCriada> CriarAssinaturaAsync(decimal valorMensal, string motivo, string cardToken, string email, DateTime inicio, string backUrl)
    {
        var r = await mp.CriarAssinatura(valorMensal, motivo, cardToken, email, inicio, backUrl);
        return new AssinaturaCriada(r.PreapprovalId, r.Status, r.Erro);
    }

    public Task<bool> CancelarAssinaturaAsync(string preapprovalId) => mp.CancelarAssinatura(preapprovalId);
    public Task<bool> AtualizarValorAssinaturaAsync(string preapprovalId, decimal novoValor) => mp.AtualizarValorAssinatura(preapprovalId, novoValor);
    public Task<string?> VerificarAssinaturaAsync(string preapprovalId) => mp.VerificarAssinatura(preapprovalId);
    public Task<(string? PreapprovalId, string? Status)> VerificarCobrancaAssinaturaAsync(string authorizedPaymentId) => mp.VerificarCobrancaAssinatura(authorizedPaymentId);
}

public class EnvioDeComunicadosAdaptador(ComunicadoEmailService servico) : IEnvioDeComunicados
{
    public async Task<ResultadoComunicado> EnviarAsync(List<Guid>? lojaIds, bool todasLojas, string assunto, string corpoHtml, List<string>? emailsExtras)
    {
        var r = await servico.EnviarAsync(lojaIds, todasLojas, assunto, corpoHtml, emailsExtras);
        return new ResultadoComunicado(r.TotalEnviados, r.TotalFalhas, r.Falhas);
    }
}
