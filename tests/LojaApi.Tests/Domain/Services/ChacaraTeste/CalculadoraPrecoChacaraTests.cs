using LojaApi.Domain.Chacara;

namespace LojaApi.Tests.Domain.Services.ChacaraTeste;

public class CalculadoraPrecoChacaraTests
{
    // Faixa até 50 pessoas: semana 500, fim de semana 800, pacote sex+sáb 1.400. Faixa até 100: 700 / 1.000 / 1.800.
    private static List<FaixaPrecoChacara> Faixas() =>
    [
        FaixaPrecoChacara.Criar(Guid.Empty, 50, 500m, 800m, 1400m),
        FaixaPrecoChacara.Criar(Guid.Empty, 100, 700m, 1000m, 1800m),
    ];

    private static ConfiguracaoPrecoChacara Cfg() => new()
    {
        LimitePessoasParaTaxaLimpeza = 50,
        ValorTaxaLimpeza = 250m,
    };

    private static CalculadoraPrecoChacara.ResultadoCalculo Calcular(DateTime ini, DateTime fim, int pessoas,
        List<FaixaPrecoChacara>? faixas = null, List<PeriodoEspecialChacara>? periodos = null, ConfiguracaoPrecoChacara? cfg = null) =>
        CalculadoraPrecoChacara.Calcular(ini, fim, pessoas, cfg ?? Cfg(), faixas ?? Faixas(), periodos);

    [Fact]
    public void Um_dia_de_semana_cobra_a_diaria_de_semana()
    {
        var r = Calcular(Datas.Segunda(), Datas.Segunda(), 30);
        Assert.Equal(500m, r.ValorEstadia);
        Assert.Equal(0m, r.ValorTaxaLimpeza);
        Assert.Equal(500m, r.ValorTotal);
        Assert.False(r.PacoteFechado);
    }

    [Fact]
    public void Sexta_e_sabado_juntos_cobram_o_pacote_de_fim_de_semana()
    {
        var r = Calcular(Datas.Sexta(), Datas.Sabado(), 30);
        Assert.Equal(1400m, r.ValorTotal);
        Assert.Single(r.Detalhamento);
    }

    [Fact]
    public void Sabado_e_domingo_juntos_tambem_sao_pacote()
    {
        Assert.Equal(1400m, Calcular(Datas.Sabado(), Datas.Domingo(), 30).ValorTotal);
    }

    [Fact]
    public void Sexta_a_domingo_e_pacote_mais_a_diaria_do_domingo()
    {
        var r = Calcular(Datas.Sexta(), Datas.Domingo(), 30);
        Assert.Equal(1400m + 800m, r.ValorTotal);
    }

    [Theory]
    [InlineData(5)]   // sexta
    [InlineData(6)]   // sábado
    [InlineData(0)]   // domingo
    public void Um_unico_dia_de_fim_de_semana_cobra_a_diaria_de_fim_de_semana(int diaDaSemana)
    {
        var dia = Datas.Sexta();
        while ((int)dia.DayOfWeek != diaDaSemana) dia = dia.AddDays(1);
        Assert.Equal(800m, Calcular(dia, dia, 30).ValorTotal);
    }

    [Fact]
    public void Semana_inteira_soma_cada_dia_e_o_pacote_do_fim_de_semana()
    {
        // segunda..domingo: 4 dias de semana (seg, ter, qua, qui) + pacote sex+sáb + domingo
        var r = Calcular(Datas.Segunda(-1), Datas.Domingo(), 30);
        Assert.Equal(4 * 500m + 1400m + 800m, r.ValorTotal);
    }

    [Fact]
    public void Escolhe_a_menor_faixa_que_comporta_as_pessoas()
    {
        Assert.Equal(500m, Calcular(Datas.Segunda(), Datas.Segunda(), 50).ValorTotal);   // limite da 1ª faixa
        Assert.Equal(700m + 250m, Calcular(Datas.Segunda(), Datas.Segunda(), 51).ValorTotal); // 2ª faixa (+ taxa de limpeza)
    }

    [Fact]
    public void Acima_da_maior_faixa_usa_a_ultima()
    {
        var r = Calcular(Datas.Segunda(), Datas.Segunda(), 5000);
        Assert.Equal(700m, r.ValorEstadia);
    }

    [Fact]
    public void Sem_nenhuma_faixa_lanca_erro_claro()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => Calcular(Datas.Segunda(), Datas.Segunda(), 10, faixas: []));
        Assert.Contains("faixa", ex.Message);
    }

    [Theory]
    [InlineData(50, 0)]    // no limite não cobra
    [InlineData(51, 250)]  // acima do limite cobra
    public void Taxa_de_limpeza_so_acima_do_limite_de_pessoas(int pessoas, decimal taxaEsperada)
    {
        var r = Calcular(Datas.Segunda(), Datas.Segunda(), pessoas);
        Assert.Equal(taxaEsperada, r.ValorTaxaLimpeza);
        Assert.Equal(r.ValorEstadia + taxaEsperada, r.ValorTotal);
    }

    [Fact]
    public void Periodo_especial_com_as_mesmas_datas_vira_pacote_fechado()
    {
        var ini = Datas.Sexta(4); var fim = ini.AddDays(2);
        var natal = PeriodoEspecialChacara.Criar(Guid.Empty, "Natal", ini, fim, 5500m);

        var r = Calcular(ini, fim, 30, periodos: [natal]);

        Assert.True(r.PacoteFechado);
        Assert.Equal(5500m, r.ValorEstadia);
        Assert.Equal(5500m, r.ValorTotal);
        Assert.Contains("Natal", r.Detalhamento[0]);
    }

    [Fact]
    public void Pacote_fechado_tambem_cobra_taxa_de_limpeza_acima_do_limite()
    {
        var ini = Datas.Sexta(4); var fim = ini.AddDays(2);
        var natal = PeriodoEspecialChacara.Criar(Guid.Empty, "Natal", ini, fim, 5500m);

        var r = Calcular(ini, fim, 80, periodos: [natal]);

        Assert.Equal(5500m + 250m, r.ValorTotal);
        Assert.Equal(250m, r.ValorTaxaLimpeza);
    }

    [Fact]
    public void Periodo_especial_so_em_parte_da_estadia_cobra_o_valor_proporcional_dos_dias()
    {
        // período de 4 dias por 4.000 (1.000/dia); estadia de 2 desses dias + 1 dia de semana normal antes
        var inicioPeriodo = Datas.Segunda(5); var fimPeriodo = inicioPeriodo.AddDays(3);
        var periodo = PeriodoEspecialChacara.Criar(Guid.Empty, "Feriadão", inicioPeriodo, fimPeriodo, 4000m);

        var r = Calcular(inicioPeriodo.AddDays(-1), inicioPeriodo.AddDays(1), 30, periodos: [periodo]);

        // domingo anterior (diária de fim de semana 800) + 2 dias do feriadão (1.000 cada)
        Assert.False(r.PacoteFechado);
        Assert.Equal(800m + 2 * 1000m, r.ValorTotal);
    }

    [Fact]
    public void Fim_de_semana_encostado_em_periodo_especial_nao_forma_pacote_com_ele()
    {
        // sábado normal seguido de domingo que pertence a um período especial: o sábado é cobrado sozinho
        var sabado = Datas.Sabado(6);
        var periodo = PeriodoEspecialChacara.Criar(Guid.Empty, "Evento", sabado.AddDays(1), sabado.AddDays(1), 2000m);

        var r = Calcular(sabado, sabado.AddDays(1), 30, periodos: [periodo]);

        Assert.Equal(800m + 2000m, r.ValorTotal);
    }

    // ── Ajuste por horário de entrada/saída ──────────────────────────────────

    private static List<HorarioChacara> Horarios() =>
    [
        HorarioChacara.Criar(Guid.Empty, "entrada", "15:00", -40m, 0),
        HorarioChacara.Criar(Guid.Empty, "saida", "22:00", 100m, 0),
    ];

    [Fact]
    public void Ajustes_de_horario_somam_e_descontam_do_total()
    {
        var base_ = Calcular(Datas.Sexta(), Datas.Sabado(), 30);
        var r = CalculadoraPrecoChacara.AplicarAjustesDeHorario(base_, Horarios(), "15:00", "22:00");

        Assert.Equal(1400m - 40m + 100m, r.ValorTotal);
        Assert.Equal(base_.ValorEstadia, r.ValorEstadia);         // o ajuste entra só no total
        Assert.Equal(base_.Detalhamento.Count + 2, r.Detalhamento.Count);
    }

    [Fact]
    public void Horario_sem_ajuste_cadastrado_nao_muda_nada()
    {
        var base_ = Calcular(Datas.Sexta(), Datas.Sabado(), 30);
        Assert.Same(base_, CalculadoraPrecoChacara.AplicarAjustesDeHorario(base_, Horarios(), "09:00", null));
    }

    [Fact]
    public void Pacote_fechado_nao_recebe_ajuste_de_horario()
    {
        var ini = Datas.Sexta(4); var fim = ini.AddDays(2);
        var natal = PeriodoEspecialChacara.Criar(Guid.Empty, "Natal", ini, fim, 5500m);
        var pacote = Calcular(ini, fim, 30, periodos: [natal]);

        Assert.Same(pacote, CalculadoraPrecoChacara.AplicarAjustesDeHorario(pacote, Horarios(), "15:00", "22:00"));
    }

    [Fact]
    public void AjusteHorario_Obter_ignora_hora_vazia_e_tipo_errado()
    {
        var h = Horarios();
        Assert.Equal(-40m, AjusteHorarioChacaraService.Obter(h, "entrada", "15:00"));
        Assert.Equal(0m, AjusteHorarioChacaraService.Obter(h, "saida", "15:00")); // 15:00 só existe como entrada
        Assert.Equal(0m, AjusteHorarioChacaraService.Obter(h, "entrada", " "));
        Assert.Equal(0m, AjusteHorarioChacaraService.Obter(h, "entrada", null));
    }
}
