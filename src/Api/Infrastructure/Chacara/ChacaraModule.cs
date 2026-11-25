using LojaApi.Application.Chacara;
using LojaApi.Infrastructure.Chacara;

namespace LojaApi.Infrastructure.Chacara;

// Ponto único de entrada do módulo Chácara: tudo que ele precisa registrar no contêiner de dependências.
public static class ChacaraModule
{
    public static IServiceCollection AddChacaraModule(this IServiceCollection services)
    {
        // Portas do Application ligadas às implementações reais
        services.AddScoped<IPagamentoOnlineChacara, PagamentoOnlineChacaraMercadoPago>();
        services.AddSingleton<IProtetorDeSegredos, ProtetorDeSegredosConfiguracao>();
        services.AddSingleton<IGeradorContratoChacara, GeradorContratoChacaraQuestPdf>();

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
