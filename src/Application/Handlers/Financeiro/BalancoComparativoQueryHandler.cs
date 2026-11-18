using LojaApi.Domain.Financeiro;
using LojaApi.Shared.Application;

namespace LojaApi.Application.Financeiro;

// Comparativo de despesas por categoria: mês escolhido x mês anterior, a série dos 12 meses do ano e o melhor mês (menor gasto) entre os que já aconteceram.
public class BalancoComparativoQueryHandler(DespesasPorCategoria despesas, CorrecaoDeFinanciamentos correcao) : IQueryHandler<BalancoComparativoQuery>
{
    public async Task<Resultado<object>> HandleAsync(BalancoComparativoQuery query)
    {
        var (lojaIdOuNulo, ano, mes, desmembrar) = query;
        if (lojaIdOuNulo is not { } lojaId) return Resultado.Ok(new { });

        await correcao.CorrigirAsync(lojaId);

        var anterior = RelatoriosRegras.MesAnterior(ano, mes);
        var meses = new List<(int Ano, int Mes)>();
        for (int m = 1; m <= 12; m++) meses.Add((ano, m));
        if (!meses.Contains(anterior)) meses.Add(anterior);

        var dados = await despesas.CalcularAsync(lojaId, meses, desmembrar);

        // Só considera "melhor mês" os meses que já aconteceram (o resto do ano ainda é previsão)
        var ultimoMesAcontecido = RelatoriosRegras.UltimoMesAcontecido(ano, DateTime.UtcNow);

        decimal ValorDe((int Ano, int Mes) chave, string nome) => dados[chave].TryGetValue(nome, out var linha) ? linha.Total : 0;

        var nomes = dados.Values.SelectMany(d => d.Keys).Distinct().ToList();
        var categorias = nomes.Select(nome =>
        {
            var serie = Enumerable.Range(1, 12).Select(m => ValorDe((ano, m), nome)).ToList();
            var atual = serie[mes - 1];
            var anteriorValor = ValorDe(anterior, nome);
            var (melhorMes, melhorValor) = RelatoriosRegras.MelhorMes(serie, ultimoMesAcontecido);
            var icone = dados.Values.Select(d => d.TryGetValue(nome, out var l) ? l.Icone : null).FirstOrDefault(i => i != null) ?? RelatoriosRegras.IconeDaCategoria;

            return new
            {
                nome,
                icone,
                atual,
                anterior = anteriorValor,
                diferenca = atual - anteriorValor,
                variacaoPct = RelatoriosRegras.VariacaoPercentual(atual, anteriorValor),
                melhorMes,
                melhorValor,
                serie,
            };
        })
        .Where(c => c.serie.Any(v => v > 0))
        .OrderByDescending(c => c.serie.Sum())
        .ToList();

        return Resultado.Ok(new
        {
            ano,
            mes,
            anterior = new { ano = anterior.Ano, mes = anterior.Mes },
            totalAtual = categorias.Sum(c => c.atual),
            totalAnterior = categorias.Sum(c => c.anterior),
            totaisMes = Enumerable.Range(0, 12).Select(i => categorias.Sum(c => c.serie[i])).ToList(),
            categorias,
        });
    }
}
