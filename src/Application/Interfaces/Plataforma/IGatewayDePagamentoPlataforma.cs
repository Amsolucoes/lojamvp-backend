namespace LojaApi.Application.Plataforma;

// Cobrança criada no Mercado Pago (Pix, boleto ou cartão). "Erro" preenchido = não deu certo.
public record CobrancaCriada(string? MpPaymentId, string? Status, string? QrCode, string? QrCodeBase64, string? BoletoUrl, string? BoletoBarcode, string? Erro)
{
    public bool Sucesso => Erro == null;
}

public record AssinaturaCriada(string? PreapprovalId, string? Status, string? Erro)
{
    public bool Sucesso => Erro == null;
}

// Porta: o Mercado Pago da mensalidade da plataforma. Implementada na Infrastructure com o MercadoPagoService.
public interface IGatewayDePagamentoPlataforma
{
    Task<CobrancaCriada> CriarPixAsync(decimal valor, string descricao, string email, string cpf, string nome, Guid pagamentoId);
    Task<CobrancaCriada> CriarBoletoAsync(decimal valor, string descricao, string email, string cpf, string nome, Guid pagamentoId);
    Task<CobrancaCriada> CriarCartaoAsync(decimal valor, string descricao, string cardToken, int parcelas, string email, string cpf, string nome, Guid pagamentoId);
    Task<string?> VerificarStatusAsync(string mpPaymentId);

    Task<AssinaturaCriada> CriarAssinaturaAsync(decimal valorMensal, string motivo, string cardToken, string email, DateTime inicio, string backUrl);
    Task<bool> CancelarAssinaturaAsync(string preapprovalId);
    Task<bool> AtualizarValorAssinaturaAsync(string preapprovalId, decimal novoValor);
    Task<string?> VerificarAssinaturaAsync(string preapprovalId);
    Task<(string? PreapprovalId, string? Status)> VerificarCobrancaAssinaturaAsync(string authorizedPaymentId);
}
