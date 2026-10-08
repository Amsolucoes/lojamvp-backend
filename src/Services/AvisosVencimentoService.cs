using System.Globalization;
using LojaApi.Data;
using LojaApi.Models;
using Microsoft.EntityFrameworkCore;

namespace LojaApi.Services;

// Todo dia às 07:00 (horário de Brasília) envia, para os aparelhos com avisos ativos, a lista
// de contas a pagar que vencem HOJE e ainda não foram pagas (lançamentos com "avisar" ligado
// e faturas de cartão que vencem hoje).
public class AvisosVencimentoService(
    IServiceProvider serviceProvider,
    ILogger<AvisosVencimentoService> logger) : BackgroundService
{
    private static readonly TimeSpan OffsetBrasilia = TimeSpan.FromHours(-3); // sem horário de verão desde 2019
    private const int HORA_AVISO = 7;
    private const int HORA_LIMITE = 12; // se o servidor estava fora do ar às 7h, ainda envia até o meio-dia
    private static readonly CultureInfo PtBr = new("pt-BR");

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("AvisosVencimentoService iniciado.");

        while (!stoppingToken.IsCancellationRequested)
        {
            var agoraBr = DateTime.UtcNow + OffsetBrasilia;
            var dentroDaJanela = agoraBr.Hour >= HORA_AVISO && agoraBr.Hour < HORA_LIMITE;

            if (dentroDaJanela)
            {
                try
                {
                    using var scope = serviceProvider.CreateScope();
                    await ProcessarAsync(scope.ServiceProvider, agoraBr.Date, stoppingToken);
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Erro ao enviar avisos de vencimento.");
                }
            }

            try
            {
                // Na janela, confere de 2 em 2 minutos (barato: só age se houver aparelho pendente);
                // fora dela, de 10 em 10.
                await Task.Delay(dentroDaJanela ? TimeSpan.FromMinutes(2) : TimeSpan.FromMinutes(10), stoppingToken);
            }
            catch (TaskCanceledException) { break; }
        }
    }

    private async Task ProcessarAsync(IServiceProvider sp, DateTime hojeBr, CancellationToken ct)
    {
        var push = sp.GetRequiredService<PushService>();
        if (!push.Configurado) return;

        var db = sp.GetRequiredService<AppDbContext>();

        // Início do dia de hoje (Brasília) em UTC — aparelhos já avisados hoje ficam de fora
        var inicioDiaUtc = DateTime.SpecifyKind(hojeBr - OffsetBrasilia, DateTimeKind.Utc);

        var pendentes = await db.AssinaturasPush
            .Where(a => a.UltimoAvisoEm == null || a.UltimoAvisoEm < inicioDiaUtc)
            .ToListAsync(ct);
        if (pendentes.Count == 0) return;

        foreach (var porLoja in pendentes.GroupBy(a => a.LojaId))
        {
            var itens = await BuscarVencimentosDeHojeAsync(db, porLoja.Key, hojeBr, ct);

            foreach (var assinatura in porLoja)
            {
                if (itens.Count > 0)
                {
                    var (titulo, corpo) = MontarMensagem(itens);
                    var r = await push.EnviarAsync(assinatura, titulo, corpo, "/financeiro?aba=pagar", "vencimentos-hoje", ct);
                    if (r == PushService.Resultado.AssinaturaInvalida)
                    {
                        db.AssinaturasPush.Remove(assinatura);
                        continue;
                    }
                }
                assinatura.UltimoAvisoEm = DateTime.UtcNow;
            }

            await db.SaveChangesAsync(ct);
        }
    }

    private record ItemVencimento(string Descricao, decimal Valor);

    private static async Task<List<ItemVencimento>> BuscarVencimentosDeHojeAsync(AppDbContext db, Guid lojaId, DateTime hojeBr, CancellationToken ct)
    {
        var inicio = DateTime.SpecifyKind(hojeBr.Date, DateTimeKind.Utc);
        var fim = inicio.AddDays(1);
        var itens = new List<ItemVencimento>();

        // Contas a pagar avulsas/parceladas/fixas que vencem hoje e não foram pagas
        var lancamentos = await db.LancamentosFinanceiros.AsNoTracking()
            .Where(l => l.LojaId == lojaId && l.Tipo == "pagar" && l.Status == "pendente" && l.Avisar
                        && l.Vencimento >= inicio && l.Vencimento < fim)
            .Select(l => new { l.Descricao, l.Valor })
            .ToListAsync(ct);
        itens.AddRange(lancamentos.Select(l => new ItemVencimento(l.Descricao, l.Valor)));

        // Faturas de cartão que vencem hoje e ainda têm saldo a pagar
        var cartoes = await db.CartoesCredito.AsNoTracking()
            .Where(c => c.LojaId == lojaId && c.Ativo)
            .ToListAsync(ct);
        var diasNoMes = DateTime.DaysInMonth(hojeBr.Year, hojeBr.Month);

        foreach (var cartao in cartoes)
        {
            if (Math.Min(cartao.DiaVencimento, diasNoMes) != hojeBr.Day) continue;

            var fechamentoAtual = new DateTime(hojeBr.Year, hojeBr.Month, Math.Min(cartao.DiaFechamento, diasNoMes), 0, 0, 0, DateTimeKind.Utc);
            var cicloInicio = fechamentoAtual.AddMonths(-1).AddDays(1);

            var totalCiclo = await db.LancamentosCartao.AsNoTracking()
                .Where(l => l.CartaoCreditoId == cartao.Id && l.DataCompra.Date >= cicloInicio.Date && l.DataCompra.Date <= fechamentoAtual.Date)
                .SumAsync(l => (decimal?)l.Valor, ct) ?? 0;
            if (totalCiclo <= 0) continue;

            var fatura = await db.FaturasCartao.AsNoTracking()
                .FirstOrDefaultAsync(f => f.CartaoCreditoId == cartao.Id && f.MesReferencia.Year == hojeBr.Year && f.MesReferencia.Month == hojeBr.Month, ct);
            if (fatura?.Status == "pago" || fatura?.Status == "financiada" || fatura?.Status == "parcial") continue;

            var antecipado = fatura is null ? 0 : await db.PagamentosAntecipadosFatura.AsNoTracking()
                .Where(p => p.FaturaCartaoId == fatura.Id)
                .SumAsync(p => (decimal?)p.Valor, ct) ?? 0;

            var restante = totalCiclo - antecipado;
            if (restante > 0) itens.Add(new ItemVencimento($"Fatura {cartao.Nome}", restante));
        }

        return itens;
    }

    private static (string Titulo, string Corpo) MontarMensagem(List<ItemVencimento> itens)
    {
        string Moeda(decimal v) => v.ToString("C", PtBr);

        if (itens.Count == 1)
            return ("Conta vence hoje", $"{itens[0].Descricao} — {Moeda(itens[0].Valor)}");

        var total = itens.Sum(i => i.Valor);
        var nomes = string.Join(", ", itens.Take(3).Select(i => i.Descricao));
        if (itens.Count > 3) nomes += $" e mais {itens.Count - 3}";
        return ($"{itens.Count} contas vencem hoje", $"Total {Moeda(total)} · {nomes}");
    }
}
