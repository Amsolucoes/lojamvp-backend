using LojaApi.Domain.Equipe;
using LojaApi.Models;
using LojaApi.Shared.Application;
using LojaApi.src.Models.Funcionarios;

namespace LojaApi.Application.Equipe;

// Desfaz um fechamento: as comissões voltam para pendente e a conta a pagar é removida.
public class DesfazerFechamentoCommandHandler(IEquipeRepository repositorio) : ICommandHandler<DesfazerFechamentoCommand>
{
    public async Task<Resultado<object>> HandleAsync(DesfazerFechamentoCommand command)
    {
        var (lojaId, id) = command;
        var fechamento = await repositorio.ObterFechamentoAsync(id, lojaId);
        if (fechamento is null) return Resultado.NaoEncontrado();

        if (fechamento.LancamentoFinanceiroId.HasValue)
        {
            var lancamento = await repositorio.ObterLancamentoFinanceiroAsync(fechamento.LancamentoFinanceiroId.Value);
            if (lancamento != null)
            {
                if (lancamento.Status == "pago")
                    return Resultado.Invalido("Essa comissão já foi paga no Financeiro. Estorne o lançamento lá antes de desfazer o fechamento.");

                repositorio.Remover(lancamento);
            }
        }

        foreach (var c in await repositorio.ListarComissoesDoFechamentoAsync(id))
        {
            c.Status = StatusComissao.Pendente;
            c.PagoEm = null;
            c.FechamentoId = null;
        }

        repositorio.Remover(fechamento);
        await repositorio.SalvarAsync();

        return Resultado.Ok(new { mensagem = "Fechamento desfeito. As comissões voltaram para pendente." });
    }
}
