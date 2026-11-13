using LojaApi.Domain.Servicos;

namespace LojaApi.Tests.Domain.AggregatesModel.ServicosTeste;

public class PlanoUsoRegrasTests
{
    // ── Dias da semana ───────────────────────────────────────────────────────

    [Fact]
    public void Normalizar_ordena_tira_repetidos_e_ignora_o_que_esta_fora_de_0_a_6()
    {
        Assert.Equal("1,3", PlanoUsoRegras.NormalizarDias([3, 1, 1, 9, -1]));
        Assert.Equal("0", PlanoUsoRegras.NormalizarDias([0]));
    }

    [Fact]
    public void Sem_dias_ou_com_os_sete_dias_nao_ha_restricao()
    {
        Assert.Null(PlanoUsoRegras.NormalizarDias(null));
        Assert.Null(PlanoUsoRegras.NormalizarDias([]));
        Assert.Null(PlanoUsoRegras.NormalizarDias([0, 1, 2, 3, 4, 5, 6]));
        Assert.Null(PlanoUsoRegras.NormalizarDias([9]));
    }

    [Fact]
    public void Ler_dias_aceita_espacos_e_ignora_lixo()
    {
        Assert.Equal([1, 3, 5], PlanoUsoRegras.LerDias("5, 1 ,3,3,x,9"));
        Assert.Empty(PlanoUsoRegras.LerDias(null));
        Assert.Empty(PlanoUsoRegras.LerDias(""));
    }

    [Fact]
    public void Descreve_os_dias_por_extenso() =>
        Assert.Equal("segunda, quarta, sábado", PlanoUsoRegras.DescreverDias("1,3,6"));

    [Fact]
    public void Dia_permitido_no_horario_local()
    {
        var segunda = new DateTime(2030, 6, 10, 9, 0, 0);   // segunda-feira
        Assert.True(PlanoUsoRegras.DiaPermitidoLocal("1", segunda));
        Assert.False(PlanoUsoRegras.DiaPermitidoLocal("2,3", segunda));
        Assert.True(PlanoUsoRegras.DiaPermitidoLocal(null, segunda));   // sem restrição
    }

    [Fact]
    public void Dia_permitido_converte_o_utc_para_o_horario_da_loja()
    {
        var segundaMadrugadaUtc = new DateTime(2030, 6, 10, 1, 0, 0, DateTimeKind.Utc);   // ainda domingo à noite no Brasil
        Assert.True(PlanoUsoRegras.DiaPermitido("0", segundaMadrugadaUtc));
        Assert.False(PlanoUsoRegras.DiaPermitido("1", segundaMadrugadaUtc));
    }

    // ── Mensagens de validação ───────────────────────────────────────────────

    [Fact]
    public void Dia_fora_do_permitido_gera_mensagem_com_os_dias_do_plano()
    {
        var sexta = new DateTime(2030, 6, 14, 9, 0, 0);
        Assert.Equal("O plano 'Plano Banho' só pode ser usado em: segunda, terça. Escolha outro dia.", PlanoUsoRegras.ValidarDia("Plano Banho", "1,2", sexta));
        Assert.Null(PlanoUsoRegras.ValidarDia("Plano Banho", "5", sexta));
        Assert.Null(PlanoUsoRegras.ValidarDia("Plano Banho", null, sexta));
    }

    [Theory]
    [InlineData(4, 0, 0, null)]          // primeiro uso de 4
    [InlineData(4, 2, 1, null)]          // 3 usados: o quarto ainda cabe
    [InlineData(4, 3, 1, "O plano 'P' permite 4 uso(s) por mês e já há 4 usado(s) ou agendado(s) neste mês.")]
    [InlineData(1, 0, 1, "O plano 'P' permite 1 uso(s) por mês e já há 1 usado(s) ou agendado(s) neste mês.")]
    [InlineData(2, 5, 0, "O plano 'P' permite 2 uso(s) por mês e já há 5 usado(s) ou agendado(s) neste mês.")]
    public void Limite_mensal_conta_usados_mais_agendados_mais_o_novo(int limite, int usados, int agendados, string? esperado) =>
        Assert.Equal(esperado, PlanoUsoRegras.ValidarLimite("P", limite, usados, agendados));

    // ── Serviços do plano ────────────────────────────────────────────────────

    [Fact]
    public void Servicos_incluidos_vem_de_uma_lista_separada_por_virgula()
    {
        var a = Guid.NewGuid(); var b = Guid.NewGuid(); var fora = Guid.NewGuid();
        var lista = $"{a},{b}";
        Assert.Equal(2, PlanoUsoRegras.ServicosIncluidos(lista).Length);
        Assert.True(PlanoUsoRegras.Inclui(lista, a));
        Assert.True(PlanoUsoRegras.Inclui(lista.ToUpperInvariant(), b));   // não diferencia maiúsculas
        Assert.False(PlanoUsoRegras.Inclui(lista, fora));
        Assert.False(PlanoUsoRegras.Inclui(null, a));
        Assert.Empty(PlanoUsoRegras.ServicosIncluidos(""));
    }

    [Fact]
    public void Ids_dos_servicos_ignora_o_que_nao_e_guid()
    {
        var a = Guid.NewGuid();
        Assert.Equal([a], PlanoUsoRegras.IdsDosServicos($"{a},lixo,{Guid.Empty}"));
    }

    // ── Mês ──────────────────────────────────────────────────────────────────

    [Fact]
    public void Primeiro_dia_do_mes_em_utc() =>
        Assert.Equal(new DateTime(2030, 6, 1, 0, 0, 0, DateTimeKind.Utc), PlanoUsoRegras.PrimeiroDiaDoMesUtc(new DateTime(2030, 6, 17, 15, 30, 0, DateTimeKind.Utc)));

    [Fact]
    public void Mes_de_uso_segue_o_horario_da_loja_e_nao_o_utc()
    {
        // 01/03 02:00 UTC ainda é 28/02 23:00 no Brasil: pertence a fevereiro
        var (ini, fim) = PlanoUsoRegras.IntervaloDoMesUtc(new DateTime(2030, 3, 1, 2, 0, 0, DateTimeKind.Utc));
        Assert.Equal(new DateTime(2030, 2, 1, 3, 0, 0, DateTimeKind.Utc), ini);
        Assert.Equal(new DateTime(2030, 3, 1, 3, 0, 0, DateTimeKind.Utc), fim);
    }

    [Fact]
    public void Mes_de_um_horario_local_de_agendamento()
    {
        var (ini, fim) = PlanoUsoRegras.IntervaloDoMesUtcDeHorarioLocal(new DateTime(2030, 6, 20, 14, 0, 0));
        Assert.Equal(new DateTime(2030, 6, 1, 3, 0, 0, DateTimeKind.Utc), ini);
        Assert.Equal(new DateTime(2030, 7, 1, 3, 0, 0, DateTimeKind.Utc), fim);
    }
}
