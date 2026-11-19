using LojaApi.Shared.Application;

namespace LojaApi.Application.Bonificacoes;

// Quanto a loja gastou em brindes no período: total (quantidade e custo) e o detalhe por produto e por cliente, do que mais custou para o que menos custou.
// As bonificações estornadas não contam.
public class ResumoBonificacoesQueryHandler(IBonificacoesRepository repositorio) : IQueryHandler<ResumoBonificacoesQuery>
{
    public async Task<Resultado<object>> HandleAsync(ResumoBonificacoesQuery query)
    {
        var (lojaIdOuNulo, de, ate) = query;
        if (lojaIdOuNulo is not { } lojaId)
            return Resultado.Ok(new ResumoBonificacoesDto(0, 0, 0, [], []));

        var lista = await repositorio.ListarAsync(lojaId, null, null, de, ate, incluirEstornadas: false);

        var porProduto = lista
            .GroupBy(b => (b.ProdutoId, b.NomeProduto))
            .Select(g => new BonificacaoPorProdutoDto(g.Key.ProdutoId, g.Key.NomeProduto, g.Sum(b => b.Quantidade), g.Sum(b => b.CustoTotal())))
            .OrderByDescending(p => p.Custo).ThenBy(p => p.NomeProduto)
            .ToList();
        var porCliente = lista
            .GroupBy(b => (b.ClienteId, b.NomeCliente))
            .Select(g => new BonificacaoPorClienteDto(g.Key.ClienteId, g.Key.NomeCliente, g.Count(), g.Sum(b => b.Quantidade), g.Sum(b => b.CustoTotal())))
            .OrderByDescending(c => c.Custo).ThenBy(c => c.NomeCliente)
            .ToList();

        return Resultado.Ok(new ResumoBonificacoesDto(lista.Count, lista.Sum(b => b.Quantidade), lista.Sum(b => b.CustoTotal()), porProduto, porCliente));
    }
}
