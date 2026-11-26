using System.Globalization;

namespace LojaApi.Domain.Financeiro;

// Regras puras de "pagar contas em lote" (sem banco): acréscimo (juros/multa), datas, divisão do pagamento em mais de uma conta
// e parcelas no cartão. O handler (Application) busca os dados, chama estas regras e grava.
public static class PagamentoEmLoteRegras
{
    public const string FormaConta = "conta";
    public const string FormaCartao = "cartao";
    public const string ModoAvulsa = "avulsa";
    public const string NomeCategoriaJuros = "Juros";
    public const int TamanhoMaximoDaDescricao = 150;

    // ── Valores ──────────────────────────────────────────────────────────────

    // Valor informado (acréscimo ou parte de divisão) em duas casas
    public static decimal ArredondarValor(decimal valor) => Math.Round(valor, 2);

    public static string? ValidarAcrescimo(decimal acrescimo) =>
        acrescimo < 0 ? "O juros/multa não pode ser negativo." : null;

    // A forma de pagamento (pix, boleto...) só vale quando se paga pela conta; no cartão a forma já é o crédito
    public static string? FormaDePagamentoGravada(string forma, string? formaNormalizada) =>
        forma == FormaConta ? formaNormalizada : null;

    // ── Datas ────────────────────────────────────────────────────────────────

    // Data usada em vencimentos e compras criados pelo lote: o dia escolhido (ou hoje) ao meio-dia
    public static DateTime DataBase(DateTime? data, DateTime agora) => LancamentosRegras.AoMeioDia(data ?? agora);

    // Quando o pagamento aconteceu: no dia escolhido, ao meio-dia; sem data escolhida, o instante exato
    public static DateTime PagoEm(DateTime? data, DateTime agora) => data.HasValue ? LancamentosRegras.AoMeioDia(data.Value) : agora;

    // ── Descrições ───────────────────────────────────────────────────────────

    public static string DescricaoDoAcrescimo(IReadOnlyList<string> descricoesDasContas) =>
        Cortar(descricoesDasContas.Count == 1
            ? $"Juros/multa - {descricoesDasContas[0]}"
            : $"Juros/multa - {descricoesDasContas.Count} contas");

    public static string DescricaoDaParte(string descricaoBase, int numero, int total) =>
        Cortar($"{descricaoBase} (parte {numero}/{total})");

    private static string Cortar(string texto) =>
        texto.Length <= TamanhoMaximoDaDescricao ? texto : texto[..TamanhoMaximoDaDescricao];

    // ── Divisão do pagamento em mais de uma conta bancária ───────────────────

    // Devolve a mensagem do primeiro erro; null quando não há divisão (qtdPartes = 0) ou quando ela é permitida
    public static string? ValidarDivisao(string forma, int qtdPartes, int qtdLancamentos, string? modoDoLancamento)
    {
        if (qtdPartes == 0) return null;
        if (forma != FormaConta) return "A divisão do pagamento só vale para pagar pela conta.";
        if (qtdPartes < 2) return "Para dividir, informe ao menos duas contas.";
        if (qtdLancamentos != 1 || modoDoLancamento != ModoAvulsa) return "Só é possível dividir o pagamento de uma conta avulsa por vez.";
        return null;
    }

    public static string? ValidarValorDaParte(decimal valor) =>
        valor <= 0 ? "O valor de cada parte deve ser maior que zero." : null;

    public static string? ValidarSomaDasPartes(IEnumerable<decimal> partes, decimal valorDaConta) =>
        partes.Sum() != valorDaConta
            ? $"A soma das partes deve ser igual ao valor da conta ({valorDaConta.ToString("0.00", CultureInfo.InvariantCulture)})."
            : null;

    // ── Pagamento no cartão de crédito ───────────────────────────────────────

    public static string? ValidarParcelasNoCartao(int parcelas) =>
        parcelas < 1 || parcelas > LancamentosRegras.MaxParcelasNoCartao
            ? $"Escolha entre 1 e {LancamentosRegras.MaxParcelasNoCartao} parcelas."
            : null;

    // Uma compra do cartão gerada a partir de uma conta paga no cartão
    public record ParcelaDoCartao(string Descricao, decimal Valor, DateTime DataCompra, bool Parcelada, int? NumeroParcela, int? TotalParcelas);

    // À vista: uma compra avulsa com a descrição original. Parcelado: "descrição (i/n)", uma por mês, e a última absorve os centavos
    public static IReadOnlyList<ParcelaDoCartao> ParcelasDoCartao(string descricao, decimal valor, int parcelas, DateTime dataBase)
    {
        if (parcelas == 1) return [new(descricao, valor, dataBase, false, null, null)];

        var valores = LancamentosRegras.ValoresDasParcelasDoCartao(valor, parcelas);
        var lista = new List<ParcelaDoCartao>(parcelas);
        for (int i = 0; i < parcelas; i++)
            lista.Add(new($"{descricao} ({i + 1}/{parcelas})", valores[i], dataBase.AddMonths(i), true, i + 1, parcelas));
        return lista;
    }
}
