using Microsoft.EntityFrameworkCore;
using LojaApi.Data;
using LojaApi.Models;

namespace LojaApi.Services;

public class TenantService(AppDbContext db, ILogger<TenantService> logger)
{
    // Bloqueia quando passa 1 dia do vencimento (vence dia D → bloqueia na madrugada de D+1).
    private const int DIAS_ATRASO_BLOQUEIO = 1;
    private static readonly TimeSpan OffsetBrasilia = TimeSpan.FromHours(-3); // sem horário de verão desde 2019

    // Dias de atraso contados em dias corridos no horário de Brasília (negativo = ainda não venceu).
    public static int DiasDeAtraso(DateTime vencimentoUtc, DateTime agoraUtc)
        => ((agoraUtc + OffsetBrasilia).Date - (vencimentoUtc + OffsetBrasilia).Date).Days;

    public async Task VerificarStatusAsync()
    {
        var lojas = await db.Lojas
            .Where(l => l.Status != StatusLoja.Cancelado)
            .ToListAsync();

        foreach (var loja in lojas)
            AtualizarStatusLoja(loja);

        await db.SaveChangesAsync();
        logger.LogInformation("Status verificado para {N} lojas.", lojas.Count);
    }

    public void AtualizarStatusLoja(Loja loja)
    {
        var agora = DateTime.UtcNow;

        // Trial: só bloqueia após a carência de 1 dia do fim do trial
        if (loja.Status == StatusLoja.Trial)
        {
            var diasAposTrial = DiasDeAtraso(loja.TrialAte, agora);
            if (diasAposTrial >= DIAS_ATRASO_BLOQUEIO)
            {
                loja.Status = StatusLoja.Bloqueado;
                loja.AtualizadoEm = agora;
            }
            return;
        }

        // Ativo: bloqueia 1 dia após o vencimento
        if (loja.Status == StatusLoja.Ativo && loja.ProximoVencimento.HasValue)
        {
            var diasAtraso = DiasDeAtraso(loja.ProximoVencimento.Value, agora);
            if (diasAtraso >= DIAS_ATRASO_BLOQUEIO)
            {
                loja.Status = StatusLoja.Bloqueado;
                loja.AtualizadoEm = agora;
            }
        }
    }

    public async Task<bool> RegistrarPagamentoAsync(
        Guid lojaId, decimal valor, DateTime vencimento,
        DateTime pagoEm, string formaPagamento,
        string? obs, Guid? adminId,
        string? mpPaymentId = null)
    {
        var loja = await db.Lojas.FindAsync(lojaId);
        if (loja == null) return false;

        // ── Idempotência: se este pagamento do MP já foi processado, ignora ──
        if (!string.IsNullOrEmpty(mpPaymentId))
        {
            var jaProcessado = await db.Pagamentos
                .AnyAsync(p => p.MpPaymentId == mpPaymentId && p.Status == "pago");
            if (jaProcessado)
            {
                logger.LogInformation("Pagamento MP {Id} já processado, ignorando duplicata.", mpPaymentId);
                return true;
            }
        }

        // Marca pagamento existente como pago ou cria novo
        var pagamento = await db.Pagamentos
            .FirstOrDefaultAsync(p => p.LojaId == lojaId &&
                                      (p.Status == "pendente" || p.Status == "atrasado"));

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
            db.Pagamentos.Add(new Pagamento
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

        // Reativa loja
        loja.Status = StatusLoja.Ativo;
        loja.UltimaCobranca = pagoEm;

        // Verifica fim da promoção
        if (loja.Promocional && loja.ValorPosPromocional.HasValue)
        {
            var faturasPagas = await db.Pagamentos
                .CountAsync(p => p.LojaId == lojaId && p.Status == "pago");

            if (faturasPagas >= loja.MesesPromocional)
            {
                loja.MensalidadeValor = loja.ValorPosPromocional.Value;
                loja.Promocional = false;
            }
        }

        loja.ProximoVencimento = ProximoVencimentoDate(loja.MensalidadeDia);
        loja.AtualizadoEm = DateTime.UtcNow;

        // Cria próxima fatura pendente
        db.Pagamentos.Add(new Pagamento
        {
            LojaId = lojaId,
            Valor = loja.MensalidadeValor,
            Status = "pendente",
            Vencimento = loja.ProximoVencimento.Value,
        });

        await db.SaveChangesAsync();
        return true;
    }

    public static DateTime ProximoVencimentoDate(int dia)
    {
        // Sempre o mês seguinte ao atual (pagou em julho → vence em agosto)
        var agora = DateTime.UtcNow;
        var mesAlvo = new DateTime(agora.Year, agora.Month, 1, 12, 0, 0, DateTimeKind.Utc).AddMonths(1);
        var maxDia = DateTime.DaysInMonth(mesAlvo.Year, mesAlvo.Month);
        // Hora 12:00 UTC evita "voltar um dia" no fuso local (-3/-4h)
        return new DateTime(mesAlvo.Year, mesAlvo.Month, Math.Min(dia, maxDia), 12, 0, 0, DateTimeKind.Utc);
    }

    public static string GerarSchemaNome(string nomeLoja)
    {
        var slug = new string(nomeLoja.ToLower()
            .Replace(" ", "_").Replace("-", "_")
            .Where(c => char.IsLetterOrDigit(c) || c == '_').ToArray());
        return $"loja_{slug}_{DateTime.UtcNow.Ticks % 10000}";
    }

    public async Task<(bool Ativa, string? Motivo)> VerificarAcessoAsync(Guid lojaId)
    {
        var loja = await db.Lojas.FindAsync(lojaId);
        if (loja == null) return (false, "Loja não encontrada.");

        AtualizarStatusLoja(loja);
        await db.SaveChangesAsync();

        return loja.Status switch
        {
            StatusLoja.Ativo     => (true, null),
            StatusLoja.Trial     => (true, null),
            StatusLoja.Bloqueado => (false, "Acesso bloqueado por inadimplência. Acesse o painel para regularizar."),
            StatusLoja.Cancelado => (false, "Esta loja foi cancelada."),
            _                    => (false, "Acesso não permitido.")
        };
    }

    // Retorna info do trial/carência para exibir contador
    public static (string Fase, int DiasRestantes) CalcularSituacao(Loja loja)
    {
        var agora = DateTime.UtcNow;

        if (loja.Status == StatusLoja.Trial)
        {
            var diasTrial = (int)Math.Ceiling((loja.TrialAte - agora).TotalDays);
            if (diasTrial >= 0)
                return ("trial", diasTrial);

            // Trial vencido, em carência
            var diasCarencia = DIAS_ATRASO_BLOQUEIO - DiasDeAtraso(loja.TrialAte, agora);
            return ("carencia", Math.Max(0, diasCarencia));
        }

        if (loja.Status == StatusLoja.Ativo && loja.ProximoVencimento.HasValue)
        {
            var diasAteVenc = (int)Math.Ceiling((loja.ProximoVencimento.Value - agora).TotalDays);
            if (diasAteVenc < 0)
            {
                var diasCarencia = DIAS_ATRASO_BLOQUEIO - DiasDeAtraso(loja.ProximoVencimento.Value, agora);
                return ("carencia", Math.Max(0, diasCarencia));
            }
            return ("ativo", diasAteVenc);
        }

        return (loja.Status.ToString().ToLower(), 0);
    }
}
