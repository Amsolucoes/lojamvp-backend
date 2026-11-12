using LojaApi.Modules.Chacara.Application;
using LojaApi.Modules.Chacara.Infrastructure;

namespace LojaApi.Modules.Chacara;

// Ponto único de entrada do módulo Chácara: tudo que ele precisa registrar no contêiner de dependências.
public static class ChacaraModule
{
    public static IServiceCollection AddChacaraModule(this IServiceCollection services)
    {
        services.AddScoped<ReservaChacaraNotificacaoService>();

        // Casos de uso (camada de aplicação)
        services.AddScoped<HorariosChacaraService>();
        services.AddScoped<PeriodosEspeciaisChacaraService>();
        services.AddScoped<FaixasPrecoChacaraService>();
        services.AddScoped<ConfiguracaoPrecoChacaraService>();
        services.AddScoped<FotosChacaraService>();
        services.AddScoped<InfoChacaraService>();
        services.AddScoped<DashboardChacaraService>();
        services.AddScoped<ReservasChacaraService>();
        services.AddScoped<CredencialMercadoPagoChacaraService>();
        services.AddScoped<SiteChacaraService>();
        services.AddScoped<PagamentoReservaChacaraService>();

        services.AddHostedService<ReservaChacaraRotinaService>();
        return services;
    }
}
