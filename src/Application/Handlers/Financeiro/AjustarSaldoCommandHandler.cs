using LojaApi.Domain.Financeiro;
using LojaApi.Models;
using LojaApi.Shared.Application;

namespace LojaApi.Application.Financeiro;

// Ajuste manual do saldo da conta: "entrada" soma, "saida" tira e "ajuste" leva o saldo até o valor informado
// (registra a diferença). Fica no histórico de ajustes da conta.
public class AjustarSaldoCommandHandler(IFinanceiroRepository repositorio) : ICommandHandler<AjustarSaldoCommand>
{
    public async Task<Resultado<object>> HandleAsync(AjustarSaldoCommand command)
    {
        var (lojaId, id, req) = command;
        if (lojaId is null) return Resultado.NaoEncontrado();
        var conta = await repositorio.ObterContaAsync(id, lojaId.Value);
        if (conta is null) return Resultado.NaoEncontrado();

        var saldoAtual = (await repositorio.CalcularSaldosAsync(lojaId.Value, [conta]))[conta.Id];
        var valorRegistrado = FinanceiroRegras.ValorDoAjuste(req.Tipo, req.NovoSaldo, req.Valor, saldoAtual);

        repositorio.Adicionar(new AjusteContaBancaria
        {
            LojaId = lojaId.Value,
            ContaBancariaId = id,
            Tipo = req.Tipo,
            Valor = valorRegistrado,
            Observacao = req.Observacao,
        });
        await repositorio.SalvarAsync();

        var novoSaldo = (await repositorio.CalcularSaldosAsync(lojaId.Value, [conta]))[conta.Id];
        return Resultado.Ok(new SaldoAtualDto(novoSaldo));
    }
}
