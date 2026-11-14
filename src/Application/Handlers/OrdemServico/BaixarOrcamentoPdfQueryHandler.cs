using LojaApi.Shared.Application;

namespace LojaApi.Application.OrdemServico;

// PDF do orçamento (A4).
public class BaixarOrcamentoPdfQueryHandler(IOrcamentoNotificacao notificacao) : IQueryHandler<BaixarOrcamentoPdfQuery>
{
    public async Task<Resultado<object>> HandleAsync(BaixarOrcamentoPdfQuery query)
    {
        var (lojaId, id) = query;
        if (lojaId is null) return Resultado.Invalido("Loja não encontrada.");

        var pdf = await notificacao.GerarPdfAsync(id, lojaId.Value);
        if (pdf is null) return Resultado.NaoEncontrado();

        return Resultado.Ok(new ArquivoPdfDto(pdf, $"orcamento-{id.ToString()[..8]}.pdf"));
    }
}
