using Microsoft.EntityFrameworkCore;
using LojaApi.Data;
using LojaApi.Models;

namespace LojaApi.Services;

public class PlanosService(AppDbContext db, ILogger<PlanosService> logger)
{
    public async Task GerarPendenciasMensaisAsync()
    {
        var agora = DateTime.UtcNow;
        var mesAtual = new DateTime(agora.Year, agora.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var assinaturasAtivas = await db.AssinaturasCliente
            .Where(a => a.Status == "ativa")
            .ToListAsync();

        int criadas = 0;

        foreach (var assinatura in assinaturasAtivas)
        {
            // Ainda não chegou o mês em que essa assinatura deve começar a ser cobrada
            if (assinatura.MesInicioCobranca > mesAtual) continue;

            var jaExiste = await db.PagamentosPlano
                .AnyAsync(p => p.AssinaturaId == assinatura.Id && p.MesReferencia == mesAtual);

            if (jaExiste) continue;

            var plano = await db.Planos.FindAsync(assinatura.PlanoId);
            if (plano is null) continue;

            db.PagamentosPlano.Add(new PagamentoPlano
            {
                AssinaturaId = assinatura.Id,
                LojaId = assinatura.LojaId,
                MesReferencia = mesAtual,
                Valor = plano.Valor,
                Status = "pendente",
            });
            criadas++;
        }

        if (criadas > 0)
        {
            await db.SaveChangesAsync();
            logger.LogInformation("{N} pendência(s) de plano geradas para o mês {Mes}.", criadas, mesAtual.ToString("yyyy-MM"));
        }
    }
}