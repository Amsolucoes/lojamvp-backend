using LojaApi.Shared.Application;
using Microsoft.Extensions.Logging;

namespace LojaApi.Application.Plataforma;

// Gera a cobrança de uma fatura no Mercado Pago (Pix, boleto ou cartão). Cartão aprovado na hora já registra o pagamento.
// O assinante só mexe nas faturas da própria loja (o superadmin, em qualquer uma).
public class CriarCobrancaCommandHandler(IPlataformaRepository repositorio, IGatewayDePagamentoPlataforma gateway, ILogger<CriarCobrancaCommandHandler> logger) : ICommandHandler<CriarCobrancaCommand>
{
    public async Task<Resultado<object>> HandleAsync(CriarCobrancaCommand command)
    {
        var (lojaId, ehSuperadmin, req) = command;
        var pagamento = await repositorio.ObterPagamentoComLojaAsync(req.PagamentoId);

        if (pagamento is null || (!ehSuperadmin && pagamento.LojaId != lojaId))
            return Resultado.NaoEncontrado("Fatura não encontrada.");

        if (pagamento.Status == "pago")
            return Resultado.Invalido("Esta fatura já foi paga.");

        var loja = pagamento.Loja;
        var cpf = req.CpfPagador ?? loja.Cpf ?? loja.Cnpj ?? "00000000000";
        var nome = req.NomePagador ?? loja.Nome;
        var email = req.EmailPagador ?? loja.Email;
        var descricao = $"Mensalidade {loja.Nome} - {pagamento.Vencimento:MM/yyyy}";

        var result = req.FormaPagamento switch
        {
            "pix" => await gateway.CriarPixAsync(pagamento.Valor, descricao, email, cpf, nome, pagamento.Id),
            "boleto" => await gateway.CriarBoletoAsync(pagamento.Valor, descricao, email, cpf, nome, pagamento.Id),
            "cartao" => await gateway.CriarCartaoAsync(pagamento.Valor, descricao, req.CardToken!, req.Parcelas ?? 1, email, cpf, nome, pagamento.Id),
            _ => new CobrancaCriada(null, null, null, null, null, null, "Forma de pagamento inválida.")
        };

        if (!result.Sucesso)
            return Resultado.Invalido(result.Erro!);

        pagamento.MpPaymentId = result.MpPaymentId;
        pagamento.MpQrCode = result.QrCode;
        pagamento.MpQrCodeBase64 = result.QrCodeBase64;
        pagamento.MpBoletoUrl = result.BoletoUrl;
        pagamento.MpBoletoBarcode = result.BoletoBarcode;
        pagamento.FormaPagamento = req.FormaPagamento;

        // Cartão aprovado na hora
        if (result.Status == "approved")
            await PlataformaApoio.RegistrarPagamentoAsync(repositorio, logger, loja.Id, pagamento.Valor, pagamento.Vencimento,
                DateTime.UtcNow, req.FormaPagamento, null, null, result.MpPaymentId);

        await repositorio.SalvarAsync();

        return Resultado.Ok(new PagamentoMpResponse(result.Status ?? "", result.QrCode, result.QrCodeBase64, result.BoletoUrl, result.BoletoBarcode, result.MpPaymentId));
    }
}
