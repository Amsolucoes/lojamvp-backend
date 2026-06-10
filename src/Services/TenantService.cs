using Microsoft.EntityFrameworkCore;
using LojaApi.Data;
using LojaApi.Models;

namespace LojaApi.Services;

public class TenantService(AppDbContext db, ILogger<TenantService> logger)
{
    private const int DIAS_ATRASO_BLOQUEIO = 3;

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

        if (loja.Status == StatusLoja.Trial && agora > loja.TrialAte)
        {
            loja.Status       = StatusLoja.Bloqueado;
            loja.AtualizadoEm = agora;
            return;
        }

        if ((loja.Status == StatusLoja.Ativo || loja.Status == StatusLoja.Trial) &&
            loja.ProximoVencimento.HasValue)
        {
            var diasAtraso = (int)(agora - loja.ProximoVencimento.Value).TotalDays;
            if (diasAtraso > DIAS_ATRASO_BLOQUEIO)
            {
                loja.Status       = StatusLoja.Bloqueado;
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

        // Marca pagamento existente como pago ou cria novo
        var pagamento = await db.Pagamentos
            .FirstOrDefaultAsync(p => p.LojaId == lojaId &&
                                      (p.Status == "pendente" || p.Status == "atrasado"));

        if (pagamento != null)
        {
            pagamento.Status         = "pago";
            pagamento.PagoEm         = pagoEm;
            pagamento.FormaPagamento = formaPagamento;
            pagamento.Observacao     = obs;
            pagamento.MpPaymentId    = mpPaymentId;
        }
        else
        {
            db.Pagamentos.Add(new Pagamento
            {
                LojaId          = lojaId,
                Valor           = valor,
                Status          = "pago",
                Vencimento      = vencimento,
                PagoEm          = pagoEm,
                FormaPagamento  = formaPagamento,
                Observacao      = obs,
                MpPaymentId     = mpPaymentId,
                RegistradoPorId = adminId,
            });
        }

        // Reativa loja e cria próxima fatura
        loja.Status             = StatusLoja.Ativo;
        loja.UltimaCobranca     = pagoEm;
        loja.ProximoVencimento  = ProximoVencimentoDate(loja.MensalidadeDia);
        loja.AtualizadoEm       = DateTime.UtcNow;

        // Cria próxima fatura pendente
        db.Pagamentos.Add(new Pagamento
        {
            LojaId     = lojaId,
            Valor      = loja.MensalidadeValor,
            Status     = "pendente",
            Vencimento = loja.ProximoVencimento.Value,
        });

        await db.SaveChangesAsync();
        return true;
    }

    public static DateTime ProximoVencimentoDate(int dia)
    {
        var agora   = DateTime.UtcNow;
        var maxDia  = DateTime.DaysInMonth(agora.Year, agora.Month);
        var proximo = new DateTime(agora.Year, agora.Month, Math.Min(dia, maxDia));
        if (proximo <= agora) proximo = proximo.AddMonths(1);
        return proximo;
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
}
