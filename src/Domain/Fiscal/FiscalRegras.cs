namespace LojaApi.Domain.Fiscal;

public static class StatusEmissaoFiscal
{
    public const string Processando = "processando";
    public const string Autorizada = "autorizada";
    public const string Cancelada = "cancelada";
}

// Regras da emissão fiscal (NFC-e) que não dependem de banco nem do provedor.
public static class FiscalRegras
{
    public const string ModuloNfce = "nfce";
    public const int MinimoCaracteresJustificativa = 15;

    public static bool AmbienteValido(string? ambiente) => ambiente is "homologacao" or "producao";

    // Série em branco usa a 1
    public static string SerieOuPadrao(string? serie) => string.IsNullOrWhiteSpace(serie) ? "1" : serie.Trim();

    // Só fica "ativo" quando houver de fato um provedor e uma credencial configurados;
    // enquanto isso, a emissão continua bloqueada com uma mensagem clara.
    public static bool ConfiguracaoAtiva(string? provedor, string? credencial) =>
        !string.IsNullOrWhiteSpace(provedor) && !string.IsNullOrWhiteSpace(credencial);

    // O módulo vem numa lista separada por vírgula ("chacara_reservas,nfce")
    public static bool ModuloAtivo(string? modulosAtivos, string modulo) =>
        (modulosAtivos ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Contains(modulo);

    public static bool JustificativaValida(string? justificativa) =>
        !string.IsNullOrWhiteSpace(justificativa) && justificativa.Trim().Length >= MinimoCaracteresJustificativa;

    public static bool PodeCancelar(string statusDaNota) => statusDaNota == StatusEmissaoFiscal.Autorizada;
}
