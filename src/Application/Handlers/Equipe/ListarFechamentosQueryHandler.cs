using LojaApi.Domain.Equipe;
using LojaApi.Models;
using LojaApi.Shared.Application;
using LojaApi.src.Models.Funcionarios;

namespace LojaApi.Application.Equipe;

// Histórico de fechamentos já pagos.
public class ListarFechamentosQueryHandler(IEquipeRepository repositorio) : IQueryHandler<ListarFechamentosQuery>
{
    public async Task<Resultado<object>> HandleAsync(ListarFechamentosQuery query)
    {
        var (lojaId, profissionalId) = query;
        if (lojaId is null) return Resultado.Ok(Array.Empty<object>());
        return Resultado.Ok(await repositorio.ListarFechamentosAsync(lojaId.Value, profissionalId));
    }
}
