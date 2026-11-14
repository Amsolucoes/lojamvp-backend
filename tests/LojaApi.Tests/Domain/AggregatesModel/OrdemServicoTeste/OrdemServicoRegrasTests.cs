using LojaApi.Domain.OrdemServico;

namespace LojaApi.Tests.Domain.AggregatesModel.OrdemServicoTeste;

// Regras puras de OrdemServico: estados da ordem, itens, datas, pagamento, comissão dos mecânicos e patrimônio (sem banco, sem HTTP).
public class OrdemServicoRegrasTests
{
    [Theory]
    [InlineData("peca", true)]
    [InlineData("servico", true)]
    [InlineData("Peca", false)]
    [InlineData("outro", false)]
    [InlineData("", false)]
    public void Tipo_de_item_so_aceita_peca_ou_servico(string tipo, bool esperado) =>
        Assert.Equal(esperado, OrdemServicoRegras.TipoDeItemValido(tipo));

    [Theory]
    [InlineData("abc1d23", "ABC1D23")]
    [InlineData("  abc1d23  ", "ABC1D23")]
    [InlineData("", null)]
    [InlineData("   ", null)]
    [InlineData(null, null)]
    public void Placa_e_aparada_em_maiusculas_e_em_branco_vira_nula(string? placa, string? esperado) =>
        Assert.Equal(esperado, OrdemServicoRegras.NormalizarPlaca(placa));

    [Theory]
    [InlineData(15, 10, 15)]
    [InlineData(0, 10, 10)]
    [InlineData(0, null, 0)]
    [InlineData(-5, 8, 8)]
    public void Percentual_do_mecanico_usa_o_informado_ou_o_padrao_do_cadastro(int informado, int? padrao, int esperado) =>
        Assert.Equal(esperado, OrdemServicoRegras.PercentualDoMecanico(informado, padrao));

    [Fact]
    public void Subtotal_do_item_e_quantidade_vezes_valor() =>
        Assert.Equal(75m, OrdemServicoRegras.SubtotalDoItem(3, 25m));

    [Theory]
    [InlineData("pendente", true, true, true, false, false, false, true, false)]
    [InlineData("em_andamento", true, false, true, false, true, false, false, true)]
    [InlineData("concluido", false, false, false, false, false, true, false, true)]
    [InlineData("cancelado", false, false, true, true, false, false, false, false)]
    public void Cada_estado_permite_so_as_acoes_certas(string status, bool editar, bool aprovar, bool cancelar, bool reabrir, bool concluir, bool desfazer, bool excluir, bool ajustarDatas)
    {
        Assert.Equal(editar, OrdemServicoRegras.PodeEditar(status));
        Assert.Equal(aprovar, OrdemServicoRegras.PodeAprovar(status));
        Assert.Equal(cancelar && status != "concluido", OrdemServicoRegras.PodeCancelar(status));
        Assert.Equal(reabrir, OrdemServicoRegras.PodeReabrir(status));
        Assert.Equal(concluir, OrdemServicoRegras.PodeConcluir(status));
        Assert.Equal(desfazer, OrdemServicoRegras.PodeDesfazerConclusao(status));
        Assert.Equal(excluir, OrdemServicoRegras.PodeExcluir(status));
        Assert.Equal(ajustarDatas, OrdemServicoRegras.PodeAjustarDatas(status));
    }

    [Fact]
    public void Ajustar_datas_vira_utc_mantem_o_que_nao_veio_e_recusa_saida_antes_da_entrada()
    {
        var atualEntrada = new DateTime(2031, 5, 1, 8, 0, 0, DateTimeKind.Utc);
        var atualSaida = new DateTime(2031, 5, 3, 8, 0, 0, DateTimeKind.Utc);

        var (entrada, saida, erro) = OrdemServicoRegras.AjustarDatas(atualEntrada, atualSaida, new DateTime(2031, 5, 2, 9, 0, 0), null);
        Assert.Null(erro);
        Assert.Equal((new DateTime(2031, 5, 2, 9, 0, 0, DateTimeKind.Utc), atualSaida), (entrada, saida));
        Assert.Equal(DateTimeKind.Utc, entrada!.Value.Kind);

        var invalido = OrdemServicoRegras.AjustarDatas(atualEntrada, atualSaida, new DateTime(2031, 5, 4), null);
        Assert.Equal("A data/hora de saída não pode ser antes da entrada.", invalido.Erro);

        // sem as duas datas, não há o que comparar
        Assert.Null(OrdemServicoRegras.AjustarDatas(null, null, new DateTime(2031, 5, 4), null).Erro);
    }

    [Fact]
    public void Vencimento_escolhido_fica_ao_meio_dia_utc_e_sem_vencimento_vale_agora()
    {
        var agora = new DateTime(2031, 3, 1, 15, 45, 0, DateTimeKind.Utc);

        Assert.Equal(new DateTime(2031, 3, 20, 12, 0, 0, DateTimeKind.Utc), OrdemServicoRegras.VencimentoDoLancamento(new DateTime(2031, 3, 20, 23, 30, 0), agora));
        Assert.Equal(agora, OrdemServicoRegras.VencimentoDoLancamento(null, agora));
    }

    [Theory]
    [InlineData(false, "pix", null, null)]
    [InlineData(true, "pix", null, "Pago via pix")]
    [InlineData(true, "cartao", 1, "Pago via cartao")]
    [InlineData(true, "cartao", 3, "Pago via cartao em 3x")]
    public void Observacao_do_pagamento_so_existe_quando_ja_pago(bool jaPago, string forma, int? parcelas, string? esperado) =>
        Assert.Equal(esperado, OrdemServicoRegras.ObservacaoDoPagamento(jaPago, forma, parcelas));

    [Fact]
    public void Descricao_do_lancamento_usa_o_veiculo_ou_o_inicio_do_id()
    {
        var id = Guid.Parse("12345678-aaaa-bbbb-cccc-1234567890ab");

        Assert.Equal("Ordem de Serviço — Gol 2015", OrdemServicoRegras.DescricaoDoLancamento("Gol 2015", id));
        Assert.Equal("Ordem de Serviço — 12345678", OrdemServicoRegras.DescricaoDoLancamento(null, id));
    }

    // ── Comissão ──

    private static readonly Guid Alinhamento = Guid.NewGuid();
    private static readonly ItemComissionavel[] Itens =
    [
        new("peca", null, 200m),
        new("servico", Alinhamento, 80m),
        new("servico", null, 20m),
    ];

    [Fact]
    public void Comissao_na_base_total_soma_pecas_e_servicos()
    {
        var c = OrdemServicoRegras.CalcularComissao(Itens, "total", 10m, _ => null);

        Assert.Equal((300m, 10m, 30m), (c!.ValorBase, c.PercentualEfetivo, c.Valor));
    }

    [Fact]
    public void Comissao_na_base_servico_ignora_as_pecas()
    {
        var c = OrdemServicoRegras.CalcularComissao(Itens, "servico", 10m, _ => null);

        Assert.Equal((100m, 10m, 10m), (c!.ValorBase, c.PercentualEfetivo, c.Valor));
    }

    [Fact]
    public void Excecao_por_servico_vale_so_naquele_item_e_o_percentual_efetivo_e_a_media()
    {
        var c = OrdemServicoRegras.CalcularComissao(Itens, "total", 10m, servico => servico == Alinhamento ? 0m : null);

        Assert.Equal((300m, 7.33m, 22m), (c!.ValorBase, c.PercentualEfetivo, c.Valor));     // (200 + 20) × 10% = 22 sobre 300
    }

    [Fact]
    public void Sem_percentual_ou_com_comissao_zero_nao_ha_comissao()
    {
        Assert.Null(OrdemServicoRegras.CalcularComissao(Itens, "total", 0m, _ => null));
        Assert.Null(OrdemServicoRegras.CalcularComissao(Itens, "total", -5m, _ => null));
        Assert.Null(OrdemServicoRegras.CalcularComissao(Itens.Where(i => i.Tipo == "servico" && i.ServicoId == Alinhamento), "total", 10m, _ => 0m));
        Assert.Null(OrdemServicoRegras.CalcularComissao([], "total", 10m, _ => null));
    }

    [Fact]
    public void Comissao_e_arredondada_em_duas_casas()
    {
        var c = OrdemServicoRegras.CalcularComissao([new("servico", null, 33.33m)], "total", 10m, _ => null);

        Assert.Equal(3.33m, c!.Valor);
    }

    // ── Patrimônio ──

    [Theory]
    [InlineData("  Manuais ", "Manuais")]
    [InlineData("", null)]
    [InlineData("   ", null)]
    [InlineData(null, null)]
    public void Categoria_de_patrimonio_e_aparada_e_em_branco_vira_nula(string? categoria, string? esperado) =>
        Assert.Equal(esperado, OrdemServicoRegras.NormalizarCategoriaDePatrimonio(categoria));

    [Fact]
    public void Valor_total_e_diferenca_da_contagem()
    {
        Assert.Equal(120m, OrdemServicoRegras.ValorTotalDoPatrimonio(4, 30m));
        Assert.Equal(-1, OrdemServicoRegras.DiferencaDaContagem(3, 4));
        Assert.Equal(2, OrdemServicoRegras.DiferencaDaContagem(6, 4));
        Assert.Equal(0, OrdemServicoRegras.DiferencaDaContagem(4, 4));
    }

    [Fact]
    public void Data_da_contagem_vira_utc_e_sem_data_vale_agora()
    {
        var agora = new DateTime(2031, 3, 1, 15, 45, 0, DateTimeKind.Utc);

        var data = OrdemServicoRegras.DataDaContagem(new DateTime(2031, 2, 1, 10, 0, 0), agora);

        Assert.Equal((new DateTime(2031, 2, 1, 10, 0, 0, DateTimeKind.Utc), DateTimeKind.Utc), (data, data.Kind));
        Assert.Equal(agora, OrdemServicoRegras.DataDaContagem(null, agora));
    }
}
