using LojaApi.Application.Ajuda;

namespace LojaApi.Infrastructure.Ajuda;

public static class AjudaModule
{
    public static IServiceCollection AddAjudaModule(this IServiceCollection services)
    {
        services.AddScoped<IAjudaRepository, AjudaRepository>();
        services.AddScoped<CategoriasAjudaService>();
        services.AddScoped<VideosAjudaService>();
        return services;
    }
}
