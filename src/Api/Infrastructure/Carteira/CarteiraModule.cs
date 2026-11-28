using LojaApi.Application.Carteira;

namespace LojaApi.Infrastructure.Carteira;

public static class CarteiraModule
{
    public static IServiceCollection AddCarteiraModule(this IServiceCollection services)
    {
        services.AddScoped<ICarteiraRepository, CarteiraRepository>();
        services.AddScoped<CarteiraService>();
        return services;
    }
}
