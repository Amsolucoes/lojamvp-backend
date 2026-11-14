using LojaApi.Application.NfImportacao;

namespace LojaApi.Infrastructure.NfImportacao;

public static class NfImportacaoModule
{
    public static IServiceCollection AddNfImportacaoModule(this IServiceCollection services)
    {
        services.AddScoped<INfImportacaoRepository, NfImportacaoRepository>();
        services.AddHandlersDoModulo("NfImportacao");   // todos os handlers de Application/Handlers/NfImportacao
        return services;
    }
}
