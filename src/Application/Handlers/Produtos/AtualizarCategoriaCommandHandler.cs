using LojaApi.Domain.Produtos;
using LojaApi.Shared.Application;

namespace LojaApi.Application.Produtos;

// Edita uma categoria; se o nome mudou, os produtos que usavam o nome antigo passam para o novo.
public class AtualizarCategoriaCommandHandler(IProdutosRepository repositorio) : ICommandHandler<AtualizarCategoriaCommand>
{
    public async Task<Resultado<object>> HandleAsync(AtualizarCategoriaCommand command)
    {
        var (lojaId, id, req) = command;
        if (lojaId is null) return Resultado.Invalido("Loja não encontrada.");

        var cat = await repositorio.ObterCategoriaAsync(id, lojaId.Value);
        if (cat is null) return Resultado.NaoEncontrado();

        if (string.IsNullOrWhiteSpace(req.Nome))
            return Resultado.Invalido("Nome da categoria é obrigatório.");

        // Se mudou o nome, atualiza os produtos que usam o nome antigo
        var nomeAntigo = cat.Nome;
        var nomeNovo = req.Nome.Trim();
        if (ProdutosRegras.MudouDeNome(nomeAntigo, nomeNovo))
        {
            if (await repositorio.CategoriaComNomeExisteAsync(lojaId.Value, nomeNovo, id))
                return Resultado.Conflito("Já existe uma categoria com esse nome.");

            await repositorio.RenomearCategoriaDosProdutosAsync(lojaId.Value, nomeAntigo, nomeNovo);
        }

        cat.Nome = nomeNovo;
        cat.TipoTamanho = req.TipoTamanho;
        cat.UsaTamanho = req.UsaTamanho;
        cat.UsaCor = req.UsaCor;
        cat.TamanhosPersonalizados = req.TamanhosPersonalizados;
        await repositorio.SalvarAsync();

        return Resultado.Ok(new CategoriaProdutoDto(cat.Id, cat.Nome, cat.TipoTamanho, cat.UsaTamanho, cat.UsaCor, cat.TamanhosPersonalizados));
    }
}
