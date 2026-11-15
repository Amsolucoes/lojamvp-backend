using LojaApi.Shared.Application;

namespace LojaApi.Application.Plataforma;

// Cancela a assinatura recorrente da loja no Mercado Pago.
public class CancelarAssinaturaCommandHandler(IPlataformaRepository repositorio, IGatewayDePagamentoPlataforma gateway) : ICommandHandler<CancelarAssinaturaCommand>
{
    public async Task<Resultado<object>> HandleAsync(CancelarAssinaturaCommand command)
    {
        if (command.LojaId is not { } lojaId) return Resultado.Invalido("Loja não encontrada.");

        var loja = await repositorio.ObterLojaAsync(lojaId);
        if (loja is null || loja.MpPreapprovalId is null)
            return Resultado.Invalido("Nenhuma assinatura ativa.");

        if (!await gateway.CancelarAssinaturaAsync(loja.MpPreapprovalId))
            return Resultado.Invalido("Erro ao cancelar no Mercado Pago.");

        loja.AssinaturaStatus = "cancelled";
        loja.AtualizadoEm = DateTime.UtcNow;
        await repositorio.SalvarAsync();

        return Resultado.Ok(new StatusDto("cancelled"));
    }
}
