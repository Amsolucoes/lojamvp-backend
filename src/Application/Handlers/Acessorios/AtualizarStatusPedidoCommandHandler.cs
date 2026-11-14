using LojaApi.Domain.Acessorios;
using LojaApi.Shared.Application;
using LojaApi.src.Models;
using static LojaApi.Application.Acessorios.AcessoriosApoio;

namespace LojaApi.Application.Acessorios;

// Superadmin muda o status do pedido. Ao cancelar (se ainda não estava cancelado), devolve o estoque na hora.
public class AtualizarStatusPedidoCommandHandler(IAcessoriosRepository repositorio) : ICommandHandler<AtualizarStatusPedidoCommand>
{
    public async Task<Resultado<object>> HandleAsync(AtualizarStatusPedidoCommand command)
    {
        var (id, req) = command;
        var pedido = await repositorio.ObterPedidoComItensAsync(id);
        if (pedido is null) return Resultado.NaoEncontrado();

        if (!StatusPedidoAcessorio.Validos.Contains(req.Status))
            return Resultado.Invalido("Status inválido.");

        // Ao cancelar (e ele ainda não estava cancelado), devolve o estoque na hora —
        // sem isso, ficaria preso até a expiração automática de 30min.
        if (req.Status == StatusPedidoAcessorio.Cancelado && pedido.Status != StatusPedidoAcessorio.Cancelado)
            await DevolverEstoqueAsync(repositorio, pedido);

        pedido.Status = req.Status;
        if (req.CodigoRastreio != null) pedido.CodigoRastreio = req.CodigoRastreio;
        if (req.Status == StatusPedidoAcessorio.Enviado) pedido.EnviadoEm = DateTime.UtcNow;
        pedido.ExpiraEm = null; // já não precisa mais do prazo, seja pago ou cancelado

        await repositorio.SalvarAsync();

        return Resultado.Ok(new
        {
            pedido.Id,
            pedido.ClienteNome,
            pedido.Status,
            pedido.CodigoRastreio,
            pedido.PagoEm,
            pedido.EnviadoEm,
        });
    }
}
