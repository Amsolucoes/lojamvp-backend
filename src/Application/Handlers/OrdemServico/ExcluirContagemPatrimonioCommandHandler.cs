using LojaApi.Shared.Application;

namespace LojaApi.Application.OrdemServico;

// Exclui uma contagem (os itens dela vão junto).
public class ExcluirContagemPatrimonioCommandHandler(IOrdemServicoRepository repositorio) : ICommandHandler<ExcluirContagemPatrimonioCommand>
{
    public async Task<Resultado<object>> HandleAsync(ExcluirContagemPatrimonioCommand command)
    {
        var (lojaId, id) = command;
        var contagem = await repositorio.ObterContagemAsync(id, lojaId);
        if (contagem is null) return Resultado.NaoEncontrado();

        repositorio.Remover(contagem); // cascade remove os itens junto
        await repositorio.SalvarAsync();
        return Resultado.Ok(new { mensagem = "Contagem excluída." });
    }
}
