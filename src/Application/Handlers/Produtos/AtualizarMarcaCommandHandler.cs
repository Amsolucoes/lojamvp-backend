using LojaApi.Shared.Application;

namespace LojaApi.Application.Produtos;

// Renomeia uma marca da loja (nome único entre as ativas).
public class AtualizarMarcaCommandHandler(IProdutosRepository repositorio) : ICommandHandler<AtualizarMarcaCommand>
{
    public async Task<Resultado<object>> HandleAsync(AtualizarMarcaCommand command)
    {
        var (lojaId, id, req) = command;
        if (lojaId is null) return Resultado.Invalido("Loja não encontrada.");

        var marca = await repositorio.ObterMarcaAsync(id, lojaId);
        if (marca is null) return Resultado.NaoEncontrado();

        if (string.IsNullOrWhiteSpace(req.Nome))
            return Resultado.Invalido("Nome da marca é obrigatório.");

        var nomeNovo = req.Nome.Trim();
        if (await repositorio.MarcaComNomeExisteAsync(lojaId.Value, nomeNovo, id))
            return Resultado.Conflito("Já existe uma marca com esse nome.");

        marca.Nome = nomeNovo;
        await repositorio.SalvarAsync();

        return Resultado.Ok(new MarcaDto(marca.Id, marca.Nome));
    }
}
