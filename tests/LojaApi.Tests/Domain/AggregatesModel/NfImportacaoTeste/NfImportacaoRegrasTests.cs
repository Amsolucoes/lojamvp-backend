using System.Xml.Linq;
using LojaApi.Domain.NfImportacao;

namespace LojaApi.Tests.Domain.AggregatesModel.NfImportacaoTeste;

public class NfImportacaoRegrasTests
{
    [Theory]
    [InlineData("Blusa Cloe Cor: Azul; Tamanho: M", "Blusa Cloe", "Azul", "M")]
    [InlineData("Blusa Cloe - Tamanho:G", "Blusa Cloe ", null, "G")]
    [InlineData("  Camisa lisa ", "Camisa lisa", null, null)]
    public void Extrai_cor_e_tamanho_do_nome(string xProd, string nome, string? cor, string? tamanho) =>
        Assert.Equal((nome, cor, tamanho), NfImportacaoRegras.ExtrairVariacao(xProd));

    [Theory]
    [InlineData("blusa de renda", "Blusa")]
    [InlineData("CALÇA jeans", "Calça")]
    public void Sugere_a_primeira_palavra_como_categoria(string nome, string esperado) =>
        Assert.Equal(esperado, NfImportacaoRegras.SugerirCategoria(nome));

    [Fact]
    public void Candidato_precisa_de_duas_palavras_em_comum_e_vence_o_primeiro_com_mais_palavras()
    {
        var produtos = new[] { "Blusa Renda", "Blusa Renda Manga Longa", "Blusa Renda Manga Longa Cloe", "Calça" };
        Assert.Equal("Blusa Renda Manga Longa Cloe", NfImportacaoRegras.MelhorCandidatoPorNome("Blusa de Renda Manga Longa Cloe", produtos, p => p));
        Assert.Null(NfImportacaoRegras.MelhorCandidatoPorNome("Blusa", produtos, p => p));
        Assert.Null(NfImportacaoRegras.MelhorCandidatoPorNome("de a", produtos, p => p));
    }

    [Fact]
    public void Variacao_ignora_maiuscula_e_trata_vazio_como_nulo() =>
        Assert.True(NfImportacaoRegras.MesmaVariacao("AZUL", null, "azul", ""));

    [Theory]
    [InlineData("fracionado", "g", "fracionado", "g")]
    [InlineData("fracionado", null, "fracionado", "kg")]
    [InlineData("qualquer", "g", "unidade", "un")]
    public void Tipo_de_venda_e_unidade(string tipo, string? unidade, string tipoFinal, string unidadeFinal)
    {
        var t = NfImportacaoRegras.TipoDeVenda(tipo);
        Assert.Equal((tipoFinal, unidadeFinal), (t, NfImportacaoRegras.UnidadeDeMedida(t, unidade)));
    }

    [Fact]
    public void Etiqueta_do_movimento_muda_com_a_origem()
    {
        Assert.Equal("Importação NF 7", NfImportacaoRegras.EtiquetaMovimento("xml", "7"));
        Assert.Equal("Nota fiscal manual 7", NfImportacaoRegras.EtiquetaMovimento("manual", "7"));
    }

    [Fact]
    public void Data_de_emissao_fica_so_a_data_em_utc()
    {
        var d = NfImportacaoRegras.DataEmissaoUtc(new DateTime(2031, 3, 15, 23, 30, 0));
        Assert.Equal(new DateTime(2031, 3, 15, 0, 0, 0, DateTimeKind.Utc), d);
        Assert.Equal(DateTimeKind.Utc, d!.Value.Kind);
        Assert.Null(NfImportacaoRegras.DataEmissaoUtc(null));
    }

    [Fact]
    public void Identifica_a_nota_e_recusa_xml_sem_nfe_ou_sem_chave()
    {
        const string ns = "http://www.portalfiscal.inf.br/nfe";
        var chave = new string('1', 44);
        Assert.Equal(chave, NfImportacaoRegras.Identificar(XDocument.Parse($"<nfe xmlns='{ns}'><infNFe Id='NFe{chave}'/></nfe>")).ChaveAcesso);
        Assert.Equal("XML não parece ser uma NF-e válida.", NfImportacaoRegras.Identificar(XDocument.Parse("<a/>")).Erro);
        Assert.Equal("Não foi possível identificar a chave de acesso da nota.", NfImportacaoRegras.Identificar(XDocument.Parse($"<nfe xmlns='{ns}'><infNFe Id='NFe123'/></nfe>")).Erro);
    }

    [Fact]
    public void Le_itens_da_nota_e_trata_sem_gtin_como_nulo()
    {
        const string ns = "http://www.portalfiscal.inf.br/nfe";
        var doc = XDocument.Parse($"<nfe xmlns='{ns}'><infNFe Id='NFe{new string('1', 44)}'><emit><CNPJ>111</CNPJ><xNome>Alfa</xNome></emit><ide><nNF>9</nNF></ide>" +
            "<det><prod><cProd>A</cProd><cEAN>SEM GTIN</cEAN><xProd>Tênis</xProd><qCom>2.5</qCom><vUnCom>10</vUnCom><vProd>25</vProd></prod></det></infNFe></nfe>");
        var (inf, chave, _) = NfImportacaoRegras.Identificar(doc);

        var nota = NfImportacaoRegras.LerNota(inf!, chave!);

        Assert.Equal(("111", "Alfa", "9"), (nota.CnpjFornecedor, nota.NomeFornecedor, nota.NumeroNf));
        Assert.Equal(new ItemNotaXml("A", null, "Tênis", 2.5m, 10m, 25m), Assert.Single(nota.Itens));
    }
}
