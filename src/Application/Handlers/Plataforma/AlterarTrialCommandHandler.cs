using LojaApi.Models;
using LojaApi.Shared.Application;

namespace LojaApi.Application.Plataforma;

// Muda o fim do trial (até 23:59 do dia escolhido); se a loja estava bloqueada, volta a ser trial.
public class AlterarTrialCommandHandler(IPlataformaRepository repositorio) : ICommandHandler<AlterarTrialCommand>
{
    public async Task<Resultado<object>> HandleAsync(AlterarTrialCommand command)
    {
        var (id, req) = command;
        var loja = await repositorio.ObterLojaAsync(id);
        if (loja is null) return Resultado.NaoEncontrado();

        loja.TrialAte = DateTime.SpecifyKind(req.TrialAte.Date, DateTimeKind.Utc).AddHours(23).AddMinutes(59);

        if (loja.Status == StatusLoja.Bloqueado)
            loja.Status = StatusLoja.Trial;

        loja.AtualizadoEm = DateTime.UtcNow;
        await repositorio.SalvarAsync();

        return Resultado.Ok(new TrialAlteradoDto(loja.Id, loja.TrialAte, loja.Status));
    }
}
