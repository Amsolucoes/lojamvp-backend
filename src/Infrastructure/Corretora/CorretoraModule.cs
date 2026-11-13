using LojaApi.Application.Corretora;

namespace LojaApi.Infrastructure.Corretora;

public static class CorretoraModule
{
    public static IServiceCollection AddCorretoraModule(this IServiceCollection services)
    {
        services.AddScoped<ICorretoraRepository, CorretoraRepository>();
        services.AddScoped<CorretoraService>();
        return services;
    }
}
