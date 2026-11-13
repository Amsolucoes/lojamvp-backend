using LojaApi.Domain.Equipe;
using LojaApi.Models;
using LojaApi.Shared.Application;
using LojaApi.src.Models.Funcionarios;

namespace LojaApi.Application.Equipe;

// Define (ou atualiza) a comissão de um serviço específico para o funcionário.
public class DefinirComissaoServicoCommandHandler(IEquipeRepository repositorio) : ICommandHandler<DefinirComissaoServicoCommand>
{
    public async Task<Resultado<object>> HandleAsync(DefinirComissaoServicoCommand command)
    {
        var (lojaId, id, req) = command;
        var profissional = await repositorio.ObterProfissionalAsync(id, lojaId);
        if (profissional is null) return Resultado.NaoEncontrado();

        if (!await repositorio.ServicoDaLojaExisteAsync(req.ServicoId, lojaId!.Value))
            return Resultado.Invalido("Serviço não encontrado.");

        var existente = await repositorio.ObterComissaoServicoAsync(id, req.ServicoId);
        if (existente != null)
        {
            existente.ComissaoPercentual = req.ComissaoPercentual;
        }
        else
        {
            repositorio.Adicionar(new ComissaoServicoProfissional
            {
                LojaId = lojaId.Value,
                ProfissionalId = id,
                ServicoId = req.ServicoId,
                ComissaoPercentual = req.ComissaoPercentual,
            });
        }

        await repositorio.SalvarAsync();
        return Resultado.Ok(new { mensagem = "Comissão do serviço definida." });
    }
}
