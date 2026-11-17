using LojaApi.Domain.Financeiro;

namespace LojaApi.Tests.Domain.AggregatesModel.FinanceiroTeste;

public class ContasAPagarRegrasTests
{
    [Fact]
    public void Periodo_com_de_e_ate_vai_do_inicio_de_ao_fim_do_dia_ate_inclusive()
    {
        var (inicio, fim) = ContasAPagarRegras.PeriodoConsultado(2020, 5, new DateTime(2032, 1, 10, 15, 0, 0), new DateTime(2032, 1, 20, 8, 0, 0), new DateTime(2026, 11, 17));

        Assert.Equal(new DateTime(2032, 1, 10, 0, 0, 0, DateTimeKind.Utc), inicio);
        Assert.Equal(new DateTime(2032, 1, 21, 0, 0, 0, DateTimeKind.Utc), fim);
        Assert.Equal(DateTimeKind.Utc, inicio.Kind);
    }

    [Fact]
    public void Periodo_sem_intervalo_e_o_mes_informado_ou_o_atual()
    {
        var hoje = new DateTime(2026, 11, 17);

        Assert.Equal((new DateTime(2032, 3, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2032, 4, 1, 0, 0, 0, DateTimeKind.Utc)), ContasAPagarRegras.PeriodoConsultado(2032, 3, null, null, hoje));
        Assert.Equal((new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 12, 1, 0, 0, 0, DateTimeKind.Utc)), ContasAPagarRegras.PeriodoConsultado(null, null, null, null, hoje));
        Assert.Equal(new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc), ContasAPagarRegras.PeriodoConsultado(null, 5, null, null, hoje).Inicio);
        // só um dos dois (de ou até) não vale como intervalo
        Assert.Equal(new DateTime(2032, 3, 1, 0, 0, 0, DateTimeKind.Utc), ContasAPagarRegras.PeriodoConsultado(2032, 3, new DateTime(2032, 1, 1), null, hoje).Inicio);
    }

    [Fact]
    public void Meses_do_periodo_vao_do_mes_do_inicio_ao_mes_do_ultimo_dia()
    {
        var um = ContasAPagarRegras.MesesDoPeriodo(new DateTime(2032, 1, 1), new DateTime(2032, 2, 1));
        var tres = ContasAPagarRegras.MesesDoPeriodo(new DateTime(2031, 11, 10), new DateTime(2032, 2, 1));

        Assert.Equal([(2032, 1)], um);
        Assert.Equal([(2031, 11), (2031, 12), (2032, 1)], tres);
    }

    [Fact]
    public void Meses_do_periodo_param_em_36()
    {
        var meses = ContasAPagarRegras.MesesDoPeriodo(new DateTime(2030, 1, 1), new DateTime(2040, 1, 1));

        Assert.Equal(36, meses.Count);
        Assert.Equal((2030, 1), meses[0]);
        Assert.Equal((2032, 12), meses[^1]);
    }

    [Theory]
    [InlineData("pago", true)]
    [InlineData("financiada", true)]
    [InlineData("parcial", true)]
    [InlineData("pendente", false)]
    [InlineData(null, false)]
    public void Compras_resolvidas_quando_a_fatura_foi_paga_financiada_ou_paga_em_parte(string? status, bool esperado) =>
        Assert.Equal(esperado, ContasAPagarRegras.ComprasResolvidas(status));

    [Fact]
    public void Compras_pendentes_descontam_o_antecipado_sem_ficar_negativas_e_zeram_se_resolvidas()
    {
        Assert.Equal(70m, ContasAPagarRegras.ComprasPendentes(100m, 30m, false));
        Assert.Equal(0m, ContasAPagarRegras.ComprasPendentes(100m, 150m, false));
        Assert.Equal(100m, ContasAPagarRegras.ComprasPendentes(100m, 0m, false));
        Assert.Equal(0m, ContasAPagarRegras.ComprasPendentes(100m, 30m, true));
    }
}
