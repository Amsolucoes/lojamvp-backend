using LojaApi.Application.Equipe;

namespace LojaApi.Infrastructure.Equipe;

public static class EquipeModule
{
    public static IServiceCollection AddEquipeModule(this IServiceCollection services)
    {
        services.AddScoped<IEquipeRepository, EquipeRepository>();
        services.AddScoped<ILancamentoFixoFinanceiro, LancamentoFixoFinanceiroAdapter>();
        services.AddHandlersDoModulo("Equipe");   // todos os handlers de Application/Handlers/Equipe
        return services;
    }
}
