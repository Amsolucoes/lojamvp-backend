using Microsoft.EntityFrameworkCore;
using LojaApi.Data;
using LojaApi.Application.Plataforma;
using LojaApi.Domain.Plataforma;
using LojaApi.Models;

namespace LojaApi.Services;

public class TenantService(AppDbContext db, ILogger<TenantService> logger)
{
    // Bloqueia quando passa 1 dia do vencimento (vence dia D → bloqueia na madrugada de D+1).
    private const int DIAS_ATRASO_BLOQUEIO = PlataformaRegras.DiasAtrasoBloqueio;

    // Dias de atraso contados em dias corridos no horário de Brasília (negativo = ainda não venceu).
    public static int DiasDeAtraso(DateTime vencimentoUtc, DateTime agoraUtc)
        => PlataformaRegras.DiasDeAtraso(vencimentoUtc, agoraUtc);

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

    public void AtualizarStatusLoja(Loja loja) => PlataformaApoio.AtualizarStatusDaLoja(loja, DateTime.UtcNow);

    public static DateTime ProximoVencimentoDate(int dia) => PlataformaRegras.ProximoVencimento(dia, DateTime.UtcNow);

    public static string GerarSchemaNome(string nomeLoja) => PlataformaRegras.GerarSchemaNome(nomeLoja, DateTime.UtcNow);

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
        => PlataformaRegras.CalcularSituacao(loja.Status.ToString(), loja.TrialAte, loja.ProximoVencimento, DateTime.UtcNow);
}
