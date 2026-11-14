using LojaApi.Application.OrdemServico;

namespace LojaApi.Infrastructure.OrdemServico;

public static class OrdemServicoModule
{
    public static IServiceCollection AddOrdemServicoModule(this IServiceCollection services)
    {
        services.AddScoped<IOrdemServicoRepository, OrdemServicoRepository>();
        services.AddScoped<IOrcamentoNotificacao, OrdemServicoNotificacaoService>();
        services.AddScoped<IRelatorioPatrimonioPdf, RelatorioPatrimonioPdf>();
        services.AddHandlersDoModulo("OrdemServico");   // todos os handlers de Application/Handlers/OrdemServico
        return services;
    }
}
