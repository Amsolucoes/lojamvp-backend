namespace LojaApi.Application.Chacara;

public record CredencialMpDto(bool Ok, string? Token, string? PublicKey);

public record ResultadoPagamentoOnlineDto(string? MpPaymentId, string? Status, string? QrCode, string? QrCodeBase64, string? Erro)
{
    public bool Sucesso => Erro == null;
}

public record ArquivoContratoDto(byte[] Pdf, string NomeArquivo);
