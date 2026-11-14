using LojaApi.Domain.OrdemServico;
using LojaApi.Shared.Application;

namespace LojaApi.Application.OrdemServico;

// Ajusta à mão a data/hora de entrada (aprovação) e de saída (conclusão) de uma ordem aprovada ou concluída.
public class AjustarDatasOrcamentoCommandHandler(IOrdemServicoRepository repositorio) : ICommandHandler<AjustarDatasOrcamentoCommand>
{
    public async Task<Resultado<object>> HandleAsync(AjustarDatasOrcamentoCommand command)
    {
        var (lojaId, id, req) = command;
        var orcamento = await repositorio.ObterOrcamentoAsync(id, lojaId);
        if (orcamento is null) return Resultado.NaoEncontrado();

        if (!OrdemServicoRegras.PodeAjustarDatas(orcamento.Status))
            return Resultado.Invalido("Só é possível ajustar entrada/saída de uma ordem aprovada ou concluída.");

        var (aprovadoEm, concluidoEm, erro) = OrdemServicoRegras.AjustarDatas(orcamento.AprovadoEm, orcamento.ConcluidoEm, req.AprovadoEm, req.ConcluidoEm);
        if (erro is not null) return Resultado.Invalido(erro);

        orcamento.AprovadoEm = aprovadoEm;
        orcamento.ConcluidoEm = concluidoEm;
        await repositorio.SalvarAsync();

        return Resultado.Ok(new { orcamento.Id, orcamento.AprovadoEm, orcamento.ConcluidoEm });
    }
}
