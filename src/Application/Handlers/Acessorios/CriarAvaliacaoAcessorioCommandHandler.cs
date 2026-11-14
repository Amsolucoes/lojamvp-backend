using LojaApi.Domain.Acessorios;
using LojaApi.Shared.Application;
using LojaApi.src.Models;
using static LojaApi.Application.Acessorios.AcessoriosApoio;

namespace LojaApi.Application.Acessorios;

// O cliente avalia um produto: só quem tem pedido PAGO com esse produto e esse e-mail, uma vez por pedido.
public class CriarAvaliacaoAcessorioCommandHandler(IAcessoriosRepository repositorio) : ICommandHandler<CriarAvaliacaoAcessorioCommand>
{
    public async Task<Resultado<object>> HandleAsync(CriarAvaliacaoAcessorioCommand command)
    {
        var req = command.Req;
        if (req.Nota < 1 || req.Nota > 5)
            return Resultado.Invalido("A nota deve ser de 1 a 5.");

        if (string.IsNullOrWhiteSpace(req.Email))
            return Resultado.Invalido("Informe o e-mail usado na compra.");

        // Só quem tem pedido PAGO com esse produto, nesse e-mail, pode avaliar
        var pedido = await repositorio.ObterUltimoPedidoPagoComProdutoAsync(req.Email.Trim().ToLower(), req.ProdutoId);

        if (pedido is null)
            return Resultado.Invalido("Não encontramos uma compra paga desse produto com esse e-mail.");

        var jaAvaliou = await repositorio.JaAvaliouAsync(pedido.Id, req.ProdutoId);
        if (jaAvaliou)
            return Resultado.Invalido("Você já avaliou este produto.");

        var avaliacao = new AvaliacaoAcessorio
        {
            ProdutoId = req.ProdutoId,
            PedidoId = pedido.Id,
            ClienteNome = pedido.ClienteNome,
            Nota = req.Nota,
            Comentario = string.IsNullOrWhiteSpace(req.Comentario) ? null : req.Comentario.Trim(),
        };
        repositorio.Adicionar(avaliacao);
        await repositorio.SalvarAsync();

        return Resultado.Ok(new { mensagem = "Avaliação enviada, obrigado!" });
    }
}
