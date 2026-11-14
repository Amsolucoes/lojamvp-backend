using LojaApi.Shared.Application;

namespace LojaApi.Application.Produtos;

// Atualiza custo e venda de vários produtos de uma vez (tela de Precificação); só os da loja.
public class AtualizarPrecosEmLoteCommandHandler(IProdutosRepository repositorio) : ICommandHandler<AtualizarPrecosEmLoteCommand>
{
    public async Task<Resultado<object>> HandleAsync(AtualizarPrecosEmLoteCommand command)
    {
        var (lojaId, req) = command;
        if (req.Produtos.Count == 0) return Resultado.Ok(new { atualizados = 0 });

        var ids = req.Produtos.Select(p => p.Id).ToList();
        var produtos = await repositorio.ListarProdutosDaLojaPorIdsAsync(ids, lojaId);

        var porId = req.Produtos.ToDictionary(p => p.Id);
        foreach (var produto in produtos)
        {
            var item = porId[produto.Id];
            produto.PrecoCusto = item.PrecoCusto;
            produto.PrecoVenda = item.PrecoVenda;
            produto.AtualizadoEm = DateTime.UtcNow;
        }

        await repositorio.SalvarAsync();
        return Resultado.Ok(new { atualizados = produtos.Count });
    }
}
