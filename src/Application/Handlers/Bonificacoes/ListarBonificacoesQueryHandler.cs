using LojaApi.Shared.Application;

namespace LojaApi.Application.Bonificacoes;

// Bonificações da loja, da mais nova para a mais antiga, com filtro por cliente, produto e período. Por padrão não traz as estornadas.
public class ListarBonificacoesQueryHandler(IBonificacoesRepository repositorio) : IQueryHandler<ListarBonificacoesQuery>
{
    public async Task<Resultado<object>> HandleAsync(ListarBonificacoesQuery query)
    {
        var (lojaIdOuNulo, clienteId, produtoId, de, ate, incluirEstornadas) = query;
        if (lojaIdOuNulo is not { } lojaId) return Resultado.Ok(Array.Empty<BonificacaoDto>());

        var lista = await repositorio.ListarAsync(lojaId, clienteId, produtoId, de, ate, incluirEstornadas);
        return Resultado.Ok(lista.Select(BonificacoesApoio.ParaDto).ToList());
    }
}
