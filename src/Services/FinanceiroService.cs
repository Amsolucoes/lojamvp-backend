using Microsoft.EntityFrameworkCore;
using LojaApi.Data;
using LojaApi.Models;

namespace LojaApi.src.Data.Services;

public class FinanceiroService(AppDbContext db, ILogger<FinanceiroService> logger)
{
    public async Task GerarLancamentosFixosDoMesAsync()
    {
        var agora = DateTime.UtcNow;
        var mesAtual = new DateTime(agora.Year, agora.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var proximoMes = mesAtual.AddMonths(1);

        var fixos = await db.LancamentosFixos.Where(f => f.Ativa).ToListAsync();
        int criados = 0;

        foreach (var fixo in fixos)
        {
            var vencimento = new DateTime(mesAtual.Year, mesAtual.Month,
                Math.Min(fixo.DiaVencimento, DateTime.DaysInMonth(mesAtual.Year, mesAtual.Month)),
                0, 0, 0, DateTimeKind.Utc);

            var jaExiste = await db.LancamentosFinanceiros.AnyAsync(l =>
                l.LancamentoFixoId == fixo.Id &&
                l.Vencimento >= mesAtual && l.Vencimento < proximoMes);

            if (jaExiste) continue;

            db.LancamentosFinanceiros.Add(new LancamentoFinanceiro
            {
                LojaId = fixo.LojaId,
                ContaBancariaId = fixo.ContaBancariaId,
                Tipo = fixo.Tipo,
                Modo = "fixa",
                Descricao = fixo.Descricao,
                Categoria = fixo.Categoria,
                Valor = fixo.Valor,
                Vencimento = vencimento,
                Status = "pendente",
                LancamentoFixoId = fixo.Id,
            });
            criados++;
        }

        if (criados > 0)
        {
            await db.SaveChangesAsync();
            logger.LogInformation("{N} lançamento(s) fixo(s) gerados para o mês {Mes}.", criados, mesAtual.ToString("yyyy-MM"));
        }
    }
}