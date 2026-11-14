using LojaApi.Shared.Application;

namespace LojaApi.Application.Produtos;

// Lista simplificada (id + nome) dos fornecedores ativos, usada no seletor do cadastro de produto.
public class ListarFornecedoresAtivosQueryHandler(IProdutosRepository repositorio) : IQueryHandler<ListarFornecedoresAtivosQuery>
{
    public async Task<Resultado<object>> HandleAsync(ListarFornecedoresAtivosQuery query) =>
        query.LojaId is null
            ? Resultado.Ok(Array.Empty<object>())
            : Resultado.Ok(await repositorio.ListarFornecedoresAtivosAsync(query.LojaId.Value));
}
