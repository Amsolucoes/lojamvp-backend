using LojaApi.Shared.Application;

namespace LojaApi.Application.Vendas;

// Exclui um movimento do caixa e o espelho dele no Financeiro.
public class ExcluirMovimentoCaixaCommandHandler(IVendasRepository repositorio) : ICommandHandler<ExcluirMovimentoCaixaCommand>
{
    public async Task<Resultado<object>> HandleAsync(ExcluirMovimentoCaixaCommand command)
    {
        var (lojaId, id) = command;
        var movimento = lojaId is null ? null : await repositorio.ObterMovimentoCaixaAsync(id, lojaId);
        if (movimento is null) return Resultado.NaoEncontrado();

        // Remove o espelho no Financeiro também, se existir
        if (movimento.AjusteContaBancariaId.HasValue)
        {
            var ajuste = await repositorio.ObterAjusteDaContaAsync(movimento.AjusteContaBancariaId.Value);
            if (ajuste != null) repositorio.Remover(ajuste);
        }

        repositorio.Remover(movimento);
        await repositorio.SalvarAsync();

        return Resultado.Ok(new { mensagem = "Movimento excluído." });
    }
}
