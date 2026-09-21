using LojaApi.Models;
using LojaApi.src.Models.Fiscal;

namespace LojaApi.src.Services.Fiscal;

public record ResultadoEmissaoFiscal(
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

// Abstração do provedor de emissão fiscal (Focus NFe, PlugNotas, eNotas etc.), pra trocar de
// provedor no futuro sem alterar controller, modelo de dados ou o resto da API.
public interface IProvedorFiscal
{
    string Chave { get; }

    Task<ResultadoEmissaoFiscal> EmitirNfceAsync(Venda venda, ConfiguracaoFiscal configuracao, CancellationToken ct = default);

    Task<ResultadoEmissaoFiscal> CancelarNfceAsync(EmissaoFiscal emissao, string justificativa, ConfiguracaoFiscal configuracao, CancellationToken ct = default);
}
