using LojaApi.Domain.Financeiro;

namespace LojaApi.Tests.Domain.AggregatesModel.FinanceiroTeste;

public class FaturaRegrasTests
{
    [Theory]
    [InlineData(1, "Jan")]
    [InlineData(2, "Fev")]
    [InlineData(5, "Mai")]
    [InlineData(9, "Set")]
    [InlineData(12, "Dez")]
    public void Abreviacao_do_mes_em_portugues(int mes, string esperado) => Assert.Equal(esperado, FaturaRegras.AbreviacaoDoMes(mes));

    [Fact]
    public void Parcela_absorvida_ganha_o_sufixo_com_motivo_e_data_e_troca_o_anterior()
    {
        var data = new DateTime(2032, 1, 12, 12, 0, 0, DateTimeKind.Utc);

        var primeira = FaturaRegras.DescricaoDaParcelaAbsorvida("Financiamento", data, "pagamento parcial");
        var segunda = FaturaRegras.DescricaoDaParcelaAbsorvida(primeira, new DateTime(2032, 2, 20), "pagamento parcial");

        Assert.Equal("Financiamento (absorvida no pagamento parcial de 12/01/2032)", primeira);
        Assert.Equal("Financiamento (absorvida no pagamento parcial de 20/02/2032)", segunda);
    }

    [Fact]
    public void Parcela_refinanciada_troca_so_o_sufixo_de_refinanciamento()
    {
        var data = new DateTime(2032, 1, 20);

        var primeira = FaturaRegras.DescricaoDaParcelaRefinanciada("Financiamento", data);
        var segunda = FaturaRegras.DescricaoDaParcelaRefinanciada(primeira, new DateTime(2032, 3, 1));
        var deOutroMotivo = FaturaRegras.DescricaoDaParcelaRefinanciada("Financiamento (absorvida no pagamento parcial de 12/01/2032)", data);

        Assert.Equal("Financiamento (absorvida no refinanciamento de 20/01/2032)", primeira);
        Assert.Equal("Financiamento (absorvida no refinanciamento de 01/03/2032)", segunda);
        Assert.Equal("Financiamento (absorvida no pagamento parcial de 12/01/2032) (absorvida no refinanciamento de 20/01/2032)", deOutroMotivo);
    }

    [Fact]
    public void Descricao_absorvida_nunca_passa_de_150_caracteres()
    {
        var longa = new string('x', 200);

        var r = FaturaRegras.DescricaoDaParcelaAbsorvida(longa, new DateTime(2032, 1, 12), "pagamento parcial");

        Assert.Equal(150, r.Length);
        Assert.EndsWith(" (absorvida no pagamento parcial de 12/01/2032)", r);
    }

    [Fact]
    public void Reconhece_e_remove_o_sufixo_da_absorcao_no_pagamento_parcial()
    {
        var d = "Parcela (absorvida no pagamento parcial de 12/01/2032)";

        Assert.True(FaturaRegras.FoiAbsorvidaNoPagamentoParcial(d));
        Assert.False(FaturaRegras.FoiAbsorvidaNoPagamentoParcial("Parcela (absorvida no refinanciamento de 12/01/2032)"));
        Assert.Equal("Parcela", FaturaRegras.RemoverSufixoDeAbsorcao(d));
        Assert.Equal("Parcela", FaturaRegras.RemoverSufixoDeAbsorcao("Parcela"));
    }

    [Theory]
    [InlineData(0, 100, false)]
    [InlineData(0.01, 100, true)]
    [InlineData(99.99, 100, true)]
    [InlineData(100, 100, false)]
    [InlineData(150, 100, false)]
    public void Pagamento_parcial_vai_de_mais_que_zero_a_menos_que_o_total(double pago, double total, bool valido) =>
        Assert.Equal(valido, FaturaRegras.PagamentoParcialValido((decimal)pago, (decimal)total));

    [Theory]
    [InlineData(-1, 100, false)]
    [InlineData(0, 100, true)]
    [InlineData(99.99, 100, true)]
    [InlineData(100, 100, false)]
    public void Entrada_vai_de_zero_a_menos_que_o_total(double entrada, double total, bool valida) =>
        Assert.Equal(valida, FaturaRegras.EntradaValida((decimal)entrada, (decimal)total));

    [Theory]
    [InlineData(1, false)]
    [InlineData(2, true)]
    [InlineData(24, true)]
    [InlineData(25, false)]
    public void Parcelas_do_financiamento_vao_de_2_a_24(int parcelas, bool valido) =>
        Assert.Equal(valido, FaturaRegras.TotalDeParcelasDoFinanciamentoValido(parcelas));

    [Fact]
    public void Juros_do_saldo_rotativo_e_da_parcela_financiada()
    {
        Assert.Equal(110m, FaturaRegras.SaldoRotativoComJuros(100m, 10m));
        Assert.Equal(100m, FaturaRegras.SaldoRotativoComJuros(100m, 0m));
        // 120 + 2% x 3 meses = 127,20 em 3 parcelas = 42,40
        Assert.Equal(42.40m, FaturaRegras.ValorDaParcelaFinanciada(120m, 2m, 3));
        Assert.Equal(33.33m, FaturaRegras.ValorDaParcelaFinanciada(100m, 0m, 3));
    }

    [Fact]
    public void Primeiro_mes_das_parcelas_avanca_ate_um_ciclo_que_ainda_nao_fechou()
    {
        // cartão fecha dia 10 e vence dia 15; hoje é 20/08/2032: o ciclo de ago (fecha 10/08) já fechou, o de set (fecha 10/09) não
        var r = FaturaRegras.PrimeiroMesDasParcelas(10, 15, new DateTime(2032, 7, 1), new DateTime(2032, 8, 20));

        Assert.Equal(new DateTime(2032, 9, 1), r);
        Assert.Equal(new DateTime(2032, 7, 1), FaturaRegras.PrimeiroMesDasParcelas(10, 15, new DateTime(2032, 7, 1), new DateTime(2032, 7, 10)));
    }

    [Fact]
    public void Mes_base_do_financiamento_e_o_do_mes_da_criacao_ou_o_seguinte_se_o_ciclo_ja_fechou()
    {
        Assert.Equal(new DateTime(2032, 1, 1), FaturaRegras.MesBaseDoFinanciamento(10, 15, new DateTime(2032, 1, 2)));
        Assert.Equal(new DateTime(2032, 1, 1), FaturaRegras.MesBaseDoFinanciamento(10, 15, new DateTime(2032, 1, 10)));
        Assert.Equal(new DateTime(2032, 2, 1), FaturaRegras.MesBaseDoFinanciamento(10, 15, new DateTime(2032, 1, 11)));
    }

    [Fact]
    public void Vencimento_corrigido_da_parcela_avanca_um_mes_por_numero_no_dia_do_cartao()
    {
        var mesBase = new DateTime(2032, 1, 1);

        Assert.Equal(new DateTime(2032, 1, 15, 12, 0, 0, DateTimeKind.Utc), FaturaRegras.VencimentoCorrigidoDaParcela(15, mesBase, 1));
        Assert.Equal(new DateTime(2032, 3, 15, 12, 0, 0, DateTimeKind.Utc), FaturaRegras.VencimentoCorrigidoDaParcela(15, mesBase, 3));
        Assert.Equal(new DateTime(2032, 1, 15, 12, 0, 0, DateTimeKind.Utc), FaturaRegras.VencimentoCorrigidoDaParcela(15, mesBase, null));
        Assert.Equal(new DateTime(2032, 2, 29, 12, 0, 0, DateTimeKind.Utc), FaturaRegras.VencimentoCorrigidoDaParcela(31, mesBase, 2));
    }
}
