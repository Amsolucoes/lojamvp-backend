using LojaApi.Shared.Application;

namespace LojaApi.Application.Plataforma;

// Apaga a loja e todos os dados dela (e os usuários que só pertenciam a ela), numa transação: se algo ficar preso, nada é apagado.
public class ExcluirLojaCommandHandler(IAdminRepository repositorio) : ICommandHandler<ExcluirLojaCommand>
{
    public async Task<Resultado<object>> HandleAsync(ExcluirLojaCommand command)
    {
        if (!await repositorio.LojaExisteAsync(command.Id)) return Resultado.NaoEncontrado();

        var (ok, erro) = await repositorio.ExcluirLojaComTodosOsDadosAsync(command.Id);
        return ok
            ? Resultado.Ok(new MensagemDto("Loja e todos os dados foram removidos."))
            : Resultado.Falha(erro!);
    }
}
