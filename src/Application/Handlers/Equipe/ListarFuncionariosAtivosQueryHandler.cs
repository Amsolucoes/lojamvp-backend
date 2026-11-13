using LojaApi.Domain.Equipe;
using LojaApi.Models;
using LojaApi.Shared.Application;
using LojaApi.src.Models.Funcionarios;

namespace LojaApi.Application.Equipe;

// Lista simplificada (id + nome) dos funcionários ativos, usada em seletores de outras telas.
public class ListarFuncionariosAtivosQueryHandler(IEquipeRepository repositorio) : IQueryHandler<ListarFuncionariosAtivosQuery>
{
    public async Task<Resultado<object>> HandleAsync(ListarFuncionariosAtivosQuery query)
    {
        var lojaId = query.LojaId;
        if (lojaId is null) return Resultado.Ok(Array.Empty<object>());
        return Resultado.Ok(await repositorio.ListarProfissionaisAtivosAsync(lojaId.Value));
    }
}
