using LojaApi.Models;
using LojaApi.Shared.Application;

namespace LojaApi.Application.Plataforma;

// Ativa os avisos push de um aparelho. O mesmo aparelho pode trocar de usuário/loja: atualiza em vez de duplicar.
public class AssinarPushCommandHandler(IPlataformaRepository repositorio) : ICommandHandler<AssinarPushCommand>
{
    public async Task<Resultado<object>> HandleAsync(AssinarPushCommand command)
    {
        var (usuarioId, lojaIdOuNulo, req) = command;
        if (string.IsNullOrWhiteSpace(req.Endpoint) || string.IsNullOrWhiteSpace(req.ChavePublica) || string.IsNullOrWhiteSpace(req.ChaveAuth))
            return Resultado.Invalido("Assinatura inválida.");

        if (lojaIdOuNulo is not { } lojaId) return Resultado.Invalido("Loja não encontrada.");

        var existente = await repositorio.ObterAssinaturaPushPorEndpointAsync(req.Endpoint);
        if (existente is null)
        {
            repositorio.Adicionar(new AssinaturaPush
            {
                UsuarioId = usuarioId,
                LojaId = lojaId,
                Endpoint = req.Endpoint,
                ChavePublica = req.ChavePublica,
                ChaveAuth = req.ChaveAuth,
            });
        }
        else
        {
            existente.UsuarioId = usuarioId;
            existente.LojaId = lojaId;
            existente.ChavePublica = req.ChavePublica;
            existente.ChaveAuth = req.ChaveAuth;
            existente.UltimoAvisoEm = null;
        }
        await repositorio.SalvarAsync();
        return Resultado.Ok(new AtivoPushDto(true));
    }
}
