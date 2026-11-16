using LojaApi.Domain.Financeiro;

namespace LojaApi.Tests.Domain.AggregatesModel.FinanceiroTeste;

public class CartaoRegrasTests
{
    [Theory]
    [InlineData("visa", "visa")]
    [InlineData("  MasterCard ", "mastercard")]
    [InlineData("ELO", "elo")]
    [InlineData("amex", "amex")]
    [InlineData("Hipercard", "hipercard")]
    [InlineData("diners", "diners")]
    [InlineData("paypal", null)]
    [InlineData("", null)]
    [InlineData(null, null)]
    public void Bandeira_so_vale_se_for_uma_das_conhecidas(string? valor, string? esperado) =>
        Assert.Equal(esperado, CartaoRegras.NormalizarBandeira(valor));

    [Theory]
    [InlineData(1, 1)]
    [InlineData(28, 28)]
    [InlineData(0, 1)]
    [InlineData(29, 1)]
    [InlineData(-1, 1)]
    public void Dia_da_compra_fixa_vai_de_1_a_28_e_fora_disso_vale_1(int dia, int esperado) =>
        Assert.Equal(esperado, CartaoRegras.DiaDaCompraFixa(dia));

    [Theory]
    [InlineData(1, false)]
    [InlineData(2, true)]
    [InlineData(24, true)]
    [InlineData(25, false)]
    public void Parcelas_da_compra_vao_de_2_a_24(int total, bool valido) =>
        Assert.Equal(valido, CartaoRegras.TotalDeParcelasDaCompraValido(total));

    [Fact]
    public void Vencimento_da_fatura_limita_o_dia_ao_ultimo_do_mes_ao_meio_dia()
    {
        Assert.Equal(new DateTime(2032, 2, 29, 12, 0, 0, DateTimeKind.Utc), CartaoRegras.VencimentoDaFatura(31, 2032, 2));
        Assert.Equal(new DateTime(2031, 2, 28, 12, 0, 0, DateTimeKind.Utc), CartaoRegras.VencimentoDaFatura(31, 2031, 2));
        Assert.Equal(new DateTime(2032, 5, 15, 12, 0, 0, DateTimeKind.Utc), CartaoRegras.VencimentoDaFatura(15, 2032, 5));
    }

    [Fact]
    public void Ciclo_da_fatura_vai_do_dia_seguinte_ao_fechamento_anterior_ate_o_fechamento_inclusive()
    {
        var (inicio, fim) = CartaoRegras.CicloDaFatura(10, new DateTime(2032, 3, 15, 12, 0, 0, DateTimeKind.Utc));

        Assert.Equal(new DateTime(2032, 2, 11, 0, 0, 0, DateTimeKind.Utc), inicio);
        Assert.Equal(new DateTime(2032, 3, 10, 0, 0, 0, DateTimeKind.Utc), fim);

        var (iniFev, fimFev) = CartaoRegras.CicloDaFatura(31, new DateTime(2032, 2, 15, 12, 0, 0, DateTimeKind.Utc));
        Assert.Equal(new DateTime(2032, 1, 30, 0, 0, 0, DateTimeKind.Utc), iniFev);
        Assert.Equal(new DateTime(2032, 2, 29, 0, 0, 0, DateTimeKind.Utc), fimFev);
    }

    [Theory]
    [InlineData(2032, 1, 5, 2032, 1, 15)]    // antes do fechamento (dia 10): fatura do próprio mês
    [InlineData(2032, 1, 10, 2032, 1, 15)]   // o dia do fechamento ainda é desta fatura
    [InlineData(2032, 1, 11, 2032, 2, 15)]   // depois do fechamento: fatura do mês seguinte
    [InlineData(2032, 12, 20, 2033, 1, 15)]  // virada de ano
    public void Fatura_da_compra_e_a_do_mes_ou_a_do_mes_seguinte_se_passou_do_fechamento(int ac, int mc, int dc, int av, int mv, int dv) =>
        Assert.Equal(new DateTime(av, mv, dv, 12, 0, 0, DateTimeKind.Utc), CartaoRegras.VencimentoDaFaturaDaCompra(10, 15, new DateTime(ac, mc, dc, 12, 0, 0, DateTimeKind.Utc)));
}
