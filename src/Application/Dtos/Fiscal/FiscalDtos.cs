namespace LojaApi.Application.Fiscal;

public record ConfiguracaoFiscalDto(
    bool Ativo, string? Provedor, string Ambiente, string? InscricaoEstadual,
    string? RegimeTributario, string SerieNfce, int ProximoNumeroNfce, bool CredencialConfigurada);

public record EmissaoFiscalDto(
    Guid Id, Guid VendaId, string Tipo, string Status, string? Provedor,
    string? ChaveAcesso, string? NumeroNota, string? SerieNota, string? Protocolo,
    string? UrlDanfe, string? UrlXml, string? MotivoErro, DateTime SolicitadaEm, DateTime? ProcessadaEm);

public record ResultadoEmissaoFiscalDto(
    bool Sucesso,
    string Status, // autorizada | rejeitada | processando | erro
    string? ChaveAcesso = null,
    string? NumeroNota = null,
    string? SerieNota = null,
    string? Protocolo = null,
    string? UrlDanfe = null,
    string? UrlXml = null,
    string? MotivoErro = null
);
