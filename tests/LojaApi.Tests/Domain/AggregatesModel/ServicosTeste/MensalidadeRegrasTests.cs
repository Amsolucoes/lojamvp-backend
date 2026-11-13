using LojaApi.Domain.Servicos;

namespace LojaApi.Tests.Domain.AggregatesModel.ServicosTeste;

public class MensalidadeRegrasTests
{
    private static readonly DateTime Mes = new(2030, 6, 1, 0, 0, 0, DateTimeKind.Utc);
    private static DateTime Agora(int dia) => new(2030, 6, dia, 12, 0, 0, DateTimeKind.Utc);

    // ── Atraso ───────────────────────────────────────────────────────────────

    [Fact]
    public void Mes_anterior_pendente_esta_em_atraso() =>
        Assert.True(MensalidadeRegras.EmAtraso("pendente", Mes.AddMonths(-1), Mes, 10, Agora(1)));

    [Theory]
    [InlineData(9, false)]
    [InlineData(10, false)]   // no dia do vencimento ainda não está atrasada
    [InlineData(11, true)]
    [InlineData(28, true)]
    public void Mes_atual_so_atrasa_depois_do_dia_de_vencimento(int hoje, bool esperado) =>
        Assert.Equal(esperado, MensalidadeRegras.EmAtraso("pendente", Mes, Mes, 10, Agora(hoje)));

    [Fact]
    public void Mes_futuro_nunca_esta_em_atraso() =>
        Assert.False(MensalidadeRegras.EmAtraso("pendente", Mes.AddMonths(1), Mes, 1, Agora(28)));

    [Fact]
    public void Mensalidade_paga_nunca_esta_em_atraso() =>
        Assert.False(MensalidadeRegras.EmAtraso("pago", Mes.AddMonths(-3), Mes, 10, Agora(28)));

    // ── Valor recebido e itens da venda ──────────────────────────────────────

    [Fact]
    public void Sem_valor_informado_recebe_o_valor_da_mensalidade()
    {
        Assert.Equal(150m, MensalidadeRegras.Recebido(150, null));
        Assert.Equal(180m, MensalidadeRegras.Recebido(150, 180));
        Assert.Equal(100m, MensalidadeRegras.Recebido(150, 100));
    }

    [Fact]
    public void Quitar_sem_valor_informado_gera_um_item_sem_o_mes_no_nome()
    {
        var itens = MensalidadeRegras.ItensDaVenda("Plano Banho", Mes, 150);
        Assert.Equal([new ItemMensalidade("Mensalidade - Plano Banho", 150m)], itens);
    }

    [Fact]
    public void Valor_igual_ao_da_mensalidade_gera_um_item_com_o_mes_no_nome()
    {
        var itens = MensalidadeRegras.ItensDaVenda("Plano Banho", Mes, 150, 150);
        Assert.Equal([new ItemMensalidade("Mensalidade - Plano Banho (06/2030)", 150m)], itens);
    }

    [Fact]
    public void Pago_a_mais_separa_o_excedente_como_juros_e_multa()
    {
        var itens = MensalidadeRegras.ItensDaVenda("Plano Banho", Mes, 150, 180);
        Assert.Equal([new ItemMensalidade("Mensalidade - Plano Banho (06/2030)", 150m), new ItemMensalidade("Juros/multa - Plano Banho (06/2030)", 30m)], itens);
        Assert.Equal(180m, itens.Sum(i => i.Valor));
    }

    [Fact]
    public void Pago_a_menos_registra_so_o_que_foi_recebido()
    {
        var itens = MensalidadeRegras.ItensDaVenda("Plano Banho", Mes, 150, 100);
        Assert.Equal([new ItemMensalidade("Mensalidade - Plano Banho (06/2030)", 100m)], itens);
    }
}
