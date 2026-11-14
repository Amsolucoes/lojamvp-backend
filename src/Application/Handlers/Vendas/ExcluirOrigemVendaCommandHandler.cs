using LojaApi.Shared.Application;

namespace LojaApi.Application.Vendas;

// Exclui uma origem; se já foi usada em vendas, só desativa.
public class ExcluirOrigemVendaCommandHandler(IVendasRepository repositorio) : ICommandHandler<ExcluirOrigemVendaCommand>
{
    public async Task<Resultado<object>> HandleAsync(ExcluirOrigemVendaCommand command)
    {
        var (lojaId, id) = command;
        var origem = lojaId is null ? null : await repositorio.ObterOrigemAsync(id, lojaId);
        if (origem is null) return Resultado.NaoEncontrado();

        if (await repositorio.OrigemEmUsoAsync(id))
        {
            origem.Ativa = false; // desativa em vez de excluir, se já usada em vendas
            await repositorio.SalvarAsync();
            return Resultado.Ok(new { mensagem = "Origem em uso — foi desativada em vez de excluída." });
        }

        repositorio.Remover(origem);
        await repositorio.SalvarAsync();
        return Resultado.Ok(new { mensagem = "Origem excluída." });
    }
}
