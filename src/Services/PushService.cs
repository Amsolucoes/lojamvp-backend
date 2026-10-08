using System.Net;
using System.Text.Json;
using LojaApi.Models;
using WebPush;

namespace LojaApi.Services;

// Envia notificações push (Web Push / VAPID) para os aparelhos que ativaram o aviso.
// Configuração (variáveis de ambiente ou appsettings): Push__PublicKey, Push__PrivateKey, Push__Subject
public class PushService(IConfiguration config, ILogger<PushService> logger)
{
    private readonly WebPushClient _client = new();

    public string? ChavePublica => config["Push:PublicKey"];
    private string? ChavePrivada => config["Push:PrivateKey"];
    private string Subject => config["Push:Subject"] ?? "mailto:contato@aldevsoftware.com.br";

    public bool Configurado => !string.IsNullOrWhiteSpace(ChavePublica) && !string.IsNullOrWhiteSpace(ChavePrivada);

    public enum Resultado { Enviado, AssinaturaInvalida, Falhou }

    public async Task<Resultado> EnviarAsync(AssinaturaPush assinatura, string titulo, string corpo, string url, string tag, CancellationToken ct = default)
    {
        if (!Configurado) return Resultado.Falhou;

        var payload = JsonSerializer.Serialize(new { title = titulo, body = corpo, url, tag });
        var subscription = new PushSubscription(assinatura.Endpoint, assinatura.ChavePublica, assinatura.ChaveAuth);
        var vapid = new VapidDetails(Subject, ChavePublica!, ChavePrivada!);

        try
        {
            await _client.SendNotificationAsync(subscription, payload, vapid, ct);
            return Resultado.Enviado;
        }
        catch (WebPushException ex) when (ex.StatusCode == HttpStatusCode.Gone || ex.StatusCode == HttpStatusCode.NotFound)
        {
            // Aparelho desinstalou o app / revogou a permissão — a assinatura não vale mais.
            return Resultado.AssinaturaInvalida;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Falha ao enviar push para {Endpoint}.", assinatura.Endpoint);
            return Resultado.Falhou;
        }
    }
}
