using LojaApi.Shared.Application;

namespace LojaApi.Application.OrdemServico;

// Atualiza nome, ordem e situação de uma categoria do checklist.
public class AtualizarChecklistCategoriaCommandHandler(IOrdemServicoRepository repositorio) : ICommandHandler<AtualizarChecklistCategoriaCommand>
{
    public async Task<Resultado<object>> HandleAsync(AtualizarChecklistCategoriaCommand command)
    {
        var (lojaId, id, req) = command;
        var categoria = await repositorio.ObterCategoriaAsync(id, lojaId);
        if (categoria is null) return Resultado.NaoEncontrado();

        categoria.Nome = req.Nome.Trim();
        categoria.Ordem = req.Ordem;
        categoria.Ativa = req.Ativa;
        await repositorio.SalvarAsync();

        return Resultado.Ok(new { categoria.Id, categoria.Nome, categoria.Ordem, categoria.Ativa });
    }
}
