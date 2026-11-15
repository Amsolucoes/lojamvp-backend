using LojaApi.Domain.Plataforma;
using LojaApi.Shared.Application;

namespace LojaApi.Application.Plataforma;

// Cria a assinatura recorrente no cartão (cobra a mensalidade todo mês no dia do vencimento).
public class CriarAssinaturaCommandHandler(IPlataformaRepository repositorio, IGatewayDePagamentoPlataforma gateway) : ICommandHandler<CriarAssinaturaCommand>
{
    public async Task<Resultado<object>> HandleAsync(CriarAssinaturaCommand command)
    {
        var (lojaIdOuNulo, req) = command;
        if (lojaIdOuNulo is not { } lojaId) return Resultado.Invalido("Loja não encontrada.");

        var loja = await repositorio.ObterLojaAsync(lojaId);
        if (loja is null) return Resultado.NaoEncontrado("Loja não encontrada.");

        if (loja.AssinaturaStatus == "authorized")
            return Resultado.Invalido("Esta loja já tem uma assinatura ativa.");

        var email = req.EmailPagador ?? loja.Email;
        var inicio = PlataformaRegras.ProximoDiaDeCobranca(loja.MensalidadeDia, DateTime.UtcNow);

        var result = await gateway.CriarAssinaturaAsync(loja.MensalidadeValor, "Assinatura AL Dev Software", req.CardToken, email, inicio,
            "https://app.aldevsoftware.com.br/pagamento");

        if (!result.Sucesso)
            return Resultado.Invalido(result.Erro!);

        loja.MpPreapprovalId = result.PreapprovalId;
        loja.AssinaturaStatus = result.Status ?? "authorized";
        loja.AssinaturaCartaoFinal = req.CartaoFinal;
        loja.AtualizadoEm = DateTime.UtcNow;
        await repositorio.SalvarAsync();

        return Resultado.Ok(new AssinaturaResponse(loja.AssinaturaStatus ?? "", loja.MpPreapprovalId, loja.AssinaturaCartaoFinal));
    }
}
