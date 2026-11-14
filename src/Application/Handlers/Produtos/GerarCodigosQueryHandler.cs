using LojaApi.Domain.Produtos;
using LojaApi.Shared.Application;

namespace LojaApi.Application.Produtos;

// Gera códigos de barras EAN-13 de uso interno que ainda não existem na loja (nem entre os "ignorados" da tela).
public class GerarCodigosQueryHandler(IProdutosRepository repositorio, Func<long>? sorteio = null) : IQueryHandler<GerarCodigosQuery>
{
    // O sorteio dos 11 dígitos pode ser trocado nos testes; em produção usa o gerador aleatório do .NET
    private readonly Func<long> _sorteio = sorteio ?? (() => Random.Shared.NextInt64(0, 100_000_000_000L));

    public async Task<Resultado<object>> HandleAsync(GerarCodigosQuery query)
    {
        var (lojaId, quantidadePedida, ignorar) = query;
        var quantidade = ProdutosRegras.QuantidadeDeCodigos(quantidadePedida);

        var usados = ProdutosRegras.CodigosIgnorados(ignorar);
        usados.UnionWith(await repositorio.ListarCodigosInternosAsync(lojaId));

        var codigos = new List<string>();
        while (codigos.Count < quantidade)
        {
            var codigo = ProdutosRegras.CodigoInterno(_sorteio());
            if (!usados.Add(codigo)) continue;
            codigos.Add(codigo);
        }
        return Resultado.Ok(new { codigos });
    }
}
