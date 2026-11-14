using LojaApi.Shared.Application;

namespace LojaApi.Application.Produtos;

// Fornecedores da loja por nome, com a quantidade de produtos ativos e busca por nome, CNPJ/CPF ou telefone.
public class ListarFornecedoresQueryHandler(IProdutosRepository repositorio) : IQueryHandler<ListarFornecedoresQuery>
{
    public async Task<Resultado<object>> HandleAsync(ListarFornecedoresQuery query) =>
        Resultado.Ok(await repositorio.ListarFornecedoresAsync(query.LojaId, query.Busca));
}
