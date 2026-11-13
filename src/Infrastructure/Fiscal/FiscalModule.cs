using LojaApi.Application.Fiscal;

namespace LojaApi.Infrastructure.Fiscal;

public static class FiscalModule
{
    public static IServiceCollection AddFiscalModule(this IServiceCollection services)
    {
        services.AddScoped<IFiscalRepository, FiscalRepository>();
        services.AddScoped<FiscalService>();
        // Sem provedor terceirizado contratado ainda — troca fácil quando um for integrado.
        services.AddScoped<IProvedorFiscal, ProvedorFiscalNaoConfigurado>();
        return services;
    }
}
