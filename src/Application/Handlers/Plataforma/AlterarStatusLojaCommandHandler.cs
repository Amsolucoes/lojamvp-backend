using LojaApi.Models;
using LojaApi.Shared.Application;

namespace LojaApi.Application.Plataforma;

// Bloqueia, desbloqueia ou cancela uma loja (o nome do status vale em qualquer caixa).
public class AlterarStatusLojaCommandHandler(IPlataformaRepository repositorio) : ICommandHandler<AlterarStatusLojaCommand>
{
    public async Task<Resultado<object>> HandleAsync(AlterarStatusLojaCommand command)
    {
        var (id, req) = command;
        var loja = await repositorio.ObterLojaAsync(id);
        if (loja is null) return Resultado.NaoEncontrado();

        if (!Enum.TryParse<StatusLoja>(req.Status, true, out var novoStatus))
            return Resultado.Invalido("Status inválido.");

        loja.Status = novoStatus;
        loja.AtualizadoEm = DateTime.UtcNow;
        await repositorio.SalvarAsync();

        return Resultado.Ok(new MensagemDto($"Status alterado para {novoStatus}."));
    }
}
