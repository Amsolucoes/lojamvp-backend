using LojaApi.Shared.Application;
using Microsoft.Extensions.Logging;

namespace LojaApi.Application.Plataforma;

// Status de uma fatura. Se há uma cobrança no Mercado Pago ainda não paga, confere lá e, se foi aprovada, registra o pagamento.
// O assinante só vê as faturas da própria loja (o superadmin, qualquer uma).
public class ConsultarStatusPagamentoCommandHandler(IPlataformaRepository repositorio, IGatewayDePagamentoPlataforma gateway, ILogger<ConsultarStatusPagamentoCommandHandler> logger) : ICommandHandler<ConsultarStatusPagamentoCommand>
{
    public async Task<Resultado<object>> HandleAsync(ConsultarStatusPagamentoCommand command)
    {
        var (id, lojaId, ehSuperadmin) = command;
        var pagamento = await repositorio.ObterPagamentoAsync(id);
        if (pagamento is null || (!ehSuperadmin && pagamento.LojaId != lojaId)) return Resultado.NaoEncontrado();

        if (pagamento.MpPaymentId != null && pagamento.Status != "pago")
        {
            var status = await gateway.VerificarStatusAsync(pagamento.MpPaymentId);
            if (status == "approved")
                await PlataformaApoio.RegistrarPagamentoAsync(repositorio, logger, pagamento.LojaId, pagamento.Valor, pagamento.Vencimento,
                    DateTime.UtcNow, pagamento.FormaPagamento ?? "pix", null, null, pagamento.MpPaymentId);
        }

        var atualizado = await repositorio.ObterPagamentoAsync(id);
        return Resultado.Ok(new StatusDto(atualizado?.Status));
    }
}
