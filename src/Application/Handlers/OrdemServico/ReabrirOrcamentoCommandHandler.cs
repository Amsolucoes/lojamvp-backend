using LojaApi.Domain.OrdemServico;
using LojaApi.Shared.Application;

namespace LojaApi.Application.OrdemServico;

// Reabre uma ordem cancelada, direto em andamento (ela já tinha sido aprovada antes).
public class ReabrirOrcamentoCommandHandler(IOrdemServicoRepository repositorio) : ICommandHandler<ReabrirOrcamentoCommand>
{
    public async Task<Resultado<object>> HandleAsync(ReabrirOrcamentoCommand command)
    {
        var (lojaId, id) = command;
        var orcamento = await repositorio.ObterOrcamentoAsync(id, lojaId);
        if (orcamento is null) return Resultado.NaoEncontrado();

        if (!OrdemServicoRegras.PodeReabrir(orcamento.Status))
            return Resultado.Invalido("Só é possível reabrir uma ordem cancelada.");

        // Volta direto pra em_andamento — ela já tinha sido aprovada antes de ser cancelada,
        // então não faz sentido pedir aprovação de novo.
        orcamento.Status = OrdemServicoRegras.EmAndamento;
        await repositorio.SalvarAsync();

        return Resultado.Ok(new { orcamento.Id, orcamento.Status });
    }
}
