using LojaApi.Domain.Financeiro;
using LojaApi.Shared.Application;

namespace LojaApi.Application.Financeiro;

// Lançamentos que compõem uma categoria do balanço (inclusive as compras dentro do cartão). O balanço desmembrado soma, na categoria, despesas avulsas
// + compras do cartão. Contas a Pagar só lista as avulsas (as compras ficam dentro da linha da fatura, que some quando ela é paga), então o clique na
// categoria precisa deste detalhe para fechar com o valor do balanço.
public class BalancoDetalheQueryHandler(IRelatoriosConsultas consultas, IContasAPagarConsultas contasAPagar, DespesasPorCategoria despesas, CorrecaoDeFinanciamentos correcao)
    : IQueryHandler<BalancoDetalheQuery>
{
    private sealed record Item(string Origem, string Descricao, decimal Valor, DateTime Data, string Status, string? CartaoNome, string? ContaNome = null, Guid? Id = null, string? Modo = null);

    public async Task<Resultado<object>> HandleAsync(BalancoDetalheQuery query)
    {
        var (lojaIdOuNulo, ano, mes, categoria, desmembrar) = query;
        if (lojaIdOuNulo is not { } lojaId) return Resultado.Ok(new { });

        await correcao.CorrigirAsync(lojaId);

        var inicio = new DateTime(ano, mes, 1, 0, 0, 0, DateTimeKind.Utc);
        var fim = inicio.AddMonths(1);
        var hoje = DateTime.UtcNow.Date;
        var semCategoria = categoria == RelatoriosRegras.SemCategoria;
        var itens = new List<Item>();

        // Despesas avulsas/parceladas/fixas da categoria
        var avulsos = await consultas.ListarAPagarDaCategoriaAsync(lojaId, inicio, fim, semCategoria ? null : categoria);
        itens.AddRange(avulsos.Select(l => new Item("avulso", l.Descricao, l.Valor, l.Vencimento, l.Status, null, l.ContaNome, l.Id, l.Modo)));

        var cartoes = await consultas.ListarCartoesAtivosAsync(lojaId);
        if (cartoes.Count > 0)
        {
            var cartaoIds = cartoes.Select(c => c.Id).ToList();
            var dados = await DadosDosCartoes.CarregarAsync(consultas, contasAPagar, cartaoIds, inicio.AddMonths(-1), fim, inicio, fim);
            var parcelasFinanciamento = await contasAPagar.ListarParcelasDeFinanciamentoAsync(cartaoIds, inicio, fim);

            if (categoria == RelatoriosRegras.CategoriaCartao)
            {
                // Visão agrupada: uma linha por cartão, com o valor que o balanço considera da fatura do mês
                foreach (var cartao in cartoes)
                {
                    var parcelasDoMes = parcelasFinanciamento.Where(p => p.CartaoOrigemId == cartao.Id).Select(p => new RelatoriosRegras.ParcelaDoMes(p.Status, p.Vencimento, p.Valor));
                    var total = dados.ContribuicaoNoMes(cartao, ano, mes, hoje, parcelasDoMes).Total;
                    if (total <= 0) continue;
                    var status = dados.Fatura(cartao.Id, ano, mes)?.Status;
                    itens.Add(new Item("cartao_fatura", $"Fatura {cartao.Nome}", total, CartaoRegras.VencimentoDaFatura(cartao.DiaVencimento, ano, mes),
                        ContasAPagarRegras.ComprasResolvidas(status) ? "pago" : "pendente", cartao.Nome));
                }
            }
            else if (desmembrar && categoria == RelatoriosRegras.CategoriaFinanciamento)
            {
                foreach (var p in parcelasFinanciamento)
                {
                    var cartao = cartoes.First(c => c.Id == p.CartaoOrigemId);
                    itens.Add(new Item("cartao_parcela", p.Descricao, p.Valor, p.Vencimento, p.Status, cartao.Nome));
                }
            }
            else if (desmembrar)
            {
                // Compras no cartão feitas (data da compra) dentro do mês escolhido, qualquer que seja a fatura
                var compras = (await consultas.ListarComprasCategorizadasAsync(cartaoIds, inicio, fim))
                    .Where(c => semCategoria ? c.CategoriaNome == null : c.CategoriaNome == categoria);

                foreach (var c in compras)
                {
                    var cartao = cartoes.First(x => x.Id == c.CartaoId);
                    itens.Add(new Item("cartao_item", c.Descricao, c.Valor, c.DataCompra, "cartao", cartao.Nome, null, c.Id, c.Modo));
                }
            }
        }

        // Valor que o balanço mostra para essa categoria (para o usuário conferir com a soma da lista)
        var calculo = await despesas.CalcularAsync(lojaId, [(ano, mes)], desmembrar);
        var valorBalanco = calculo[(ano, mes)].TryGetValue(categoria, out var linha) ? linha.Total : 0m;

        return Resultado.Ok(new
        {
            categoria,
            ano,
            mes,
            valorBalanco,
            totalItens = itens.Sum(i => i.Valor),
            itens = itens.OrderBy(i => i.Data).Select(i => new
            {
                origem = i.Origem,
                descricao = i.Descricao,
                valor = i.Valor,
                data = i.Data,
                status = i.Status,
                cartaoNome = i.CartaoNome,
                contaNome = i.ContaNome,
                id = i.Id,
                modo = i.Modo,
            }).ToList(),
        });
    }
}
