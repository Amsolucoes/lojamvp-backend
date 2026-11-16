using LojaApi.Models;
using LojaApi.Shared.Application;

namespace LojaApi.Application.Financeiro;

// Cria uma conta bancária (o nome fica sem espaços nas pontas; o saldo atual começa no saldo inicial).
public class CriarContaCommandHandler(IFinanceiroRepository repositorio) : ICommandHandler<CriarContaCommand>
{
    public async Task<Resultado<object>> HandleAsync(CriarContaCommand command)
    {
        var (lojaId, req) = command;
        if (lojaId is null) return Resultado.Invalido("Loja não encontrada.");

        var conta = new ContaBancaria { LojaId = lojaId.Value, Nome = req.Nome.Trim(), SaldoInicial = req.SaldoInicial, Banco = req.Banco, Limite = req.Limite };
        repositorio.Adicionar(conta);
        await repositorio.SalvarAsync();

        return Resultado.Ok(new ContaBancariaDto(conta.Id, conta.Nome, conta.SaldoInicial, conta.Ativa, conta.Banco, conta.Limite, conta.SaldoInicial));
    }
}
