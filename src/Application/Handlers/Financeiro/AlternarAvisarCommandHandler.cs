using LojaApi.Shared.Application;

namespace LojaApi.Application.Financeiro;

// Liga ou desliga o aviso de vencimento de um lançamento.
public class AlternarAvisarCommandHandler(ILancamentosRepository repositorio) : ICommandHandler<AlternarAvisarCommand>
{
    public async Task<Resultado<object>> HandleAsync(AlternarAvisarCommand command)
    {
        var (lojaIdOuNulo, id, req) = command;
        if (lojaIdOuNulo is not { } lojaId) return Resultado.NaoEncontrado();
        var lanc = await repositorio.ObterAsync(id, lojaId);
        if (lanc is null) return Resultado.NaoEncontrado();

        lanc.Avisar = req.Avisar;
        await repositorio.SalvarAsync();
        return Resultado.Ok(new { lanc.Id, lanc.Avisar });
    }
}
