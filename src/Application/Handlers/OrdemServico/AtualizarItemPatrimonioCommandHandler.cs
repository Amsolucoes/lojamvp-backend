using LojaApi.Domain.OrdemServico;
using LojaApi.Shared.Application;

namespace LojaApi.Application.OrdemServico;

// Atualiza um item de patrimônio.
public class AtualizarItemPatrimonioCommandHandler(IOrdemServicoRepository repositorio) : ICommandHandler<AtualizarItemPatrimonioCommand>
{
    public async Task<Resultado<object>> HandleAsync(AtualizarItemPatrimonioCommand command)
    {
        var (lojaId, id, req) = command;
        var item = await repositorio.ObterItemPatrimonioAsync(id, lojaId);
        if (item is null) return Resultado.NaoEncontrado();

        item.Nome = req.Nome.Trim();
        item.Categoria = OrdemServicoRegras.NormalizarCategoriaDePatrimonio(req.Categoria);
        item.QuantidadeEsperada = req.QuantidadeEsperada;
        item.ValorUnitario = req.ValorUnitario;
        item.Observacao = req.Observacao;
        item.Ativo = req.Ativo;
        item.AtualizadoEm = DateTime.UtcNow;
        await repositorio.SalvarAsync();

        return Resultado.Ok(new { item.Id, item.Nome, item.Categoria, item.QuantidadeEsperada, item.ValorUnitario, item.Ativo });
    }
}
