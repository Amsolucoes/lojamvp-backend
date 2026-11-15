using LojaApi.Shared.Application;
using Microsoft.Extensions.Logging;

namespace LojaApi.Application.Plataforma;

// O superadmin registra um pagamento recebido por fora: paga a fatura em aberto, reativa a loja e gera a próxima fatura.
public class RegistrarPagamentoManualCommandHandler(IPlataformaRepository repositorio, ILogger<RegistrarPagamentoManualCommandHandler> logger) : ICommandHandler<RegistrarPagamentoManualCommand>
{
    public async Task<Resultado<object>> HandleAsync(RegistrarPagamentoManualCommand command)
    {
        var (adminId, req) = command;
        var vencimentoUtc = DateTime.SpecifyKind(req.Vencimento, DateTimeKind.Utc);
        var pagoEmUtc = DateTime.SpecifyKind(req.PagoEm, DateTimeKind.Utc);

        var ok = await PlataformaApoio.RegistrarPagamentoAsync(repositorio, logger,
            req.LojaId, req.Valor, vencimentoUtc, pagoEmUtc, req.FormaPagamento, req.Observacao, adminId);

        return ok
            ? Resultado.Ok(new MensagemDto("Pagamento registrado e loja reativada."))
            : Resultado.NaoEncontrado("Loja não encontrada.");
    }
}
