using LojaApi.Models;
using LojaApi.Shared.Application;

namespace LojaApi.Application.Plataforma;

// Cria uma categoria de produto na loja.
public class AdicionarCategoriaCommandHandler(IPlataformaRepository repositorio) : ICommandHandler<AdicionarCategoriaCommand>
{
    public async Task<Resultado<object>> HandleAsync(AdicionarCategoriaCommand command)
    {
        var (lojaId, req) = command;
        if (lojaId is null) return Resultado.NaoEncontrado();

        var categoria = new CategoriaLoja { LojaId = lojaId.Value, Nome = req.Nome, Ordem = req.Ordem };
        repositorio.Adicionar(categoria);
        await repositorio.SalvarAsync();
        return Resultado.Ok(new CategoriaCriadaDto(categoria.Id, categoria.Nome, categoria.Ativo, categoria.Ordem));
    }
}
