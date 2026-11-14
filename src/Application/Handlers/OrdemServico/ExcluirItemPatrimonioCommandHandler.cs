using LojaApi.Shared.Application;

namespace LojaApi.Application.OrdemServico;

// Exclui um item de patrimônio; se já entrou numa contagem, só desativa.
public class ExcluirItemPatrimonioCommandHandler(IOrdemServicoRepository repositorio) : ICommandHandler<ExcluirItemPatrimonioCommand>
{
    public async Task<Resultado<object>> HandleAsync(ExcluirItemPatrimonioCommand command)
    {
        var (lojaId, id) = command;
        var item = await repositorio.ObterItemPatrimonioAsync(id, lojaId);
        if (item is null) return Resultado.NaoEncontrado();

        if (await repositorio.ItemPatrimonioJaContadoAsync(id))
        {
            item.Ativo = false;
            await repositorio.SalvarAsync();
            return Resultado.Ok(new { mensagem = "Item já usado em alguma contagem — foi desativado em vez de excluído." });
        }

        repositorio.Remover(item);
        await repositorio.SalvarAsync();
        return Resultado.Ok(new { mensagem = "Item excluído." });
    }
}
