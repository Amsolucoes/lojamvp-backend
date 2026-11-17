using LojaApi.Shared.Application;

namespace LojaApi.Application.Plataforma;

// Muda o valor da mensalidade (vale para a fatura em aberto, se ainda não tem cobrança gerada, e para as próximas) e, se pedido e houver assinatura ativa, também a assinatura no Mercado Pago.
// Se o Mercado Pago falhar, o valor da loja muda mesmo assim e a resposta traz um aviso.
public class AtualizarValorLojaCommandHandler(IPlataformaRepository repositorio, IGatewayDePagamentoPlataforma gateway) : ICommandHandler<AtualizarValorLojaCommand>
{
    public async Task<Resultado<object>> HandleAsync(AtualizarValorLojaCommand command)
    {
        var (id, req) = command;
        var loja = await repositorio.ObterLojaAsync(id);
        if (loja is null) return Resultado.NaoEncontrado("Loja não encontrada.");

        if (req.NovoValor <= 0)
            return Resultado.Invalido("Valor inválido.");

        loja.MensalidadeValor = req.NovoValor;

        var sincronizou = false;
        string? avisoMp = null;

        if (req.SincronizarAssinatura)
        {
            if (string.IsNullOrEmpty(loja.MpPreapprovalId) || loja.AssinaturaStatus != "authorized")
            {
                avisoMp = "A loja não tem assinatura ativa para sincronizar. Só o valor das próximas faturas foi alterado.";
            }
            else
            {
                sincronizou = await gateway.AtualizarValorAssinaturaAsync(loja.MpPreapprovalId, req.NovoValor);
                if (!sincronizou)
                    avisoMp = "O valor da loja foi alterado, mas não foi possível atualizar a assinatura no Mercado Pago. Tente novamente ou verifique manualmente.";
            }
        }

        loja.AtualizadoEm = DateTime.UtcNow;
        await PlataformaApoio.SincronizarFaturaEmAbertoAsync(repositorio, loja);
        await repositorio.SalvarAsync();

        return Resultado.Ok(new ValorAtualizadoDto(
            sincronizou ? "Valor atualizado na loja e na assinatura recorrente." : "Valor atualizado.",
            avisoMp,
            sincronizou));
    }
}
