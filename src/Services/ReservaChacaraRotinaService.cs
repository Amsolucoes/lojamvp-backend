using LojaApi.src.Services;

namespace LojaApi.Services;

// Roda de hora em hora: expira reservas sem confirmação/pagamento (libera a data) e envia o aviso
// de 24h das reservas feitas sem pagamento online. A rotina diária (BloqueioAutomaticoService) continua existindo.
public class ReservaChacaraRotinaService(IServiceProvider serviceProvider, ILogger<ReservaChacaraRotinaService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(60), stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }

            try
            {
                using var scope = serviceProvider.CreateScope();
                var servico = scope.ServiceProvider.GetRequiredService<ReservaChacaraNotificacaoService>();
                await servico.EnviarAvisosReservasSemPagamentoAsync();
                await servico.ExpirarReservasVencidasAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro na rotina horária de reservas da chácara.");
            }
        }
    }
}
