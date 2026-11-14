namespace LojaApi.Domain.Vendas;

// Regras puras de Vendas (sem banco): data da venda, nome do item com variação, comissão, exclusão no mês, troca, caixa e origens.
public static class VendasRegras
{
    public static readonly string[] OrigensPadrao = ["Loja física", "Site", "WhatsApp"];

    public static readonly string[] NomesDosMeses = ["Jan", "Fev", "Mar", "Abr", "Mai", "Jun", "Jul", "Ago", "Set", "Out", "Nov", "Dez"];

    public const string MovimentoEntrada = "entrada";
    public const string MovimentoSaida = "saida";

    // ── Venda ──

    // A data da venda pode ser passada, nunca futura. Usa a hora de agora no dia escolhido, para manter a ordem sensata dentro do dia.
    public static (DateTime? Data, string? Erro) DataDaVenda(DateTime? dataVenda, DateTime agoraUtc)
    {
        if (!dataVenda.HasValue) return (null, null);
        if (dataVenda.Value.Date > agoraUtc.Date) return (null, "A data da venda não pode ser no futuro.");

        return (DateTime.SpecifyKind(dataVenda.Value.Date, DateTimeKind.Utc).Add(agoraUtc.TimeOfDay), null);
    }

    // "Camisa" + tamanho M + cor Azul => "Camisa (M / Azul)"; sem tamanho nem cor, fica só o nome
    public static string NomeDoProdutoComVariacao(string nomeProduto, string? tamanho, string? cor)
    {
        var partes = new[] { tamanho, cor }.Where(s => !string.IsNullOrWhiteSpace(s));
        var label = string.Join(" / ", partes);
        return string.IsNullOrEmpty(label) ? nomeProduto : $"{nomeProduto} ({label})";
    }

    public static decimal ValorDaComissao(decimal baseComissao, decimal percentual) => Math.Round(baseComissao * (percentual / 100m), 2);

    // Só vendas do mês atual (e que não sejam do futuro) podem ser excluídas
    public static bool PodeExcluirNoMes(DateTime criadaEm, DateTime hojeUtc)
    {
        var inicioMes = new DateTime(hojeUtc.Year, hojeUtc.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        return !(criadaEm < inicioMes || criadaEm.Date > hojeUtc.Date);
    }

    // ── Troca ──

    // Positivo: o cliente paga a diferença. Negativo: sobra crédito para ele.
    public static decimal DiferencaDaTroca(decimal totalDevolvido, decimal totalNovo) => totalNovo - totalDevolvido;

    public static decimal CreditoDaTroca(decimal diferenca) => diferenca < 0 ? Math.Abs(diferenca) : 0;

    public static string MensagemDaTroca(decimal diferenca, decimal creditoGerado) => diferenca > 0
        ? $"Cliente deve pagar a diferença de {diferenca:C}"
        : diferenca < 0
            ? $"Crédito de {creditoGerado:C} gerado para o cliente"
            : "Troca realizada sem diferença";

    // ── Caixa ──

    public static bool TipoDeMovimentoValido(string tipo) => tipo == MovimentoEntrada || tipo == MovimentoSaida;

    // O movimento fica sempre ao meio-dia UTC do dia escolhido
    public static DateTime DataDoMovimentoUtc(DateTime data) => DateTime.SpecifyKind(data.Date, DateTimeKind.Utc).AddHours(12);

    // Texto do ajuste espelhado na conta bancária: "Reforço de caixa — Origem (observação)"
    public static string DescricaoDoAjuste(string tipo, string? origemNome, string? observacao) =>
        $"{(tipo == MovimentoEntrada ? "Reforço de caixa" : "Sangria de caixa")}" + (origemNome != null ? $" — {origemNome}" : "") + (observacao != null ? $" ({observacao})" : "");
}
