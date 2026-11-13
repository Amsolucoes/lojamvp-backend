using LojaApi.Domain.Chacara;

namespace LojaApi.Tests.Domain.Entities.ChacaraTeste;

public class ConfiguracaoPrecoChacaraTests
{
    [Fact]
    public void Valores_padrao()
    {
        var c = new ConfiguracaoPrecoChacara();
        Assert.Equal(3, c.MinimoPessoas);
        Assert.Equal(50, c.LimitePessoasParaTaxaLimpeza);
        Assert.Equal(250m, c.ValorTaxaLimpeza);
        Assert.Equal(5m, c.PercentualTaxaCartao);
    }

    [Theory]
    [InlineData(800, 2)]   // no limite ainda vale "até o limite"
    [InlineData(100, 2)]
    [InlineData(800.01, 3)]
    [InlineData(5000, 3)]
    public void ParcelasMaximas_depende_do_valor(double valor, int esperado) =>
        Assert.Equal(esperado, new ConfiguracaoPrecoChacara().ParcelasMaximas((decimal)valor));

    [Fact]
    public void ComTaxaDoCartao_repassa_o_percentual_e_arredonda_em_2_casas()
    {
        Assert.Equal(105m, new ConfiguracaoPrecoChacara { PercentualTaxaCartao = 5m }.ComTaxaDoCartao(100m));
        Assert.Equal(100m, new ConfiguracaoPrecoChacara { PercentualTaxaCartao = 0m }.ComTaxaDoCartao(100m));
        Assert.Equal(1076.67m, new ConfiguracaoPrecoChacara { PercentualTaxaCartao = 7.5m }.ComTaxaDoCartao(1001.55m));
    }

    [Theory]
    [InlineData(null, null, null, true)]
    [InlineData(0.0, 1, 12, true)]
    [InlineData(15.0, 1, 12, true)]
    [InlineData(-0.1, null, null, false)]
    [InlineData(15.1, null, null, false)]
    [InlineData(null, 0, null, false)]
    [InlineData(null, null, 13, false)]
    [InlineData(5.0, 13, 3, false)]
    public void ValidarCartao_limites(double? taxa, int? ate, int? acima, bool valido)
    {
        var erro = ConfiguracaoPrecoChacara.ValidarCartao((decimal?)taxa, ate, acima);
        Assert.Equal(valido, erro is null);
    }

    [Fact]
    public void Atualizar_campos_opcionais_que_nao_vieram_mantem_o_valor_atual()
    {
        var c = new ConfiguracaoPrecoChacara { PercentualTaxaCartao = 5m, ValorLimiteParcelas = 800m, ParcelasAteLimite = 2, ParcelasAcimaLimite = 3 };

        c.Atualizar(4, 60, 300m, 100m, 40m, percentualTaxaCartao: null, valorLimiteParcelas: null, parcelasAteLimite: null, parcelasAcimaLimite: null);

        Assert.Equal(4, c.MinimoPessoas);
        Assert.Equal(60, c.LimitePessoasParaTaxaLimpeza);
        Assert.Equal(300m, c.ValorTaxaLimpeza);
        Assert.Equal(100m, c.ValorMultaNaoLimpeza);
        Assert.Equal(40m, c.PercentualEntradaMinimo);
        Assert.Equal(5m, c.PercentualTaxaCartao);
        Assert.Equal(800m, c.ValorLimiteParcelas);
        Assert.Equal(2, c.ParcelasAteLimite);
        Assert.Equal(3, c.ParcelasAcimaLimite);
    }

    [Fact]
    public void Atualizar_com_campos_opcionais_troca_os_valores()
    {
        var c = new ConfiguracaoPrecoChacara();
        c.Atualizar(5, 70, 350m, 120m, 30m, 7.5m, 900m, 3, 6);
        Assert.Equal(7.5m, c.PercentualTaxaCartao);
        Assert.Equal(900m, c.ValorLimiteParcelas);
        Assert.Equal(3, c.ParcelasAteLimite);
        Assert.Equal(6, c.ParcelasAcimaLimite);
    }
}

public class HorarioPeriodoFaixaTests
{
    [Theory]
    [InlineData("entrada", "00:00")]
    [InlineData("entrada", "08:30")]
    [InlineData("saida", "23:59")]
    public void Horario_valido(string tipo, string hora) => Assert.Null(HorarioChacara.Validar(tipo, hora));

    [Theory]
    [InlineData("xxx", "08:00", "Tipo deve ser 'entrada' ou 'saida'.")]
    [InlineData("Entrada", "08:00", "Tipo deve ser 'entrada' ou 'saida'.")]
    [InlineData("entrada", "24:00", "Informe um horário válido (HH:mm).")]
    [InlineData("entrada", "8:00", "Informe um horário válido (HH:mm).")]
    [InlineData("entrada", "08:60", "Informe um horário válido (HH:mm).")]
    [InlineData("saida", "", "Informe um horário válido (HH:mm).")]
    [InlineData("saida", "08:00 ", "Informe um horário válido (HH:mm).")]
    public void Horario_invalido(string tipo, string hora, string erro) => Assert.Equal(erro, HorarioChacara.Validar(tipo, hora));

    [Fact]
    public void Periodo_especial_exige_data_final_depois_da_inicial()
    {
        var d = new DateTime(2030, 12, 24);
        Assert.Null(PeriodoEspecialChacara.ValidarDatas(d, d));            // mesmo dia vale
        Assert.Null(PeriodoEspecialChacara.ValidarDatas(d, d.AddDays(2)));
        Assert.Equal("Data final não pode ser antes da data inicial.", PeriodoEspecialChacara.ValidarDatas(d, d.AddDays(-1)));
    }

    [Fact]
    public void Periodo_especial_criar_e_atualizar_normalizam_nome_e_datas()
    {
        var p = PeriodoEspecialChacara.Criar(Guid.NewGuid(), "  Natal ", new DateTime(2030, 12, 24, 3, 0, 0), new DateTime(2030, 12, 26), 5000m);
        Assert.Equal("Natal", p.Nome);
        Assert.Equal(new DateTime(2030, 12, 24, 12, 0, 0, DateTimeKind.Utc), p.DataInicio);
        Assert.Equal(new DateTime(2030, 12, 26, 12, 0, 0, DateTimeKind.Utc), p.DataFim);

        p.Atualizar(" Natal 2030 ", new DateTime(2030, 12, 23), new DateTime(2030, 12, 26), 5500m);
        Assert.Equal("Natal 2030", p.Nome);
        Assert.Equal(5500m, p.ValorTotal);
    }

    [Fact]
    public void Faixa_criar_e_atualizar()
    {
        var f = FaixaPrecoChacara.Criar(Guid.NewGuid(), 50, 500m, 800m, 1400m);
        Assert.Equal((50, 500m, 800m, 1400m), (f.PessoasAte, f.ValorDiariaSemana, f.ValorDiariaFimSemana, f.ValorPacote2DiasFimSemana));

        f.Atualizar(120, 710m, 1010m, 1810m);
        Assert.Equal((120, 710m, 1010m, 1810m), (f.PessoasAte, f.ValorDiariaSemana, f.ValorDiariaFimSemana, f.ValorPacote2DiasFimSemana));
    }

    [Fact]
    public void DiaDeReserva_fixa_meio_dia_UTC_e_nao_vira_o_dia()
    {
        var local = new DateTime(2030, 6, 1, 23, 59, 0, DateTimeKind.Local);
        var r = DiaDeReserva.MeioDiaUtc(local);
        Assert.Equal(new DateTime(2030, 6, 1, 12, 0, 0, DateTimeKind.Utc), r);
    }

    [Theory]
    [InlineData("pix", true)]
    [InlineData("cartao", true)]
    [InlineData("combinado", true)]
    [InlineData("sem_pagamento_online", false)]
    [InlineData("boleto", false)]
    [InlineData(null, false)]
    public void FormaPagamento_valida(string? forma, bool esperado) => Assert.Equal(esperado, FormaPagamentoReserva.Valida(forma));
}
