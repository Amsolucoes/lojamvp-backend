namespace LojaApi.Application.Fiscal;

public record SalvarConfiguracaoFiscalRequest(
    string? Provedor, string Ambiente, string? CredencialToken,
    string? InscricaoEstadual, string? RegimeTributario, string SerieNfce);

public record CancelarEmissaoRequest(string Justificativa);
