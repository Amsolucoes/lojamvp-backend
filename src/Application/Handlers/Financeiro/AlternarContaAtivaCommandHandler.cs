using LojaApi.Shared.Application;

namespace LojaApi.Application.Financeiro;

// Liga ou desliga a conta (ativa ⇄ inativa).
public class AlternarContaAtivaCommandHandler(IFinanceiroRepository repositorio) : ICommandHandler<AlternarContaAtivaCommand>
{
    public async Task<Resultado<object>> HandleAsync(AlternarContaAtivaCommand command)
    {
        var (lojaId, id) = command;
        if (lojaId is null) return Resultado.NaoEncontrado();
        var conta = await repositorio.ObterContaAsync(id, lojaId.Value);
        if (conta is null) return Resultado.NaoEncontrado();

        conta.Ativa = !conta.Ativa;
        await repositorio.SalvarAsync();
        return Resultado.Ok(new ContaAtivaDto(conta.Id, conta.Ativa));
    }
}
