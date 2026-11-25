using LojaApi.Application.Turmas;

namespace LojaApi.Infrastructure.Turmas;

public static class TurmasModule
{
    public static IServiceCollection AddTurmasModule(this IServiceCollection services)
    {
        services.AddScoped<ITurmasRepository, TurmasRepository>();
        services.AddScoped<GeradorSessoesTurmaService>();
        services.AddScoped<TurmasService>();
        return services;
    }
}
