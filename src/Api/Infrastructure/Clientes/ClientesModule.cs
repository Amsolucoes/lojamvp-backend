using LojaApi.Application.Clientes;

namespace LojaApi.Infrastructure.Clientes;

public static class ClientesModule
{
    public static IServiceCollection AddClientesModule(this IServiceCollection services)
    {
        services.AddScoped<IClientesRepository, ClientesRepository>();
        services.AddScoped<ClientesService>();
        return services;
    }
}
