using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LojaApi.Data;
using LojaApi.DTOs;

namespace LojaApi.Controllers;

[ApiController]
[Route("api/relatorios")]
[Authorize]
public class RelatoriosController(AppDbContext db) : ControllerBase
{
    // GET /api/relatorios/resumo?de=2024-01-01&ate=2024-01-31
    [HttpGet("resumo")]
    public async Task<IActionResult> Resumo([FromQuery] DateTime? de, [FromQuery] DateTime? ate)
    {
        var q = db.Vendas.AsQueryable();
        if (de.HasValue)  q = q.Where(v => v.CriadaEm >= de.Value);
        if (ate.HasValue) q = q.Where(v => v.CriadaEm <= ate.Value.AddDays(1));

        var vendas = await q.Include(v => v.Itens).ToListAsync();

        return Ok(new ResumoVendasDto(
            TotalVendido:       vendas.Sum(v => v.TotalFinal),
            TotalDescontos:     vendas.Sum(v => v.Desconto),
            TicketMedio:        vendas.Count > 0 ? vendas.Sum(v => v.TotalFinal) / vendas.Count : 0,
            TotalItensVendidos: vendas.Sum(v => v.Itens.Sum(i => i.Quantidade)),
            TotalVendas:        vendas.Count
        ));
    }

    // GET /api/relatorios/produtos-ranking?de=...&ate=...&top=10
    [HttpGet("produtos-ranking")]
    public async Task<IActionResult> ProdutosRanking(
        [FromQuery] DateTime? de,
        [FromQuery] DateTime? ate,
        [FromQuery] int top = 10)
    {
        var qVendas = db.Vendas.AsQueryable();
        if (de.HasValue)  qVendas = qVendas.Where(v => v.CriadaEm >= de.Value);
        if (ate.HasValue) qVendas = qVendas.Where(v => v.CriadaEm <= ate.Value.AddDays(1));

        var itens = await qVendas
            .Include(v => v.Itens).ThenInclude(i => i.Produto)
            .SelectMany(v => v.Itens)
            .ToListAsync();

        var ranking = itens
            .Where(i => i.ProdutoId.HasValue)
            .GroupBy(i => i.ProdutoId!.Value)
            .Select(g => new ProdutoRankingDto(
                Id:             g.Key,
                Nome:           g.First().Produto?.Nome ?? "",
                Categoria:      g.First().Produto?.Categoria ?? "",
                QtdVendida:     g.Sum(i => i.Quantidade),
                Receita:        g.Sum(i => i.Subtotal),
                LucroEstimado:  g.Sum(i => i.Subtotal - i.Quantidade * (i.Produto?.PrecoCusto ?? 0))
            ))
            .OrderByDescending(r => r.QtdVendida)
            .Take(top)
            .ToList();

        return Ok(ranking);
    }

    // GET /api/relatorios/fluxo-diario?mes=6&ano=2025
    [HttpGet("fluxo-diario")]
    public async Task<IActionResult> FluxoDiario([FromQuery] int mes, [FromQuery] int ano)
    {
        var de  = new DateTime(ano, mes, 1, 0, 0, 0, DateTimeKind.Utc);
        var ate = de.AddMonths(1);

        var vendas = await db.Vendas
            .Where(v => v.CriadaEm >= de && v.CriadaEm < ate)
            .ToListAsync();

        var dias = Enumerable.Range(1, DateTime.DaysInMonth(ano, mes))
            .Select(d =>
            {
                var data = new DateTime(ano, mes, d);
                var vs   = vendas.Where(v => v.CriadaEm.Date == data.Date).ToList();
                return new FluxoDiaDto(
                    Data:       data.ToString("dd/MM"),
                    QtdVendas:  vs.Count,
                    Entradas:   vs.Sum(v => v.TotalFinal),
                    Descontos:  vs.Sum(v => v.Desconto)
                );
            })
            .ToList();

        return Ok(dias);
    }

    // GET /api/relatorios/fluxo-mensal?ano=2025
    [HttpGet("fluxo-mensal")]
    public async Task<IActionResult> FluxoMensal([FromQuery] int ano)
    {
        var de  = new DateTime(ano, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var ate = de.AddYears(1);

        var vendas = await db.Vendas
            .Where(v => v.CriadaEm >= de && v.CriadaEm < ate)
            .ToListAsync();

        var nomesMeses = new[] { "Jan","Fev","Mar","Abr","Mai","Jun","Jul","Ago","Set","Out","Nov","Dez" };

        var meses = Enumerable.Range(1, 12)
            .Select(m =>
            {
                var vs = vendas.Where(v => v.CriadaEm.Month == m).ToList();
                return new FluxoMesDto(
                    Mes:       m,
                    NomeMes:   nomesMeses[m - 1],
                    QtdVendas: vs.Count,
                    Entradas:  vs.Sum(v => v.TotalFinal),
                    Descontos: vs.Sum(v => v.Desconto)
                );
            })
            .ToList();

        return Ok(meses);
    }
}
