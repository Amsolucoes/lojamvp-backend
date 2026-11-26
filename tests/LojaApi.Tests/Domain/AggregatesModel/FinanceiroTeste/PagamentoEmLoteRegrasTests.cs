using LojaApi.Domain.Financeiro;

namespace LojaApi.Tests.Domain.AggregatesModel.FinanceiroTeste;

public class PagamentoEmLoteRegrasTests
{
    private static readonly DateTime Agora = new(2032, 5, 20, 15, 30, 45, DateTimeKind.Utc);

    // ── Acréscimo (juros/multa pago a mais) ──────────────────────────────────

    [Theory]
    [InlineData(-0.01)]
    [InlineData(-1)]
    public void Acrescimo_negativo_e_recusado(double valor) =>
        Assert.Equal("O juros/multa não pode ser negativo.", PagamentoEmLoteRegras.ValidarAcrescimo((decimal)valor));

    [Theory]
    [InlineData(0)]
    [InlineData(0.01)]
    [InlineData(500)]
    public void Acrescimo_zero_ou_positivo_e_aceito(double valor) =>
        Assert.Null(PagamentoEmLoteRegras.ValidarAcrescimo((decimal)valor));

    [Theory]
    [InlineData("7.5", "7.50")]
    [InlineData("1.005", "1.00")]   // arredondamento do .NET (para o par): 1,005 -> 1,00
    [InlineData("1.015", "1.02")]
    [InlineData("1.006", "1.01")]
    [InlineData("3", "3")]
    public void Acrescimo_e_arredondado_em_duas_casas(string valor, string esperado) =>
        Assert.Equal(decimal.Parse(esperado, System.Globalization.CultureInfo.InvariantCulture),
            PagamentoEmLoteRegras.ArredondarValor(decimal.Parse(valor, System.Globalization.CultureInfo.InvariantCulture)));

    [Fact]
    public void Descricao_do_acrescimo_de_uma_conta_leva_o_nome_dela()
    {
        Assert.Equal("Juros/multa - Energia", PagamentoEmLoteRegras.DescricaoDoAcrescimo(["Energia"]));
    }

    [Theory]
    [InlineData(2)]
    [InlineData(5)]
    public void Descricao_do_acrescimo_de_varias_contas_leva_so_a_quantidade(int quantas)
    {
        var descricoes = Enumerable.Range(1, quantas).Select(i => $"Conta {i}").ToList();

        Assert.Equal($"Juros/multa - {quantas} contas", PagamentoEmLoteRegras.DescricaoDoAcrescimo(descricoes));
    }

    [Fact]
    public void Descricao_do_acrescimo_e_cortada_em_150_caracteres()
    {
        var r = PagamentoEmLoteRegras.DescricaoDoAcrescimo([new string('x', 200)]);

        Assert.Equal(150, r.Length);
        Assert.StartsWith("Juros/multa - xxx", r);
    }

    [Fact]
    public void Descricao_do_acrescimo_com_exatamente_150_caracteres_nao_e_cortada()
    {
        var nome = new string('x', 150 - "Juros/multa - ".Length);

        var r = PagamentoEmLoteRegras.DescricaoDoAcrescimo([nome]);

        Assert.Equal(150, r.Length);
        Assert.EndsWith(nome, r);
    }

    [Fact]
    public void Descricao_do_acrescimo_com_151_caracteres_perde_o_ultimo()
    {
        var nome = new string('x', 151 - "Juros/multa - ".Length);

        var r = PagamentoEmLoteRegras.DescricaoDoAcrescimo([nome]);

        Assert.Equal(150, r.Length);
        Assert.Equal(("Juros/multa - " + nome)[..150], r);
    }

    // ── Forma e datas ────────────────────────────────────────────────────────

    [Fact]
    public void Forma_de_pagamento_so_vale_quando_paga_pela_conta()
    {
        Assert.Equal("pix", PagamentoEmLoteRegras.FormaDePagamentoGravada("conta", "pix"));
        Assert.Null(PagamentoEmLoteRegras.FormaDePagamentoGravada("conta", null));
        Assert.Null(PagamentoEmLoteRegras.FormaDePagamentoGravada("cartao", "pix"));
        Assert.Null(PagamentoEmLoteRegras.FormaDePagamentoGravada("outra", "pix"));
    }

    [Fact]
    public void Data_base_sem_data_usa_agora_ao_meio_dia()
    {
        Assert.Equal(new DateTime(2032, 5, 20, 12, 0, 0, DateTimeKind.Utc), PagamentoEmLoteRegras.DataBase(null, Agora));
    }

    [Fact]
    public void Data_base_com_data_usa_o_dia_pedido_ao_meio_dia()
    {
        var r = PagamentoEmLoteRegras.DataBase(new DateTime(2032, 6, 1, 23, 59, 0), Agora);

        Assert.Equal(new DateTime(2032, 6, 1, 12, 0, 0, DateTimeKind.Utc), r);
    }

    [Fact]
    public void Pago_em_sem_data_guarda_o_instante_exato_de_agora()
    {
        Assert.Equal(Agora, PagamentoEmLoteRegras.PagoEm(null, Agora));
    }

    [Fact]
    public void Pago_em_com_data_vai_ao_meio_dia_do_dia_pedido()
    {
        var r = PagamentoEmLoteRegras.PagoEm(new DateTime(2032, 6, 1, 3, 0, 0), Agora);

        Assert.Equal(new DateTime(2032, 6, 1, 12, 0, 0, DateTimeKind.Utc), r);
    }

    // ── Divisão do pagamento em mais de uma conta ────────────────────────────

    [Fact]
    public void Sem_divisao_nao_ha_o_que_validar_em_nenhuma_forma()
    {
        Assert.Null(PagamentoEmLoteRegras.ValidarDivisao("conta", 0, 3, "parcelada"));
        Assert.Null(PagamentoEmLoteRegras.ValidarDivisao("cartao", 0, 3, "parcelada"));
    }

    [Fact]
    public void Divisao_so_vale_para_pagar_pela_conta()
    {
        Assert.Equal("A divisão do pagamento só vale para pagar pela conta.", PagamentoEmLoteRegras.ValidarDivisao("cartao", 2, 1, "avulsa"));
        Assert.Equal("A divisão do pagamento só vale para pagar pela conta.", PagamentoEmLoteRegras.ValidarDivisao("outra", 2, 1, "avulsa"));
    }

    [Fact]
    public void Divisao_precisa_de_ao_menos_duas_contas()
    {
        Assert.Equal("Para dividir, informe ao menos duas contas.", PagamentoEmLoteRegras.ValidarDivisao("conta", 1, 1, "avulsa"));
        Assert.Null(PagamentoEmLoteRegras.ValidarDivisao("conta", 2, 1, "avulsa"));
        Assert.Null(PagamentoEmLoteRegras.ValidarDivisao("conta", 3, 1, "avulsa"));
    }

    [Theory]
    [InlineData(2, "avulsa")]
    [InlineData(0, "avulsa")]
    [InlineData(1, "parcelada")]
    [InlineData(1, "fixa")]
    [InlineData(1, null)]
    public void Divisao_so_vale_para_uma_unica_conta_avulsa(int quantosLancamentos, string? modo) =>
        Assert.Equal("Só é possível dividir o pagamento de uma conta avulsa por vez.", PagamentoEmLoteRegras.ValidarDivisao("conta", 2, quantosLancamentos, modo));

    [Fact]
    public void Na_divisao_o_erro_da_forma_vem_antes_do_da_quantidade_e_este_antes_do_do_tipo()
    {
        Assert.Equal("A divisão do pagamento só vale para pagar pela conta.", PagamentoEmLoteRegras.ValidarDivisao("cartao", 1, 2, "parcelada"));
        Assert.Equal("Para dividir, informe ao menos duas contas.", PagamentoEmLoteRegras.ValidarDivisao("conta", 1, 2, "parcelada"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Valor_de_cada_parte_precisa_ser_maior_que_zero(double valor) =>
        Assert.Equal("O valor de cada parte deve ser maior que zero.", PagamentoEmLoteRegras.ValidarValorDaParte((decimal)valor));

    [Fact]
    public void Valor_de_parte_positivo_e_aceito() =>
        Assert.Null(PagamentoEmLoteRegras.ValidarValorDaParte(0.01m));

    [Fact]
    public void Soma_das_partes_igual_ao_valor_da_conta_e_aceita() =>
        Assert.Null(PagamentoEmLoteRegras.ValidarSomaDasPartes([60m, 40m], 100m));

    [Fact]
    public void Soma_das_partes_diferente_informa_o_valor_da_conta_com_duas_casas_e_ponto()
    {
        Assert.Equal("A soma das partes deve ser igual ao valor da conta (100.50).", PagamentoEmLoteRegras.ValidarSomaDasPartes([50m, 40m], 100.5m));
        Assert.Equal("A soma das partes deve ser igual ao valor da conta (100.00).", PagamentoEmLoteRegras.ValidarSomaDasPartes([60m, 40.01m], 100m));
    }

    [Fact]
    public void Soma_um_centavo_abaixo_tambem_e_recusada() =>
        Assert.NotNull(PagamentoEmLoteRegras.ValidarSomaDasPartes([60m, 39.99m], 100m));

    [Fact]
    public void Descricao_da_parte_leva_o_numero_e_o_total()
    {
        Assert.Equal("Fornecedor (parte 1/2)", PagamentoEmLoteRegras.DescricaoDaParte("Fornecedor", 1, 2));
        Assert.Equal("Fornecedor (parte 3/3)", PagamentoEmLoteRegras.DescricaoDaParte("Fornecedor", 3, 3));
    }

    [Fact]
    public void Descricao_da_parte_e_cortada_em_150_caracteres()
    {
        var r = PagamentoEmLoteRegras.DescricaoDaParte(new string('x', 150), 1, 2);

        Assert.Equal(150, r.Length);
        Assert.Equal(new string('x', 150), r);
    }

    [Fact]
    public void Descricao_da_parte_com_exatamente_150_caracteres_nao_e_cortada()
    {
        var baseCurta = new string('x', 150 - " (parte 1/2)".Length);

        var r = PagamentoEmLoteRegras.DescricaoDaParte(baseCurta, 1, 2);

        Assert.Equal(baseCurta + " (parte 1/2)", r);
        Assert.Equal(150, r.Length);
    }

    [Fact]
    public void Descricao_da_parte_com_151_caracteres_perde_o_ultimo()
    {
        var baseLonga = new string('x', 151 - " (parte 1/2)".Length);

        var r = PagamentoEmLoteRegras.DescricaoDaParte(baseLonga, 1, 2);

        Assert.Equal(150, r.Length);
        Assert.Equal((baseLonga + " (parte 1/2)")[..150], r);
    }

    // ── Pagamento no cartão ──────────────────────────────────────────────────

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(25)]
    [InlineData(100)]
    public void Parcelas_no_cartao_fora_de_1_a_24_sao_recusadas(int parcelas) =>
        Assert.Equal("Escolha entre 1 e 24 parcelas.", PagamentoEmLoteRegras.ValidarParcelasNoCartao(parcelas));

    [Theory]
    [InlineData(1)]
    [InlineData(12)]
    [InlineData(24)]
    public void Parcelas_no_cartao_de_1_a_24_sao_aceitas(int parcelas) =>
        Assert.Null(PagamentoEmLoteRegras.ValidarParcelasNoCartao(parcelas));

    [Fact]
    public void A_vista_no_cartao_vira_uma_compra_avulsa_com_a_descricao_original()
    {
        var dataBase = new DateTime(2032, 5, 20, 12, 0, 0, DateTimeKind.Utc);

        var r = Assert.Single(PagamentoEmLoteRegras.ParcelasDoCartao("Sofá", 90m, 1, dataBase));

        Assert.Equal(("Sofá", 90m, dataBase, false, (int?)null, (int?)null), (r.Descricao, r.Valor, r.DataCompra, r.Parcelada, r.NumeroParcela, r.TotalParcelas));
    }

    [Fact]
    public void Parcelado_no_cartao_numera_as_parcelas_move_a_data_mes_a_mes_e_a_ultima_absorve_os_centavos()
    {
        var dataBase = new DateTime(2032, 5, 31, 12, 0, 0, DateTimeKind.Utc);

        var r = PagamentoEmLoteRegras.ParcelasDoCartao("Sofá", 100m, 3, dataBase);

        Assert.Equal(["Sofá (1/3)", "Sofá (2/3)", "Sofá (3/3)"], r.Select(p => p.Descricao));
        Assert.Equal([33.33m, 33.33m, 33.34m], r.Select(p => p.Valor));
        Assert.Equal([dataBase, new DateTime(2032, 6, 30, 12, 0, 0, DateTimeKind.Utc), new DateTime(2032, 7, 31, 12, 0, 0, DateTimeKind.Utc)], r.Select(p => p.DataCompra));
        Assert.Equal([1, 2, 3], r.Select(p => p.NumeroParcela));
        Assert.All(r, p => Assert.Equal((true, (int?)3), (p.Parcelada, p.TotalParcelas)));
        Assert.Equal(100m, r.Sum(p => p.Valor));
    }

    [Fact]
    public void Parcelado_em_duas_vezes_divide_ao_meio()
    {
        var r = PagamentoEmLoteRegras.ParcelasDoCartao("Mesa", 60m, 2, new DateTime(2032, 5, 20, 12, 0, 0, DateTimeKind.Utc));

        Assert.Equal([30m, 30m], r.Select(p => p.Valor));
        Assert.Equal(2, r.Count);
    }
}
