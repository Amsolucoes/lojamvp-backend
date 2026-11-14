using LojaApi.Domain.Acessorios;
using LojaApi.Shared.Application;
using LojaApi.src.Models;
using static LojaApi.Application.Acessorios.AcessoriosApoio;

namespace LojaApi.Application.Acessorios;

// Consulta o pagamento e, se o Pix caiu depois do prazo (pedido já cancelado), reativa o pedido quando ainda há estoque.
public class ConsultarStatusPedidoComReativacaoCommandHandler(IAcessoriosRepository repositorio, IPagamentoOnlineAcessorio pagamento) : ICommandHandler<ConsultarStatusPedidoComReativacaoCommand>
{
    public async Task<Resultado<object>> HandleAsync(ConsultarStatusPedidoComReativacaoCommand command)
    {
        var pedido = await repositorio.ObterPedidoComItensAsync(command.Id);
        if (pedido is null) return Resultado.NaoEncontrado();

        // Cobre tanto o pedido ainda esperando (dentro do prazo) quanto o que já expirou e foi
        // cancelado — o Pix pode cair no Mercado Pago minutos depois do prazo de 30min.
        if ((pedido.Status == StatusPedidoAcessorio.AguardandoPagamento || pedido.Status == StatusPedidoAcessorio.Cancelado) && pedido.MpPaymentId != null)
        {
            var statusAtual = await pagamento.VerificarStatus(pedido.MpPaymentId);
            if (statusAtual == "approved" && pedido.Status != StatusPedidoAcessorio.Pago)
            {
                if (pedido.Status == StatusPedidoAcessorio.Cancelado)
                {
                    // O estoque já foi devolvido quando expirou — só reserva de novo se ainda tiver
                    // disponível. Se alguém comprou esse estoque nesse meio-tempo, não reativa sozinho.
                    var semEstoque = new List<string>();
                    foreach (var item in pedido.Itens)
                    {
                        var produto = await repositorio.ObterProdutoAsync(item.ProdutoId);
                        if (produto is null || produto.Estoque < item.Quantidade)
                            semEstoque.Add(item.NomeProduto);
                    }

                    if (semEstoque.Count > 0)
                    {
                        // Não conseguiu revivir — fica cancelado mesmo, com o motivo pro suporte decidir
                        return Resultado.Ok(new { pedido.Status, aviso = $"Pagamento identificado, mas sem estoque suficiente para: {string.Join(", ", semEstoque)}. Fale com o suporte." });
                    }

                    foreach (var item in pedido.Itens)
                    {
                        var produto = await repositorio.ObterProdutoAsync(item.ProdutoId);
                        if (produto != null) produto.Estoque -= item.Quantidade;
                    }
                }

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
