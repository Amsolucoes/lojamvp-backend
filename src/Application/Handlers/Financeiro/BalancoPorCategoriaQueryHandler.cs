using LojaApi.Domain.Financeiro;
using LojaApi.Shared.Application;

namespace LojaApi.Application.Financeiro;

// Balanço do mês por categoria: receitas e despesas agrupadas (mensalidades de planos viram "Mensalidade/Assinatura" e o cartão vira "Cartão de Crédito",
// pela mesma regra do resumo mensal, para os números sempre baterem) e a visão desmembrada, que distribui o cartão entre as categorias das compras.
public class BalancoPorCategoriaQueryHandler(IRelatoriosConsultas consultas, IContasAPagarConsultas contasAPagar, DespesasPorCategoria despesas, CorrecaoDeFinanciamentos correcao)
    : IQueryHandler<BalancoPorCategoriaQuery>
{
    private sealed record Linha(string Nome, string Icone, decimal Valor);

    public async Task<Resultado<object>> HandleAsync(BalancoPorCategoriaQuery query)
    {
        var (lojaIdOuNulo, ano, mes) = query;
        if (lojaIdOuNulo is not { } lojaId) return Resultado.Ok(new { });

        await correcao.CorrigirAsync(lojaId);

        var inicio = new DateTime(ano, mes, 1, 0, 0, 0, DateTimeKind.Utc);
        var fim = inicio.AddMonths(1);
        var hoje = DateTime.UtcNow.Date;

        var doMes = await consultas.ListarLancamentosDoPeriodoAsync(lojaId, inicio, fim);

        // Planos entram como receita agrupada em "Mensalidade/Assinatura"
        var pagamentosPlanoMes = (await consultas.ListarPagamentosDePlanoAsync(lojaId, inicio, fim, apenasAssinaturasAtivas: false)).Sum(p => p.Valor);

        // Cartões entram como despesa agrupada em "Cartão de Crédito", pela mesma regra do resumo mensal e de Contas a Pagar
        var cartoes = await consultas.ListarCartoesAtivosAsync(lojaId);
        var dados = await DadosDosCartoes.CarregarAsync(consultas, contasAPagar, cartoes.Select(c => c.Id).ToList(), inicio.AddMonths(-1), fim, inicio, fim);
        decimal totalCartoesMes = 0;
        foreach (var cartao in cartoes)
        {
            var parcelas = doMes.Where(l => l.CartaoOrigemId == cartao.Id).Select(l => new RelatoriosRegras.ParcelaDoMes(l.Status, l.Vencimento, l.Valor));
            totalCartoesMes += dados.ContribuicaoNoMes(cartao, ano, mes, hoje, parcelas).Total;
        }

        static List<Linha> Agrupar(IEnumerable<LancamentoDoMesDto> lancamentos) => lancamentos
            .GroupBy(l => new { Nome = l.CategoriaNome ?? RelatoriosRegras.SemCategoria, Icone = l.CategoriaIcone ?? RelatoriosRegras.IconeDaCategoria })
            .Select(g => new Linha(g.Key.Nome, g.Key.Icone, g.Sum(x => x.Valor)))
            .ToList();

        var receitasPorCategoria = Agrupar(doMes.Where(l => l.Tipo == "receber"));
        // Parcelas de financiamento de cartão ficam de fora de propósito: o valor da compra original já foi contado como despesa no mês em que a fatura
        // venceu (via totalCartoesMes). Contar de novo cada parcela no mês em que vence duplicaria a mesma dívida.
        var despesasPorCategoria = Agrupar(doMes.Where(l => l.Tipo == "pagar" && !l.CartaoOrigemId.HasValue));

        if (pagamentosPlanoMes > 0)
        {
            var existente = receitasPorCategoria.FirstOrDefault(r => r.Nome == RelatoriosRegras.CategoriaMensalidade);
            receitasPorCategoria = receitasPorCategoria.Where(r => r.Nome != RelatoriosRegras.CategoriaMensalidade)
                .Append(new Linha(RelatoriosRegras.CategoriaMensalidade, "💳", (existente?.Valor ?? 0) + pagamentosPlanoMes))
                .ToList();
        }

        if (totalCartoesMes > 0)
        {
            var existente = despesasPorCategoria.FirstOrDefault(d => d.Nome == RelatoriosRegras.CategoriaCartao);
            despesasPorCategoria = despesasPorCategoria.Where(d => d.Nome != RelatoriosRegras.CategoriaCartao)
                .Append(new Linha(RelatoriosRegras.CategoriaCartao, "💳", (existente?.Valor ?? 0) + totalCartoesMes))
                .ToList();
        }

        var totalReceitas = receitasPorCategoria.Sum(r => r.Valor);
        var totalDespesas = despesasPorCategoria.Sum(d => d.Valor);

        // Visão "desmembrada": em vez de uma linha única "Cartão de Crédito", distribui o valor do cartão entre as categorias das compras
        // (e soma às categorias das despesas avulsas)
        var desmembradas = await despesas.CalcularAsync(lojaId, [(ano, mes)], true);
        var despesasDesmembradas = desmembradas[(ano, mes)]
            .Select(kv => new { nome = kv.Key, icone = kv.Value.Icone, valor = kv.Value.Total, valorCartao = kv.Value.Cartao, valorAvulso = kv.Value.Avulso })
            .OrderByDescending(x => x.valor)
            .ToList();

        return Resultado.Ok(new
        {
            receitas = receitasPorCategoria.OrderByDescending(r => r.Valor).Select(r => new { nome = r.Nome, icone = r.Icone, valor = r.Valor }),
            despesas = despesasPorCategoria.OrderByDescending(d => d.Valor).Select(d => new { nome = d.Nome, icone = d.Icone, valor = d.Valor }),
            despesasDesmembradas,
            totalReceitas,
            totalDespesas,
            saldo = totalReceitas - totalDespesas,
        });
    }
}
