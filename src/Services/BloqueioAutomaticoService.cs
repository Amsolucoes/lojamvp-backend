namespace LojaApi.Services;

public class BloqueioAutomaticoService(
    IServiceProvider serviceProvider,
    ILogger<BloqueioAutomaticoService> logger) : BackgroundService
{
    // Roda todo dia às 06:00 UTC (03:00 no horário de Brasília)
    private const int HORA_EXECUCAO_UTC = 6;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("BloqueioAutomaticoService iniciado.");

        while (!stoppingToken.IsCancellationRequested)
        {
            var agora = DateTime.UtcNow;

            // Calcula quanto falta até a próxima execução (06:00 UTC)
            var proximaExecucao = new DateTime(agora.Year, agora.Month, agora.Day, HORA_EXECUCAO_UTC, 0, 0, DateTimeKind.Utc);
            if (proximaExecucao <= agora)
                proximaExecucao = proximaExecucao.AddDays(1);

            var espera = proximaExecucao - agora;
            logger.LogInformation("Próxima verificação de bloqueio em {Horas:F1}h ({Data} UTC).", espera.TotalHours, proximaExecucao);

            try
            {
                await Task.Delay(espera, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break; // app está encerrando
            }

            // Executa a verificação
            try
            {
                using var scope = serviceProvider.CreateScope();
                var tenantService = scope.ServiceProvider.GetRequiredService<TenantService>();
                await tenantService.VerificarStatusAsync();

                var planosService = scope.ServiceProvider.GetRequiredService<PlanosService>();
                await planosService.GerarPendenciasMensaisAsync();

                var financeiroService = scope.ServiceProvider.GetRequiredService<FinanceiroService>();
                await financeiroService.GerarPendenciasMensaisAsync();
                await financeiroService.GerarPendenciasCartaoFixoAsync();

                logger.LogInformation("Verificação automática de bloqueio concluída em {Data} UTC.", DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro na verificação automática de bloqueio.");
            }
        }
    }
}