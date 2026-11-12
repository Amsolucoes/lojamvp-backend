using LojaApi.Modules.Carteira.Application;

namespace LojaApi.Modules.Carteira;

public static class CarteiraModule
{
    public static IServiceCollection AddCarteiraModule(this IServiceCollection services)
    {
        services.AddScoped<CarteiraService>();
        return services;
    }
}
