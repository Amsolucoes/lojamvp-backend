using LojaApi.Domain.Acessorios;
using LojaApi.Shared.Application;
using LojaApi.src.Models;
using static LojaApi.Application.Acessorios.AcessoriosApoio;

namespace LojaApi.Application.Acessorios;

// Exclui um produto; se já foi usado em algum pedido, só desativa (o histórico não perde o vínculo).
public class ExcluirProdutoAcessorioCommandHandler(IAcessoriosRepository repositorio) : ICommandHandler<ExcluirProdutoAcessorioCommand>
{
    public async Task<Resultado<object>> HandleAsync(ExcluirProdutoAcessorioCommand command)
    {
        var id = command.Id;
        var produto = await repositorio.ObterProdutoAsync(id);
        if (produto is null) return Resultado.NaoEncontrado();

        var temPedidos = await repositorio.ProdutoTemPedidosAsync(id);
        if (temPedidos)
        {
            produto.Ativo = false;
            await repositorio.SalvarAsync();
            return Resultado.Ok(new { mensagem = "Produto já usado em pedidos — foi desativado em vez de excluído." });
        }

        repositorio.Remover(produto);
        await repositorio.SalvarAsync();
        return Resultado.Ok(new { mensagem = "Produto excluído." });
    }
}
