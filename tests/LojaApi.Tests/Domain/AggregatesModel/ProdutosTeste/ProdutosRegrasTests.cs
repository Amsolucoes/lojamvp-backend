using LojaApi.Domain.Produtos;

namespace LojaApi.Tests.Domain.AggregatesModel.ProdutosTeste;

// Regras puras de Produtos: códigos de barras, estoque exibido, movimentos, endereço do fornecedor e estilo das etiquetas (sem banco, sem HTTP).
public class ProdutosRegrasTests
{
    [Theory]
    [InlineData(-5, 1)]
    [InlineData(0, 1)]
    [InlineData(1, 1)]
    [InlineData(50, 50)]
    [InlineData(200, 200)]
    [InlineData(5000, 200)]
    public void Quantidade_de_codigos_fica_entre_1_e_200(int pedida, int esperada) =>
        Assert.Equal(esperada, ProdutosRegras.QuantidadeDeCodigos(pedida));

    [Fact]
    public void Codigos_ignorados_vem_separados_por_virgula_sem_espacos_nem_vazios()
    {
        var ignorados = ProdutosRegras.CodigosIgnorados(" 111 , 222,, 333 ,");

        Assert.Equal(["111", "222", "333"], ignorados.OrderBy(x => x));
        Assert.Empty(ProdutosRegras.CodigosIgnorados(null));
        Assert.Empty(ProdutosRegras.CodigosIgnorados(""));
    }

    [Theory]
    [InlineData("400638133393", 1)]       // 4006381333931: exemplo clássico da GS1
    [InlineData("590123412345", 7)]       // 5901234123457
    [InlineData("978020137962", 4)]       // 9780201379624 (ISBN)
    [InlineData("000000000000", 0)]
    public void Digito_verificador_ean13_segue_o_calculo_da_gs1(string doze, int esperado) =>
        Assert.Equal(esperado, ProdutosRegras.DigitoVerificadorEan13(doze));

    [Theory]
    [InlineData(0L)]
    [InlineData(1L)]
    [InlineData(99_999_999_999L)]
    [InlineData(12_345_678_901L)]
    public void Codigo_interno_tem_13_digitos_comeca_com_2_e_dv_valido(long aleatorio)
    {
        var c = ProdutosRegras.CodigoInterno(aleatorio);

        Assert.Equal(13, c.Length);
        Assert.StartsWith("2", c);
        Assert.All(c, ch => Assert.True(char.IsDigit(ch)));
        Assert.Equal(c[12] - '0', ProdutosRegras.DigitoVerificadorEan13(c[..12]));
        Assert.Equal(aleatorio.ToString("D11"), c[1..12]);
    }

    [Fact]
    public void Estoque_exibido_soma_as_variacoes_ativas_ou_usa_o_do_produto()
    {
        Assert.Equal(5m, ProdutosRegras.EstoqueExibido(9m, [2, 3]));
        Assert.Equal(9m, ProdutosRegras.EstoqueExibido(9m, []));
        Assert.Equal(9m, ProdutosRegras.EstoqueExibido(9m, [0, 0]));      // sem saldo nas variações: vale o do produto
    }

    [Theory]
    [InlineData("555", "555")]
    [InlineData("", null)]
    [InlineData("   ", null)]
    [InlineData(null, null)]
    public void Codigo_da_variacao_em_branco_vira_nulo(string? codigo, string? esperado) =>
        Assert.Equal(esperado, ProdutosRegras.CodigoDaVariacao(codigo));

    [Theory]
    [InlineData("M", "Azul", "M / Azul")]
    [InlineData("M", null, "M")]
    [InlineData(" ", "Azul", "Azul")]
    [InlineData(null, null, "")]
    public void Rotulo_da_variacao_junta_tamanho_e_cor(string? tamanho, string? cor, string esperado) =>
        Assert.Equal(esperado, ProdutosRegras.RotuloDaVariacao(tamanho, cor));

    [Fact]
    public void Observacao_da_edicao_de_variacao_cita_a_grade_so_quando_existe()
    {
        Assert.Equal("Editado via cadastro do produto (M / Azul)", ProdutosRegras.ObservacaoDaEdicaoDeVariacao("M / Azul"));
        Assert.Equal("Editado via cadastro do produto", ProdutosRegras.ObservacaoDaEdicaoDeVariacao(""));
    }

    [Theory]
    [InlineData("entrada", 1, null)]
    [InlineData("saida", 1, null)]
    [InlineData("ajuste", 0, null)]
    [InlineData("ajuste", 5, null)]
    [InlineData("troca", 1, "Tipo de movimento inválido.")]
    [InlineData("Entrada", 1, "Tipo de movimento inválido.")]
    [InlineData("ajuste", -1, "A quantidade não pode ser negativa.")]
    [InlineData("entrada", -1, "A quantidade não pode ser negativa.")]
    [InlineData("entrada", 0, "Informe uma quantidade maior que zero.")]
    [InlineData("saida", 0, "Informe uma quantidade maior que zero.")]
    public void Movimento_valida_tipo_e_quantidade(string tipo, decimal quantidade, string? erro) =>
        Assert.Equal(erro, ProdutosRegras.ValidarMovimento(tipo, quantidade));

    [Fact]
    public void Novo_estoque_soma_subtrai_ou_define()
    {
        Assert.Equal(15m, ProdutosRegras.NovoEstoque("entrada", 10m, 5m));
        Assert.Equal(6m, ProdutosRegras.NovoEstoque("saida", 10m, 4m));
        Assert.Equal(3m, ProdutosRegras.NovoEstoque("ajuste", 10m, 3m));
        Assert.Equal(15, ProdutosRegras.NovoEstoque("entrada", 10, 5));
        Assert.Equal(6, ProdutosRegras.NovoEstoque("saida", 10, 4));
        Assert.Equal(0, ProdutosRegras.NovoEstoque("ajuste", 10, 0));
    }

    [Theory]
    [InlineData("entrada", null, "P / Azul", "Entrada - P / Azul")]
    [InlineData("saida", "  ", "P / Azul", "Saída - P / Azul")]
    [InlineData("ajuste", "", "P / Azul", "Ajuste - P / Azul")]
    [InlineData("saida", "  quebra ", "P / Azul", "quebra - P / Azul")]
    public void Observacao_do_ajuste_de_variacao_usa_a_palavra_do_tipo_ou_o_texto_digitado(string tipo, string? obs, string rotulo, string esperado) =>
        Assert.Equal(esperado, ProdutosRegras.ObservacaoDoAjusteDeVariacao(tipo, obs, rotulo));

    [Theory]
    [InlineData("Roupas", "ROUPAS", false)]
    [InlineData("Roupas", "roupas", false)]
    [InlineData("Roupas", "Vestuário", true)]
    public void Categoria_so_muda_de_nome_se_nao_for_so_a_caixa(string antigo, string novo, bool mudou) =>
        Assert.Equal(mudou, ProdutosRegras.MudouDeNome(antigo, novo));

    [Theory]
    [InlineData("  abc  ", 10, "abc")]
    [InlineData("abcdef", 3, "abc")]
    [InlineData("", 5, null)]
    [InlineData("   ", 5, null)]
    [InlineData(null, 5, null)]
    public void Limpar_apara_corta_no_maximo_e_vazio_vira_nulo(string? valor, int max, string? esperado) =>
        Assert.Equal(esperado, ProdutosRegras.Limpar(valor, max));

    [Fact]
    public void Endereco_detalhado_monta_o_texto_completo_e_poe_a_uf_em_maiusculas()
    {
        var e = ProdutosRegras.MontarEndereco("79000-000", " Rua A ", "10", "Centro", "Campo Grande", "ms", "ignorado");

        Assert.Equal("Rua A, 10 - Centro - Campo Grande/MS - 79000-000", e.Texto);
        Assert.Equal(("Rua A", "10", "Centro", "Campo Grande", "MS", "79000-000"), (e.Rua, e.Numero, e.Bairro, e.Cidade, e.Uf, e.Cep));
    }

    [Fact]
    public void Endereco_parcial_so_junta_o_que_existe()
    {
        Assert.Equal("Dourados/MS", ProdutosRegras.MontarEndereco(null, null, null, null, "Dourados", "ms", null).Texto);
        Assert.Equal("Rua B - 79000-000", ProdutosRegras.MontarEndereco("79000-000", "Rua B", null, null, null, null, null).Texto);
    }

    [Fact]
    public void Sem_nenhum_detalhe_vale_o_endereco_digitado_mesmo_com_cep()
    {
        var e = ProdutosRegras.MontarEndereco("79000-000", null, null, null, null, null, "  Av. Brasil, 100 ");

        Assert.Equal("Av. Brasil, 100", e.Texto);
        Assert.Equal("79000-000", e.Cep);                                     // o CEP sozinho não conta como detalhe
        Assert.Null(ProdutosRegras.MontarEndereco(null, null, null, null, null, null, "  ").Texto);
    }

    [Theory]
    [InlineData(40, 30, true)]
    [InlineData(0, 30, false)]
    [InlineData(40, 0, false)]
    [InlineData(-1, 30, false)]
    public void Etiqueta_precisa_de_largura_e_altura_positivas(decimal largura, decimal altura, bool valido) =>
        Assert.Equal(valido, ProdutosRegras.TamanhoDeEtiquetaValido(largura, altura));

    [Fact]
    public void Estilo_da_etiqueta_aplica_os_padroes_quando_vem_em_branco()
    {
        Assert.Equal(("#000000", "#FFFFFF", "Arial, sans-serif", null), (ProdutosRegras.CorDoTexto(" "), ProdutosRegras.CorDeFundo(null), ProdutosRegras.FonteDaEtiqueta(""), ProdutosRegras.LogoDaEtiqueta("  ")));
        Assert.Equal(("#112233", "#FFEEDD", "Roboto", "http://x/l.png"), (ProdutosRegras.CorDoTexto(" #112233 "), ProdutosRegras.CorDeFundo("#FFEEDD"), ProdutosRegras.FonteDaEtiqueta(" Roboto "), ProdutosRegras.LogoDaEtiqueta(" http://x/l.png ")));
    }

    [Theory]
    [InlineData(null, 100)]
    [InlineData(49, 100)]
    [InlineData(50, 50)]
    [InlineData(150, 150)]
    [InlineData(300, 300)]
    [InlineData(301, 100)]
    public void Escala_da_fonte_vai_de_50_a_300_e_fora_disso_volta_a_100(int? escala, int esperada) =>
        Assert.Equal(esperada, ProdutosRegras.EscalaDaFonte(escala));
}
