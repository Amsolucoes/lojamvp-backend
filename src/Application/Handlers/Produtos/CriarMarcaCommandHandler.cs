using LojaApi.Shared.Application;
using LojaApi.src.Models;

namespace LojaApi.Application.Produtos;

// Cria uma marca da loja (nome único entre as ativas).
public class CriarMarcaCommandHandler(IProdutosRepository repositorio) : ICommandHandler<CriarMarcaCommand>
{
    public async Task<Resultado<object>> HandleAsync(CriarMarcaCommand command)
    {
        var (lojaId, req) = command;
        if (lojaId is null) return Resultado.Invalido("Loja não encontrada.");

        if (string.IsNullOrWhiteSpace(req.Nome))
            return Resultado.Invalido("Nome da marca é obrigatório.");

        var nome = req.Nome.Trim();

        if (await repositorio.MarcaComNomeExisteAsync(lojaId.Value, nome, null))
            return Resultado.Conflito("Já existe uma marca com esse nome.");

        var marca = new Marca
        {
            LojaId = lojaId.Value,
            Nome = nome,
        };
        repositorio.Adicionar(marca);
        await repositorio.SalvarAsync();

        return Resultado.Ok(new MarcaDto(marca.Id, marca.Nome));
    }
}
