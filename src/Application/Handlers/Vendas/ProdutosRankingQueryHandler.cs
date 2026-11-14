using LojaApi.Shared.Application;

namespace LojaApi.Application.Vendas;

// Produtos mais vendidos num período, com receita e lucro estimado.
public class ProdutosRankingQueryHandler(IVendasRepository repositorio) : IQueryHandler<ProdutosRankingQuery>
{
    public async Task<Resultado<object>> HandleAsync(ProdutosRankingQuery query)
    {
        var itens = await repositorio.ListarItensVendidosAsync(query.LojaId, query.De, query.Ate);

        var ranking = itens
            .Where(i => i.ProdutoId.HasValue)
            .GroupBy(i => i.ProdutoId!.Value)
            .Select(g => new ProdutoRankingDto(
                Id:             g.Key,
                Nome:           g.First().Produto?.Nome ?? "",
                Categoria:      g.First().Produto?.Categoria ?? "",
                QtdVendida:     g.Sum(i => i.Quantidade),
                Receita:        g.Sum(i => i.Subtotal),
                LucroEstimado:  g.Sum(i => i.Subtotal - i.Quantidade * (i.Produto?.PrecoCusto ?? 0))
            ))
            .OrderByDescending(r => r.QtdVendida)
            .Take(query.Top)
            .ToList();

        return Resultado.Ok(ranking);
    }
}
