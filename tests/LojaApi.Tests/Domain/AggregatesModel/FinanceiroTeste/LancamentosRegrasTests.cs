using LojaApi.Domain.Financeiro;

namespace LojaApi.Tests.Domain.AggregatesModel.FinanceiroTeste;

public class LancamentosRegrasTests
{
    [Fact]
    public void Ao_meio_dia_zera_a_hora_marca_utc_e_soma_12h()
    {
        var r = LancamentosRegras.AoMeioDia(new DateTime(2032, 3, 10, 23, 45, 0, DateTimeKind.Local));

        Assert.Equal(new DateTime(2032, 3, 10, 12, 0, 0, DateTimeKind.Utc), r);
        Assert.Equal(DateTimeKind.Utc, r.Kind);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(28, 28)]
    [InlineData(15, 15)]
    [InlineData(0, 10)]
    [InlineData(29, 10)]
    [InlineData(31, 10)]
    [InlineData(-3, 10)]
    public void Dia_do_fixo_vai_de_1_a_28_e_fora_disso_vale_10(int dia, int esperado) =>
        Assert.Equal(esperado, LancamentosRegras.DiaDoFixo(dia));

    [Fact]
    public void Total_de_parcelas_com_data_fim_conta_os_meses_inclusive_e_ignora_o_informado()
    {
        var primeiro = new DateTime(2032, 1, 31);

        Assert.Equal(6, LancamentosRegras.TotalDeParcelas(primeiro, new DateTime(2032, 6, 1), 99));
        Assert.Equal(1, LancamentosRegras.TotalDeParcelas(primeiro, new DateTime(2032, 1, 1), null));
        Assert.Equal(13, LancamentosRegras.TotalDeParcelas(primeiro, new DateTime(2033, 1, 15), null));
        Assert.Equal(5, LancamentosRegras.TotalDeParcelas(primeiro, null, 5));
        Assert.Equal(0, LancamentosRegras.TotalDeParcelas(primeiro, null, null));
    }

    [Theory]
    [InlineData(1, false)]
    [InlineData(2, true)]
    [InlineData(120, true)]
    [InlineData(121, false)]
    public void Total_de_parcelas_do_parcelamento_vai_de_2_a_120(int total, bool valido) =>
        Assert.Equal(valido, LancamentosRegras.TotalDeParcelasValido(total));

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(360, true)]
    [InlineData(361, false)]
    public void Total_alterado_vai_de_1_a_360(int total, bool valido) =>
        Assert.Equal(valido, LancamentosRegras.TotalAlteradoValido(total));

    [Fact]
    public void Reajustar_dia_mantem_o_mes_limita_ao_ultimo_dia_e_fica_ao_meio_dia()
    {
        Assert.Equal(new DateTime(2032, 2, 29, 12, 0, 0, DateTimeKind.Utc), LancamentosRegras.ReajustarDia(new DateTime(2032, 2, 5, 0, 0, 0, DateTimeKind.Utc), 31));
        Assert.Equal(new DateTime(2031, 2, 28, 12, 0, 0, DateTimeKind.Utc), LancamentosRegras.ReajustarDia(new DateTime(2031, 2, 5, 0, 0, 0, DateTimeKind.Utc), 31));
        Assert.Equal(new DateTime(2032, 4, 20, 12, 0, 0, DateTimeKind.Utc), LancamentosRegras.ReajustarDia(new DateTime(2032, 4, 5, 8, 0, 0, DateTimeKind.Utc), 20));
    }

    [Fact]
    public void Primeiro_dia_do_mes_vai_para_o_dia_1_a_meia_noite_utc() =>
        Assert.Equal(new DateTime(2032, 7, 1, 0, 0, 0, DateTimeKind.Utc), LancamentosRegras.PrimeiroDiaDoMes(new DateTime(2032, 7, 19, 15, 30, 0)));

    [Fact]
    public void Parcelas_do_cartao_dividem_o_valor_e_a_ultima_absorve_os_centavos()
    {
        Assert.Equal([33.33m, 33.33m, 33.34m], LancamentosRegras.ValoresDasParcelasDoCartao(100m, 3));
        Assert.Equal([50m, 50m], LancamentosRegras.ValoresDasParcelasDoCartao(100m, 2));
        Assert.Equal([0.33m, 0.33m, 0.34m], LancamentosRegras.ValoresDasParcelasDoCartao(1m, 3));
        Assert.Equal([10m], LancamentosRegras.ValoresDasParcelasDoCartao(10m, 1));
        Assert.Equal(100m, LancamentosRegras.ValoresDasParcelasDoCartao(100m, 7).Sum());
    }
}
