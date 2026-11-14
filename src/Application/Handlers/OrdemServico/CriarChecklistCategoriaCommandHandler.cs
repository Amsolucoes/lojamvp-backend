using LojaApi.Shared.Application;
using LojaApi.src.Models.OrdemServico;

namespace LojaApi.Application.OrdemServico;

// Cria uma categoria do checklist (ex.: Freios, Suspensão).
public class CriarChecklistCategoriaCommandHandler(IOrdemServicoRepository repositorio) : ICommandHandler<CriarChecklistCategoriaCommand>
{
    public async Task<Resultado<object>> HandleAsync(CriarChecklistCategoriaCommand command)
    {
        var (lojaId, req) = command;
        if (lojaId is null) return Resultado.Invalido("Loja não encontrada.");

        var categoria = new ChecklistCategoria
        {
            LojaId = lojaId.Value,
            Nome = req.Nome.Trim(),
            Ordem = req.Ordem,
            Ativa = req.Ativa,
        };
        repositorio.Adicionar(categoria);
        await repositorio.SalvarAsync();

        return Resultado.Ok(new { categoria.Id, categoria.Nome, categoria.Ordem, categoria.Ativa });
    }
}
