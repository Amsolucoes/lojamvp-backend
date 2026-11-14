using LojaApi.Domain.Acessorios;
using LojaApi.Shared.Application;
using LojaApi.src.Models;
using static LojaApi.Application.Acessorios.AcessoriosApoio;

namespace LojaApi.Application.Acessorios;

// Vitrine pública: produtos ativos (com filtro opcional de categoria). Antes, libera o estoque de pedidos vencidos.
public class ListarVitrineQueryHandler(IAcessoriosRepository repositorio) : IQueryHandler<ListarVitrineQuery>
{
    public async Task<Resultado<object>> HandleAsync(ListarVitrineQuery query)
    {
        await LiberarPedidosExpiradosAsync(repositorio);

        var limiteNovo = AcessoriosRegras.LimiteProdutoNovo(DateTime.UtcNow);
        return Resultado.Ok(await repositorio.ListarVitrineAsync(query.Categoria, limiteNovo));
    }
}
