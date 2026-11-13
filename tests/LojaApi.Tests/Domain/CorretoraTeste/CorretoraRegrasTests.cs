using LojaApi.Domain.Corretora;

namespace LojaApi.Tests.Domain.CorretoraTeste;

public class CorretoraRegrasTests
{
    private static readonly DateTime Hoje = new(2030, 6, 10);

    [Theory]
    [InlineData("lead", true)]
    [InlineData("contato", true)]
    [InlineData("proposta", true)]
    [InlineData("negociacao", true)]
    [InlineData("ganho", true)]
    [InlineData("perdido", true)]
    [InlineData("Lead", false)]
    [InlineData("voando", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void Etapas_do_funil(string? etapa, bool esperado) => Assert.Equal(esperado, CorretoraRegras.EtapaValida(etapa));

    [Theory]
    [InlineData("ativa", true)]
    [InlineData("vencida", true)]
    [InlineData("renovada", true)]
    [InlineData("cancelada", true)]
    [InlineData("sumiu", false)]
    [InlineData(null, false)]
    public void Status_da_apolice(string? status, bool esperado) => Assert.Equal(esperado, CorretoraRegras.StatusApoliceValido(status));

    [Theory]
    [InlineData("lead", true)]
    [InlineData("negociacao", true)]
    [InlineData("ganho", false)]
    [InlineData("perdido", false)]
    public void Em_andamento_ate_ganhar_ou_perder(string etapa, bool esperado) => Assert.Equal(esperado, CorretoraRegras.EmAndamento(etapa));

    [Fact]
    public void Vigencia_e_guardada_ao_meio_dia_em_utc()
    {
        var v = CorretoraRegras.NormalizarVigencia(new DateTime(2030, 6, 10, 23, 45, 0));
        Assert.Equal(new DateTime(2030, 6, 10, 12, 0, 0, DateTimeKind.Utc), v);
        Assert.Equal(DateTimeKind.Utc, v.Kind);
    }

    [Theory]
    [InlineData("2030-06-10", 0)]
    [InlineData("2030-06-11", 1)]
    [InlineData("2030-07-10", 30)]
    [InlineData("2030-06-09", -1)]
    public void Dias_para_vencer(string fim, int esperado) =>
        Assert.Equal(esperado, CorretoraRegras.DiasParaVencer(DateTime.Parse(fim).AddHours(12), Hoje));

    [Theory]
    [InlineData("ativa", "2030-06-10", true)]     // vence hoje
    [InlineData("ativa", "2030-07-10", true)]     // 30 dias
    [InlineData("ativa", "2030-07-11", false)]    // 31
    [InlineData("ativa", "2030-06-09", false)]    // já venceu
    [InlineData("vencida", "2030-06-15", false)]  // só conta a ativa
    [InlineData("renovada", "2030-06-15", false)]
    public void Vencendo_em_ate_30_dias(string status, string fim, bool esperado) =>
        Assert.Equal(esperado, CorretoraRegras.VenceEmAte30Dias(status, DateTime.Parse(fim), Hoje));

    [Fact]
    public void Descricao_e_valor_da_comissao()
    {
        Assert.Equal("Comissão — Saúde (Ana)", CorretoraRegras.DescricaoDaComissao("Saúde", "Ana"));
        Assert.Equal("Comissão — Plano ()", CorretoraRegras.DescricaoDaComissao("Plano", null));
        Assert.Equal(120m, CorretoraRegras.ValorDaComissao(120m, 800m));
        Assert.Equal(800m, CorretoraRegras.ValorDaComissao(null, 800m));
        Assert.Equal(0m, CorretoraRegras.ValorDaComissao(null, null));
        Assert.Equal(0m, CorretoraRegras.ValorDaComissao(0m, 800m));   // zero informado vale (não cai no estimado)
    }

    [Fact]
    public void Resumo_do_funil_separa_em_andamento_de_implantadas_e_ignora_perdidas()
    {
        var r = CorretoraRegras.Resumir(
        [
            new("lead", 2, 100m), new("proposta", 3, 200m), new("negociacao", null, null),
            new("ganho", 4, 400m), new("perdido", 8, 800m),
        ]);

        Assert.Equal(new TotaisDoFunil(3, 5, 300m), r.EmAndamento);
        Assert.Equal(new TotaisDoFunil(1, 4, 400m), r.Implantadas);
    }

    [Fact]
    public void Resumo_vazio() => Assert.Equal(new ResumoDoFunil(default, default), CorretoraRegras.Resumir([]));
}
