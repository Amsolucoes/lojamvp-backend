using LojaApi.Shared.Application;

namespace LojaApi.Application.OrdemServico;

// Exclui um item do checklist; se já foi usado num orçamento, só desativa.
public class ExcluirChecklistItemCommandHandler(IOrdemServicoRepository repositorio) : ICommandHandler<ExcluirChecklistItemCommand>
{
    public async Task<Resultado<object>> HandleAsync(ExcluirChecklistItemCommand command)
    {
        var (lojaId, id) = command;
        var item = await repositorio.ObterItemChecklistAsync(id, lojaId);
        if (item is null) return Resultado.NaoEncontrado();

        if (await repositorio.ItemDeChecklistEmUsoAsync(id))
        {
            item.Ativo = false;
            await repositorio.SalvarAsync();
            return Resultado.Ok(new { mensagem = "Item em uso em orçamentos — foi desativado em vez de excluído." });
        }

        repositorio.Remover(item);
        await repositorio.SalvarAsync();
        return Resultado.Ok(new { mensagem = "Item excluído." });
    }
}
