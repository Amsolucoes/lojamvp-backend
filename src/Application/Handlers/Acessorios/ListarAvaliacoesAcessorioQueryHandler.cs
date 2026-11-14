using LojaApi.Domain.Acessorios;
using LojaApi.Shared.Application;
using LojaApi.src.Models;
using static LojaApi.Application.Acessorios.AcessoriosApoio;

namespace LojaApi.Application.Acessorios;

// Avaliações de um produto, da mais nova para a mais antiga, com a média e o total.
public class ListarAvaliacoesAcessorioQueryHandler(IAcessoriosRepository repositorio) : IQueryHandler<ListarAvaliacoesAcessorioQuery>
{
    public async Task<Resultado<object>> HandleAsync(ListarAvaliacoesAcessorioQuery query)
    {
        var lista = await repositorio.ListarAvaliacoesAsync(query.ProdutoId);

        var media = lista.Count > 0 ? Math.Round(lista.Average(a => a.Nota), 1) : 0;

        return Resultado.Ok(new { media, total = lista.Count, avaliacoes = lista });
    }
}
