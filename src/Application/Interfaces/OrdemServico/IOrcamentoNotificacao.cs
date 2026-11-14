namespace LojaApi.Application.OrdemServico;

// Porta: o que a ordem precisa de fora para falar com o cliente (e-mail e PDF do orçamento).
// Implementada em Infrastructure/OrdemServico (Resend e geração do PDF).
public interface IOrcamentoNotificacao
{
    Task<ResultadoEnvioOrcamentoDto> EnviarPorEmailAsync(Guid orcamentoId, Guid lojaId);
    Task<byte[]?> GerarPdfAsync(Guid orcamentoId, Guid lojaId);
}
