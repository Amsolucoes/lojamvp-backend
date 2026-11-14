using LojaApi.Domain.Produtos;
using LojaApi.Shared.Application;
using LojaApi.src.Models.Etiquetas;

namespace LojaApi.Application.Produtos;

// Cria um modelo de etiqueta.
public class CriarModeloEtiquetaCommandHandler(IProdutosRepository repositorio) : ICommandHandler<CriarModeloEtiquetaCommand>
{
    public async Task<Resultado<object>> HandleAsync(CriarModeloEtiquetaCommand command)
    {
        var (lojaId, req) = command;
        if (lojaId is null) return Resultado.Invalido("Loja não encontrada.");

        if (string.IsNullOrWhiteSpace(req.Nome))
            return Resultado.Invalido("Digite um nome para o modelo.");
        if (!ProdutosRegras.TamanhoDeEtiquetaValido(req.LarguraMm, req.AlturaMm))
            return Resultado.Invalido("Largura e altura devem ser maiores que zero.");

        var modelo = new ConfiguracaoEtiqueta { LojaId = lojaId.Value };
        ModeloEtiquetaApoio.Preencher(modelo, req);

        repositorio.Adicionar(modelo);
        await repositorio.SalvarAsync();
        return Resultado.Ok(modelo);
    }
}
