using LojaApi.Domain.Carteira;

namespace LojaApi.Tests.Domain.CarteiraTeste;

public class RegrasCarteiraTests
{
    private static readonly DateTime Hoje = new(2030, 6, 15, 10, 0, 0, DateTimeKind.Utc);

    // ── Comprar na carteira ──────────────────────────────────────────────────

    [Fact]
    public void Sem_limite_qualquer_valor_pode_ir_para_a_carteira() =>
        Assert.Null(RegrasCarteira.ValidarCompra(saldoAtual: 5000m, valor: 100000m, limite: null));

    [Theory]
    [InlineData(0, 100, 100, true)]     // chega exatamente no limite: pode
    [InlineData(0, 100.01, 100, false)]
    [InlineData(60, 40, 100, true)]     // 60 já devidos + 40 = 100
    [InlineData(60, 40.01, 100, false)]
    [InlineData(150, 1, 100, false)]    // já estourado (limite foi reduzido depois)
    [InlineData(0, 1, 0, false)]        // limite zero = não vende na carteira
    public void Limite_considera_o_que_o_cliente_ja_deve(double saldo, double valor, double limite, bool permitido)
    {
        var erro = RegrasCarteira.ValidarCompra((decimal)saldo, (decimal)valor, (decimal)limite);
        Assert.Equal(permitido, erro is null);
    }

    [Fact]
    public void Mensagem_do_limite_mostra_limite_saldo_e_disponivel()
    {
        var erro = RegrasCarteira.ValidarCompra(60m, 50m, 100m)!;
        Assert.Contains("R$ 100,00", erro);   // limite
        Assert.Contains("R$ 60,00", erro);    // já deve
        Assert.Contains("R$ 40,00", erro);    // disponível
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void Compra_exige_valor_positivo(int valor) =>
        Assert.Equal("Informe um valor maior que zero.", RegrasCarteira.ValidarCompra(0, valor, null));

    [Theory]
    [InlineData(null, true)]
    [InlineData(0.0, true)]
    [InlineData(500.0, true)]
    [InlineData(-1.0, false)]
    public void Limite_nao_pode_ser_negativo(double? limite, bool valido) =>
        Assert.Equal(valido, RegrasCarteira.ValidarLimite((decimal?)limite) is null);

    // ── Receber o pagamento ──────────────────────────────────────────────────

    [Theory]
    [InlineData("dinheiro")]
    [InlineData("pix")]
    [InlineData("credito")]
    [InlineData("debito")]
    public void Recebimento_aceita_as_formas_do_caixa(string forma) =>
        Assert.Null(RegrasCarteira.ValidarRecebimento(100m, 50m, forma, Hoje, Hoje));

    [Theory]
    [InlineData("carteira")]    // não dá para pagar a carteira com a própria carteira
    [InlineData("boleto")]
    [InlineData("")]
    [InlineData(null)]
    public void Recebimento_recusa_forma_invalida(string? forma) =>
        Assert.Equal("Forma de pagamento inválida.", RegrasCarteira.ValidarRecebimento(100m, 50m, forma, Hoje, Hoje));

    [Fact]
    public void Recebimento_nao_pode_passar_do_saldo()
    {
        Assert.Null(RegrasCarteira.ValidarRecebimento(100m, 100m, "pix", Hoje, Hoje));           // quitar tudo
        var erro = RegrasCarteira.ValidarRecebimento(100m, 100.01m, "pix", Hoje, Hoje)!;
        Assert.Equal("O cliente deve apenas R$ 100,00 na carteira.", erro);
    }

    [Fact]
    public void Recebimento_sem_divida_e_recusado() =>
        Assert.NotNull(RegrasCarteira.ValidarRecebimento(0m, 10m, "pix", Hoje, Hoje));

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Recebimento_exige_valor_positivo(int valor) =>
        Assert.Equal("Informe um valor maior que zero.", RegrasCarteira.ValidarRecebimento(100m, valor, "pix", Hoje, Hoje));

    [Fact]
    public void Recebimento_aceita_data_passada_mas_nao_futura()
    {
        Assert.Null(RegrasCarteira.ValidarRecebimento(100m, 10m, "pix", Hoje.AddDays(-30), Hoje));
        Assert.Null(RegrasCarteira.ValidarRecebimento(100m, 10m, "pix", Hoje.Date.AddHours(23), Hoje));   // mesmo dia
        Assert.Equal("A data não pode ser no futuro.", RegrasCarteira.ValidarRecebimento(100m, 10m, "pix", Hoje.AddDays(1), Hoje));
    }

    // ── Quanto da venda foi para a carteira ──────────────────────────────────

    [Fact]
    public void Venda_toda_na_carteira_pelo_JSON() =>
        Assert.Equal(120m, RegrasCarteira.ValorNaCarteira("[{\"forma\":\"carteira\",\"valor\":120,\"parcelas\":1}]", "carteira", 120m));

    [Fact]
    public void Venda_dividida_so_conta_a_parte_da_carteira() =>
        Assert.Equal(70m, RegrasCarteira.ValorNaCarteira(
            "[{\"forma\":\"pix\",\"valor\":50,\"parcelas\":1},{\"forma\":\"carteira\",\"valor\":70,\"parcelas\":1}]", "pix", 120m));

    [Fact]
    public void Venda_sem_carteira_devolve_zero() =>
        Assert.Equal(0m, RegrasCarteira.ValorNaCarteira("[{\"forma\":\"pix\",\"valor\":120}]", "pix", 120m));

    [Theory]
    [InlineData(null, "carteira", 80, 80)]   // sem JSON: vale a forma principal
    [InlineData("", "carteira", 80, 80)]
    [InlineData("não é json", "carteira", 80, 80)]
    [InlineData("não é json", "pix", 80, 0)]
    [InlineData(null, "dinheiro", 80, 0)]
    [InlineData("{\"forma\":\"carteira\"}", "carteira", 80, 80)]   // JSON que não é lista: ignora
    public void Sem_JSON_legivel_vale_a_forma_principal(string? json, string principal, double devido, double esperado) =>
        Assert.Equal((decimal)esperado, RegrasCarteira.ValorNaCarteira(json, principal, (decimal)devido));

    [Fact]
    public void JSON_com_itens_quebrados_nao_derruba_o_calculo() =>
        Assert.Equal(30m, RegrasCarteira.ValorNaCarteira("[1,{\"valor\":5},{\"forma\":\"carteira\"},{\"forma\":\"carteira\",\"valor\":30}]", "pix", 100m));

    // ── Compras em aberto (os pagamentos abatem as mais antigas primeiro) ────

    private static (Guid, DateTime, decimal) Compra(int dia, decimal valor) => (Guid.NewGuid(), new DateTime(2030, 6, dia), valor);

    [Fact]
    public void Sem_pagamento_todas_as_compras_estao_em_aberto()
    {
        var r = RegrasCarteira.ComprasEmAberto([Compra(1, 100m), Compra(2, 50m)], 0m);
        Assert.Equal([100m, 50m], r.Select(c => c.EmAberto));
    }

    [Fact]
    public void Pagamento_abate_a_compra_mais_antiga_primeiro()
    {
        var c1 = Compra(1, 100m); var c2 = Compra(5, 50m);
        var r = RegrasCarteira.ComprasEmAberto([c2, c1], 120m);   // fora de ordem de propósito

        Assert.Single(r);
        Assert.Equal(c2.Item1, r[0].VendaId);     // a de 100 quitou (100) e a de 50 ficou com 30 restantes
        Assert.Equal(30m, r[0].EmAberto);
        Assert.Equal(50m, r[0].Valor);
    }

    [Fact]
    public void Pagamento_parcial_deixa_o_restante_da_compra_em_aberto()
    {
        var r = RegrasCarteira.ComprasEmAberto([Compra(1, 100m)], 40m);
        Assert.Equal(60m, r.Single().EmAberto);
    }

    [Fact]
    public void Tudo_pago_nao_deixa_nada_em_aberto() =>
        Assert.Empty(RegrasCarteira.ComprasEmAberto([Compra(1, 100m), Compra(2, 50m)], 150m));
}

public class MovimentoCarteiraTests
{
    [Fact]
    public void Compra_nasce_com_tipo_venda_e_valor()
    {
        var loja = Guid.NewGuid(); var cliente = Guid.NewGuid(); var venda = Guid.NewGuid();
        var m = MovimentoCarteira.DeCompra(loja, cliente, venda, 80m, new DateTime(2030, 6, 1));

        Assert.True(m.EhCompra());
        Assert.Equal((loja, cliente, venda, 80m), (m.LojaId, m.ClienteId, m.VendaId, m.Valor));
        Assert.Null(m.FormaPagamento);
    }

    [Fact]
    public void Pagamento_guarda_forma_observacao_e_a_entrada_do_caixa()
    {
        var caixa = Guid.NewGuid();
        var m = MovimentoCarteira.DePagamento(Guid.NewGuid(), Guid.NewGuid(), 50m, "pix", "  fim do mês ", new DateTime(2030, 6, 30), caixa);

        Assert.False(m.EhCompra());
        Assert.Equal("pix", m.FormaPagamento);
        Assert.Equal("fim do mês", m.Observacao);
        Assert.Equal(caixa, m.MovimentoCaixaId);
        Assert.Null(m.VendaId);
    }

    [Fact]
    public void Observacao_em_branco_vira_nula()
    {
        var m = MovimentoCarteira.DePagamento(Guid.NewGuid(), Guid.NewGuid(), 50m, "pix", "   ", DateTime.UtcNow, Guid.NewGuid());
        Assert.Null(m.Observacao);
    }
}

public class AlocacaoDePagamentosTests
{
    private static RegrasCarteira.CompraParaAlocar Compra(int dia, decimal valor, Guid? id = null) =>
        new(id ?? Guid.NewGuid(), new DateTime(2030, 6, dia, 15, 0, 0, DateTimeKind.Utc), new DateTime(2030, 6, dia, 15, 0, 0, DateTimeKind.Utc), valor);

    private static RegrasCarteira.PagamentoParaAlocar Pagamento(int dia, decimal valor, string forma = "pix", Guid? id = null) =>
        new(id ?? Guid.NewGuid(), new DateTime(2030, 6, dia, 12, 0, 0, DateTimeKind.Utc), new DateTime(2030, 6, dia, 18, 0, 0, DateTimeKind.Utc), valor, forma);

    [Fact]
    public void Pagamento_que_cobre_a_compra_inteira_quita_e_guarda_a_forma()
    {
        var c = Compra(1, 100m);
        var pg = Pagamento(1, 100m, "pix");

        var (compras, pagamentos) = RegrasCarteira.AlocarPagamentos([c], [pg]);

        Assert.Equal(0m, compras.Single().EmAberto);
        var parte = compras.Single().Pagamentos.Single();
        Assert.Equal(("pix", 100m), (parte.Forma, parte.Valor));
        Assert.Equal((c.VendaId, 100m), pagamentos.Single().Abateu.Single());
    }

    [Fact]
    public void Pagamento_parcial_deixa_o_resto_em_aberto()
    {
        var (compras, _) = RegrasCarteira.AlocarPagamentos([Compra(1, 100m)], [Pagamento(2, 30m, "dinheiro")]);
        Assert.Equal(70m, compras.Single().EmAberto);
        Assert.Equal(30m, compras.Single().Pagamentos.Single().Valor);
    }

    [Fact]
    public void Pagamento_maior_que_a_primeira_compra_passa_para_a_proxima()
    {
        var a = Compra(1, 100m); var b = Compra(5, 50m);
        var pg = Pagamento(10, 120m, "pix");

        var (compras, pagamentos) = RegrasCarteira.AlocarPagamentos([b, a], [pg]);   // fora de ordem de propósito

        Assert.Equal(0m, compras.Single(x => x.VendaId == a.VendaId).EmAberto);
        Assert.Equal(30m, compras.Single(x => x.VendaId == b.VendaId).EmAberto);     // 50 - 20
        Assert.Equal([(a.VendaId, 100m), (b.VendaId, 20m)], pagamentos.Single().Abateu);
    }

    [Fact]
    public void Varios_pagamentos_com_formas_diferentes_na_mesma_compra()
    {
        var c = Compra(1, 100m);
        var (compras, _) = RegrasCarteira.AlocarPagamentos([c], [Pagamento(2, 40m, "dinheiro"), Pagamento(3, 60m, "pix")]);

        var partes = compras.Single().Pagamentos;
        Assert.Equal(0m, compras.Single().EmAberto);
        Assert.Equal([("dinheiro", 40m), ("pix", 60m)], partes.Select(p => (p.Forma, p.Valor)));
    }

    [Fact]
    public void Sem_pagamentos_tudo_continua_em_aberto()
    {
        var (compras, pagamentos) = RegrasCarteira.AlocarPagamentos([Compra(1, 100m), Compra(2, 50m)], []);
        Assert.Equal([100m, 50m], compras.Select(c => c.EmAberto));
        Assert.Empty(pagamentos);
    }

    [Fact]
    public void Pagamento_de_hoje_ao_meio_dia_e_compra_de_hoje_a_tarde_ainda_se_encontram()
    {
        // o pagamento é gravado ao meio-dia do dia; a compra tem a hora exata (depois do meio-dia): a ordem é por dia
        var (compras, _) = RegrasCarteira.AlocarPagamentos([Compra(3, 100m)], [Pagamento(3, 100m)]);
        Assert.Equal(0m, compras.Single().EmAberto);
    }
}
