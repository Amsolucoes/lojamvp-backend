using LojaApi.Application.Vendas;

namespace LojaApi.Infrastructure.Vendas;

public static class VendasModule
{
    public static IServiceCollection AddVendasModule(this IServiceCollection services)
    {
        services.AddScoped<IVendasRepository, VendasRepository>();
        services.AddHandlersDoModulo("Vendas");   // todos os handlers de Application/Handlers/Vendas
        return services;
    }
}
