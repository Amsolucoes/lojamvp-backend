using LojaApi.Domain.OrdemServico;
using LojaApi.Shared.Application;

namespace LojaApi.Application.OrdemServico;

// Exclui um orçamento que ainda está pendente (depois disso, só cancelar).
public class ExcluirOrcamentoCommandHandler(IOrdemServicoRepository repositorio) : ICommandHandler<ExcluirOrcamentoCommand>
{
    public async Task<Resultado<object>> HandleAsync(ExcluirOrcamentoCommand command)
    {
        var (lojaId, id) = command;
        var orcamento = await repositorio.ObterOrcamentoAsync(id, lojaId);
        if (orcamento is null) return Resultado.NaoEncontrado();

        if (!OrdemServicoRegras.PodeExcluir(orcamento.Status))
            return Resultado.Invalido("Só é possível excluir um orçamento ainda pendente. Cancele em vez de excluir.");

        repositorio.Remover(orcamento);
        await repositorio.SalvarAsync();
        return Resultado.Ok(new { mensagem = "Orçamento excluído." });
    }
}
