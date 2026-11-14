namespace LojaApi.Domain.Produtos;

// Endereço do fornecedor já limpo: os campos detalhados e o texto completo (compatível com dados e telas antigas)
public record EnderecoDoFornecedor(string? Cep, string? Rua, string? Numero, string? Bairro, string? Cidade, string? Uf, string? Texto);

// Regras puras de Produtos (sem banco): códigos de barras, estoque exibido, movimentos, endereço do fornecedor e estilo das etiquetas.
public static class ProdutosRegras
{
    public const string MovimentoEntrada = "entrada";
    public const string MovimentoSaida = "saida";
    public const string MovimentoAjuste = "ajuste";

    // ── Códigos de barras internos ──

    // Quantos códigos gerar por vez: de 1 a 200
    public static int QuantidadeDeCodigos(int quantidade) => Math.Clamp(quantidade, 1, 200);

    // Códigos já usados na tela (ainda não salvos), vindos separados por vírgula
    public static HashSet<string> CodigosIgnorados(string? ignorar) =>
        new((ignorar ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

    // EAN-13 de uso interno: prefixo 2 (faixa reservada pela GS1 para uso restrito à loja) + 11 dígitos + dígito verificador.
    // Não são válidos para NF-e/marketplaces, mas funcionam em leitores e etiquetas.
    public static string CodigoInterno(long aleatorio)
    {
        var corpo = "2" + aleatorio.ToString("D11");
        return corpo + DigitoVerificadorEan13(corpo);
    }

    public static int DigitoVerificadorEan13(string doze)
    {
        var soma = 0;
        for (var i = 0; i < 12; i++)
            soma += (doze[i] - '0') * (i % 2 == 0 ? 1 : 3);
        return (10 - soma % 10) % 10;
    }

    // ── Estoque e variações ──

    // Produto com variações mostra a soma das variações ativas; sem nenhuma com saldo, vale o estoque do próprio produto
    public static decimal EstoqueExibido(decimal estoqueDoProduto, IEnumerable<int> estoqueDasVariacoesAtivas)
    {
        var soma = estoqueDasVariacoesAtivas.Sum();
        return soma > 0 ? soma : estoqueDoProduto;
    }

    // Código em branco vira nulo
    public static string? CodigoDaVariacao(string? codigo) => string.IsNullOrWhiteSpace(codigo) ? null : codigo;

    // "M" + "Azul" => "M / Azul"
    public static string RotuloDaVariacao(string? tamanho, string? cor) =>
        string.Join(" / ", new[] { tamanho, cor }.Where(s => !string.IsNullOrWhiteSpace(s)));

    public static string ObservacaoDaEdicaoDeVariacao(string rotulo) => "Editado via cadastro do produto" + (rotulo != "" ? $" ({rotulo})" : "");

    // ── Movimentos de estoque ──

    // Entrada soma, saída subtrai e ajuste define o total. Entrada e saída precisam de quantidade maior que zero.
    public static string? ValidarMovimento(string tipo, decimal quantidade)
    {
        if (tipo != MovimentoEntrada && tipo != MovimentoSaida && tipo != MovimentoAjuste) return "Tipo de movimento inválido.";
        if (quantidade < 0) return "A quantidade não pode ser negativa.";
        if (tipo != MovimentoAjuste && quantidade == 0) return "Informe uma quantidade maior que zero.";
        return null;
    }

    public static decimal NovoEstoque(string tipo, decimal atual, decimal quantidade) => tipo switch
    {
        MovimentoEntrada => atual + quantidade,
        MovimentoSaida => atual - quantidade,
        _ => quantidade,        // ajuste: define o estoque total
    };

    public static int NovoEstoque(string tipo, int atual, int quantidade) => tipo switch
    {
        MovimentoEntrada => atual + quantidade,
        MovimentoSaida => atual - quantidade,
        _ => quantidade,
    };

    // "Entrada - P / Azul" (ou a observação digitada no lugar da palavra)
    public static string ObservacaoDoAjusteDeVariacao(string tipo, string? observacao, string rotulo)
    {
        var texto = string.IsNullOrWhiteSpace(observacao)
            ? (tipo == MovimentoEntrada ? "Entrada" : tipo == MovimentoSaida ? "Saída" : "Ajuste")
            : observacao.Trim();
        return $"{texto} - {rotulo}";
    }

    // ── Categorias ──

    public static bool MudouDeNome(string nomeAntigo, string nomeNovo) => !nomeAntigo.Equals(nomeNovo, StringComparison.OrdinalIgnoreCase);

    // ── Fornecedor ──

    public static string? Limpar(string? valor, int max)
    {
        var t = valor?.Trim();
        if (string.IsNullOrEmpty(t)) return null;
        return t.Length > max ? t[..max] : t;
    }

    // Com algum detalhe (rua, número, bairro, cidade ou UF), o texto completo é montado a partir deles; sem nenhum, vale o endereço digitado
    public static EnderecoDoFornecedor MontarEndereco(string? cep, string? rua, string? numero, string? bairro, string? cidade, string? uf, string? enderecoInformado)
    {
        var c = Limpar(cep, 9);
        var r = Limpar(rua, 150);
        var n = Limpar(numero, 20);
        var b = Limpar(bairro, 100);
        var ci = Limpar(cidade, 100);
        var u = Limpar(uf, 2)?.ToUpperInvariant();

        var temDetalhe = r != null || n != null || b != null || ci != null || u != null;
        if (!temDetalhe) return new EnderecoDoFornecedor(c, r, n, b, ci, u, Limpar(enderecoInformado, 300));

        var ruaNumero = string.Join(", ", new[] { r, n }.Where(x => x != null));
        var cidadeUf = string.Join("/", new[] { ci, u }.Where(x => x != null));
        var partes = new[] { ruaNumero, b, cidadeUf, c }.Where(x => !string.IsNullOrEmpty(x));
        return new EnderecoDoFornecedor(c, r, n, b, ci, u, Limpar(string.Join(" - ", partes), 300));
    }

    // ── Modelos de etiqueta ──

    public static bool TamanhoDeEtiquetaValido(decimal larguraMm, decimal alturaMm) => larguraMm > 0 && alturaMm > 0;

    public static string CorDoTexto(string? cor) => string.IsNullOrWhiteSpace(cor) ? "#000000" : cor.Trim();
    public static string CorDeFundo(string? cor) => string.IsNullOrWhiteSpace(cor) ? "#FFFFFF" : cor.Trim();
    public static string FonteDaEtiqueta(string? fonte) => string.IsNullOrWhiteSpace(fonte) ? "Arial, sans-serif" : fonte.Trim();
    public static string? LogoDaEtiqueta(string? url) => string.IsNullOrWhiteSpace(url) ? null : url.Trim();

    // A escala da fonte vai de 50% a 300%; fora disso (ou sem valor) fica em 100%
    public static int EscalaDaFonte(int? escala) => escala is >= 50 and <= 300 ? escala.Value : 100;
}
