using LojaApi.Application.Financeiro;

namespace LojaApi.Infrastructure.Financeiro;

public static class FinanceiroModule
{
    public static IServiceCollection AddFinanceiroModule(this IServiceCollection services)
    {
        services.AddScoped<IFinanceiroRepository, FinanceiroRepository>();
        services.AddHandlersDoModulo("Financeiro");   // todos os handlers de Application/Handlers/Financeiro
        return services;
    }
}
