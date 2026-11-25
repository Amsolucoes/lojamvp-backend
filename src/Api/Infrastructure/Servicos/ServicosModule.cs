using LojaApi.Application.Servicos;

namespace LojaApi.Infrastructure.Servicos;

// Registro do módulo Serviços/Agenda (catálogo, agenda, planos de assinatura e agendamento online)
public static class ServicosModule
{
    public static IServiceCollection AddServicosModule(this IServiceCollection services)
    {
        services.AddScoped<CatalogoServicosService>();
        services.AddScoped<AgendamentosService>();
        services.AddScoped<PlanosAssinaturaService>();
        services.AddScoped<AgendamentoOnlineService>();
        return services;
    }
}
