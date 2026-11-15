using LojaApi.Models;

namespace LojaApi.Application.Plataforma;

public enum ResultadoEnvioPush { Enviado, AssinaturaInvalida, Falhou }

// Porta: avisos push no navegador (Web Push). Implementada na Infrastructure com o PushService.
public interface IServicoDePush
{
    bool Configurado { get; }
    string? ChavePublica { get; }
    Task<ResultadoEnvioPush> EnviarAsync(AssinaturaPush assinatura, string titulo, string corpo, string url, string tag);
}
