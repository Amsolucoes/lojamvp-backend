using LojaApi.Shared.Application;

namespace LojaApi.Application.Plataforma;

// Chave pública que o navegador precisa para se inscrever nos avisos push.
public class ChavePublicaPushQueryHandler(IServicoDePush push) : IQueryHandler<ChavePublicaPushQuery>
{
    public Task<Resultado<object>> HandleAsync(ChavePublicaPushQuery query) =>
        Task.FromResult(push.Configurado
            ? Resultado.Ok(new ChavePushDto(push.ChavePublica))
            : Resultado.Indisponivel("Notificações push não configuradas no servidor."));
}
