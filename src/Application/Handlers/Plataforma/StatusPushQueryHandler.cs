using LojaApi.Shared.Application;

namespace LojaApi.Application.Plataforma;

// Se o aparelho (endpoint) do usuário está com os avisos ativos.
public class StatusPushQueryHandler(IPlataformaRepository repositorio, IServicoDePush push) : IQueryHandler<StatusPushQuery>
{
    public async Task<Resultado<object>> HandleAsync(StatusPushQuery query)
    {
        var (usuarioId, endpoint) = query;
        var ativo = !string.IsNullOrEmpty(endpoint) && await repositorio.AssinaturaPushExisteAsync(usuarioId, endpoint);
        return Resultado.Ok(new StatusPushDto(push.Configurado, ativo));
    }
}
