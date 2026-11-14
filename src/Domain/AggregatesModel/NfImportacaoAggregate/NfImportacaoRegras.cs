using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace LojaApi.Domain.NfImportacao;

// Detalhe de cada item processado, guardado em JSON na nota (NfImportada.ItensJson) para permitir editar e desfazer.
// Os nomes das propriedades são o formato gravado no banco: não renomear.
public record ItemImportadoDetalhe(
    Guid ProdutoId, Guid? VariacaoId, decimal Quantidade,
    bool ProdutoCriado, bool VariacaoCriada, bool CategoriaCriada, Guid? CategoriaId
);

// Um item da NF-e como veio no XML
public record ItemNotaXml(string CodigoFornecedor, string? Gtin, string Descricao, decimal Quantidade, decimal ValorUnitario, decimal ValorTotal);

// A NF-e lida do XML
public record NotaFiscalXml(string CnpjFornecedor, string NomeFornecedor, string NumeroNf, string ChaveAcesso, List<ItemNotaXml> Itens);

// Regras puras da importação de nota fiscal (sem banco): leitura do XML, nome/variação do produto, categoria sugerida e etiquetas dos movimentos.
public static class NfImportacaoRegras
{
    private static readonly XNamespace Ns = "http://www.portalfiscal.inf.br/nfe";

    public const string OrigemXml = "xml";
    public const string OrigemManual = "manual";
    public const string AcaoNovo = "novo";

    // ── Leitura do XML da NF-e ──

    // Acha o nó da nota e a chave de acesso (Id="NFe<44 dígitos>"). Devolve o erro em texto quando o XML não serve.
    public static (XElement? InfNFe, string? ChaveAcesso, string? Erro) Identificar(XDocument doc)
    {
        var infNFe = doc.Descendants(Ns + "infNFe").FirstOrDefault();
        if (infNFe is null) return (null, null, "XML não parece ser uma NF-e válida.");

        var idAttr = infNFe.Attribute("Id")?.Value ?? "";
        var chave = idAttr.StartsWith("NFe") ? idAttr[3..] : idAttr;
        if (string.IsNullOrWhiteSpace(chave) || chave.Length != 44)
            return (null, null, "Não foi possível identificar a chave de acesso da nota.");

        return (infNFe, chave, null);
    }

    // Lê fornecedor, número e itens. Número fora do formato quebra (FormatException), como antes.
    public static NotaFiscalXml LerNota(XElement infNFe, string chaveAcesso)
    {
        var emit = infNFe.Element(Ns + "emit");
        var cnpj = emit?.Element(Ns + "CNPJ")?.Value ?? "";
        var nome = emit?.Element(Ns + "xNome")?.Value ?? "Fornecedor";
        var numero = infNFe.Element(Ns + "ide")?.Element(Ns + "nNF")?.Value ?? "";

        var itens = new List<ItemNotaXml>();
        foreach (var det in infNFe.Elements(Ns + "det"))
        {
            var prod = det.Element(Ns + "prod");
            if (prod is null) continue;

            var cEan = prod.Element(Ns + "cEAN")?.Value ?? "";
            var gtin = (string.IsNullOrWhiteSpace(cEan) || cEan == "SEM GTIN") ? null : cEan;
            itens.Add(new ItemNotaXml(
                prod.Element(Ns + "cProd")?.Value ?? "",
                gtin,
                prod.Element(Ns + "xProd")?.Value ?? "",
                Decimal(prod, "qCom"), Decimal(prod, "vUnCom"), Decimal(prod, "vProd")));
        }
        return new NotaFiscalXml(cnpj, nome, numero, chaveAcesso, itens);
    }

    private static decimal Decimal(XElement prod, string campo) =>
        decimal.Parse(prod.Element(Ns + campo)?.Value ?? "0", System.Globalization.CultureInfo.InvariantCulture);

    // ── Nome do produto ──

    // Extrai "Cor:X;Tamanho:Y" do nome do produto, se existir
    public static (string NomeBase, string? Cor, string? Tamanho) ExtrairVariacao(string xProd)
    {
        var match = Regex.Match(xProd, @"^(?<base>.*?)\s*(?<vars>(?:Cor|Tamanho)\s*:.*)$", RegexOptions.IgnoreCase);
        if (!match.Success) return (xProd.Trim(), null, null);

        var nomeBase = match.Groups["base"].Value.Trim().TrimEnd(':', ';', '-', ',');
        var varsPart = match.Groups["vars"].Value;

        string? cor = null, tamanho = null;
        foreach (var par in varsPart.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var kv = par.Split(':', 2);
            if (kv.Length != 2) continue;
            var chave = kv[0].Trim().ToLowerInvariant();
            var valor = kv[1].Trim();
            if (chave == "cor") cor = valor;
            else if (chave == "tamanho") tamanho = valor;
        }

        return (nomeBase, cor, tamanho);
    }

    // Primeira palavra do nome-base vira sugestão de categoria (ex: "Blusa de Renda..." -> "Blusa")
    public static string SugerirCategoria(string nomeBase)
    {
        var primeira = nomeBase.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "Outro";
        return char.ToUpper(primeira[0]) + primeira[1..].ToLowerInvariant();
    }

    // Similaridade simples por palavras em comum — suficiente pra sugestão, não pra match automático.
    // Precisa de 2 palavras (com mais de 2 letras no nome da nota) em comum; vence a primeira com mais palavras.
    public static T? MelhorCandidatoPorNome<T>(string nomeBase, IEnumerable<T> candidatos, Func<T, string> nomeDe) where T : class
    {
        var separadores = new[] { ' ', ':', ';', ',', '-' };
        var palavrasNf = nomeBase.ToLowerInvariant()
            .Split(separadores, StringSplitOptions.RemoveEmptyEntries)
            .Where(p => p.Length > 2)
            .ToHashSet();

        if (palavrasNf.Count == 0) return null;

        T? melhor = null;
        int melhorScore = 0;
        foreach (var c in candidatos)
        {
            var palavras = nomeDe(c).ToLowerInvariant().Split(separadores, StringSplitOptions.RemoveEmptyEntries).ToHashSet();
            var score = palavrasNf.Intersect(palavras).Count();
            if (score > melhorScore && score >= 2)
            {
                melhorScore = score;
                melhor = c;
            }
        }
        return melhor;
    }

    // Cor e tamanho do item batem (sem diferenciar maiúscula; vazio = vazio) com os da variação
    public static bool MesmaVariacao(string? corA, string? tamanhoA, string? corB, string? tamanhoB) =>
        string.Equals(corA ?? "", corB ?? "", StringComparison.OrdinalIgnoreCase)
        && string.Equals(tamanhoA ?? "", tamanhoB ?? "", StringComparison.OrdinalIgnoreCase);

    public static bool TemVariacao(string? cor, string? tamanho) => cor != null || tamanho != null;

    // ── Produto novo vindo da nota ──

    public static string NomeDaCategoria(string? categoriaNome) => string.IsNullOrWhiteSpace(categoriaNome) ? "Outro" : categoriaNome.Trim();

    public static string? GtinOuNulo(string? gtin) => string.IsNullOrWhiteSpace(gtin) ? null : gtin;

    public static string TipoDeVenda(string? tipoVenda) => tipoVenda == "fracionado" ? "fracionado" : "unidade";

    // Fracionado usa a unidade informada (ou kg); venda por unidade é sempre "un"
    public static string UnidadeDeMedida(string tipoVendaFinal, string? unidadeMedida) =>
        tipoVendaFinal == "fracionado" && !string.IsNullOrWhiteSpace(unidadeMedida) ? unidadeMedida : (tipoVendaFinal == "fracionado" ? "kg" : "un");

    // ── Datas e etiquetas ──

    // Postgres exige DateTimeKind.Utc pra gravar em timestamptz: o valor do JSON vem Unspecified. Fica só a data.
    public static DateTime? DataEmissaoUtc(DateTime? data) =>
        data.HasValue ? DateTime.SpecifyKind(data.Value.Date, DateTimeKind.Utc) : null;

    // Observação dos movimentos de estoque de cada origem — é por ela que a nota acha os próprios movimentos
    public static string EtiquetaMovimentoXml(string numeroNf) => $"Importação NF {numeroNf}";
    public static string EtiquetaMovimentoManual(string numeroNf) => $"Nota fiscal manual {numeroNf}";
    public static string EtiquetaMovimento(string origem, string numeroNf) =>
        origem == OrigemManual ? EtiquetaMovimentoManual(numeroNf) : EtiquetaMovimentoXml(numeroNf);

    public static string MensagemJaImportada(DateTime importadoEm, int qtdItens) =>
        $"Esta nota já foi importada em {importadoEm:dd/MM/yyyy HH:mm} ({qtdItens} item(ns)).";

    public static string MensagemNotaDuplicada(string numeroNf, string fornecedor) =>
        $"Já existe uma nota {numeroNf} lançada para o fornecedor {fornecedor}.";

    // Texto da variação no detalhe: "Cor / Tamanho" (só o que existir)
    public static string RotuloDaVariacao(string? cor, string? tamanho) =>
        string.Join(" / ", new[] { cor, tamanho }.Where(s => !string.IsNullOrWhiteSpace(s)));
}
