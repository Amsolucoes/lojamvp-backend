using LojaApi.Domain.Acessorios;
using LojaApi.Shared.Application;
using LojaApi.src.Models;
using static LojaApi.Application.Acessorios.AcessoriosApoio;

namespace LojaApi.Application.Acessorios;

// O cliente consulta o status do pagamento (polling na tela de checkout). Só confere quem ainda está aguardando.
public class ConsultarStatusPedidoCommandHandler(IAcessoriosRepository repositorio, IPagamentoOnlineAcessorio pagamento) : ICommandHandler<ConsultarStatusPedidoCommand>
{
    public async Task<Resultado<object>> HandleAsync(ConsultarStatusPedidoCommand command)
    {
        var pedido = await repositorio.ObterPedidoAsync(command.Id);
        if (pedido is null) return Resultado.NaoEncontrado();

        if (pedido.Status == StatusPedidoAcessorio.AguardandoPagamento && pedido.MpPaymentId != null)
        {
            var statusAtual = await pagamento.VerificarStatus(pedido.MpPaymentId);
            if (statusAtual == "approved" && pedido.Status != StatusPedidoAcessorio.Pago)
            {
                pedido.Status = StatusPedidoAcessorio.Pago;
                pedido.PagoEm = DateTime.UtcNow;
                pedido.MpStatus = statusAtual;
                pedido.ExpiraEm = null;
                await repositorio.SalvarAsync();
            }
        }

        return Resultado.Ok(new { pedido.Status });
    }
}
