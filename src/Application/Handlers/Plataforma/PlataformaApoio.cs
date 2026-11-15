using LojaApi.Domain.Plataforma;
using LojaApi.Models;

namespace LojaApi.Application.Plataforma;

// O que vários handlers da Plataforma compartilham.
public static class PlataformaApoio
{
    // Bloqueia a loja em Trial ou Ativo quando passou a carência do trial/vencimento
    public static void AtualizarStatusDaLoja(Loja loja, DateTime agoraUtc)
    {
        if (loja.Status == StatusLoja.Trial)
        {
            if (PlataformaRegras.DiasDeAtraso(loja.TrialAte, agoraUtc) >= PlataformaRegras.DiasAtrasoBloqueio)
            {
                loja.Status = StatusLoja.Bloqueado;
                loja.AtualizadoEm = agoraUtc;
            }
            return;
        }

        if (loja.Status == StatusLoja.Ativo && loja.ProximoVencimento.HasValue
            && PlataformaRegras.DiasDeAtraso(loja.ProximoVencimento.Value, agoraUtc) >= PlataformaRegras.DiasAtrasoBloqueio)
        {
            loja.Status = StatusLoja.Bloqueado;
            loja.AtualizadoEm = agoraUtc;
        }
    }

    public static PagamentoDto ParaDto(Pagamento p) => new(
        p.Id, p.LojaId, p.Loja?.Nome ?? "",
        p.Valor, p.Status,
        p.Vencimento, p.PagoEm,
        p.FormaPagamento, p.Observacao,
        p.MpQrCode, p.MpQrCodeBase64,
        p.MpBoletoUrl, p.MpBoletoBarcode,
        p.CriadoEm
    );
}
