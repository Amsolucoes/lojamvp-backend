using LojaApi.Shared.Application;
using LojaApi.src.Models.OrdemServico;

namespace LojaApi.Application.OrdemServico;

// Cria um item do checklist (ex.: Pastilha) numa categoria da loja.
public class CriarChecklistItemCommandHandler(IOrdemServicoRepository repositorio) : ICommandHandler<CriarChecklistItemCommand>
{
    public async Task<Resultado<object>> HandleAsync(CriarChecklistItemCommand command)
    {
        var (lojaId, req) = command;
        var categoria = await repositorio.ObterCategoriaAsync(req.CategoriaId, lojaId);
        if (categoria is null) return Resultado.Invalido("Categoria de checklist não encontrada.");

        var item = new ChecklistItem
        {
            LojaId = lojaId!.Value,
            CategoriaId = categoria.Id,
            Nome = req.Nome.Trim(),
            Ordem = req.Ordem,
            Ativo = req.Ativo,
        };
        repositorio.Adicionar(item);
        await repositorio.SalvarAsync();

        return Resultado.Ok(new { item.Id, item.CategoriaId, item.Nome, item.Ordem, item.Ativo });
    }
}
