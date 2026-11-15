using LojaApi.Models;
using LojaApi.Shared.Application;

namespace LojaApi.Application.Plataforma;

// Configurações visuais da loja do assinante (com o motivo quando está bloqueada).
public class ConfigClienteQueryHandler(IPlataformaRepository repositorio) : IQueryHandler<ConfigClienteQuery>
{
    public async Task<Resultado<object>> HandleAsync(ConfigClienteQuery query)
    {
        var loja = await repositorio.ObterMinhaLojaComPagamentosAsync(query.UsuarioId);
        if (loja is null) return Resultado.NaoEncontrado();

        return Resultado.Ok(new LojaConfigDto(
            loja.Id, loja.Nome, loja.CorPrimaria,
            loja.LogoUrl, loja.Status.ToString(),
            loja.Status == StatusLoja.Bloqueado ? "Acesso bloqueado por inadimplência." : null,
            loja.Endereco, loja.Telefone));
    }
}
