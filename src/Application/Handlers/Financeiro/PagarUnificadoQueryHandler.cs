using LojaApi.Domain.Financeiro;
using LojaApi.Shared.Application;

namespace LojaApi.Application.Financeiro;

// Contas a Pagar unificado: lançamentos a pagar do período + as faturas dos cartões que vencem nele (agrupadas, uma linha por fatura,
// ou detalhadas, uma linha por compra e por parcela de financiamento), ordenados pelo vencimento.
// As parcelas de financiamento de faturas antigas que vencem no mês SEMPRE entram na fatura do cartão, nunca como linha avulsa.
public class PagarUnificadoQueryHandler(IContasAPagarConsultas consultas, CorrecaoDeFinanciamentos correcao) : IQueryHandler<PagarUnificadoQuery>
{
    public async Task<Resultado<object>> HandleAsync(PagarUnificadoQuery query)
    {
        var (lojaIdOuNulo, ano, mes, modo, de, ate) = query;
        if (lojaIdOuNulo is not { } lojaId) return Resultado.Ok(Array.Empty<object>());

        await correcao.CorrigirAsync(lojaId);

        var (inicioMes, fimMes) = ContasAPagarRegras.PeriodoConsultado(ano, mes, de, ate, DateTime.UtcNow);

        var lancamentos = (await consultas.ListarAPagarAsync(lojaId, inicioMes, fimMes))
            .Select(l => (l.Vencimento, Linha: (object)new
            {
                l.Id,
                descricao = l.Descricao,
                observacao = l.Observacao,
                categoriaNome = l.CategoriaNome,
                categoriaId = l.CategoriaId,
                contaBancariaId = l.ContaBancariaId,
                modo = l.Modo,
                avisar = l.Avisar,
                valor = l.Valor,
                vencimento = l.Vencimento,
                status = l.Status,
                pagoEm = l.PagoEm,
                formaPagamento = l.FormaPagamento,
                numeroParcela = l.NumeroParcela,
                totalParcelas = l.TotalParcelas,
                origem = "avulso",
                cartaoId = (Guid?)null,
                cartaoNome = (string?)null,
            }))
            .ToList();

        var cartoes = await consultas.ListarCartoesAtivosAsync(lojaId);
        var mesesParaChecar = ContasAPagarRegras.MesesDoPeriodo(inicioMes, fimMes);

        // Itens, faturas, antecipados e parcelas de todos os cartões são carregados de uma vez para todo o período
        var itensTodos = new List<ItemDeCartaoDto>();
        var parcelasFinanciamento = new List<ParcelaDoCartaoDto>();
        var dados = new DadosDeCartoes();
        if (cartoes.Count > 0 && mesesParaChecar.Count > 0)
        {
            var cartaoIds = cartoes.Select(c => c.Id).ToList();
            var primeiroMes = new DateTime(mesesParaChecar[0].Ano, mesesParaChecar[0].Mes, 1, 0, 0, 0, DateTimeKind.Utc);
            var ultimo = mesesParaChecar[^1];
            var aposUltimoMes = new DateTime(ultimo.Ano, ultimo.Mes, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(1);

            itensTodos = await consultas.ListarItensDeCartaoAsync(cartaoIds, primeiroMes.AddMonths(-1), aposUltimoMes);
            parcelasFinanciamento = await consultas.ListarParcelasDeFinanciamentoAsync(cartaoIds, primeiroMes, aposUltimoMes);
            dados = await DadosDeCartoes.CarregarAsync(consultas, cartaoIds, primeiroMes, aposUltimoMes);
        }

        var linhasCartao = new List<(DateTime Vencimento, object Linha)>();
        foreach (var cartao in cartoes)
            foreach (var (anoC, mesC) in mesesParaChecar)
            {
                var vencimentoFatura = CartaoRegras.VencimentoDaFatura(cartao.DiaVencimento, anoC, mesC);
                if (vencimentoFatura < inicioMes || vencimentoFatura >= fimMes) continue;
                var (inicio, fim) = CartaoRegras.CicloDaFatura(cartao.DiaFechamento, vencimentoFatura);

                var itensCiclo = itensTodos
                    .Where(l => l.CartaoId == cartao.Id && l.DataCompra.Date >= inicio.Date && l.DataCompra.Date <= fim.Date)
                    .OrderBy(l => l.DataCompra)
                    .ToList();
                var parcelasDoMes = parcelasFinanciamento
                    .Where(l => l.CartaoOrigemId == cartao.Id && l.Vencimento.Year == anoC && l.Vencimento.Month == mesC)
                    .ToList();

                if (itensCiclo.Count == 0 && parcelasDoMes.Count == 0) continue;

                var faturaExistente = dados.Fatura(cartao.Id, anoC, mesC);
                var comprasResolvidas = ContasAPagarRegras.ComprasResolvidas(faturaExistente?.Status);
                var totalAntecipadoLinha = dados.Antecipado(faturaExistente);

                var totalComprasPendente = ContasAPagarRegras.ComprasPendentes(itensCiclo.Sum(i => i.Valor), totalAntecipadoLinha, comprasResolvidas);
                var financiamentoPendente = parcelasDoMes.Where(l => l.Status == "pendente").Sum(l => l.Valor);

                var totalRestanteLinha = totalComprasPendente + financiamentoPendente;
                if (totalRestanteLinha <= 0) continue;

                if (modo == "detalhado")
                {
                    if (!comprasResolvidas)
                        foreach (var item in itensCiclo)
                            linhasCartao.Add((vencimentoFatura, LinhaDeItem(item.Id, $"{cartao.Nome} — {item.Descricao}", item.CategoriaNome, item.Valor, vencimentoFatura, null, null, cartao)));

                    foreach (var parcela in parcelasDoMes.Where(l => l.Status == "pendente"))
                        linhasCartao.Add((vencimentoFatura, LinhaDeItem(parcela.Id, $"{cartao.Nome} — {parcela.Descricao}", null, parcela.Valor, vencimentoFatura, parcela.NumeroParcela, parcela.TotalParcelas, cartao)));
                }
                else
                {
                    var descricaoBase = totalComprasPendente > 0
                        ? $"Fatura {cartao.Nome}" + (totalAntecipadoLinha > 0 ? $" (já antecipado {totalAntecipadoLinha:C})" : "")
                        : $"Fatura {cartao.Nome} (parcela de financiamento)";

                    linhasCartao.Add((vencimentoFatura, new
                    {
                        id = cartao.Id,
                        descricao = descricaoBase,
                        categoriaNome = (string?)null,
                        valor = totalRestanteLinha,
                        vencimento = vencimentoFatura,
                        status = "pendente",
                        pagoEm = (DateTime?)null,
                        numeroParcela = (int?)null,
                        totalParcelas = (int?)null,
                        origem = "cartao_fatura",
                        cartaoId = cartao.Id,
                        cartaoNome = cartao.Nome,
                    }));
                }
            }

        var unificado = lancamentos.Concat(linhasCartao).OrderBy(x => x.Vencimento).Select(x => x.Linha).ToList();
        return Resultado.Ok(unificado);
    }

    private static object LinhaDeItem(Guid id, string descricao, string? categoriaNome, decimal valor, DateTime vencimento, int? numeroParcela, int? totalParcelas, CartaoAtivoDto cartao) => new
    {
        id,
        descricao,
        categoriaNome,
        valor,
        vencimento,
        status = "pendente",
        pagoEm = (DateTime?)null,
        numeroParcela,
        totalParcelas,
        origem = "cartao_item",
        cartaoId = cartao.Id,
        cartaoNome = cartao.Nome,
    };
}
