namespace LojaApi.Domain.Financeiro;

// Regras puras dos relatórios do Financeiro (resumo mensal e anual, balanço, comparativo, alertas e limite dos cartões): sem banco, sem HTTP.
public static class RelatoriosRegras
{
    public const string SemCategoria = "Sem categoria";
    public const string IconeDaCategoria = "📁";
    public const string CategoriaCartao = "Cartão de Crédito";
    public const string CategoriaMensalidade = "Mensalidade/Assinatura";
    public const string CategoriaFinanciamento = "Financiamento de fatura";

    // Parcela de financiamento de fatura (lançamento ligado ao cartão) que vence no mês
    public readonly record struct ParcelaDoMes(string Status, DateTime Vencimento, decimal Valor);

    // O que o cartão pesa no mês, por situação (regime de caixa)
    public readonly record struct Contribuicao(decimal Pago, decimal Pendente, decimal Vencido)
    {
        public decimal Total => Pago + Pendente + Vencido;
    }

    // Dados de uma fatura já registrada (quando existe)
    public readonly record struct FaturaRegistrada(string Status, decimal Total, decimal Antecipado);

    // Fonte única de "quanto de cartão conta nesse mês", usada pelo resumo mensal, pelo balanço e pelo resumo anual (os três nunca saem de sincronia).
    // Conta o que sobrou da fatura do ciclo que vence no mês (se ainda não foi resolvida por pagamento, parcial ou financiamento)
    // e as parcelas de financiamento de faturas antigas que vencem no mês, pagas ou pendentes.
    // somaDeCompras = total das compras do cartão entre as datas do ciclo (inclusive); status/antecipado = da fatura do mês, se existir.
    public static Contribuicao ContribuicaoDoCartaoNoMes(int diaFechamento, int diaVencimento, int ano, int mes, DateTime hoje,
        Func<DateTime, DateTime, decimal> somaDeCompras, FaturaRegistrada? fatura, IEnumerable<ParcelaDoMes> parcelasDoMes)
    {
        decimal pago = 0, pendente = 0, vencido = 0;

        var vencimentoFatura = CartaoRegras.VencimentoDaFatura(diaVencimento, ano, mes);
        var (inicio, fim) = CartaoRegras.CicloDaFatura(diaFechamento, vencimentoFatura);
        var totalFatura = somaDeCompras(inicio, fim);

        if (totalFatura > 0)
        {
            var totalRestante = totalFatura - (fatura?.Antecipado ?? 0);
            var resolvida = ContasAPagarRegras.ComprasResolvidas(fatura?.Status);

            if (resolvida) pago += totalRestante;
            else if (totalRestante <= 0) pago += totalFatura;
            else if (vencimentoFatura.Date < hoje) vencido += totalRestante;
            else pendente += totalRestante;
        }

        foreach (var p in parcelasDoMes)
        {
            if (p.Status == "pago") pago += p.Valor;
            else if (p.Vencimento.Date < hoje) vencido += p.Valor;
            else pendente += p.Valor;
        }

        return new Contribuicao(pago, pendente, vencido);
    }

    // Fatura "a pagar agora" em memória: a mais antiga já fechada e não paga; sem nenhuma, o ciclo em aberto (só informativo)
    public static (DateTime Vencimento, decimal Total, string Status) CicloAtualDoCartao(int diaFechamento, int diaVencimento, DateTime hoje,
        Func<DateTime, DateTime, decimal> somaDeCompras, Func<int, int, string?> statusDaFatura)
    {
        for (int offset = -3; offset <= 0; offset++)
        {
            var referencia = new DateTime(hoje.Year, hoje.Month, 1).AddMonths(offset);
            var vencimento = CartaoRegras.VencimentoDaFatura(diaVencimento, referencia.Year, referencia.Month);
            var ciclo = CartaoRegras.CicloDaFatura(diaFechamento, vencimento);

            if (!(hoje.Date > ciclo.Fim.Date)) continue;

            var total = somaDeCompras(ciclo.Inicio, ciclo.Fim);
            if (total <= 0) continue;

            var status = statusDaFatura(vencimento.Year, vencimento.Month);
            if (status == "pago") continue;

            return (vencimento, total, status ?? "pendente");
        }

        var (vencimentoAberto, cicloAberto) = CartaoRegras.CicloEmAberto(diaFechamento, diaVencimento, hoje);
        var totalAberto = somaDeCompras(cicloAberto.Inicio, cicloAberto.Fim);
        return (vencimentoAberto, totalAberto, statusDaFatura(vencimentoAberto.Year, vencimentoAberto.Month) ?? "pendente");
    }

    // Limite usado de um cartão: faturas dos últimos 12 meses ainda não quitadas + compras futuras + parcelas de financiamento pendentes.
    // faturaDe(ano, mes) devolve a fatura registrada do mês, se houver.
    public static decimal LimiteUsado(int diaFechamento, int diaVencimento, DateTime hoje, Func<DateTime, DateTime, decimal> somaDeCompras,
        Func<int, int, FaturaRegistrada?> faturaDe, IReadOnlyList<(string Modo, DateTime DataCompra, decimal Valor)> compras, decimal financiamentoPendente)
    {
        var (vencimentoAberto, cicloAberto) = CartaoRegras.CicloEmAberto(diaFechamento, diaVencimento, hoje);

        decimal usado = 0;
        var cursor = new DateTime(vencimentoAberto.Year, vencimentoAberto.Month, 1).AddMonths(-11);
        for (int i = 0; i < 12; i++)
        {
            var vencimentoCiclo = CartaoRegras.VencimentoDaFatura(diaVencimento, cursor.Year, cursor.Month);
            var (cInicio, cFim) = CartaoRegras.CicloDaFatura(diaFechamento, vencimentoCiclo);
            var totalCiclo = somaDeCompras(cInicio, cFim);

            if (totalCiclo > 0)
            {
                var fatura = faturaDe(cursor.Year, cursor.Month);

                // "financiada": virou pagamento parcelado à parte; "parcial": o restante já foi lançado como compra real no próximo ciclo
                // (com juros). Nos dois casos o ciclo não conta de novo, senão a mesma dívida seria somada duas vezes.
                if (!ContasAPagarRegras.ComprasResolvidas(fatura?.Status))
                    usado += totalCiclo - (fatura?.Antecipado ?? 0);
                else if (fatura is { Total: > 0 } registrada && totalCiclo > registrada.Total)
                    // Fatura já quitada/financiada: o que foi comprado nesse ciclo DEPOIS do pagamento ainda consome o limite
                    usado += totalCiclo - registrada.Total;
            }

            cursor = cursor.AddMonths(1);
        }

        // Compras futuras (parceladas ou avulsas de um ciclo além do aberto) ainda não somadas acima. Cobrança fixa futura NÃO entra:
        // só conta quando já está dentro de uma fatura gerada (fechada ou aberta).
        foreach (var compra in compras.Where(c => c.Modo != "fixa" && c.DataCompra.Date > cicloAberto.Fim.Date))
        {
            var vencimento = CartaoRegras.VencimentoDaFaturaDaCompra(diaFechamento, diaVencimento, compra.DataCompra);
            var status = faturaDe(vencimento.Year, vencimento.Month)?.Status;
            if (status != "pago" && status != "financiada") usado += compra.Valor;
        }

        // Parcelas de financiamento de faturas antigas (ficam em lançamentos financeiros, não nas compras do cartão): tudo que ainda não foi pago
        return usado + financiamentoPendente;
    }

    // Último mês do ano já "acontecido" (o resto do ano ainda é previsão): 12 em anos passados, o mês atual no ano corrente, 0 em anos futuros
    public static int UltimoMesAcontecido(int ano, DateTime agora) => ano < agora.Year ? 12 : ano == agora.Year ? agora.Month : 0;

    // Mês com o menor gasto (maior que zero) entre os meses já acontecidos
    public static (int? Mes, decimal? Valor) MelhorMes(IReadOnlyList<decimal> serie, int ultimoMesAcontecido)
    {
        int? melhorMes = null;
        decimal? melhorValor = null;
        for (int m = 1; m <= ultimoMesAcontecido; m++)
        {
            var v = serie[m - 1];
            if (v > 0 && (melhorValor is null || v < melhorValor))
            {
                melhorValor = v;
                melhorMes = m;
            }
        }
        return (melhorMes, melhorValor);
    }

    // Variação em % em relação ao mês anterior (sem valor quando o anterior foi zero)
    public static decimal? VariacaoPercentual(decimal atual, decimal anterior) => anterior > 0 ? (atual - anterior) / anterior * 100m : null;

    public static (int Ano, int Mes) MesAnterior(int ano, int mes) => mes == 1 ? (ano - 1, 12) : (ano, mes - 1);
}
