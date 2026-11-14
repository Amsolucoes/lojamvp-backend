using LojaApi.Shared.Application;

namespace LojaApi.Application.Vendas;

// Atualiza nome, ordem e situação de uma origem da loja.
public class AtualizarOrigemVendaCommandHandler(IVendasRepository repositorio) : ICommandHandler<AtualizarOrigemVendaCommand>
{
    public async Task<Resultado<object>> HandleAsync(AtualizarOrigemVendaCommand command)
    {
        var (lojaId, id, req) = command;
        var origem = lojaId is null ? null : await repositorio.ObterOrigemAsync(id, lojaId);
        if (origem is null) return Resultado.NaoEncontrado();

        origem.Nome = req.Nome.Trim();
        origem.Ordem = req.Ordem;
        origem.Ativa = req.Ativa;
        await repositorio.SalvarAsync();

        return Resultado.Ok(origem);
    }
}
