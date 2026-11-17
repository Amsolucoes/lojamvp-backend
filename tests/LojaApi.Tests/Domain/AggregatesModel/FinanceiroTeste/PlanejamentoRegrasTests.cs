using LojaApi.Domain.Financeiro;

namespace LojaApi.Tests.Domain.AggregatesModel.FinanceiroTeste;

// Regras puras do Planejamento: percentual, receita, tipo do teto e valores das metas (sem banco, sem HTTP).
public class PlanejamentoRegrasTests
{
    [Theory]
    [InlineData(0, true)]
    [InlineData(100, true)]
    [InlineData(30.5, true)]
    [InlineData(-0.01, false)]
    [InlineData(100.01, false)]
    public void Percentual_a_guardar_fica_entre_0_e_100(double pct, bool valido) =>
        Assert.Equal(valido, PlanejamentoRegras.PercentualValido((decimal)pct));

    [Fact]
    public void Receita_planejada_negativa_e_recusada_mas_ausente_ou_zero_vale()
    {
        Assert.True(PlanejamentoRegras.ReceitaPlanejadaNegativa(-1m));
        Assert.False(PlanejamentoRegras.ReceitaPlanejadaNegativa(0m));
        Assert.False(PlanejamentoRegras.ReceitaPlanejadaNegativa(null));
    }

    [Fact]
    public void So_receita_maior_que_zero_e_gravada()
    {
        Assert.Equal(5000m, PlanejamentoRegras.ReceitaParaGravar(5000m));
        Assert.Null(PlanejamentoRegras.ReceitaParaGravar(0m));
        Assert.Null(PlanejamentoRegras.ReceitaParaGravar(null));
    }

    [Theory]
    [InlineData("receber", "receber")]
    [InlineData("pagar", "pagar")]
    [InlineData("outro", "pagar")]
    [InlineData(null, "pagar")]
    [InlineData("RECEBER", "pagar")]
    public void Tipo_do_teto_e_receber_ou_pagar(string? tipo, string esperado) =>
        Assert.Equal(esperado, PlanejamentoRegras.TipoDoTeto(tipo));

    [Fact]
    public void Teto_aceita_zero_mas_nao_negativo()
    {
        Assert.True(PlanejamentoRegras.ValorDoTetoValido(0m));
        Assert.False(PlanejamentoRegras.ValorDoTetoValido(-0.01m));
    }

    [Fact]
    public void Valor_alvo_da_meta_precisa_ser_maior_que_zero()
    {
        Assert.True(PlanejamentoRegras.ValorAlvoValido(0.01m));
        Assert.False(PlanejamentoRegras.ValorAlvoValido(0m));
        Assert.False(PlanejamentoRegras.ValorAlvoValido(-5m));
    }

    [Theory]
    [InlineData(null, "🎯")]
    [InlineData("", "🎯")]
    [InlineData("   ", "🎯")]
    [InlineData(" 🚗 ", "🚗")]
    public void Icone_da_meta_tem_padrao_e_e_aparado(string? icone, string esperado) =>
        Assert.Equal(esperado, PlanejamentoRegras.IconeDaMeta(icone));

    [Fact]
    public void Guardado_e_aporte_nunca_ficam_negativos()
    {
        Assert.Equal(0m, PlanejamentoRegras.ValorGuardadoValido(-10m));
        Assert.Equal(7m, PlanejamentoRegras.ValorGuardadoValido(7m));
        Assert.Equal(150m, PlanejamentoRegras.NovoValorGuardado(100m, 50m));
        Assert.Equal(40m, PlanejamentoRegras.NovoValorGuardado(100m, -60m));
        Assert.Equal(0m, PlanejamentoRegras.NovoValorGuardado(100m, -500m));
    }

    [Fact]
    public void Data_alvo_vira_so_o_dia_em_utc()
    {
        Assert.Null(PlanejamentoRegras.DataAlvoEmUtc(null));
        var d = PlanejamentoRegras.DataAlvoEmUtc(new DateTime(2027, 3, 5, 14, 30, 0, DateTimeKind.Local))!.Value;
        Assert.Equal(new DateTime(2027, 3, 5), d);
        Assert.Equal(DateTimeKind.Utc, d.Kind);
    }
}
