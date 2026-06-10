using LojaApi.Services;

namespace LojaApi.Middleware;

public class StatusCheckService(IServiceProvider services, ILogger<StatusCheckService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("StatusCheckService iniciado.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = services.CreateScope();
                var tenantService = scope.ServiceProvider.GetRequiredService<TenantService>();
                await tenantService.VerificarStatusAsync();
                logger.LogInformation("Verificação de status concluída: {Time}", DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro na verificação de status das lojas.");
            }

            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }
}