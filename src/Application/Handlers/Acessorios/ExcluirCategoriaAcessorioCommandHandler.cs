using LojaApi.Domain.Acessorios;
using LojaApi.Shared.Application;
using LojaApi.src.Models;
using static LojaApi.Application.Acessorios.AcessoriosApoio;

namespace LojaApi.Application.Acessorios;

// Exclui uma categoria; se algum produto a usa, só desativa.
public class ExcluirCategoriaAcessorioCommandHandler(IAcessoriosRepository repositorio) : ICommandHandler<ExcluirCategoriaAcessorioCommand>
{
    public async Task<Resultado<object>> HandleAsync(ExcluirCategoriaAcessorioCommand command)
    {
        var categoria = await repositorio.ObterCategoriaAsync(command.Id);
        if (categoria is null) return Resultado.NaoEncontrado();

        var emUso = await repositorio.CategoriaEmUsoAsync(categoria.Chave);
        if (emUso)
        {
            categoria.Ativa = false;
            await repositorio.SalvarAsync();
            return Resultado.Ok(new { mensagem = "Categoria em uso por produto(s) — foi desativada em vez de excluída." });
        }

        repositorio.Remover(categoria);
        await repositorio.SalvarAsync();
        return Resultado.Ok(new { mensagem = "Categoria excluída." });
    }
}
