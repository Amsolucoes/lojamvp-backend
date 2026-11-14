using LojaApi.Models;
using LojaApi.Shared.Application;

namespace LojaApi.Application.Produtos;

// Cria uma categoria da loja (nome único entre as ativas), no fim da ordem.
public class CriarCategoriaCommandHandler(IProdutosRepository repositorio) : ICommandHandler<CriarCategoriaCommand>
{
    public async Task<Resultado<object>> HandleAsync(CriarCategoriaCommand command)
    {
        var (lojaId, req) = command;
        if (lojaId is null) return Resultado.Invalido("Loja não encontrada.");

        if (string.IsNullOrWhiteSpace(req.Nome))
            return Resultado.Invalido("Nome da categoria é obrigatório.");

        // Evita duplicada
        if (await repositorio.CategoriaComNomeExisteAsync(lojaId.Value, req.Nome, null))
            return Resultado.Conflito("Já existe uma categoria com esse nome.");

        var cat = new CategoriaLoja
        {
            LojaId = lojaId.Value,
            Nome = req.Nome.Trim(),
            TipoTamanho = req.TipoTamanho,
            UsaTamanho = req.UsaTamanho,
            UsaCor = req.UsaCor,
            TamanhosPersonalizados = req.TamanhosPersonalizados,
            Ordem = await repositorio.ProximaOrdemDeCategoriaAsync(lojaId.Value),
        };
        repositorio.Adicionar(cat);
        await repositorio.SalvarAsync();

        return Resultado.Ok(new CategoriaProdutoDto(cat.Id, cat.Nome, cat.TipoTamanho, cat.UsaTamanho, cat.UsaCor, cat.TamanhosPersonalizados));
    }
}
