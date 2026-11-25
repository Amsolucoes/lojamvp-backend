using LojaApi.Application.Acessorios;

namespace LojaApi.Infrastructure.Acessorios;

public static class AcessoriosModule
{
    public static IServiceCollection AddAcessoriosModule(this IServiceCollection services)
    {
        services.AddScoped<IAcessoriosRepository, AcessoriosRepository>();
        services.AddScoped<IPagamentoOnlineAcessorio, PagamentoOnlineAcessorioMercadoPago>();
        services.AddHandlersDoModulo("Acessorios");   // todos os handlers de Application/Handlers/Acessorios
        return services;
    }
}
