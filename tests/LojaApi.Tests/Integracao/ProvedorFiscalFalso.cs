using LojaApi.Models;
using LojaApi.src.Models.Fiscal;
using LojaApi.src.Services.Fiscal;

namespace LojaApi.Tests.Integracao;

// Provedor fiscal de mentira (nada sai para a internet). Por padrão recusa, igual ao provedor "não configurado" de verdade;
// o teste troca o que ele responde com Responder(...) e chama Restaurar() no fim.
public sealed class ProvedorFiscalFalso : IProvedorFiscal
{
    private static readonly ResultadoEmissaoFiscal Recusa = new(false, "erro", MotivoErro: "Nenhum provedor de emissão fiscal está configurado para esta loja.");

    private ResultadoEmissaoFiscal _emitir = Recusa;
    private ResultadoEmissaoFiscal _cancelar = Recusa;

    public string Chave => "falso";

    public void Responder(ResultadoEmissaoFiscal emitir, ResultadoEmissaoFiscal cancelar) { _emitir = emitir; _cancelar = cancelar; }
    public void Restaurar() { _emitir = Recusa; _cancelar = Recusa; }

    public Task<ResultadoEmissaoFiscal> EmitirNfceAsync(Venda venda, ConfiguracaoFiscal configuracao, CancellationToken ct = default) => Task.FromResult(_emitir);
    public Task<ResultadoEmissaoFiscal> CancelarNfceAsync(EmissaoFiscal emissao, string justificativa, ConfiguracaoFiscal configuracao, CancellationToken ct = default) => Task.FromResult(_cancelar);
}
