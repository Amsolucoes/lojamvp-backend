using LojaApi.Domain.Plataforma;
using LojaApi.Models;
using Microsoft.Extensions.Logging;

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

    // ── Lojas no painel do superadmin ──

    public static LojaDto ParaLojaDto(Loja l, DateTime agora)
    {
        var dias = l.ProximoVencimento.HasValue
            ? Math.Max(0, PlataformaRegras.DiasDeAtraso(l.ProximoVencimento.Value, agora))
            : 0;
        var (fase, diasRest) = PlataformaRegras.CalcularSituacao(l.Status.ToString(), l.TrialAte, l.ProximoVencimento, DateTime.UtcNow);
        var ultimoLogin = l.Usuarios
            .Where(ul => ul.Usuario != null)
            .Select(ul => ul.Usuario!.UltimoLoginEm)
            .Where(d => d.HasValue)
            .OrderByDescending(d => d)
            .FirstOrDefault();
        return new(
            l.Id, l.Nome, l.Email, l.Cnpj, l.Cpf, l.Telefone, l.Endereco,
            l.CorPrimaria, l.LogoUrl, l.Status.ToString(),
            l.TrialAte, l.MensalidadeDia, l.MensalidadeValor,
            l.ProximoVencimento, l.UltimaCobranca,
            l.SchemaNome, l.CriadoEm,
            l.Usuarios.Count,
            l.Pagamentos.Where(p => p.Status == "pago").Sum(p => p.Valor),
            EmAtraso: dias > 0, DiasAtraso: dias,
            Promocional: l.Promocional,
            Fase: fase, DiasRestantes: diasRest,
            TipoPlano: l.TipoPlano, ModulosAtivos: l.ModulosAtivos,
            EhTeste: l.EhTeste,
            AssinaturaStatus: l.AssinaturaStatus,
            AssinaturaCartaoFinal: l.AssinaturaCartaoFinal,
            UltimoLoginEm: ultimoLogin);
    }

    public static LojaResumoDto ParaResumoDto(Loja l, DateTime agora)
    {
        var dias = l.ProximoVencimento.HasValue
            ? Math.Max(0, (int)(agora - l.ProximoVencimento.Value).TotalDays) : 0;
        return new(l.Id, l.Nome, l.Email, l.Status.ToString(), l.ProximoVencimento, l.MensalidadeValor, dias > 0, dias);
    }

    // ── Mensalidade ──

    // Registra o pagamento da mensalidade: paga a fatura em aberto (ou cria uma já paga), reativa a loja, encerra a promoção
    // quando o número de faturas pagas chega ao previsto e gera a próxima fatura pendente. Devolve falso se a loja não existe.
    // Um mesmo pagamento do Mercado Pago não é registrado duas vezes.
    public static async Task<bool> RegistrarPagamentoAsync(
        IPlataformaRepository repositorio, ILogger logger,
        Guid lojaId, decimal valor, DateTime vencimento, DateTime pagoEm, string formaPagamento,
        string? obs, Guid? adminId, string? mpPaymentId = null)
    {
        var loja = await repositorio.ObterLojaAsync(lojaId);
        if (loja == null) return false;

        if (!string.IsNullOrEmpty(mpPaymentId) && await repositorio.PagamentoDoMpJaPagoAsync(mpPaymentId))
        {
            logger.LogInformation("Pagamento MP {Id} já processado, ignorando duplicata.", mpPaymentId);
            return true;
        }

        var pagamento = await repositorio.ObterFaturaEmAbertoAsync(lojaId);
        if (pagamento != null)
        {
            pagamento.Status = "pago";
            pagamento.PagoEm = pagoEm;
            pagamento.FormaPagamento = formaPagamento;
            pagamento.Observacao = obs;
            pagamento.MpPaymentId = mpPaymentId;
        }
        else
        {
            repositorio.Adicionar(new Pagamento
            {
                LojaId = lojaId,
                Valor = valor,
                Status = "pago",
                Vencimento = vencimento,
                PagoEm = pagoEm,
                FormaPagamento = formaPagamento,
                Observacao = obs,
                MpPaymentId = mpPaymentId,
                RegistradoPorId = adminId,
            });
        }

        loja.Status = StatusLoja.Ativo;
        loja.UltimaCobranca = pagoEm;

        // Fim da promoção
        if (loja.Promocional && loja.ValorPosPromocional.HasValue)
        {
            var faturasPagas = await repositorio.ContarFaturasPagasAsync(lojaId);
            if (faturasPagas >= loja.MesesPromocional)
            {
                loja.MensalidadeValor = loja.ValorPosPromocional.Value;
                loja.Promocional = false;
            }
        }

        loja.ProximoVencimento = PlataformaRegras.ProximoVencimento(loja.MensalidadeDia, DateTime.UtcNow);
        loja.AtualizadoEm = DateTime.UtcNow;

        // Próxima fatura pendente
        repositorio.Adicionar(new Pagamento
        {
            LojaId = lojaId,
            Valor = loja.MensalidadeValor,
            Status = "pendente",
            Vencimento = loja.ProximoVencimento.Value,
        });

        await repositorio.SalvarAsync();
        return true;
    }
}
