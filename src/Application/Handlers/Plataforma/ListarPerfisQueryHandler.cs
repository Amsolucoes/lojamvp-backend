using LojaApi.Shared.Application;

namespace LojaApi.Application.Plataforma;

// Perfis de loja ativos (rota pública do cadastro), com categorias e campos extras por ordem.
public class ListarPerfisQueryHandler(IPlataformaRepository repositorio) : IQueryHandler<ListarPerfisQuery>
{
    public async Task<Resultado<object>> HandleAsync(ListarPerfisQuery query)
    {
        var perfis = await repositorio.ListarPerfisAtivosAsync();
        return Resultado.Ok(perfis.Select(p => new PerfilDto(
            p.Id, p.Nome, p.Descricao, p.Icone, p.TipoPlanoAplica,
            p.Categorias.Select(c => new PerfilCategoriaDto(c.Id, c.Nome, c.Ordem, c.TipoTamanho)),
            p.CamposExtras.Select(c => new PerfilCampoExtraDto(c.Id, c.Chave, c.Label, c.Tipo, c.Opcoes, c.Obrigatorio, c.Ordem)))));
    }
}
