using LojaApi.Domain.Acessorios;
using LojaApi.Shared.Application;
using LojaApi.src.Models;
using static LojaApi.Application.Acessorios.AcessoriosApoio;

namespace LojaApi.Application.Acessorios;

// Cria uma categoria; a chave (sem acento, minúscula) é gerada a partir do nome e não pode repetir.
public class CriarCategoriaAcessorioCommandHandler(IAcessoriosRepository repositorio) : ICommandHandler<CriarCategoriaAcessorioCommand>
{
    public async Task<Resultado<object>> HandleAsync(CriarCategoriaAcessorioCommand command)
    {
        var req = command.Req;
        if (string.IsNullOrWhiteSpace(req.Nome))
            return Resultado.Invalido("Digite o nome da categoria.");

        var chave = AcessoriosRegras.GerarChaveCategoria(req.Nome);
        if (chave.Length == 0)
            return Resultado.Invalido("Nome inválido.");

        var jaExiste = await repositorio.CategoriaComChaveExisteAsync(chave);
        if (jaExiste)
            return Resultado.Conflito("Já existe uma categoria com esse nome.");

        var maiorOrdem = await repositorio.MaiorOrdemDeCategoriaAsync();

        var categoria = new CategoriaAcessorio
        {
            Nome = req.Nome.Trim(),
            Chave = chave,
            Ordem = maiorOrdem + 1,
        };
        repositorio.Adicionar(categoria);
        await repositorio.SalvarAsync();

        return Resultado.Ok(categoria);
    }
}
