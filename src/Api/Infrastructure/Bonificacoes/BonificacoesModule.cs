using LojaApi.Application.Bonificacoes;

namespace LojaApi.Infrastructure.Bonificacoes;

public static class BonificacoesModule
{
    public static IServiceCollection AddBonificacoesModule(this IServiceCollection services)
    {
        services.AddScoped<IBonificacoesRepository, BonificacoesRepository>();
        services.AddHandlersDoModulo("Bonificacoes");   // todos os handlers de Application/Handlers/Bonificacoes
        return services;
    }
}
