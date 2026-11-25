using LojaApi.Shared.Application;

namespace LojaApi.Shared;

// Serviços usados por todos os módulos
public static class SharedModule
{
    public static IServiceCollection AddSharedKernel(this IServiceCollection services)
    {
        services.AddScoped<ILojaAtual, LojaAtualService>();
        return services;
    }
}
