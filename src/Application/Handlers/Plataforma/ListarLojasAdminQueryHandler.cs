using LojaApi.Models;
using LojaApi.Shared.Application;

namespace LojaApi.Application.Plataforma;

// Lista as lojas (da mais nova para a mais antiga), filtrando por situação e por nome, e-mail ou CNPJ. Situação desconhecida não filtra.
public class ListarLojasAdminQueryHandler(IAdminRepository repositorio) : IQueryHandler<ListarLojasAdminQuery>
{
    public async Task<Resultado<object>> HandleAsync(ListarLojasAdminQuery query)
    {
        StatusLoja? status = !string.IsNullOrEmpty(query.Status) && Enum.TryParse<StatusLoja>(query.Status, true, out var s) ? s : null;
        var agora = DateTime.UtcNow;
        var lista = await repositorio.ListarLojasAsync(status, query.Busca);
        return Resultado.Ok(lista.Select(l => PlataformaApoio.ParaLojaDto(l, agora)));
    }
}
