using LojaApi.Domain.Produtos;
using LojaApi.Shared.Application;

namespace LojaApi.Application.Produtos;

// Atualiza um modelo de etiqueta da loja.
public class AtualizarModeloEtiquetaCommandHandler(IProdutosRepository repositorio) : ICommandHandler<AtualizarModeloEtiquetaCommand>
{
    public async Task<Resultado<object>> HandleAsync(AtualizarModeloEtiquetaCommand command)
    {
        var (lojaId, id, req) = command;
        var modelo = await repositorio.ObterModeloAsync(id, lojaId);
        if (modelo is null) return Resultado.NaoEncontrado();

        if (string.IsNullOrWhiteSpace(req.Nome))
            return Resultado.Invalido("Digite um nome para o modelo.");
        if (!ProdutosRegras.TamanhoDeEtiquetaValido(req.LarguraMm, req.AlturaMm))
            return Resultado.Invalido("Largura e altura devem ser maiores que zero.");

        ModeloEtiquetaApoio.Preencher(modelo, req);
        await repositorio.SalvarAsync();
        return Resultado.Ok(modelo);
    }
}
