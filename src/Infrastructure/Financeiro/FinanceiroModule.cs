using LojaApi.Application.Financeiro;

namespace LojaApi.Infrastructure.Financeiro;

public static class FinanceiroModule
{
    public static IServiceCollection AddFinanceiroModule(this IServiceCollection services)
    {
        services.AddScoped<IFinanceiroRepository, FinanceiroRepository>();
        services.AddScoped<ILancamentosRepository, LancamentosRepository>();
        services.AddScoped<IGeradorDeLancamentosFixos, GeradorDeLancamentosFixosAdapter>();
        services.AddScoped<ICartoesRepository, CartoesRepository>();
        services.AddScoped<IGeradorDeComprasFixas, GeradorDeComprasFixasAdapter>();
        services.AddScoped<IFaturasRepository, FaturasRepository>();
        services.AddScoped<CorrecaoDeFinanciamentos>();
        services.AddScoped<CicloAtualDoCartao>();
        services.AddHandlersDoModulo("Financeiro");   // todos os handlers de Application/Handlers/Financeiro
        return services;
    }
}
