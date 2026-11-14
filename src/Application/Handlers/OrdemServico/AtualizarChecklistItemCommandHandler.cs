using LojaApi.Shared.Application;

namespace LojaApi.Application.OrdemServico;

// Atualiza nome, ordem e situação de um item do checklist.
public class AtualizarChecklistItemCommandHandler(IOrdemServicoRepository repositorio) : ICommandHandler<AtualizarChecklistItemCommand>
{
    public async Task<Resultado<object>> HandleAsync(AtualizarChecklistItemCommand command)
    {
        var (lojaId, id, req) = command;
        var item = await repositorio.ObterItemChecklistAsync(id, lojaId);
        if (item is null) return Resultado.NaoEncontrado();

        item.Nome = req.Nome.Trim();
        item.Ordem = req.Ordem;
        item.Ativo = req.Ativo;
        await repositorio.SalvarAsync();

        return Resultado.Ok(new { item.Id, item.CategoriaId, item.Nome, item.Ordem, item.Ativo });
    }
}
