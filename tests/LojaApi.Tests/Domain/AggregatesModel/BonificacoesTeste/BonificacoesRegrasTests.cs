using LojaApi.Domain.Bonificacoes;

namespace LojaApi.Tests.Domain.AggregatesModel.BonificacoesTeste;

// Regras puras da bonificação (sem banco, sem HTTP).
public class BonificacoesRegrasTests
{
    [Theory]
    [InlineData(1, false, null)]
    [InlineData(0.5, false, null)]
    [InlineData(3, true, null)]
    [InlineData(0, false, "Informe uma quantidade maior que zero.")]
    [InlineData(-1, true, "Informe uma quantidade maior que zero.")]
    [InlineData(1.5, true, "Para produto com variação, a quantidade deve ser um número inteiro.")]
    public void Quantidade_precisa_ser_positiva_e_inteira_para_variacao(double quantidade, bool variacao, string? erro) =>
        Assert.Equal(erro, BonificacoesRegras.ValidarQuantidade((decimal)quantidade, variacao));

    [Fact]
    public void Custo_total_e_quantidade_vezes_custo_com_duas_casas()
    {
        Assert.Equal(7.5m, BonificacoesRegras.CustoTotal(3m, 2.5m));
        Assert.Equal(1.67m, BonificacoesRegras.CustoTotal(0.333m, 5m));
        Assert.Equal(0m, BonificacoesRegras.CustoTotal(2m, 0m));
        Assert.Equal(15m, new Bonificacao { Quantidade = 3m, CustoUnitario = 5m }.CustoTotal());
    }

    [Fact]
    public void Data_de_filtro_sem_fuso_vale_como_utc()
    {
        var utc = new DateTime(2025, 1, 10, 12, 0, 0, DateTimeKind.Utc);

        Assert.Equal(utc, BonificacoesRegras.EmUtc(utc));
        Assert.Equal(DateTimeKind.Utc, BonificacoesRegras.EmUtc(new DateTime(2025, 1, 10)).Kind);
        Assert.Equal(new DateTime(2025, 1, 10), BonificacoesRegras.EmUtc(new DateTime(2025, 1, 10)));
        Assert.Equal(DateTimeKind.Utc, BonificacoesRegras.EmUtc(new DateTime(2025, 1, 10, 12, 0, 0, DateTimeKind.Local)).Kind);
    }

    [Fact]
    public void Observacao_em_branco_vira_nula_e_e_aparada_e_limitada_a_300_caracteres()
    {
        Assert.Null(BonificacoesRegras.LimparObservacao(null));
        Assert.Null(BonificacoesRegras.LimparObservacao("   "));
        Assert.Equal("aniversário", BonificacoesRegras.LimparObservacao("  aniversário "));
        Assert.Equal(300, BonificacoesRegras.LimparObservacao(new string('a', 400))!.Length);
    }

    [Fact]
    public void Texto_do_movimento_leva_o_cliente_e_a_observacao_e_cabe_nos_300_caracteres()
    {
        Assert.Equal("Bonificação - Maria", BonificacoesRegras.ObservacaoDoMovimento("Maria", null));
        Assert.Equal("Bonificação - Maria", BonificacoesRegras.ObservacaoDoMovimento("Maria", "  "));
        Assert.Equal("Bonificação - Maria: aniversário", BonificacoesRegras.ObservacaoDoMovimento("Maria", " aniversário "));
        Assert.Equal(300, BonificacoesRegras.ObservacaoDoMovimento("Maria", new string('x', 500)).Length);
    }

    [Fact]
    public void Texto_do_movimento_na_venda_leva_o_codigo_curto_da_venda_e_o_produto()
    {
        var venda = Guid.Parse("1a2b3c4d-0000-0000-0000-000000000000");

        Assert.Equal("Bonificação na venda #1a2b3c4d - Pulseira", BonificacoesRegras.ObservacaoDoMovimentoNaVenda(venda, "Pulseira"));
        Assert.Equal(300, BonificacoesRegras.ObservacaoDoMovimentoNaVenda(venda, new string('p', 400)).Length);
    }

    [Fact]
    public void Texto_do_estorno_leva_cliente_e_produto()
    {
        Assert.Equal("Estorno da bonificação - Maria - Pulseira", BonificacoesRegras.ObservacaoDoEstorno("Maria", "Pulseira"));
        Assert.Equal(300, BonificacoesRegras.ObservacaoDoEstorno("Maria", new string('p', 400)).Length);
    }

    [Theory]
    [InlineData("Pulseira", null, null, "Pulseira")]
    [InlineData("Pulseira", "M", null, "Pulseira (M)")]
    [InlineData("Pulseira", null, "Azul", "Pulseira (Azul)")]
    [InlineData("Pulseira", "M", "Azul", "Pulseira (M / Azul)")]
    [InlineData("Pulseira", " ", "", "Pulseira")]
    public void Nome_do_produto_leva_a_variacao_como_nas_vendas(string produto, string? tamanho, string? cor, string esperado) =>
        Assert.Equal(esperado, BonificacoesRegras.NomeDoProduto(produto, tamanho, cor));

    [Fact]
    public void So_se_estorna_brinde_avulso_ainda_valido()
    {
        Assert.Null(BonificacoesRegras.MotivoParaNaoEstornar(false, null));
        Assert.Equal("Esta bonificação já foi estornada.", BonificacoesRegras.MotivoParaNaoEstornar(true, null));
        Assert.Equal("Esta bonificação já foi estornada.", BonificacoesRegras.MotivoParaNaoEstornar(true, Guid.NewGuid()));
        Assert.Equal("Esta bonificação foi dada dentro de uma venda. Para desfazer, exclua a venda.", BonificacoesRegras.MotivoParaNaoEstornar(false, Guid.NewGuid()));
        Assert.True(new Bonificacao { EstornadaEm = DateTime.UtcNow }.Estornada());
        Assert.False(new Bonificacao().Estornada());
    }
}
