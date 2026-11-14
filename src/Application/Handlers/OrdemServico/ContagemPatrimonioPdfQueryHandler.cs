using LojaApi.Shared.Application;

namespace LojaApi.Application.OrdemServico;

// PDF de uma contagem, com o comparativo esperado x contado.
public class ContagemPatrimonioPdfQueryHandler(IOrdemServicoRepository repositorio, IRelatorioPatrimonioPdf pdf) : IQueryHandler<ContagemPatrimonioPdfQuery>
{
    public async Task<Resultado<object>> HandleAsync(ContagemPatrimonioPdfQuery query)
    {
        var (lojaId, id) = query;
        var contagem = await repositorio.ObterContagemCompletaAsync(id, lojaId);
        if (contagem is null) return Resultado.NaoEncontrado();

        var nomeLoja = await repositorio.ObterNomeDaLojaAsync(lojaId!.Value);

        return Resultado.Ok(new ArquivoPdfDto(pdf.GerarContagem(nomeLoja, contagem), $"contagem-patrimonio-{contagem.DataContagem:yyyy-MM-dd}.pdf"));
    }
}
