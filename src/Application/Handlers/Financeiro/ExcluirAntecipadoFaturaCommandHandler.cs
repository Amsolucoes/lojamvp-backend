using LojaApi.Shared.Application;

namespace LojaApi.Application.Financeiro;

// Exclui um pagamento antecipado de fatura.
public class ExcluirAntecipadoFaturaCommandHandler(IFaturasRepository faturas) : ICommandHandler<ExcluirAntecipadoFaturaCommand>
{
    public async Task<Resultado<object>> HandleAsync(ExcluirAntecipadoFaturaCommand command)
    {
        var (lojaIdOuNulo, id) = command;
        if (lojaIdOuNulo is not { } lojaId) return Resultado.NaoEncontrado();
        var antecipado = await faturas.ObterAntecipadoAsync(id, lojaId);
        if (antecipado is null) return Resultado.NaoEncontrado();

        faturas.Remover(antecipado);
        await faturas.SalvarAsync();
        return Resultado.Ok(new { mensagem = "Pagamento antecipado excluído." });
    }
}
