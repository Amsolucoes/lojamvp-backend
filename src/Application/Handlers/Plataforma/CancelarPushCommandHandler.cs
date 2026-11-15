using LojaApi.Shared.Application;

namespace LojaApi.Application.Plataforma;

// Desliga os avisos push de um aparelho — só o usuário dono da assinatura cancela.
public class CancelarPushCommandHandler(IPlataformaRepository repositorio) : ICommandHandler<CancelarPushCommand>
{
    public async Task<Resultado<object>> HandleAsync(CancelarPushCommand command)
    {
        var (usuarioId, req) = command;
        var existente = await repositorio.ObterAssinaturaPushDoUsuarioAsync(usuarioId, req.Endpoint);
        if (existente is not null)
        {
            repositorio.Remover(existente);
            await repositorio.SalvarAsync();
        }
        return Resultado.Ok(new AtivoPushDto(false));
    }
}
