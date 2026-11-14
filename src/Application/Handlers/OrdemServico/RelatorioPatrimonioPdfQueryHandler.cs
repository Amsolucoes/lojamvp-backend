using LojaApi.Shared.Application;

namespace LojaApi.Application.OrdemServico;

// PDF do cadastro atual de patrimônio (itens ativos e valor total).
public class RelatorioPatrimonioPdfQueryHandler(IOrdemServicoRepository repositorio, IRelatorioPatrimonioPdf pdf) : IQueryHandler<RelatorioPatrimonioPdfQuery>
{
    public async Task<Resultado<object>> HandleAsync(RelatorioPatrimonioPdfQuery query)
    {
        if (query.LojaId is null) return Resultado.NaoEncontrado();
        var lojaId = query.LojaId.Value;

        var nomeLoja = await repositorio.ObterNomeDaLojaAsync(lojaId);
        var itens = await repositorio.ListarItensAtivosDePatrimonioAsync(lojaId);

        return Resultado.Ok(new ArquivoPdfDto(pdf.GerarCadastro(nomeLoja, itens), $"patrimonio-{DateTime.UtcNow:yyyy-MM-dd}.pdf"));
    }
}
