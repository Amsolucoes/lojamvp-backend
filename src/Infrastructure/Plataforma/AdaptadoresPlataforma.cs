using LojaApi.Application.Plataforma;
using LojaApi.Models;
using LojaApi.Services;

namespace LojaApi.Infrastructure.Plataforma;

// Liga as portas da Application aos serviços da API (token JWT e avisos push).

public class GeradorDeTokenAdaptador(TokenService tokenService) : IGeradorDeToken
{
    public string Gerar(Usuario usuario, bool acessoSuporte = false) => tokenService.GerarToken(usuario, acessoSuporte);
}

public class ServicoDePushAdaptador(PushService push) : IServicoDePush
{
    public bool Configurado => push.Configurado;
    public string? ChavePublica => push.ChavePublica;

    public async Task<ResultadoEnvioPush> EnviarAsync(AssinaturaPush assinatura, string titulo, string corpo, string url, string tag) =>
        await push.EnviarAsync(assinatura, titulo, corpo, url, tag) switch
        {
            PushService.Resultado.Enviado => ResultadoEnvioPush.Enviado,
            PushService.Resultado.AssinaturaInvalida => ResultadoEnvioPush.AssinaturaInvalida,
            _ => ResultadoEnvioPush.Falhou,
        };
}
