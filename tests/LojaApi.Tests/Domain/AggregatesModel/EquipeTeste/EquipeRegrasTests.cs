using LojaApi.Domain.Equipe;

namespace LojaApi.Tests.Domain.AggregatesModel.EquipeTeste;

public class EquipeRegrasTests
{
    // ── Remuneração ──────────────────────────────────────────────────────────

    [Theory]
    [InlineData("salario_fixo", null, null, false, null, "Informe o valor do salário fixo.")]
    [InlineData("salario_fixo", 0, null, false, null, "Informe o valor do salário fixo.")]
    [InlineData("diaria", null, null, false, null, "Informe o valor da diária.")]
    [InlineData("diaria", null, -5, false, null, "Informe o valor da diária.")]
    [InlineData("comissao", null, null, true, null, "Informe o percentual de comissão em vendas.")]
    [InlineData("comissao", null, null, true, 0, "Informe o percentual de comissão em vendas.")]
    [InlineData("salario_fixo", 3000, null, false, null, null)]
    [InlineData("diaria", null, 100, false, null, null)]
    [InlineData("comissao", null, null, false, null, null)]
    [InlineData("comissao", null, null, true, 5, null)]
    [InlineData("salario_fixo", 1, null, true, 1, null)]
    public void Valida_o_que_cada_tipo_de_remuneracao_exige(string tipo, int? salario, int? diaria, bool vendasAtiva, int? vendasPct, string? esperado) =>
        Assert.Equal(esperado, EquipeRegras.ValidarRemuneracao(tipo, (decimal?)salario, (decimal?)diaria, vendasAtiva, (decimal?)vendasPct));

    [Fact]
    public void Salario_diaria_e_percentual_de_vendas_so_valem_no_tipo_certo()
    {
        Assert.Equal(3000m, EquipeRegras.SalarioAplicavel("salario_fixo", 3000));
        Assert.Null(EquipeRegras.SalarioAplicavel("comissao", 3000));
        Assert.Null(EquipeRegras.SalarioAplicavel("diaria", 3000));
        Assert.Equal(100m, EquipeRegras.DiariaAplicavel("diaria", 100));
        Assert.Null(EquipeRegras.DiariaAplicavel("salario_fixo", 100));
        Assert.Equal(7m, EquipeRegras.PercentualVendasAplicavel(true, 7));
        Assert.Null(EquipeRegras.PercentualVendasAplicavel(false, 7));
    }

    [Theory]
    [InlineData("servico", "servico")]
    [InlineData("total", "total")]
    [InlineData("qualquer", "total")]
    [InlineData("Servico", "total")]
    [InlineData(null, "total")]
    public void Base_de_calculo_so_aceita_servico_ou_total(string? entrada, string esperado) =>
        Assert.Equal(esperado, EquipeRegras.BaseDeCalculo(entrada));

    [Theory]
    [InlineData(1, 1)]
    [InlineData(28, 28)]
    [InlineData(0, null)]
    [InlineData(29, null)]
    [InlineData(-3, null)]
    [InlineData(null, null)]
    public void Dia_de_pagamento_de_1_a_28(int? dia, int? esperado) => Assert.Equal(esperado, EquipeRegras.DiaValido(dia));

    [Theory]
    [InlineData(10, 10)]
    [InlineData(null, 5)]
    [InlineData(31, 5)]
    public void Lancamento_fixo_do_salario_usa_o_dia_5_sem_dia_valido(int? dia, int esperado) => Assert.Equal(esperado, EquipeRegras.DiaDoLancamentoFixo(dia));

    [Theory]
    [InlineData("  Rua A  ", 200, "Rua A")]
    [InlineData("   ", 200, null)]
    [InlineData("", 200, null)]
    [InlineData(null, 200, null)]
    [InlineData("123456789012", 9, "123456789")]
    public void Limpar_tira_espacos_zera_vazio_e_corta_no_tamanho(string? entrada, int max, string? esperado) =>
        Assert.Equal(esperado, EquipeRegras.Limpar(entrada, max));

    [Theory]
    [InlineData(" ms ", "MS")]
    [InlineData("sp", "SP")]
    [InlineData("mato grosso", "MA")]
    [InlineData("", null)]
    public void Uf_em_maiusculas_com_2_letras(string entrada, string? esperado) => Assert.Equal(esperado, EquipeRegras.Uf(entrada));

    // ── Fechamento ───────────────────────────────────────────────────────────

    [Fact]
    public void Fechamento_valida_as_diarias()
    {
        Assert.Equal("Quantidade de diárias inválida.", EquipeRegras.ValidarFechamento(-1, 100));
        Assert.Equal("Este profissional não tem valor de diária cadastrado.", EquipeRegras.ValidarFechamento(2, null));
        Assert.Equal("Este profissional não tem valor de diária cadastrado.", EquipeRegras.ValidarFechamento(2, 0));
        Assert.Null(EquipeRegras.ValidarFechamento(2, 80));
        Assert.Null(EquipeRegras.ValidarFechamento(0, null));   // sem diária, não precisa de valor
    }

    [Fact]
    public void Valor_das_diarias() { Assert.Equal(240m, EquipeRegras.ValorDasDiarias(3, 80)); Assert.Equal(0m, EquipeRegras.ValorDasDiarias(0, 80)); Assert.Equal(0m, EquipeRegras.ValorDasDiarias(2, null)); }

    [Fact]
    public void Periodo_vai_do_comeco_do_primeiro_dia_ao_fim_do_ultimo_em_utc()
    {
        var ini = new DateTime(2030, 6, 1, 15, 30, 0);
        var fim = new DateTime(2030, 6, 30, 8, 0, 0);
        Assert.Equal(new DateTime(2030, 6, 1, 0, 0, 0, DateTimeKind.Utc), EquipeRegras.InicioDoPeriodo(ini));
        Assert.Equal(new DateTime(2030, 7, 1, 0, 0, 0, DateTimeKind.Utc), EquipeRegras.FimDoPeriodoExclusivo(fim));   // 30/06 inteiro entra
        Assert.Equal(new DateTime(2030, 6, 30, 0, 0, 0, DateTimeKind.Utc), EquipeRegras.UltimoDiaDoPeriodo(fim));
        Assert.Equal(DateTimeKind.Utc, EquipeRegras.FimDoPeriodoExclusivo(fim).Kind);
    }

    [Fact]
    public void Vencimento_do_fechamento_e_o_informado_ou_5_dias_depois_do_fim_sempre_ao_meio_dia()
    {
        var fim = new DateTime(2030, 6, 30);
        Assert.Equal(new DateTime(2030, 7, 5, 12, 0, 0, DateTimeKind.Utc), EquipeRegras.VencimentoDoFechamento(null, fim));
        Assert.Equal(new DateTime(2030, 7, 20, 12, 0, 0, DateTimeKind.Utc), EquipeRegras.VencimentoDoFechamento(new DateTime(2030, 7, 20, 23, 0, 0), fim));
    }

    [Fact]
    public void Descricao_do_fechamento_cita_diarias_so_quando_ha()
    {
        var ini = new DateTime(2030, 6, 1); var fim = new DateTime(2030, 6, 30);
        Assert.Equal($"Comissão — Ana ({ini:dd/MM} a {fim:dd/MM})", EquipeRegras.DescricaoDoFechamento("Ana", 0, ini, fim));
        Assert.Equal($"Comissão + 2 diária(s) — Ana ({ini:dd/MM} a {fim:dd/MM})", EquipeRegras.DescricaoDoFechamento("Ana", 2, ini, fim));
    }

    [Theory]
    [InlineData("Gol 2015", "ABC1D23", "Ordem de Serviço — Gol 2015 · ABC1D23")]
    [InlineData("Gol 2015", null, "Ordem de Serviço — Gol 2015")]
    [InlineData(null, "ABC1D23", "Ordem de Serviço — ABC1D23")]
    [InlineData("  ", "", "Ordem de Serviço")]
    [InlineData(null, null, "Ordem de Serviço")]
    public void Nome_da_comissao_de_ordem_de_servico(string? veiculo, string? placa, string esperado) =>
        Assert.Equal(esperado, EquipeRegras.NomeDaOrdemDeServico(veiculo, placa));
}
