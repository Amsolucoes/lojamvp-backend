using LojaApi.Domain.Plataforma;
using LojaApi.Shared.Application;

namespace LojaApi.Application.Plataforma;

// Horário de funcionamento da agenda (início de 0 a 23, fim de 1 a 24).
public class AtualizarAgendaHorarioCommandHandler(IPlataformaRepository repositorio) : ICommandHandler<AtualizarAgendaHorarioCommand>
{
    public async Task<Resultado<object>> HandleAsync(AtualizarAgendaHorarioCommand command)
    {
        var (lojaId, req) = command;
        if (lojaId is null) return Resultado.NaoEncontrado();

        var loja = await repositorio.ObterLojaAsync(lojaId.Value);
        if (loja is null) return Resultado.NaoEncontrado();

        (loja.AgendaHoraInicio, loja.AgendaHoraFim) = PlataformaRegras.HorarioDaAgenda(req.HoraInicio, req.HoraFim);
        loja.AtualizadoEm = DateTime.UtcNow;
        await repositorio.SalvarAsync();

        return Resultado.Ok(new AgendaHorarioDto(loja.AgendaHoraInicio, loja.AgendaHoraFim));
    }
}
