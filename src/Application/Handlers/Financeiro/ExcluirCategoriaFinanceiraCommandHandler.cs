using LojaApi.Shared.Application;

namespace LojaApi.Application.Financeiro;

// Exclui a categoria; se já foi usada em lançamento ou lançamento fixo, só desativa (para não quebrar o histórico).
public class ExcluirCategoriaFinanceiraCommandHandler(IFinanceiroRepository repositorio) : ICommandHandler<ExcluirCategoriaFinanceiraCommand>
{
    public async Task<Resultado<object>> HandleAsync(ExcluirCategoriaFinanceiraCommand command)
    {
        var (lojaId, id) = command;
        if (lojaId is null) return Resultado.NaoEncontrado();
        var cat = await repositorio.ObterCategoriaAsync(id, lojaId.Value);
        if (cat is null) return Resultado.NaoEncontrado();

        if (await repositorio.CategoriaEmUsoAsync(id))
        {
            cat.Ativa = false;
            await repositorio.SalvarAsync();
            return Resultado.Ok(new MensagemDto("Categoria em uso — foi desativada em vez de excluída."));
        }

        repositorio.Remover(cat);
        await repositorio.SalvarAsync();
        return Resultado.Ok(new MensagemDto("Categoria excluída."));
    }
}
