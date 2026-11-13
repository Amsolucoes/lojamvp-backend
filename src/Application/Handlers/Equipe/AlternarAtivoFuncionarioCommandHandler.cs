using LojaApi.Domain.Equipe;
using LojaApi.Models;
using LojaApi.Shared.Application;
using LojaApi.src.Models.Funcionarios;

namespace LojaApi.Application.Equipe;

// Ativa ou desativa o funcionário; o lançamento fixo do salário acompanha.
public class AlternarAtivoFuncionarioCommandHandler(IEquipeRepository repositorio) : ICommandHandler<AlternarAtivoFuncionarioCommand>
{
    public async Task<Resultado<object>> HandleAsync(AlternarAtivoFuncionarioCommand command)
    {
        var (lojaId, id) = command;
        var profissional = await repositorio.ObterProfissionalAsync(id, lojaId);
        if (profissional is null) return Resultado.NaoEncontrado();

        profissional.Ativo = !profissional.Ativo;

        // O salário fixo acompanha: desativado não gera mais lançamentos
        if (profissional.LancamentoFixoId.HasValue)
        {
            var fixo = await repositorio.ObterLancamentoFixoAsync(profissional.LancamentoFixoId.Value);
            if (fixo != null) fixo.Ativa = profissional.Ativo;
        }

        await repositorio.SalvarAsync();
        return Resultado.Ok(new { profissional.Id, profissional.Ativo });
    }
}
