using LojaApi.Shared.Application;
using LojaApi.src.Models.Etiquetas;

namespace LojaApi.Application.Produtos;

// Modelos de etiqueta da loja (o padrão primeiro); na primeira vez cria o modelo "Padrão".
public class ListarModelosEtiquetaCommandHandler(IProdutosRepository repositorio) : ICommandHandler<ListarModelosEtiquetaCommand>
{
    public async Task<Resultado<object>> HandleAsync(ListarModelosEtiquetaCommand command)
    {
        var lojaId = command.LojaId;
        if (lojaId is null) return Resultado.Invalido("Loja não encontrada.");

        var modelos = await repositorio.ListarModelosAsync(lojaId.Value);

        if (modelos.Count == 0)
        {
            var padrao = new ConfiguracaoEtiqueta { LojaId = lojaId.Value, Nome = "Padrão", Padrao = true };
            repositorio.Adicionar(padrao);
            await repositorio.SalvarAsync();
            modelos.Add(padrao);
        }

        return Resultado.Ok(modelos);
    }
}
