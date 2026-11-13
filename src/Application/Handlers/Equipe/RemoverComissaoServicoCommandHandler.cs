using LojaApi.Domain.Equipe;
using LojaApi.Models;
using LojaApi.Shared.Application;
using LojaApi.src.Models.Funcionarios;

namespace LojaApi.Application.Equipe;

// Remove a exceção de comissão por serviço (volta à comissão padrão).
public class RemoverComissaoServicoCommandHandler(IEquipeRepository repositorio) : ICommandHandler<RemoverComissaoServicoCommand>
{
    public async Task<Resultado<object>> HandleAsync(RemoverComissaoServicoCommand command)
    {
        var (lojaId, comissaoId) = command;
        var comissao = await repositorio.ObterComissaoServicoPorIdAsync(comissaoId, lojaId);
        if (comissao is null) return Resultado.NaoEncontrado();

        repositorio.Remover(comissao);
        await repositorio.SalvarAsync();
        return Resultado.Ok(new { mensagem = "Exceção removida — volta a usar a comissão padrão do profissional." });
    }
}
