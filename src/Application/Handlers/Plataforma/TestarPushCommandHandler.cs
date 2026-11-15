using LojaApi.Shared.Application;

namespace LojaApi.Application.Plataforma;

// Manda uma notificação de teste para os aparelhos do usuário; assinatura que o navegador diz não valer mais é removida.
public class TestarPushCommandHandler(IPlataformaRepository repositorio, IServicoDePush push) : ICommandHandler<TestarPushCommand>
{
    public async Task<Resultado<object>> HandleAsync(TestarPushCommand command)
    {
        if (!push.Configurado) return Resultado.Indisponivel("Notificações push não configuradas no servidor.");

        var assinaturas = await repositorio.ListarAssinaturasPushDoUsuarioAsync(command.UsuarioId);
        if (assinaturas.Count == 0) return Resultado.Invalido("Nenhum aparelho com avisos ativos.");

        var enviados = 0;
        foreach (var a in assinaturas)
        {
            var r = await push.EnviarAsync(a, "Avisos ativados ✅", "Você vai receber os vencimentos do dia às 7h.", "/financeiro?aba=pagar", "teste");
            if (r == ResultadoEnvioPush.Enviado) enviados++;
            else if (r == ResultadoEnvioPush.AssinaturaInvalida) repositorio.Remover(a);
        }
        await repositorio.SalvarAsync();
        return Resultado.Ok(new EnviadosPushDto(enviados));
    }
}
