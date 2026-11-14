using LojaApi.Shared.Application;

namespace LojaApi.Application.Produtos;

// Exclui um modelo de etiqueta (nunca o último); se era o padrão, outro assume.
public class ExcluirModeloEtiquetaCommandHandler(IProdutosRepository repositorio) : ICommandHandler<ExcluirModeloEtiquetaCommand>
{
    public async Task<Resultado<object>> HandleAsync(ExcluirModeloEtiquetaCommand command)
    {
        var (lojaId, id) = command;
        var modelo = await repositorio.ObterModeloAsync(id, lojaId);
        if (modelo is null) return Resultado.NaoEncontrado();

        if (await repositorio.ContarModelosAsync(lojaId) <= 1)
            return Resultado.Invalido("Você precisa ter ao menos um modelo de etiqueta.");

        var eraPadrao = modelo.Padrao;
        repositorio.Remover(modelo);
        await repositorio.SalvarAsync();

        if (eraPadrao)
        {
            var outro = await repositorio.ObterQualquerModeloAsync(lojaId);
            if (outro != null) { outro.Padrao = true; await repositorio.SalvarAsync(); }
        }

        return Resultado.Ok(new { mensagem = "Modelo excluído." });
    }
}
