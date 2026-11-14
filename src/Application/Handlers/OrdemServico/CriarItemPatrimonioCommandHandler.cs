using LojaApi.Domain.OrdemServico;
using LojaApi.Shared.Application;
using LojaApi.src.Models.OrdemServico;

namespace LojaApi.Application.OrdemServico;

// Cadastra um item de patrimônio.
public class CriarItemPatrimonioCommandHandler(IOrdemServicoRepository repositorio) : ICommandHandler<CriarItemPatrimonioCommand>
{
    public async Task<Resultado<object>> HandleAsync(CriarItemPatrimonioCommand command)
    {
        var (lojaId, req) = command;
        if (lojaId is null) return Resultado.Invalido("Loja não encontrada.");

        if (string.IsNullOrWhiteSpace(req.Nome)) return Resultado.Invalido("Nome é obrigatório.");

        var item = new ItemPatrimonio
        {
            LojaId = lojaId.Value,
            Nome = req.Nome.Trim(),
            Categoria = OrdemServicoRegras.NormalizarCategoriaDePatrimonio(req.Categoria),
            QuantidadeEsperada = req.QuantidadeEsperada,
            ValorUnitario = req.ValorUnitario,
            Observacao = req.Observacao,
            Ativo = req.Ativo,
        };
        repositorio.Adicionar(item);
        await repositorio.SalvarAsync();

        return Resultado.Ok(new { item.Id, item.Nome, item.Categoria, item.QuantidadeEsperada, item.ValorUnitario, item.Ativo });
    }
}
