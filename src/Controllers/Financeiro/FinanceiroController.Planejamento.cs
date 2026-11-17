using LojaApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LojaApi.Controllers.Financeiro;

// Planejamento: quanto se quer gastar por categoria no mês, quanto guardar e metas.
public partial class FinanceiroController
{
    private async Task<PlanejamentoFinanceiro> ObterOuCriarPlanejamentoAsync(Guid lojaId)
    {
        var plano = await db.PlanejamentosFinanceiros.FirstOrDefaultAsync(p => p.LojaId == lojaId);
        if (plano is null)
        {
            plano = new PlanejamentoFinanceiro { LojaId = lojaId };
            db.PlanejamentosFinanceiros.Add(plano);
            await db.SaveChangesAsync();
        }
        return plano;
    }

    [HttpGet("planejamento")]
    public async Task<IActionResult> Planejamento([FromQuery] int ano, [FromQuery] int mes)
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return Ok(new { });
        if (ano < 2000 || mes < 1 || mes > 12) return BadRequest(new { erro = "Mês inválido." });

        await CorrigirParcelasFinanciamentoEmCicloFechadoAsync(lojaId.Value);

        var plano = await ObterOuCriarPlanejamentoAsync(lojaId.Value);
        var categorias = await db.CategoriasFinanceiras.AsNoTracking()
            .Where(c => c.LojaId == lojaId && c.Ativa)
            .OrderBy(c => c.Nome)
            .ToListAsync();
        var tetos = await db.PlanejamentoCategorias.AsNoTracking().Where(p => p.LojaId == lojaId).ToListAsync();

        // Gasto do mês por categoria, com as compras do cartão pela data da compra (avulso + cartão)
        var calc = await CalcularDespesasPorCategoriaAsync(lojaId.Value, new List<(int Ano, int Mes)> { (ano, mes) }, true);
        var gastos = calc[(ano, mes)];

        var inicio = new DateTime(ano, mes, 1, 0, 0, 0, DateTimeKind.Utc);
        var fim = inicio.AddMonths(1);
        var receitaPrevista = await db.LancamentosFinanceiros.AsNoTracking()
            .Where(l => l.LojaId == lojaId && l.Tipo == "receber" && l.CartaoOrigemId == null && l.Vencimento >= inicio && l.Vencimento < fim)
            .SumAsync(l => (decimal?)l.Valor) ?? 0m;

        // Receitas do mês por categoria: recebido (pago) e ainda a receber
        var recLanc = await db.LancamentosFinanceiros.AsNoTracking()
            .Where(l => l.LojaId == lojaId && l.Tipo == "receber" && l.CartaoOrigemId == null && l.Vencimento >= inicio && l.Vencimento < fim)
            .Select(l => new { Nome = l.Categoria != null ? l.Categoria.Nome : null, l.Valor, l.Status })
            .ToListAsync();

        var linhasReceita = categorias.Where(c => c.Tipo != "pagar").Select(c =>
        {
            var teto = tetos.FirstOrDefault(t => t.CategoriaId == c.Id && t.Tipo == "receber");
            var doMes = recLanc.Where(l => l.Nome == c.Nome).ToList();
            var recebido = doMes.Where(l => l.Status == "pago").Sum(l => l.Valor);
            var aReceber = doMes.Where(l => l.Status != "pago").Sum(l => l.Valor);
            return new
            {
                categoriaId = c.Id,
                nome = c.Nome,
                icone = c.Icone ?? "💰",
                ativo = teto?.Ativo ?? false,
                valor = teto?.Valor ?? 0m,
                recebido,
                aReceber,
                total = recebido + aReceber,
            };
        }).ToList();

        var linhas = categorias.Where(c => c.Tipo != "receber").Select(c =>
        {
            var teto = tetos.FirstOrDefault(t => t.CategoriaId == c.Id && t.Tipo == "pagar");
            gastos.TryGetValue(c.Nome, out var g);
            var avulso = g?.Avulso ?? 0m;
            var cartao = g?.Cartao ?? 0m;
            return new
            {
                categoriaId = c.Id,
                nome = c.Nome,
                icone = c.Icone ?? "📁",
                ativo = teto?.Ativo ?? false,
                valor = teto?.Valor ?? 0m,
                gastoAvulso = avulso,
                gastoCartao = cartao,
                gasto = avulso + cartao,
            };
        }).ToList();

        var totalPlanejado = linhas.Where(l => l.ativo).Sum(l => l.valor);
        var gastoPlanejadas = linhas.Where(l => l.ativo).Sum(l => l.gasto);
        var gastoTotal = gastos.Values.Sum(g => g.Total);
        // Receita planejada = soma das metas de receita marcadas; sem nenhuma, usa o valor antigo/previsto
        var totalPlanejadoReceber = linhasReceita.Where(l => l.ativo).Sum(l => l.valor);
        var receita = totalPlanejadoReceber > 0 ? totalPlanejadoReceber : (plano.ReceitaPlanejada ?? receitaPrevista);
        var poupar = Math.Round(receita * plano.PercentualPoupanca / 100m, 2);

        var metas = await db.MetasPlanejamento.AsNoTracking().Where(m => m.LojaId == lojaId).OrderBy(m => m.CriadoEm).ToListAsync();
        var hoje = DateTime.UtcNow;
        var metasDto = metas.Select(m =>
        {
            decimal? porMes = null;
            if (m.DataAlvo.HasValue)
            {
                var meses = Math.Max(1, (m.DataAlvo.Value.Year - hoje.Year) * 12 + m.DataAlvo.Value.Month - hoje.Month);
                porMes = Math.Max(0m, Math.Round((m.ValorAlvo - m.ValorGuardado) / meses, 2));
            }
            return new { m.Id, m.Nome, m.Icone, m.ValorAlvo, m.ValorGuardado, m.DataAlvo, porMes };
        }).ToList();

        return Ok(new
        {
            ano,
            mes,
            receitaPlanejada = plano.ReceitaPlanejada,
            receitaPrevista,
            receita,
            percentualPoupanca = plano.PercentualPoupanca,
            poupar,
            disponivelParaGastar = receita - poupar,
            totalPlanejado,
            totalPlanejadoReceber,
            recebidoPlanejadas = linhasReceita.Where(l => l.ativo).Sum(l => l.recebido),
            aReceberPlanejadas = linhasReceita.Where(l => l.ativo).Sum(l => l.aReceber),
            gastoPlanejadas,
            gastoTotal,
            categorias = linhas,
            categoriasReceita = linhasReceita,
            metas = metasDto,
        });
    }
}
