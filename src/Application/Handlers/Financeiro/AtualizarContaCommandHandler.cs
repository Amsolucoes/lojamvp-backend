using LojaApi.Shared.Application;

namespace LojaApi.Application.Financeiro;

// Edita nome, saldo inicial, banco e limite da conta e devolve o saldo atual recalculado.
public class AtualizarContaCommandHandler(IFinanceiroRepository repositorio) : ICommandHandler<AtualizarContaCommand>
{
    public async Task<Resultado<object>> HandleAsync(AtualizarContaCommand command)
    {
        var (lojaId, id, req) = command;
        if (lojaId is null) return Resultado.NaoEncontrado();
        var conta = await repositorio.ObterContaAsync(id, lojaId.Value);
        if (conta is null) return Resultado.NaoEncontrado();

        conta.Nome = req.Nome.Trim();
        conta.SaldoInicial = req.SaldoInicial;
        conta.Banco = req.Banco;
        conta.Limite = req.Limite;
        await repositorio.SalvarAsync();

        var saldo = (await repositorio.CalcularSaldosAsync(lojaId.Value, [conta]))[conta.Id];
        return Resultado.Ok(new ContaBancariaDto(conta.Id, conta.Nome, conta.SaldoInicial, conta.Ativa, conta.Banco, conta.Limite, saldo));
    }
}
