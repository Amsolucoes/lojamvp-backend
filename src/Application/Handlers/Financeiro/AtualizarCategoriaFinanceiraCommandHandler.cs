using LojaApi.Shared.Application;

namespace LojaApi.Application.Financeiro;

// Edita nome, tipo e ícone da categoria.
public class AtualizarCategoriaFinanceiraCommandHandler(IFinanceiroRepository repositorio) : ICommandHandler<AtualizarCategoriaFinanceiraCommand>
{
    public async Task<Resultado<object>> HandleAsync(AtualizarCategoriaFinanceiraCommand command)
    {
        var (lojaId, id, req) = command;
        if (lojaId is null) return Resultado.NaoEncontrado();
        var cat = await repositorio.ObterCategoriaAsync(id, lojaId.Value);
        if (cat is null) return Resultado.NaoEncontrado();

        cat.Nome = req.Nome.Trim();
        cat.Tipo = req.Tipo;
        cat.Icone = req.Icone;
        await repositorio.SalvarAsync();

        return Resultado.Ok(new CategoriaFinanceiraDto(cat.Id, cat.Nome, cat.Tipo, cat.Icone));
    }
}
