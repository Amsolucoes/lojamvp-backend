using LojaApi.Application.Produtos;

namespace LojaApi.Infrastructure.Produtos;

public static class ProdutosModule
{
    public static IServiceCollection AddProdutosModule(this IServiceCollection services)
    {
        services.AddScoped<IProdutosRepository, ProdutosRepository>();
        services.AddHandlersDoModulo("Produtos");   // todos os handlers de Application/Handlers/Produtos
        return services;
    }
}
