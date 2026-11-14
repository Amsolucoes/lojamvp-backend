using LojaApi.Shared.Application;

namespace LojaApi.Application.Produtos;

// Produtos ativos com estoque no mínimo ou abaixo.
public class ListarAlertasEstoqueQueryHandler(IProdutosRepository repositorio) : IQueryHandler<ListarAlertasEstoqueQuery>
{
    public async Task<Resultado<object>> HandleAsync(ListarAlertasEstoqueQuery query) =>
        Resultado.Ok(await repositorio.ListarAlertasAsync(query.LojaId));
}
