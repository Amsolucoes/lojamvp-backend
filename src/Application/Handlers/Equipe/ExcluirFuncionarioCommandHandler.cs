using LojaApi.Domain.Equipe;
using LojaApi.Models;
using LojaApi.Shared.Application;
using LojaApi.src.Models.Funcionarios;

namespace LojaApi.Application.Equipe;

// Exclui o funcionário; quem já tem agendamento ou comissão é só desativado.
public class ExcluirFuncionarioCommandHandler(IEquipeRepository repositorio) : ICommandHandler<ExcluirFuncionarioCommand>
{
    public async Task<Resultado<object>> HandleAsync(ExcluirFuncionarioCommand command)
    {
        var (lojaId, id) = command;
        var profissional = await repositorio.ObterProfissionalAsync(id, lojaId);
        if (profissional is null) return Resultado.NaoEncontrado();

        if (await repositorio.ProfissionalEmUsoAsync(id))
        {
            profissional.Ativo = false;
            await repositorio.SalvarAsync();
            return Resultado.Ok(new { mensagem = "Profissional em uso — foi desativado em vez de excluído." });
        }

        repositorio.Remover(profissional);
        await repositorio.SalvarAsync();
        return Resultado.Ok(new { mensagem = "Profissional excluído." });
    }
}
