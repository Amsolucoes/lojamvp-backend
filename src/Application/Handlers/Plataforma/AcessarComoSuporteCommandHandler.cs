using LojaApi.Shared.Application;

namespace LojaApi.Application.Plataforma;

// O superadmin entra como o admin da loja (token marcado como acesso de suporte, que não conta como login do cliente).
public class AcessarComoSuporteCommandHandler(IPlataformaRepository plataforma, IAdminRepository admin, IGeradorDeToken geradorDeToken) : ICommandHandler<AcessarComoSuporteCommand>
{
    public async Task<Resultado<object>> HandleAsync(AcessarComoSuporteCommand command)
    {
        var loja = await plataforma.ObterLojaAsync(command.Id);
        if (loja is null) return Resultado.NaoEncontrado("Loja não encontrada.");

        var vinculo = await admin.ObterVinculoAdminAsync(command.Id, apenasAtivo: true);
        if (vinculo is null || vinculo.Usuario is null)
            return Resultado.NaoEncontrado("Esta loja não tem um administrador ativo.");

        var token = geradorDeToken.Gerar(vinculo.Usuario, acessoSuporte: true);
        return Resultado.Ok(new AcessoSuporteDto(token, vinculo.Usuario.Nome, vinculo.Usuario.Email, vinculo.Usuario.Role, loja.Nome));
    }
}
