using LojaApi.Domain.OrdemServico;
using LojaApi.Shared.Application;

namespace LojaApi.Application.OrdemServico;

// Detalhe de uma contagem: esperado (na época) x contado, item a item.
public class BuscarContagemPatrimonioQueryHandler(IOrdemServicoRepository repositorio) : IQueryHandler<BuscarContagemPatrimonioQuery>
{
    public async Task<Resultado<object>> HandleAsync(BuscarContagemPatrimonioQuery query)
    {
        var (lojaId, id) = query;
        var contagem = await repositorio.ObterContagemCompletaAsync(id, lojaId);
        if (contagem is null) return Resultado.NaoEncontrado();

        return Resultado.Ok(new ContagemDetalheDto(
            contagem.Id, contagem.DataContagem, contagem.Responsavel, contagem.Observacao,
            contagem.Itens.Select(i => new ItemContagemDetalheDto(
                i.Id, i.ItemPatrimonioId, i.ItemPatrimonio!.Nome, i.ItemPatrimonio.Categoria,
                i.QuantidadeEsperadaNoMomento, i.QuantidadeContada,
                OrdemServicoRegras.DiferencaDaContagem(i.QuantidadeContada, i.QuantidadeEsperadaNoMomento),
                i.ItemPatrimonio.ValorUnitario, i.Observacao)).ToList()));
    }
}
