using LojaApi.Shared.Application;

namespace LojaApi.Application.Financeiro;

// Marca um lançamento como pago ou pendente. "Absorvida sem conta" mantém o vínculo com a conta mas tira o lançamento do saldo
// (valor que não saiu de fato da conta, como dívida negociada ou consolidada com o banco).
public class MarcarPagamentoCommandHandler(ILancamentosRepository repositorio) : ICommandHandler<MarcarPagamentoCommand>
{
    public async Task<Resultado<object>> HandleAsync(MarcarPagamentoCommand command)
    {
        var (lojaIdOuNulo, id, req) = command;
        if (lojaIdOuNulo is not { } lojaId) return Resultado.NaoEncontrado();
        var lanc = await repositorio.ObterAsync(id, lojaId);
        if (lanc is null) return Resultado.NaoEncontrado();

        lanc.Status = req.Pago ? "pago" : "pendente";
        lanc.PagoEm = req.Pago ? DateTime.UtcNow : null;

        if (req.Pago && req.AbsorvidaSemConta)
            lanc.NaoAfetaSaldo = true;

        await repositorio.SalvarAsync();

        return Resultado.Ok(new { lanc.Id, lanc.Status });
    }
}
