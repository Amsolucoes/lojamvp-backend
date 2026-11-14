using LojaApi.Shared.Application;

namespace LojaApi.Application.Produtos;

// Exclui um fornecedor; se algum produto o usa, só desativa.
public class ExcluirFornecedorCommandHandler(IProdutosRepository repositorio) : ICommandHandler<ExcluirFornecedorCommand>
{
    public async Task<Resultado<object>> HandleAsync(ExcluirFornecedorCommand command)
    {
        var (lojaId, id) = command;
        var fornecedor = await repositorio.ObterFornecedorAsync(id, lojaId);
        if (fornecedor is null) return Resultado.NaoEncontrado();

        if (await repositorio.ContarProdutosDoFornecedorAsync(id) > 0)
        {
            fornecedor.Ativo = false; // em uso — desativa em vez de excluir
            await repositorio.SalvarAsync();
            return Resultado.Ok(new { mensagem = "Fornecedor em uso — foi desativado em vez de excluído." });
        }

        repositorio.Remover(fornecedor);
        await repositorio.SalvarAsync();
        return Resultado.Ok(new { mensagem = "Fornecedor excluído." });
    }
}
