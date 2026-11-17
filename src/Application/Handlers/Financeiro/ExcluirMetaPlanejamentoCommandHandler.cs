using LojaApi.Shared.Application;

namespace LojaApi.Application.Financeiro;

// Exclui uma meta da loja.
public class ExcluirMetaPlanejamentoCommandHandler(IPlanejamentoRepository repositorio) : ICommandHandler<ExcluirMetaPlanejamentoCommand>
{
    public async Task<Resultado<object>> HandleAsync(ExcluirMetaPlanejamentoCommand command)
    {
        var (lojaIdOuNulo, id) = command;
        if (lojaIdOuNulo is not { } lojaId) return Resultado.NaoEncontrado();
        var meta = await repositorio.ObterMetaAsync(id, lojaId);
        if (meta is null) return Resultado.NaoEncontrado();

        repositorio.Remover(meta);
        await repositorio.SalvarAsync();
        return Resultado.Ok(new { });
    }
}
