namespace LojaApi.Domain.Servicos;

public record ItemMensalidade(string Nome, decimal Valor);

// Regras da mensalidade do plano de assinatura
public static class MensalidadeRegras
{
    public const string Pendente = "pendente";
    public const string Pago = "pago";

    // Em atraso: ainda pendente e o mês já passou — ou é o mês atual e o dia de vencimento já passou
    public static bool EmAtraso(string status, DateTime mesReferencia, DateTime mesAtual, int diaVencimento, DateTime agora) =>
        status == Pendente &&
        (mesReferencia < mesAtual || (mesReferencia == mesAtual && agora.Day > diaVencimento));

    // Valor lançado na venda: o realmente recebido (pode ter juros/multa ou desconto), senão o da mensalidade
    public static decimal Recebido(decimal valorDaMensalidade, decimal? valorPago) => valorPago ?? valorDaMensalidade;

    // Linhas da venda da mensalidade. Pago a mais vira "Juros/multa" separado; pago a menos fica no item da mensalidade.
    // O rótulo ganha o mês de referência quando o valor foi informado manualmente.
    public static List<ItemMensalidade> ItensDaVenda(string nomePlano, DateTime mesReferencia, decimal valorDaMensalidade, decimal? valorPago = null)
    {
        var recebido = Recebido(valorDaMensalidade, valorPago);
        var mes = $"{mesReferencia:MM/yyyy}";
        var rotulo = valorPago.HasValue ? $"Mensalidade - {nomePlano} ({mes})" : $"Mensalidade - {nomePlano}";

        var itens = new List<ItemMensalidade> { new(rotulo, recebido > valorDaMensalidade ? valorDaMensalidade : recebido) };
        if (recebido > valorDaMensalidade)
            itens.Add(new($"Juros/multa - {nomePlano} ({mes})", recebido - valorDaMensalidade));
        return itens;
    }
}
