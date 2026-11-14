using LojaApi.Domain.OrdemServico;
using LojaApi.Shared.Application;

namespace LojaApi.Application.OrdemServico;

// Cancela (reprova) uma ordem que ainda não foi concluída.
public class CancelarOrcamentoCommandHandler(IOrdemServicoRepository repositorio) : ICommandHandler<CancelarOrcamentoCommand>
{
    public async Task<Resultado<object>> HandleAsync(CancelarOrcamentoCommand command)
    {
        var (lojaId, id) = command;
        var orcamento = await repositorio.ObterOrcamentoAsync(id, lojaId);
        if (orcamento is null) return Resultado.NaoEncontrado();

        if (!OrdemServicoRegras.PodeCancelar(orcamento.Status))
            return Resultado.Invalido("Não é possível cancelar uma ordem já concluída.");

        orcamento.Status = OrdemServicoRegras.Cancelado;
        await repositorio.SalvarAsync();

        return Resultado.Ok(new { orcamento.Id, orcamento.Status });
    }
}
