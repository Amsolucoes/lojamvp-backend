using LojaApi.Models;
using LojaApi.src.Models.Fiscal;

namespace LojaApi.src.Services.Fiscal;

// Implementação usada enquanto nenhum provedor terceirizado estiver contratado. Mantém a API
// de emissão fiscal pronta e funcional (sem 500/crash), só recusando a emissão de forma
// explícita até que um provedor real seja configurado em ConfiguracaoFiscal.
public class ProvedorFiscalNaoConfigurado : IProvedorFiscal
{
    public string Chave => "nao_configurado";

    private static readonly ResultadoEmissaoFiscal NaoConfigurado = new(
        Sucesso: false,
        Status: "erro",
        MotivoErro: "Nenhum provedor de emissão fiscal está configurado para esta loja.");

    public Task<ResultadoEmissaoFiscal> EmitirNfceAsync(Venda venda, ConfiguracaoFiscal configuracao, CancellationToken ct = default)
        => Task.FromResult(NaoConfigurado);

    public Task<ResultadoEmissaoFiscal> CancelarNfceAsync(EmissaoFiscal emissao, string justificativa, ConfiguracaoFiscal configuracao, CancellationToken ct = default)
        => Task.FromResult(NaoConfigurado);
}
