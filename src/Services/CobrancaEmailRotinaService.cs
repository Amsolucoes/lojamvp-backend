namespace LojaApi.Services;

// Todo dia, a partir das 09:00 (horário de Brasília), dispara os e-mails de cobrança das mensalidades
// vencidas ou que vencem hoje. Cada fatura só recebe um e-mail por dia (controlado no banco).
public class CobrancaEmailRotinaService(
    IServiceProvider serviceProvider,
    ILogger<CobrancaEmailRotinaService> logger) : BackgroundService
{
    private static readonly TimeSpan OffsetBrasilia = TimeSpan.FromHours(-3); // sem horário de verão desde 2019
    private const int HORA_ENVIO = 9;
    private const int HORA_LIMITE = 14; // se o servidor estava fora do ar às 9h, ainda envia até as 14h

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("CobrancaEmailRotinaService iniciado.");

        while (!stoppingToken.IsCancellationRequested)
        {
            var agoraUtc = DateTime.UtcNow;
            var hora = (agoraUtc + OffsetBrasilia).Hour;
            var dentroDaJanela = hora >= HORA_ENVIO && hora < HORA_LIMITE;

            if (dentroDaJanela)
            {
                try
                {
                    using var scope = serviceProvider.CreateScope();
                    var servico = scope.ServiceProvider.GetRequiredService<CobrancaEmailService>();
                    var enviados = await servico.EnviarCobrancasDoDiaAsync(agoraUtc, stoppingToken);
                    if (enviados > 0) logger.LogInformation("{N} e-mail(s) de cobrança enviados.", enviados);
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Erro na rotina de cobrança por e-mail.");
                }
            }

            try
            {
                // Na janela, tenta de hora em hora (só reenvia quem falhou); fora dela, confere a cada 10 min
                await Task.Delay(dentroDaJanela ? TimeSpan.FromMinutes(60) : TimeSpan.FromMinutes(10), stoppingToken);
            }
            catch (TaskCanceledException) { break; }
        }
    }
}
