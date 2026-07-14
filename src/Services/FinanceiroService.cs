using Microsoft.EntityFrameworkCore;
using LojaApi.Data;
using LojaApi.Models;

namespace LojaApi.Services;

public class FinanceiroService(AppDbContext db, ILogger<FinanceiroService> logger)
{
    private const int MESES_LOTE = 60;

    // ── Gera um lote de N meses a partir de um mês inicial ─────────
   public async Task GerarLoteFixoAsync(LancamentoFixo fixo, DateTime desde, int meses = MESES_LOTE)
    {
        int criados = 0;
        var inicioPermitido = fixo.DataInicio.HasValue
            ? new DateTime(fixo.DataInicio.Value.Year, fixo.DataInicio.Value.Month, 1)
            : (DateTime?)null;

        for (int i = 0; i < meses; i++)
        {
            var mesAlvo = desde.AddMonths(i);

            if (inicioPermitido.HasValue && mesAlvo < inicioPermitido.Value) continue;

            var vencimento = new DateTime(mesAlvo.Year, mesAlvo.Month,
                Math.Min(fixo.DiaVencimento, DateTime.DaysInMonth(mesAlvo.Year, mesAlvo.Month)),
                12, 0, 0, DateTimeKind.Utc);

            var proximoMes = mesAlvo.AddMonths(1);
            var jaExiste = await db.LancamentosFinanceiros.AnyAsync(l =>
                l.LancamentoFixoId == fixo.Id &&
                l.Vencimento >= mesAlvo && l.Vencimento < proximoMes);

            if (jaExiste) continue;

            db.LancamentosFinanceiros.Add(new LancamentoFinanceiro
            {
                LojaId = fixo.LojaId,
                ContaBancariaId = fixo.ContaBancariaId,
                Tipo = fixo.Tipo,
                Modo = "fixa",
                Descricao = fixo.Descricao,
                CategoriaId = fixo.CategoriaId,
                Observacao = fixo.Observacao,
                Valor = fixo.Valor,
                Vencimento = vencimento,
                Status = "pendente",
                LancamentoFixoId = fixo.Id,
            });
            criados++;
        }

        fixo.GeradoAte = desde.AddMonths(meses - 1);

        if (criados > 0)
            logger.LogInformation("{N} lançamento(s) gerados para o fixo '{Desc}' até {Ate}.", criados, fixo.Descricao, fixo.GeradoAte?.ToString("yyyy-MM"));
    }

    // ── Apaga os lançamentos futuros ainda não pagos de um fixo ────
    public async Task LimparFuturosAsync(Guid lancamentoFixoId)
    {
        var hoje = DateTime.UtcNow.Date;
        var futuros = await db.LancamentosFinanceiros
            .Where(l => l.LancamentoFixoId == lancamentoFixoId && l.Status == "pendente" && l.Vencimento >= hoje)
            .ToListAsync();

        db.LancamentosFinanceiros.RemoveRange(futuros);
    }

    // ── Job diário: garante que cada fixo ativo tem lote suficiente ─
    public async Task GerarPendenciasMensaisAsync()
    {
        var agora = DateTime.UtcNow;
        var mesAtual = new DateTime(agora.Year, agora.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var fixos = await db.LancamentosFixos.Where(f => f.Ativa).ToListAsync();

        foreach (var fixo in fixos)
        {
            if (fixo.GeradoAte is null)
            {
                // Nunca gerou nada — gera o primeiro lote de 24 meses
                await GerarLoteFixoAsync(fixo, mesAtual);
            }
            else if (mesAtual >= fixo.GeradoAte.Value)
            {
                // Chegou (ou passou) no último mês do lote — gera os próximos 24
                await GerarLoteFixoAsync(fixo, fixo.GeradoAte.Value.AddMonths(1));
            }
        }

        await db.SaveChangesAsync();
    }

    private const int CICLOS_LOTE_CARTAO = 60;

    // ── Gera um lote de N ciclos futuros para um fixo de cartão ────
    public async Task GerarLoteFixoCartaoAsync(CartaoLancamentoFixo fixo, CartaoCredito cartao, DateTime desde, int ciclos = CICLOS_LOTE_CARTAO)
    {
        int criados = 0;
        var referencia = desde;

        for (int i = 0; i < ciclos; i++)
        {
            var dataCiclo = referencia.AddMonths(i);

            var jaExiste = await db.LancamentosCartao.AnyAsync(l =>
                l.CartaoFixoId == fixo.Id &&
                l.DataCompra.Year == dataCiclo.Year && l.DataCompra.Month == dataCiclo.Month);

            if (jaExiste) continue;

            db.LancamentosCartao.Add(new LancamentoCartao
            {
                LojaId = fixo.LojaId,
                CartaoCreditoId = fixo.CartaoCreditoId,
                Descricao = fixo.Descricao,
                Valor = fixo.Valor,
                DataCompra = new DateTime(dataCiclo.Year, dataCiclo.Month, 1, 12, 0, 0, DateTimeKind.Utc),
                Modo = "fixa",
                CategoriaId = fixo.CategoriaId,
                Observacao = fixo.Observacao,
                CartaoFixoId = fixo.Id,
            });
            criados++;
        }

        fixo.GeradoAte = referencia.AddMonths(ciclos - 1);

        if (criados > 0)
            logger.LogInformation("{N} lançamento(s) de cartão fixo gerados para '{Desc}' até {Ate}.", criados, fixo.Descricao, fixo.GeradoAte?.ToString("yyyy-MM"));
    }

    public async Task LimparFuturosCartaoAsync(Guid cartaoFixoId)
    {
        var agora = DateTime.UtcNow;
        var mesAtual = new DateTime(agora.Year, agora.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var futuros = await db.LancamentosCartao
            .Where(l => l.CartaoFixoId == cartaoFixoId && l.DataCompra >= mesAtual)
            .ToListAsync();
        db.LancamentosCartao.RemoveRange(futuros);
    }

    // ── Chamado pelo job diário: garante lote suficiente pros fixos de cartão ──
    public async Task GerarPendenciasCartaoFixoAsync()
    {
        var agora = DateTime.UtcNow;
        var mesAtual = new DateTime(agora.Year, agora.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var fixos = await db.CartaoLancamentosFixos.Include(f => f.CartaoCredito).Where(f => f.Ativo).ToListAsync();

        foreach (var fixo in fixos)
        {
            if (fixo.CartaoCredito is null) continue;

            if (fixo.GeradoAte is null)
                await GerarLoteFixoCartaoAsync(fixo, fixo.CartaoCredito, mesAtual);
            else if (mesAtual >= fixo.GeradoAte.Value)
                await GerarLoteFixoCartaoAsync(fixo, fixo.CartaoCredito, fixo.GeradoAte.Value.AddMonths(1));
        }

        await db.SaveChangesAsync();
    }
}