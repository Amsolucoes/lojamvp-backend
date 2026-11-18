using LojaApi.Domain.Financeiro;
using LojaApi.Shared.Application;

namespace LojaApi.Application.Financeiro;

// Planejamento do mês: receita (planejada, prevista ou a soma dos tetos de receita), quanto guardar, o gasto do mês por categoria contra o teto
// (avulso + cartão pela data da compra), o recebido por categoria de receita e as metas (com quanto guardar por mês).
public class PlanejamentoQueryHandler(IPlanejamentoRepository planejamento, IRelatoriosConsultas consultas, DespesasPorCategoria despesas, CorrecaoDeFinanciamentos correcao)
    : IQueryHandler<PlanejamentoQuery>
{
    public async Task<Resultado<object>> HandleAsync(PlanejamentoQuery query)
    {
        var (lojaIdOuNulo, ano, mes) = query;
        if (lojaIdOuNulo is not { } lojaId) return Resultado.Ok(new { });
        if (!PlanejamentoRegras.MesValido(ano, mes)) return Resultado.Invalido("Mês inválido.");

        await correcao.CorrigirAsync(lojaId);

        var plano = await SalvarPlanejamentoConfigCommandHandler.ObterOuCriarAsync(planejamento, lojaId);
        var categorias = await planejamento.ListarCategoriasAtivasAsync(lojaId);
        var tetos = await planejamento.ListarTetosAsync(lojaId);

        // Gasto do mês por categoria, com as compras do cartão pela data da compra (avulso + cartão)
        var calculo = await despesas.CalcularAsync(lojaId, [(ano, mes)], true);
        var gastos = calculo[(ano, mes)];

        var inicio = new DateTime(ano, mes, 1, 0, 0, 0, DateTimeKind.Utc);
        var fim = inicio.AddMonths(1);
        var receitasDoMes = (await consultas.ListarLancamentosDoPeriodoAsync(lojaId, inicio, fim)).Where(l => l.Tipo == "receber" && l.CartaoOrigemId == null).ToList();
        var receitaPrevista = receitasDoMes.Sum(l => l.Valor);

        // Receitas do mês por categoria: recebido (pago) e ainda a receber
        var linhasReceita = categorias.Where(c => c.Tipo != "pagar").Select(c =>
        {
            var teto = tetos.FirstOrDefault(t => t.CategoriaId == c.Id && t.Tipo == "receber");
            var doMes = receitasDoMes.Where(l => l.CategoriaNome == c.Nome).ToList();
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
                icone = c.Icone ?? RelatoriosRegras.IconeDaCategoria,
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
        var receita = PlanejamentoRegras.ReceitaDoMes(totalPlanejadoReceber, plano.ReceitaPlanejada, receitaPrevista);
        var poupar = PlanejamentoRegras.ValorAPoupar(receita, plano.PercentualPoupanca);

        var hoje = DateTime.UtcNow;
        var metasDto = (await planejamento.ListarMetasAsync(lojaId)).Select(m =>
        {
            decimal? porMes = m.DataAlvo.HasValue ? PlanejamentoRegras.GuardarPorMes(m.ValorAlvo, m.ValorGuardado, m.DataAlvo.Value, hoje) : null;
            return new { m.Id, m.Nome, m.Icone, m.ValorAlvo, m.ValorGuardado, m.DataAlvo, porMes };
        }).ToList();

        return Resultado.Ok(new
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
