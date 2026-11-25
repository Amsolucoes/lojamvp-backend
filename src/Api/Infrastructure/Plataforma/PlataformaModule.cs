using LojaApi.Application.Plataforma;

namespace LojaApi.Infrastructure.Plataforma;

public static class PlataformaModule
{
    public static IServiceCollection AddPlataformaModule(this IServiceCollection services)
    {
        services.AddScoped<IPlataformaRepository, PlataformaRepository>();
        services.AddScoped<IGeradorDeToken, GeradorDeTokenAdaptador>();
        services.AddScoped<INotificadorDeCadastro, NotificadorDeCadastroResend>();
        services.AddScoped<IServicoDePush, ServicoDePushAdaptador>();
        services.AddScoped<IAdminRepository, AdminRepository>();
        services.AddScoped<IGatewayDePagamentoPlataforma, GatewayDePagamentoPlataformaAdaptador>();
        services.AddScoped<IEnvioDeComunicados, EnvioDeComunicadosAdaptador>();
        services.AddHandlersDoModulo("Plataforma");   // todos os handlers de Application/Handlers/Plataforma
        return services;
    }
}
