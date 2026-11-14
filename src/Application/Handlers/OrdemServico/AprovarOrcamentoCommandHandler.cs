using LojaApi.Domain.OrdemServico;
using LojaApi.Shared.Application;

namespace LojaApi.Application.OrdemServico;

// Aprova um orçamento pendente: vira ordem em andamento.
public class AprovarOrcamentoCommandHandler(IOrdemServicoRepository repositorio) : ICommandHandler<AprovarOrcamentoCommand>
{
    public async Task<Resultado<object>> HandleAsync(AprovarOrcamentoCommand command)
    {
        var (lojaId, id) = command;
        var orcamento = await repositorio.ObterOrcamentoAsync(id, lojaId);
        if (orcamento is null) return Resultado.NaoEncontrado();

        if (!OrdemServicoRegras.PodeAprovar(orcamento.Status))
            return Resultado.Invalido("Só é possível aprovar um orçamento pendente.");

        orcamento.Status = OrdemServicoRegras.EmAndamento;
        orcamento.AprovadoEm = DateTime.UtcNow;
        await repositorio.SalvarAsync();

        return Resultado.Ok(new { orcamento.Id, orcamento.Status });
    }
}
