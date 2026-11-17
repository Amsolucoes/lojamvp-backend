using LojaApi.Domain.Financeiro;

namespace LojaApi.Tests.Domain.AggregatesModel.FinanceiroTeste;

// Regras puras dos relatórios do Financeiro (sem banco, sem HTTP). Cartão de teste: fecha dia 10 e vence dia 15.
// A fatura de jun/2030 cobre as compras de 11/05 a 10/06/2030 e vence em 15/06/2030.
public class RelatoriosRegrasTests
{
    private static DateTime Utc(int ano, int mes, int dia) => new(ano, mes, dia, 12, 0, 0, DateTimeKind.Utc);

    private static readonly Func<DateTime, DateTime, decimal> SemCompras = (_, _) => 0m;
    private static Func<DateTime, DateTime, decimal> Compras(decimal total) => (_, _) => total;
    private static readonly RelatoriosRegras.ParcelaDoMes[] SemParcelas = [];

    // ── Contribuição do cartão no mês ────────────────────────────────────────

    [Fact]
    public void Sem_compras_e_sem_parcelas_o_cartao_nao_pesa_no_mes()
    {
        var c = RelatoriosRegras.ContribuicaoDoCartaoNoMes(10, 15, 2030, 6, Utc(2030, 6, 1), SemCompras, null, SemParcelas);

        Assert.Equal((0m, 0m, 0m), (c.Pago, c.Pendente, c.Vencido));
        Assert.Equal(0m, c.Total);
    }

    [Fact]
    public void Fatura_que_ainda_nao_venceu_e_pendente_e_a_vencida_e_vencido()
    {
        var aVencer = RelatoriosRegras.ContribuicaoDoCartaoNoMes(10, 15, 2030, 6, Utc(2030, 6, 14), Compras(300m), null, SemParcelas);
        var noDia = RelatoriosRegras.ContribuicaoDoCartaoNoMes(10, 15, 2030, 6, new DateTime(2030, 6, 15), Compras(300m), null, SemParcelas);
        var vencida = RelatoriosRegras.ContribuicaoDoCartaoNoMes(10, 15, 2030, 6, Utc(2030, 6, 16).Date, Compras(300m), null, SemParcelas);

        Assert.Equal((0m, 300m, 0m), (aVencer.Pago, aVencer.Pendente, aVencer.Vencido));
        Assert.Equal((0m, 300m, 0m), (noDia.Pago, noDia.Pendente, noDia.Vencido));      // vencendo hoje ainda não é atraso
        Assert.Equal((0m, 0m, 300m), (vencida.Pago, vencida.Pendente, vencida.Vencido));
    }

    [Fact]
    public void Adiantamento_abate_o_restante_e_cobrindo_tudo_a_fatura_conta_como_paga()
    {
        var parcial = RelatoriosRegras.ContribuicaoDoCartaoNoMes(10, 15, 2030, 6, Utc(2030, 6, 1), Compras(300m), new("pendente", 300m, 100m), SemParcelas);
        var total = RelatoriosRegras.ContribuicaoDoCartaoNoMes(10, 15, 2030, 6, Utc(2030, 6, 1), Compras(300m), new("pendente", 300m, 300m), SemParcelas);

        Assert.Equal((0m, 200m, 0m), (parcial.Pago, parcial.Pendente, parcial.Vencido));
        Assert.Equal((300m, 0m, 0m), (total.Pago, total.Pendente, total.Vencido));
    }

    [Theory]
    [InlineData("pago")]
    [InlineData("parcial")]
    [InlineData("financiada")]
    public void Fatura_resolvida_conta_o_restante_como_pago(string status)
    {
        var c = RelatoriosRegras.ContribuicaoDoCartaoNoMes(10, 15, 2030, 6, Utc(2030, 8, 1), Compras(300m), new(status, 300m, 50m), SemParcelas);

        Assert.Equal((250m, 0m, 0m), (c.Pago, c.Pendente, c.Vencido));
    }

    [Fact]
    public void Parcelas_de_financiamento_do_mes_entram_pagas_vencidas_ou_pendentes()
    {
        var hoje = Utc(2030, 6, 20);
        RelatoriosRegras.ParcelaDoMes[] parcelas =
        [
            new("pago", Utc(2030, 6, 5), 10m),
            new("pendente", Utc(2030, 6, 10), 20m),
            new("pendente", Utc(2030, 6, 25), 40m),
        ];

        var c = RelatoriosRegras.ContribuicaoDoCartaoNoMes(10, 15, 2030, 6, hoje.Date, SemCompras, null, parcelas);

        Assert.Equal((10m, 40m, 20m), (c.Pago, c.Pendente, c.Vencido));
    }

    [Fact]
    public void As_compras_somadas_sao_as_do_ciclo_da_fatura_do_mes()
    {
        (DateTime Inicio, DateTime Fim) pedido = default;

        RelatoriosRegras.ContribuicaoDoCartaoNoMes(10, 15, 2030, 6, Utc(2030, 6, 1), (i, f) => { pedido = (i, f); return 0m; }, null, SemParcelas);

        Assert.Equal((new DateTime(2030, 5, 11), new DateTime(2030, 6, 10)), (pedido.Inicio.Date, pedido.Fim.Date));
    }

    // ── Fatura "a pagar agora" ───────────────────────────────────────────────

    [Fact]
    public void Ciclo_atual_traz_a_fatura_fechada_mais_antiga_ainda_nao_paga()
    {
        // hoje = 20/06/2030: a fatura de jun (fechou dia 10) tem 300; a de mai também fechou
        var (vencimento, total, status) = RelatoriosRegras.CicloAtualDoCartao(10, 15, new DateTime(2030, 6, 20), (i, _) => i.Month == 5 ? 100m : i.Month == 4 ? 0m : 0m, (_, _) => null);

        Assert.Equal((new DateTime(2030, 6, 15), 100m, "pendente"), (vencimento.Date, total, status));
    }

    [Fact]
    public void Ciclo_atual_pula_fatura_paga_e_cai_no_ciclo_em_aberto_quando_nao_ha_pendente()
    {
        var (vencimento, total, status) = RelatoriosRegras.CicloAtualDoCartao(10, 15, new DateTime(2030, 6, 20), (i, _) => i.Month == 5 ? 100m : i.Month == 6 ? 70m : 0m,
            (ano, mes) => mes == 6 ? "pago" : null);

        // a fatura fechada (jun) está paga; sobra o ciclo em aberto (jul, vence 15/07, compras de 11/06 a 10/07), só informativo
        Assert.Equal((new DateTime(2030, 7, 15), 70m, "pendente"), (vencimento.Date, total, status));
    }

    [Fact]
    public void Ciclo_atual_devolve_o_status_registrado_da_fatura()
    {
        var (_, _, status) = RelatoriosRegras.CicloAtualDoCartao(10, 15, new DateTime(2030, 6, 20), (i, _) => i.Month == 5 ? 100m : 0m, (_, _) => "parcial");

        Assert.Equal("parcial", status);
    }

    // ── Limite usado ─────────────────────────────────────────────────────────

    private static decimal Limite(DateTime hoje, Func<DateTime, DateTime, decimal> soma, Func<int, int, RelatoriosRegras.FaturaRegistrada?>? faturaDe = null,
        (string Modo, DateTime DataCompra, decimal Valor)[]? compras = null, decimal financiamento = 0m) =>
        RelatoriosRegras.LimiteUsado(10, 15, hoje, soma, faturaDe ?? ((_, _) => null), compras ?? [], financiamento);

    [Fact]
    public void Limite_usado_soma_as_faturas_nao_quitadas_menos_os_adiantamentos()
    {
        var hoje = new DateTime(2030, 6, 20);
        var usado = Limite(hoje, (i, _) => i.Month is 5 or 6 ? 100m : 0m, (ano, mes) => mes == 6 && ano == 2030 ? new("pendente", 100m, 30m) : null);

        Assert.Equal(170m, usado);   // 100 (fatura de jun, já com 30 adiantados) + 100 (ciclo seguinte, em aberto)
    }

    [Theory]
    [InlineData("pago")]
    [InlineData("financiada")]
    [InlineData("parcial")]
    public void Limite_usado_ignora_ciclo_de_fatura_quitada_com_o_mesmo_total(string status)
    {
        var usado = Limite(new DateTime(2030, 6, 20), (i, _) => i.Month == 5 ? 100m : 0m, (ano, mes) => mes == 6 && ano == 2030 ? new(status, 100m, 0m) : null);

        Assert.Equal(0m, usado);
    }

    [Fact]
    public void Limite_usado_conta_o_que_foi_comprado_no_ciclo_depois_do_pagamento_da_fatura()
    {
        var usado = Limite(new DateTime(2030, 6, 20), (i, _) => i.Month == 5 ? 130m : 0m, (ano, mes) => mes == 6 && ano == 2030 ? new("pago", 100m, 0m) : null);

        Assert.Equal(30m, usado);
    }

    [Fact]
    public void Limite_usado_soma_compras_futuras_exceto_as_fixas_e_as_de_fatura_paga_ou_financiada_e_o_financiamento_pendente()
    {
        var hoje = new DateTime(2030, 6, 20);
        (string, DateTime, decimal)[] compras =
        [
            ("parcelada", Utc(2030, 9, 1), 60m),     // fatura de set: sem registro, conta
            ("avulsa", Utc(2030, 10, 1), 15m),       // fatura de out: financiada, não conta
            ("fixa", Utc(2030, 9, 2), 999m),         // cobrança fixa futura não conta
            ("parcelada", Utc(2030, 6, 5), 888m),    // dentro do ciclo já somado (não é futura)
            ("parcelada", Utc(2030, 11, 20), 25m),   // depois do fechamento: cai na fatura de dez, sem registro, conta
        ];

        var usado = Limite(hoje, SemCompras, (ano, mes) => mes == 10 && ano == 2030 ? new("financiada", 1m, 0m) : null, compras, financiamento: 40m);

        Assert.Equal(60m + 25m + 40m, usado);
    }

    [Fact]
    public void Limite_usado_conta_compra_futura_de_fatura_parcial()
    {
        (string, DateTime, decimal)[] compras = [("parcelada", Utc(2030, 9, 1), 60m)];

        Assert.Equal(60m, Limite(new DateTime(2030, 6, 20), SemCompras, (ano, mes) => mes == 9 && ano == 2030 ? new("parcial", 1m, 0m) : null, compras));
        Assert.Equal(0m, Limite(new DateTime(2030, 6, 20), SemCompras, (ano, mes) => mes == 9 && ano == 2030 ? new("pago", 1m, 0m) : null, compras));
    }

    // ── Comparativo ──────────────────────────────────────────────────────────

    [Theory]
    [InlineData(2020, 2026, 12)]
    [InlineData(2026, 2026, 11)]
    [InlineData(2027, 2026, 0)]
    public void Ultimo_mes_acontecido_depende_do_ano(int ano, int anoAtual, int esperado) =>
        Assert.Equal(esperado, RelatoriosRegras.UltimoMesAcontecido(ano, new DateTime(anoAtual, 11, 17)));

    [Fact]
    public void Melhor_mes_e_o_de_menor_gasto_maior_que_zero_entre_os_meses_acontecidos()
    {
        decimal[] serie = [0, 50, 100, 0, 20, 5, 0, 0, 0, 0, 0, 0];

        Assert.Equal((2, 50m), RelatoriosRegras.MelhorMes(serie, 4));
        Assert.Equal((6, 5m), RelatoriosRegras.MelhorMes(serie, 6));
        Assert.Equal((null, null), RelatoriosRegras.MelhorMes(serie, 0));
        Assert.Equal((null, null), RelatoriosRegras.MelhorMes(new decimal[12], 12));
    }

    [Fact]
    public void Em_empate_o_melhor_mes_e_o_primeiro()
    {
        decimal[] serie = [30, 30, 40, 0, 0, 0, 0, 0, 0, 0, 0, 0];

        Assert.Equal((1, 30m), RelatoriosRegras.MelhorMes(serie, 12));
    }

    [Fact]
    public void Variacao_percentual_nao_existe_quando_o_mes_anterior_foi_zero()
    {
        Assert.Equal(100m, RelatoriosRegras.VariacaoPercentual(100m, 50m));
        Assert.Equal(-50m, RelatoriosRegras.VariacaoPercentual(25m, 50m));
        Assert.Null(RelatoriosRegras.VariacaoPercentual(25m, 0m));
    }

    [Fact]
    public void Mes_anterior_de_janeiro_e_dezembro_do_ano_anterior()
    {
        Assert.Equal((2023, 12), RelatoriosRegras.MesAnterior(2024, 1));
        Assert.Equal((2024, 2), RelatoriosRegras.MesAnterior(2024, 3));
    }
}

// Regras novas do cartão e do planejamento usadas pelos relatórios
public class RelatoriosCicloEPlanejamentoRegrasTests
{
    [Theory]
    [InlineData(2030, 6, 5, 2030, 6, 15)]     // antes do fechamento (dia 10): a fatura do mês
    [InlineData(2030, 6, 10, 2030, 6, 15)]    // no dia do fechamento ainda pertence a ela
    [InlineData(2030, 6, 11, 2030, 7, 15)]    // depois do fechamento: a do mês seguinte
    [InlineData(2030, 12, 20, 2031, 1, 15)]   // virada do ano
    public void Ciclo_em_aberto_e_o_da_fatura_em_que_hoje_cai(int ano, int mes, int dia, int anoVenc, int mesVenc, int diaVenc)
    {
        var (vencimento, ciclo) = CartaoRegras.CicloEmAberto(10, 15, new DateTime(ano, mes, dia));

        Assert.Equal(new DateTime(anoVenc, mesVenc, diaVenc), vencimento.Date);
        Assert.True(new DateTime(ano, mes, dia) >= ciclo.Inicio.Date && new DateTime(ano, mes, dia) <= ciclo.Fim.Date);
    }

    [Theory]
    [InlineData(2024, 3, true)]
    [InlineData(2000, 1, true)]
    [InlineData(2000, 12, true)]
    [InlineData(1999, 6, false)]
    [InlineData(2024, 0, false)]
    [InlineData(2024, 13, false)]
    public void Mes_do_planejamento_precisa_ser_de_2000_em_diante_e_de_1_a_12(int ano, int mes, bool valido) =>
        Assert.Equal(valido, PlanejamentoRegras.MesValido(ano, mes));

    [Fact]
    public void Receita_do_mes_prefere_os_tetos_de_receita_depois_a_planejada_e_por_fim_a_prevista()
    {
        Assert.Equal(2000m, PlanejamentoRegras.ReceitaDoMes(2000m, 4000m, 1000m));
        Assert.Equal(4000m, PlanejamentoRegras.ReceitaDoMes(0m, 4000m, 1000m));
        Assert.Equal(1000m, PlanejamentoRegras.ReceitaDoMes(0m, null, 1000m));
    }

    [Fact]
    public void Valor_a_poupar_e_o_percentual_da_receita_com_duas_casas()
    {
        Assert.Equal(200m, PlanejamentoRegras.ValorAPoupar(2000m, 10m));
        Assert.Equal(375m, PlanejamentoRegras.ValorAPoupar(3000m, 12.5m));
        Assert.Equal(3.33m, PlanejamentoRegras.ValorAPoupar(33.333m, 10m));
        Assert.Equal(0m, PlanejamentoRegras.ValorAPoupar(0m, 30m));
    }

    [Fact]
    public void Guardar_por_mes_divide_o_que_falta_pelos_meses_ate_a_data_alvo_com_minimo_de_um_mes()
    {
        var hoje = new DateTime(2030, 6, 20);

        Assert.Equal(200m, PlanejamentoRegras.GuardarPorMes(1200m, 200m, new DateTime(2030, 11, 1), hoje));   // 5 meses
        Assert.Equal(1000m, PlanejamentoRegras.GuardarPorMes(1200m, 200m, new DateTime(2030, 6, 30), hoje));  // mesmo mês: 1
        Assert.Equal(1000m, PlanejamentoRegras.GuardarPorMes(1200m, 200m, new DateTime(2029, 1, 1), hoje));   // data passada: 1
        Assert.Equal(0m, PlanejamentoRegras.GuardarPorMes(1000m, 1500m, new DateTime(2030, 11, 1), hoje));    // já passou do alvo
        Assert.Equal(33.33m, PlanejamentoRegras.GuardarPorMes(100m, 0m, new DateTime(2030, 9, 1), hoje));     // 3 meses, arredonda
    }
}
