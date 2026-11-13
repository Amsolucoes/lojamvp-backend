using LojaApi.Application.Equipe;

namespace LojaApi.Infrastructure.Equipe;

public static class EquipeModule
{
    public static IServiceCollection AddEquipeModule(this IServiceCollection services)
    {
        services.AddScoped<IEquipeRepository, EquipeRepository>();
        services.AddScoped<ILancamentoFixoFinanceiro, LancamentoFixoFinanceiroAdapter>();
        services.AddScoped<FuncionariosService>();
        services.AddScoped<ComissoesService>();
        return services;
    }
}
