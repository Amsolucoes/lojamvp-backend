using LojaApi.Shared.Application;

namespace LojaApi.Application.Produtos;

// Ativa ou inativa vários produtos de uma vez (seleção múltipla na lista); só os da loja.
public class AtualizarStatusEmLoteCommandHandler(IProdutosRepository repositorio) : ICommandHandler<AtualizarStatusEmLoteCommand>
{
    public async Task<Resultado<object>> HandleAsync(AtualizarStatusEmLoteCommand command)
    {
        var (lojaId, req) = command;
        if (req.Ids.Count == 0) return Resultado.Ok(new { atualizados = 0 });

        var atualizados = await repositorio.AtualizarStatusEmLoteAsync(req.Ids, req.Ativo, lojaId);
        return Resultado.Ok(new { atualizados });
    }
}
