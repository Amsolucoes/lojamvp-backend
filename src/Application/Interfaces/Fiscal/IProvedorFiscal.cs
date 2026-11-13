using LojaApi.Models;
using LojaApi.src.Models.Fiscal;

namespace LojaApi.Application.Fiscal;

// Porta do provedor de emissão fiscal (Focus NFe, PlugNotas, eNotas etc.): trocar de provedor no futuro não mexe
// no service, no modelo de dados nem no resto da API. A implementação fica em Infrastructure/Fiscal.
public interface IProvedorFiscal
{
    string Chave { get; }

    Task<ResultadoEmissaoFiscalDto> EmitirNfceAsync(Venda venda, ConfiguracaoFiscal configuracao, CancellationToken ct = default);

    Task<ResultadoEmissaoFiscalDto> CancelarNfceAsync(EmissaoFiscal emissao, string justificativa, ConfiguracaoFiscal configuracao, CancellationToken ct = default);
}
