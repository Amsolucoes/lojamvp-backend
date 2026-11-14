using LojaApi.Shared.Application;

namespace LojaApi.Application.Produtos;

// "Remove" uma variação: só a desativa (o controller responde 204).
public class RemoverVariacaoCommandHandler(IProdutosRepository repositorio) : ICommandHandler<RemoverVariacaoCommand>
{
    public async Task<Resultado<object>> HandleAsync(RemoverVariacaoCommand command)
    {
        var (lojaId, produtoId, variacaoId) = command;
        var variacao = await repositorio.ObterVariacaoDoProdutoAsync(produtoId, variacaoId, lojaId);
        if (variacao is null) return Resultado.NaoEncontrado();

        variacao.Ativo = false;
        variacao.AtualizadoEm = DateTime.UtcNow;
        await repositorio.SalvarAsync();
        return Resultado.Ok(new { });
    }
}
