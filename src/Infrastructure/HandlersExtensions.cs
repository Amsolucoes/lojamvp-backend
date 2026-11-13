using LojaApi.Shared.Application;

namespace LojaApi.Infrastructure;

public static class HandlersExtensions
{
    // Registra todo handler (ICommandHandler/IQueryHandler) do módulo: as classes de LojaApi.Application.<Modulo>.
    // Assim um handler novo entra sozinho, sem precisar lembrar de uma linha de registro.
    public static IServiceCollection AddHandlersDoModulo(this IServiceCollection services, string modulo)
    {
        var espaco = $"LojaApi.Application.{modulo}";
        var handlers = typeof(Resultado<>).Assembly.GetTypes()
            .Where(t => t.Namespace == espaco && t is { IsClass: true, IsAbstract: false });

        foreach (var tipo in handlers)
        foreach (var contrato in tipo.GetInterfaces().Where(i => i.IsGenericType &&
                     (i.GetGenericTypeDefinition() == typeof(ICommandHandler<>) || i.GetGenericTypeDefinition() == typeof(IQueryHandler<>))))
            services.AddScoped(contrato, tipo);

        return services;
    }
}
