using LojaApi.Models;
using LojaApi.Shared.Application;

namespace LojaApi.Application.Vendas;

// Cria uma origem de venda (nome único na loja).
public class CriarOrigemVendaCommandHandler(IVendasRepository repositorio) : ICommandHandler<CriarOrigemVendaCommand>
{
    public async Task<Resultado<object>> HandleAsync(CriarOrigemVendaCommand command)
    {
        var (lojaId, req) = command;
        if (lojaId is null) return Resultado.Invalido("Loja não encontrada.");

        if (string.IsNullOrWhiteSpace(req.Nome)) return Resultado.Invalido("Informe o nome da origem.");

        if (await repositorio.OrigemComNomeExisteAsync(lojaId, req.Nome.Trim()))
            return Resultado.Invalido("Já existe uma origem com esse nome.");

        var origem = new OrigemVenda
        {
            LojaId = lojaId.Value,
            Nome = req.Nome.Trim(),
            Ordem = req.Ordem,
            Ativa = req.Ativa,
        };
        repositorio.Adicionar(origem);
        await repositorio.SalvarAsync();

        return Resultado.Ok(origem);
    }
}
