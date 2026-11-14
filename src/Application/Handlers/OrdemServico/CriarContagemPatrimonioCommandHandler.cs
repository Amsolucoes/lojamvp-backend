using LojaApi.Domain.OrdemServico;
using LojaApi.Shared.Application;
using LojaApi.src.Models.OrdemServico;

namespace LojaApi.Application.OrdemServico;

// Registra uma contagem do patrimônio, guardando o "esperado" da época de cada item.
public class CriarContagemPatrimonioCommandHandler(IOrdemServicoRepository repositorio) : ICommandHandler<CriarContagemPatrimonioCommand>
{
    public async Task<Resultado<object>> HandleAsync(CriarContagemPatrimonioCommand command)
    {
        var (lojaId, req) = command;
        if (lojaId is null) return Resultado.Invalido("Loja não encontrada.");

        if (req.Itens is null || req.Itens.Count == 0)
            return Resultado.Invalido("A contagem precisa ter ao menos um item.");

        var itemIds = req.Itens.Select(i => i.ItemPatrimonioId).ToList();
        var itensPatrimonio = await repositorio.ListarItensPatrimonioPorIdsAsync(itemIds, lojaId.Value);

        if (itensPatrimonio.Count != itemIds.Distinct().Count())
            return Resultado.Invalido("Algum item de patrimônio informado não foi encontrado.");

        var contagem = new ContagemPatrimonio
        {
            LojaId = lojaId.Value,
            DataContagem = OrdemServicoRegras.DataDaContagem(req.DataContagem, DateTime.UtcNow),
            Responsavel = req.Responsavel,
            Observacao = req.Observacao,
        };

        foreach (var i in req.Itens)
        {
            var itemPatrimonio = itensPatrimonio.First(x => x.Id == i.ItemPatrimonioId);
            contagem.Itens.Add(new ItemContagemPatrimonio
            {
                LojaId = lojaId.Value,
                ItemPatrimonioId = itemPatrimonio.Id,
                QuantidadeEsperadaNoMomento = itemPatrimonio.QuantidadeEsperada, // snapshot da época
                QuantidadeContada = i.QuantidadeContada,
                Observacao = i.Observacao,
            });
        }

        repositorio.Adicionar(contagem);
        await repositorio.SalvarAsync();

        return Resultado.Ok(new { contagem.Id, contagem.DataContagem });
    }
}
