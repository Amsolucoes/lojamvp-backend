using LojaApi.Domain.Equipe;
using LojaApi.Models;
using LojaApi.Shared.Application;
using LojaApi.src.Models.Funcionarios;

namespace LojaApi.Application.Equipe;

// Lista os funcionários da loja com a comissão padrão e as exceções por serviço.
public class ListarFuncionariosQueryHandler(IEquipeRepository repositorio) : IQueryHandler<ListarFuncionariosQuery>
{
    public async Task<Resultado<object>> HandleAsync(ListarFuncionariosQuery query)
    {
        var lojaId = query.LojaId;
        if (lojaId is null) return Resultado.Ok(Array.Empty<object>());
        return Resultado.Ok(await repositorio.ListarProfissionaisAsync(lojaId.Value));
    }
}
