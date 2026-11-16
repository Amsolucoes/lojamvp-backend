using System.Globalization;
using System.Text;

namespace LojaApi.Domain.Financeiro;

// Regras puras do Financeiro (sem banco): nome de categoria, categorias padrão, ajuste de saldo e texto das transferências.
public static class FinanceiroRegras
{
    public const string AjusteTipoAjuste = "ajuste";

    // Nome sem maiúscula, acento e espaços nas pontas — duas categorias com o mesmo nome assim são a mesma
    public static string NormalizarNomeCategoria(string? nome)
    {
        var decomposto = (nome ?? "").Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        return new string(decomposto.Where(ch => CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark).ToArray());
    }

    // Duas categorias são a mesma se têm o mesmo nome normalizado e tipos compatíveis ("ambos" casa com qualquer um)
    public static bool CategoriaEquivalente(string nomeExistente, string tipoExistente, string nomeNovoNormalizado, string tipoNovo) =>
        NormalizarNomeCategoria(nomeExistente) == nomeNovoNormalizado && (tipoExistente == tipoNovo || tipoExistente == "ambos" || tipoNovo == "ambos");

    public static readonly (string Nome, string Tipo, string Icone)[] CategoriasPadrao =
    [
        ("Aluguel", "pagar", "🏠"),
        ("Água", "pagar", "💧"),
        ("Luz", "pagar", "💡"),
        ("Internet", "pagar", "📶"),
        ("Fornecedor", "pagar", "📦"),
        ("Salário/Folha", "pagar", "👤"),
        ("Impostos", "pagar", "🧾"),
        ("Mensalidade/Assinatura", "receber", "💳"),
        ("Venda avulsa", "receber", "🛒"),
        ("Outros", "ambos", "📁"),
    ];

    // O que fica registrado no ajuste de saldo: no tipo "ajuste" é a diferença até o novo saldo; nos demais, o valor informado (ou zero)
    public static decimal ValorDoAjuste(string tipo, decimal novoSaldo, decimal? valor, decimal saldoAtual) =>
        tipo == AjusteTipoAjuste ? novoSaldo - saldoAtual : valor ?? 0;

    // Texto do histórico das transferências entre contas: "Transferência para X — obs"
    public static string ObservacaoDaTransferencia(string preposicao, string nomeDaOutraConta, string? observacao) =>
        $"Transferência {preposicao} {nomeDaOutraConta}" + (string.IsNullOrWhiteSpace(observacao) ? "" : $" — {observacao}");
}
