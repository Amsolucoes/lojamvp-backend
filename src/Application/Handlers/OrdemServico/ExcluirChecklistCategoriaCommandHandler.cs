using LojaApi.Shared.Application;

namespace LojaApi.Application.OrdemServico;

// Exclui uma categoria (e os itens dela), a menos que algum item já tenha sido usado num orçamento.
public class ExcluirChecklistCategoriaCommandHandler(IOrdemServicoRepository repositorio) : ICommandHandler<ExcluirChecklistCategoriaCommand>
{
    public async Task<Resultado<object>> HandleAsync(ExcluirChecklistCategoriaCommand command)
    {
        var (lojaId, id) = command;
        var categoria = await repositorio.ObterCategoriaComItensAsync(id, lojaId);
        if (categoria is null) return Resultado.NaoEncontrado();

        var itemIds = categoria.Itens.Select(i => i.Id).ToList();
        if (await repositorio.ItensDeChecklistEmUsoAsync(itemIds))
            return Resultado.Invalido("Não é possível excluir: esta categoria tem itens já usados em orçamentos. Desative os itens em vez de excluir.");

        repositorio.Remover(categoria); // cascade remove os itens junto
        await repositorio.SalvarAsync();
        return Resultado.Ok(new { mensagem = "Categoria excluída." });
    }
}
