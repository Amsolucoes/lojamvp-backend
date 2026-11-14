using LojaApi.Shared.Application;

namespace LojaApi.Application.Produtos;

// "Exclui" uma marca (só desativa), se nenhum produto ativo a usa.
public class ExcluirMarcaCommandHandler(IProdutosRepository repositorio) : ICommandHandler<ExcluirMarcaCommand>
{
    public async Task<Resultado<object>> HandleAsync(ExcluirMarcaCommand command)
    {
        var (lojaId, id) = command;
        if (lojaId is null) return Resultado.Invalido("Loja não encontrada.");

        var marca = await repositorio.ObterMarcaAsync(id, lojaId);
        if (marca is null) return Resultado.NaoEncontrado();

        var qtdProdutos = await repositorio.ContarProdutosAtivosDaMarcaAsync(id);
        if (qtdProdutos > 0)
            return Resultado.Invalido($"Não é possível excluir: {qtdProdutos} produto(s) usam esta marca.");

        marca.Ativo = false;
        await repositorio.SalvarAsync();
        return Resultado.Ok(new { mensagem = "Marca excluída." });
    }
}
