using LojaApi.Domain.Vendas;

namespace LojaApi.Tests.Domain.AggregatesModel.VendasTeste;

// Regras puras de Vendas: data da venda, nome com variação, comissão, exclusão no mês, troca e caixa (sem banco, sem HTTP).
public class VendasRegrasTests
{
    private static readonly DateTime Agora = new(2026, 6, 20, 15, 30, 0, DateTimeKind.Utc);

    [Fact]
    public void Data_da_venda_vazia_usa_agora_e_futura_e_recusada()
    {
        Assert.Equal((null, null), VendasRegras.DataDaVenda(null, Agora));
        Assert.Equal("A data da venda não pode ser no futuro.", VendasRegras.DataDaVenda(Agora.AddDays(1), Agora).Erro);
    }

    [Fact]
    public void Data_passada_ou_de_hoje_mantem_o_dia_e_usa_a_hora_de_agora()
    {
        var (data, erro) = VendasRegras.DataDaVenda(new DateTime(2026, 6, 10, 0, 0, 0), Agora);

        Assert.Null(erro);
        Assert.Equal(new DateTime(2026, 6, 10, 15, 30, 0, DateTimeKind.Utc), data);
        Assert.Equal(DateTimeKind.Utc, data!.Value.Kind);
        Assert.Equal(Agora, VendasRegras.DataDaVenda(Agora.Date, Agora).Data);
    }

    [Theory]
    [InlineData("Camisa", "M", "Azul", "Camisa (M / Azul)")]
    [InlineData("Camisa", "M", null, "Camisa (M)")]
    [InlineData("Camisa", " ", "Azul", "Camisa (Azul)")]
    [InlineData("Camisa", null, null, "Camisa")]
    [InlineData("Camisa", "", "", "Camisa")]
    public void Nome_do_produto_com_variacao_junta_tamanho_e_cor(string nome, string? tamanho, string? cor, string esperado) =>
        Assert.Equal(esperado, VendasRegras.NomeDoProdutoComVariacao(nome, tamanho, cor));

    [Theory]
    [InlineData(100, 10, 10)]
    [InlineData(33.33, 10, 3.33)]       // arredonda em 2 casas
    [InlineData(10.05, 15, 1.51)]
    [InlineData(100, 0, 0)]
    public void Comissao_e_percentual_da_base_arredondado_em_duas_casas(decimal baseComissao, decimal percentual, decimal esperado) =>
        Assert.Equal(esperado, VendasRegras.ValorDaComissao(baseComissao, percentual));

    [Fact]
    public void So_vendas_do_mes_atual_e_que_nao_sejam_do_futuro_podem_ser_excluidas()
    {
        var hoje = new DateTime(2026, 6, 20, 0, 0, 0, DateTimeKind.Utc);

        Assert.True(VendasRegras.PodeExcluirNoMes(new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc), hoje));      // primeiro instante do mês
        Assert.True(VendasRegras.PodeExcluirNoMes(new DateTime(2026, 6, 20, 23, 0, 0, DateTimeKind.Utc), hoje));    // hoje
        Assert.False(VendasRegras.PodeExcluirNoMes(new DateTime(2026, 5, 31, 23, 59, 59, DateTimeKind.Utc), hoje)); // mês passado
        Assert.False(VendasRegras.PodeExcluirNoMes(new DateTime(2026, 6, 21, 0, 0, 0, DateTimeKind.Utc), hoje));   // amanhã
    }

    [Theory]
    [InlineData(100, 150, 50, 0, "Cliente deve pagar a diferença de ")]
    [InlineData(150, 100, -50, 50, "Crédito de ")]
    [InlineData(100, 100, 0, 0, "Troca realizada sem diferença")]
    public void Troca_calcula_diferenca_credito_e_mensagem(decimal devolvido, decimal novo, decimal diferenca, decimal credito, string inicioDaMensagem)
    {
        var d = VendasRegras.DiferencaDaTroca(devolvido, novo);

        Assert.Equal(diferenca, d);
        Assert.Equal(credito, VendasRegras.CreditoDaTroca(d));
        Assert.StartsWith(inicioDaMensagem, VendasRegras.MensagemDaTroca(d, credito));
    }

    [Fact]
    public void Movimento_de_caixa_so_aceita_entrada_ou_saida_e_fica_ao_meio_dia_utc()
    {
        Assert.True(VendasRegras.TipoDeMovimentoValido("entrada"));
        Assert.True(VendasRegras.TipoDeMovimentoValido("saida"));
        Assert.False(VendasRegras.TipoDeMovimentoValido("Entrada"));
        Assert.False(VendasRegras.TipoDeMovimentoValido(""));

        var data = VendasRegras.DataDoMovimentoUtc(new DateTime(2026, 6, 20, 23, 45, 0));
        Assert.Equal(new DateTime(2026, 6, 20, 12, 0, 0, DateTimeKind.Utc), data);
        Assert.Equal(DateTimeKind.Utc, data.Kind);
    }

    [Theory]
    [InlineData("entrada", null, null, "Reforço de caixa")]
    [InlineData("saida", null, null, "Sangria de caixa")]
    [InlineData("entrada", "Site", null, "Reforço de caixa — Site")]
    [InlineData("saida", null, "troco", "Sangria de caixa (troco)")]
    [InlineData("saida", "WhatsApp", "malote", "Sangria de caixa — WhatsApp (malote)")]
    public void Descricao_do_ajuste_espelhado_na_conta_bancaria(string tipo, string? origem, string? obs, string esperado) =>
        Assert.Equal(esperado, VendasRegras.DescricaoDoAjuste(tipo, origem, obs));

    [Fact]
    public void Origens_padrao_e_nomes_dos_meses()
    {
        Assert.Equal(["Loja física", "Site", "WhatsApp"], VendasRegras.OrigensPadrao);
        Assert.Equal(12, VendasRegras.NomesDosMeses.Length);
        Assert.Equal(("Jan", "Dez"), (VendasRegras.NomesDosMeses[0], VendasRegras.NomesDosMeses[11]));
    }
}
