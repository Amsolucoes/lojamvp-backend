using LojaApi.Domain.Financeiro;

namespace LojaApi.Application.Financeiro;

// Despesas por categoria de vários meses de uma vez: base do balanço desmembrado, do detalhe do balanço, do comparativo mês a mês e do Planejamento.
// Soma as despesas avulsas por categoria e o cartão de crédito de cada mês (mesma regra da contribuição do cartão). Com desmembrar, o valor do cartão
// é distribuído entre as categorias das compras (pela data da compra); as parcelas de financiamento viram uma linha própria.
public class DespesasPorCategoria(IRelatoriosConsultas consultas, IContasAPagarConsultas contasAPagar)
{
    public sealed class Linha
    {
        public string Icone { get; set; } = RelatoriosRegras.IconeDaCategoria;
        public decimal Avulso { get; set; }
        public decimal Cartao { get; set; }
        public decimal Total => Avulso + Cartao;
    }

    public async Task<Dictionary<(int Ano, int Mes), Dictionary<string, Linha>>> CalcularAsync(Guid lojaId, List<(int Ano, int Mes)> meses, bool desmembrar)
    {
        var resultado = new Dictionary<(int Ano, int Mes), Dictionary<string, Linha>>();
        foreach (var m in meses) resultado[m] = [];
        if (meses.Count == 0) return resultado;

        var ordenados = meses.OrderBy(m => m.Ano * 12 + m.Mes).ToList();
        var inicio = new DateTime(ordenados[0].Ano, ordenados[0].Mes, 1, 0, 0, 0, DateTimeKind.Utc);
        var fim = new DateTime(ordenados[^1].Ano, ordenados[^1].Mes, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(1);
        var hoje = DateTime.UtcNow.Date;

        static void Somar(Dictionary<string, Linha> categorias, string nome, string? icone, decimal avulso, decimal cartao)
        {
            if (!categorias.TryGetValue(nome, out var linha))
            {
                linha = new Linha { Icone = icone ?? RelatoriosRegras.IconeDaCategoria };
                categorias[nome] = linha;
            }
            linha.Avulso += avulso;
            linha.Cartao += cartao;
        }

        // Despesas avulsas/parceladas/fixas. Parcelas de financiamento de cartão ficam de fora: o valor delas já entra pelo cartão (senão a dívida contaria duas vezes).
        var lancamentos = (await consultas.ListarLancamentosDoPeriodoAsync(lojaId, inicio, fim)).Where(l => l.Tipo == "pagar" && l.CartaoOrigemId == null);
        foreach (var l in lancamentos)
        {
            if (!resultado.TryGetValue((l.Vencimento.Year, l.Vencimento.Month), out var categorias)) continue;
            Somar(categorias, l.CategoriaNome ?? RelatoriosRegras.SemCategoria, l.CategoriaIcone, l.Valor, 0);
        }

        var cartoes = await consultas.ListarCartoesAtivosAsync(lojaId);
        if (cartoes.Count == 0) return resultado;

        var cartaoIds = cartoes.Select(c => c.Id).ToList();
        var dados = await DadosDosCartoes.CarregarAsync(consultas, contasAPagar, cartaoIds, inicio.AddMonths(-1), fim, inicio, fim);
        var parcelasFinanciamento = await contasAPagar.ListarParcelasDeFinanciamentoAsync(cartaoIds, inicio, fim);
        var itens = desmembrar ? await consultas.ListarComprasCategorizadasAsync(cartaoIds, inicio, fim) : [];

        foreach (var (ano, mes) in meses)
        {
            var categorias = resultado[(ano, mes)];

            if (desmembrar)
            {
                // Desmembrado: compras do cartão pela DATA DA COMPRA dentro do mês (não pela fatura que vence nele)
                foreach (var grupo in itens.Where(i => i.DataCompra.Year == ano && i.DataCompra.Month == mes).GroupBy(i => i.CategoriaNome ?? RelatoriosRegras.SemCategoria))
                    Somar(categorias, grupo.Key, grupo.First().CategoriaIcone, 0, grupo.Sum(i => i.Valor));

                var somaParcelas = parcelasFinanciamento.Where(p => p.Vencimento.Year == ano && p.Vencimento.Month == mes).Sum(p => p.Valor);
                if (somaParcelas > 0) Somar(categorias, RelatoriosRegras.CategoriaFinanciamento, "🏦", 0, somaParcelas);
                continue;
            }

            foreach (var cartao in cartoes)
            {
                var parcelasDoMes = parcelasFinanciamento
                    .Where(p => p.CartaoOrigemId == cartao.Id && p.Vencimento.Year == ano && p.Vencimento.Month == mes)
                    .Select(p => new RelatoriosRegras.ParcelaDoMes(p.Status, p.Vencimento, p.Valor));
                var total = dados.ContribuicaoNoMes(cartao, ano, mes, hoje, parcelasDoMes).Total;
                if (total <= 0) continue;
                Somar(categorias, RelatoriosRegras.CategoriaCartao, "💳", 0, total);
            }
        }

        return resultado;
    }
}
