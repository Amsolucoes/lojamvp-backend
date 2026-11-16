using LojaApi.Shared.Application;

namespace LojaApi.Application.Financeiro;

// Ativa ou desativa um fixo (desativado, deixa de gerar novos meses).
public class AlternarFixoCommandHandler(ILancamentosRepository repositorio) : ICommandHandler<AlternarFixoCommand>
{
    public async Task<Resultado<object>> HandleAsync(AlternarFixoCommand command)
    {
        var (lojaIdOuNulo, id) = command;
        if (lojaIdOuNulo is not { } lojaId) return Resultado.NaoEncontrado();
        var fixo = await repositorio.ObterFixoAsync(id, lojaId);
        if (fixo is null) return Resultado.NaoEncontrado();

        fixo.Ativa = !fixo.Ativa;
        await repositorio.SalvarAsync();
        return Resultado.Ok(new { fixo.Id, fixo.Ativa });
    }
}
