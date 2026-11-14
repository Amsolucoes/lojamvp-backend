using LojaApi.Domain.Acessorios;
using LojaApi.Shared.Application;
using LojaApi.src.Models;
using static LojaApi.Application.Acessorios.AcessoriosApoio;

namespace LojaApi.Application.Acessorios;

// Superadmin muda o status do pedido, sem mexer no estoque nem no prazo; devolve o pedido.
public class AtualizarStatusPedidoSimplesCommandHandler(IAcessoriosRepository repositorio) : ICommandHandler<AtualizarStatusPedidoSimplesCommand>
{
    public async Task<Resultado<object>> HandleAsync(AtualizarStatusPedidoSimplesCommand command)
    {
        var (id, req) = command;
        var pedido = await repositorio.ObterPedidoPelaChaveAsync(id);
        if (pedido is null) return Resultado.NaoEncontrado();

        if (!StatusPedidoAcessorio.Validos.Contains(req.Status))
            return Resultado.Invalido("Status inválido.");

        pedido.Status = req.Status;
        if (req.CodigoRastreio != null) pedido.CodigoRastreio = req.CodigoRastreio;
        if (req.Status == StatusPedidoAcessorio.Enviado) pedido.EnviadoEm = DateTime.UtcNow;

        await repositorio.SalvarAsync();
        return Resultado.Ok(pedido);
    }
}
