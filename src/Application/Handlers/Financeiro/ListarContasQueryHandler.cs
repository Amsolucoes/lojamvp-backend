using LojaApi.Shared.Application;

namespace LojaApi.Application.Financeiro;

// Contas bancárias da loja com o saldo atual de cada uma (sem loja: lista vazia).
public class ListarContasQueryHandler(IFinanceiroRepository repositorio) : IQueryHandler<ListarContasQuery>
{
    public async Task<Resultado<object>> HandleAsync(ListarContasQuery query)
    {
        if (query.LojaId is not { } lojaId) return Resultado.Ok(Array.Empty<ContaBancariaDto>());

        var contas = await repositorio.ListarContasAsync(lojaId);
        var saldos = await repositorio.CalcularSaldosAsync(lojaId, contas);

        return Resultado.Ok(contas.Select(c => new ContaBancariaDto(c.Id, c.Nome, c.SaldoInicial, c.Ativa, c.Banco, c.Limite,
            saldos.TryGetValue(c.Id, out var saldo) ? saldo : c.SaldoInicial)).ToList());
    }
}
