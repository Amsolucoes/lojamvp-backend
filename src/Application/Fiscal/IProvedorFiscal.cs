using LojaApi.Models;
using LojaApi.src.Models.Fiscal;

namespace LojaApi.Application.Fiscal;

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

// Porta do provedor de emissão fiscal (Focus NFe, PlugNotas, eNotas etc.): trocar de provedor no futuro não mexe
// no service, no modelo de dados nem no resto da API. A implementação fica em Infrastructure/Fiscal.
public interface IProvedorFiscal
{
    string Chave { get; }

    Task<ResultadoEmissaoFiscal> EmitirNfceAsync(Venda venda, ConfiguracaoFiscal configuracao, CancellationToken ct = default);

    Task<ResultadoEmissaoFiscal> CancelarNfceAsync(EmissaoFiscal emissao, string justificativa, ConfiguracaoFiscal configuracao, CancellationToken ct = default);
}
