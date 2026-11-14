using LojaApi.Shared.Application;

namespace LojaApi.Application.Produtos;

// "Exclui" uma categoria (só desativa), se nenhum produto ativo a usa.
public class ExcluirCategoriaCommandHandler(IProdutosRepository repositorio) : ICommandHandler<ExcluirCategoriaCommand>
{
    public async Task<Resultado<object>> HandleAsync(ExcluirCategoriaCommand command)
    {
        var (lojaId, id) = command;
        if (lojaId is null) return Resultado.Invalido("Loja não encontrada.");

        var cat = await repositorio.ObterCategoriaAsync(id, lojaId.Value);
        if (cat is null) return Resultado.NaoEncontrado();

        // Bloqueia se houver produtos usando a categoria
        var qtdProdutos = await repositorio.ContarProdutosAtivosDaCategoriaAsync(lojaId.Value, cat.Nome);
        if (qtdProdutos > 0)
            return Resultado.Invalido($"Não é possível excluir: {qtdProdutos} produto(s) usam esta categoria.");

        cat.Ativo = false;
        await repositorio.SalvarAsync();
        return Resultado.Ok(new { mensagem = "Categoria excluída." });
    }
}
