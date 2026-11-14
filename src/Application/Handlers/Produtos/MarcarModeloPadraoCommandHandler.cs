using LojaApi.Shared.Application;

namespace LojaApi.Application.Produtos;

// Marca um modelo como o padrão da loja (os outros deixam de ser).
public class MarcarModeloPadraoCommandHandler(IProdutosRepository repositorio) : ICommandHandler<MarcarModeloPadraoCommand>
{
    public async Task<Resultado<object>> HandleAsync(MarcarModeloPadraoCommand command)
    {
        var (lojaId, id) = command;
        var modelo = await repositorio.ObterModeloAsync(id, lojaId);
        if (modelo is null) return Resultado.NaoEncontrado();

        var todos = await repositorio.ListarModelosAsync(lojaId!.Value);
        foreach (var m in todos) m.Padrao = m.Id == id;
        await repositorio.SalvarAsync();

        return Resultado.Ok(new { mensagem = "Modelo padrão atualizado." });
    }
}
