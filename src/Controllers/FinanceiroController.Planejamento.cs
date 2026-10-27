using LojaApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LojaApi.src.Controllers;

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

    public record PlanejamentoConfigRequest(decimal? ReceitaPlanejada, decimal PercentualPoupanca);

    [HttpPut("planejamento/config")]
    public async Task<IActionResult> SalvarPlanejamentoConfig([FromBody] PlanejamentoConfigRequest req)
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return BadRequest(new { erro = "Loja não encontrada." });
        if (req.PercentualPoupanca < 0 || req.PercentualPoupanca > 100)
            return BadRequest(new { erro = "O percentual a guardar deve ficar entre 0 e 100." });
        if (req.ReceitaPlanejada < 0)
            return BadRequest(new { erro = "A receita planejada não pode ser negativa." });

        var plano = await ObterOuCriarPlanejamentoAsync(lojaId.Value);
        plano.ReceitaPlanejada = req.ReceitaPlanejada is > 0 ? req.ReceitaPlanejada : null;
        plano.PercentualPoupanca = req.PercentualPoupanca;
        await db.SaveChangesAsync();
        return Ok(new { plano.ReceitaPlanejada, plano.PercentualPoupanca });
    }

    public record PlanejamentoCategoriaItem(Guid CategoriaId, bool Ativo, decimal Valor, string? Tipo = "pagar");
    public record PlanejamentoCategoriasRequest(List<PlanejamentoCategoriaItem> Itens);

    [HttpPut("planejamento/categorias")]
    public async Task<IActionResult> SalvarPlanejamentoCategorias([FromBody] PlanejamentoCategoriasRequest req)
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return BadRequest(new { erro = "Loja não encontrada." });

        var idsValidos = await db.CategoriasFinanceiras.Where(c => c.LojaId == lojaId).Select(c => c.Id).ToListAsync();
        var existentes = await db.PlanejamentoCategorias.Where(p => p.LojaId == lojaId).ToListAsync();

        foreach (var item in req.Itens.Where(i => idsValidos.Contains(i.CategoriaId)))
        {
            if (item.Valor < 0) return BadRequest(new { erro = "O valor planejado não pode ser negativo." });
            var tipo = item.Tipo == "receber" ? "receber" : "pagar";
            var atual = existentes.FirstOrDefault(p => p.CategoriaId == item.CategoriaId && p.Tipo == tipo);
            if (atual is null)
            {
                db.PlanejamentoCategorias.Add(new PlanejamentoCategoria { LojaId = lojaId.Value, CategoriaId = item.CategoriaId, Tipo = tipo, Ativo = item.Ativo, Valor = item.Valor });
            }
            else
            {
                atual.Ativo = item.Ativo;
                atual.Valor = item.Valor;
            }
        }

        await db.SaveChangesAsync();
        return Ok(new { ok = true });
    }

    public record MetaPlanejamentoRequest(string Nome, string? Icone, decimal ValorAlvo, decimal ValorGuardado, DateTime? DataAlvo);

    [HttpPost("planejamento/metas")]
    public async Task<IActionResult> CriarMetaPlanejamento([FromBody] MetaPlanejamentoRequest req)
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return BadRequest(new { erro = "Loja não encontrada." });
        if (string.IsNullOrWhiteSpace(req.Nome)) return BadRequest(new { erro = "Informe o nome da meta." });
        if (req.ValorAlvo <= 0) return BadRequest(new { erro = "Informe o valor da meta." });

        var meta = new MetaPlanejamento
        {
            LojaId = lojaId.Value,
            Nome = req.Nome.Trim(),
            Icone = string.IsNullOrWhiteSpace(req.Icone) ? "🎯" : req.Icone.Trim(),
            ValorAlvo = req.ValorAlvo,
            ValorGuardado = Math.Max(0, req.ValorGuardado),
            DataAlvo = req.DataAlvo.HasValue ? DateTime.SpecifyKind(req.DataAlvo.Value.Date, DateTimeKind.Utc) : null,
        };
        db.MetasPlanejamento.Add(meta);
        await db.SaveChangesAsync();
        return Ok(new { meta.Id });
    }

    [HttpPut("planejamento/metas/{id:guid}")]
    public async Task<IActionResult> AtualizarMetaPlanejamento(Guid id, [FromBody] MetaPlanejamentoRequest req)
    {
        var lojaId = await GetLojaId();
        var meta = await db.MetasPlanejamento.FirstOrDefaultAsync(m => m.Id == id && m.LojaId == lojaId);
        if (meta is null) return NotFound();
        if (string.IsNullOrWhiteSpace(req.Nome)) return BadRequest(new { erro = "Informe o nome da meta." });
        if (req.ValorAlvo <= 0) return BadRequest(new { erro = "Informe o valor da meta." });

        meta.Nome = req.Nome.Trim();
        meta.Icone = string.IsNullOrWhiteSpace(req.Icone) ? "🎯" : req.Icone.Trim();
        meta.ValorAlvo = req.ValorAlvo;
        meta.ValorGuardado = Math.Max(0, req.ValorGuardado);
        meta.DataAlvo = req.DataAlvo.HasValue ? DateTime.SpecifyKind(req.DataAlvo.Value.Date, DateTimeKind.Utc) : null;
        await db.SaveChangesAsync();
        return Ok(new { meta.Id });
    }

    public record AporteMetaRequest(decimal Valor);

    // Soma (ou, com valor negativo, retira) dinheiro guardado numa meta
    [HttpPost("planejamento/metas/{id:guid}/aportar")]
    public async Task<IActionResult> AportarMetaPlanejamento(Guid id, [FromBody] AporteMetaRequest req)
    {
        var lojaId = await GetLojaId();
        var meta = await db.MetasPlanejamento.FirstOrDefaultAsync(m => m.Id == id && m.LojaId == lojaId);
        if (meta is null) return NotFound();
        if (req.Valor == 0) return BadRequest(new { erro = "Informe um valor." });

        meta.ValorGuardado = Math.Max(0, meta.ValorGuardado + req.Valor);
        await db.SaveChangesAsync();
        return Ok(new { meta.ValorGuardado });
    }

    [HttpDelete("planejamento/metas/{id:guid}")]
    public async Task<IActionResult> ExcluirMetaPlanejamento(Guid id)
    {
        var lojaId = await GetLojaId();
        var meta = await db.MetasPlanejamento.FirstOrDefaultAsync(m => m.Id == id && m.LojaId == lojaId);
        if (meta is null) return NotFound();
        db.MetasPlanejamento.Remove(meta);
        await db.SaveChangesAsync();
        return NoContent();
    }

    public record TrocarCategoriaRequest(Guid? CategoriaId, bool TodasParcelas = false);

    // Troca só a categoria de um lançamento (avulso) — usado para corrigir gastos no detalhe do planejamento
    [HttpPatch("lancamentos/{id:guid}/categoria")]
    public async Task<IActionResult> TrocarCategoriaLancamento(Guid id, [FromBody] TrocarCategoriaRequest req)
    {
        var lojaId = await GetLojaId();
        var lanc = await db.LancamentosFinanceiros.FirstOrDefaultAsync(l => l.Id == id && l.LojaId == lojaId);
        if (lanc is null) return NotFound();

        if (req.CategoriaId.HasValue && !await db.CategoriasFinanceiras.AnyAsync(c => c.Id == req.CategoriaId && c.LojaId == lojaId))
            return BadRequest(new { erro = "Categoria não encontrada." });

        var alterados = 1;
        if (lanc.Modo == "fixa" && lanc.LancamentoFixoId.HasValue)
        {
            // Conta fixa: a categoria vale para o modelo recorrente e para todos os meses já gerados
            var fixo = await db.LancamentosFixos.FirstOrDefaultAsync(f => f.Id == lanc.LancamentoFixoId.Value && f.LojaId == lojaId);
            if (fixo is not null) fixo.CategoriaId = req.CategoriaId;
            var irmaos = await db.LancamentosFinanceiros
                .Where(l => l.LojaId == lojaId && l.LancamentoFixoId == lanc.LancamentoFixoId).ToListAsync();
            foreach (var l in irmaos) l.CategoriaId = req.CategoriaId;
            alterados = irmaos.Count;
        }
        else if (req.TodasParcelas && lanc.Modo == "parcelada" && lanc.GrupoParcelamentoId.HasValue)
        {
            var parcelas = await db.LancamentosFinanceiros
                .Where(l => l.LojaId == lojaId && l.GrupoParcelamentoId == lanc.GrupoParcelamentoId).ToListAsync();
            foreach (var l in parcelas) l.CategoriaId = req.CategoriaId;
            alterados = parcelas.Count;
        }
        else
        {
            lanc.CategoriaId = req.CategoriaId;
        }

        await db.SaveChangesAsync();
        return Ok(new { lanc.Id, lanc.CategoriaId, alterados });
    }

    // Troca a categoria de uma compra do cartão (opcionalmente de todas as parcelas da mesma compra)
    [HttpPatch("cartoes/lancamentos/{id:guid}/categoria")]
    public async Task<IActionResult> TrocarCategoriaCompraCartao(Guid id, [FromBody] TrocarCategoriaRequest req)
    {
        var lojaId = await GetLojaId();
        var compra = await db.LancamentosCartao.FirstOrDefaultAsync(l => l.Id == id && l.LojaId == lojaId);
        if (compra is null) return NotFound();

        if (req.CategoriaId.HasValue && !await db.CategoriasFinanceiras.AnyAsync(c => c.Id == req.CategoriaId && c.LojaId == lojaId))
            return BadRequest(new { erro = "Categoria não encontrada." });

        var alvos = new List<LancamentoCartao> { compra };
        if (compra.Modo == "fixa" && compra.CartaoFixoId.HasValue)
        {
            // Cobrança fixa no cartão: muda o modelo recorrente e todas as compras já geradas dele
            var fixoCartao = await db.CartaoLancamentosFixos.FirstOrDefaultAsync(f => f.Id == compra.CartaoFixoId.Value && f.LojaId == lojaId);
            if (fixoCartao is not null) fixoCartao.CategoriaId = req.CategoriaId;
            alvos = await db.LancamentosCartao.Where(l => l.LojaId == lojaId && l.CartaoFixoId == compra.CartaoFixoId).ToListAsync();
        }
        else if (req.TodasParcelas && compra.GrupoParcelamentoId.HasValue)
            alvos = await db.LancamentosCartao.Where(l => l.LojaId == lojaId && l.GrupoParcelamentoId == compra.GrupoParcelamentoId).ToListAsync();

        foreach (var a in alvos) a.CategoriaId = req.CategoriaId;
        await db.SaveChangesAsync();
        return Ok(new { alterados = alvos.Count });
    }
}
