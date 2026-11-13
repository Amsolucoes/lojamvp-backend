using LojaApi.Domain.Servicos;

namespace LojaApi.Tests.Domain.Services.ServicosTeste;

public class AgendamentoRegrasTests
{
    private static readonly DateTime Dia = new(2030, 6, 10, 0, 0, 0, DateTimeKind.Unspecified);
    private static readonly DateTime Passado = new(2000, 1, 1);

    // ── Status ───────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("agendado", true)]
    [InlineData("concluido", true)]
    [InlineData("cancelado", true)]
    [InlineData("pendente", false)]   // pendente só nasce do agendamento online
    [InlineData("voando", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void Status_aceitos_pelo_painel(string? status, bool esperado) =>
        Assert.Equal(esperado, AgendamentoRegras.StatusValido(status));

    [Fact]
    public void Agendamento_pago_nao_pode_ser_cancelado_mas_pode_ser_reaberto_ou_concluido()
    {
        Assert.NotNull(AgendamentoRegras.ValidarCancelamento("cancelado", pago: true));
        Assert.Null(AgendamentoRegras.ValidarCancelamento("cancelado", pago: false));
        Assert.Null(AgendamentoRegras.ValidarCancelamento("concluido", pago: true));
        Assert.Null(AgendamentoRegras.ValidarCancelamento("agendado", pago: true));
    }

    [Fact]
    public void Agendamento_pago_nao_pode_ser_excluido()
    {
        Assert.NotNull(AgendamentoRegras.ValidarExclusao(true));
        Assert.Null(AgendamentoRegras.ValidarExclusao(false));
    }

    // ── Comissão ─────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("concluido", "agendado", true, true)]
    [InlineData("concluido", "pendente", true, true)]
    [InlineData("concluido", "concluido", true, false)]   // já estava concluído: não gera de novo
    [InlineData("concluido", "agendado", false, false)]   // sem profissional
    [InlineData("cancelado", "agendado", true, false)]
    [InlineData("agendado", "concluido", true, false)]
    public void Comissao_so_na_primeira_conclusao_com_profissional(string novo, string anterior, bool temProfissional, bool esperado) =>
        Assert.Equal(esperado, AgendamentoRegras.GeraComissao(novo, anterior, temProfissional));

    [Theory]
    [InlineData(80, 10, 8.00)]
    [InlineData(200, 25, 50.00)]
    [InlineData(33.33, 10, 3.33)]
    [InlineData(100, 12.5, 12.50)]
    public void Valor_da_comissao_e_o_percentual_do_preco(double preco, double percentual, double esperado) =>
        Assert.Equal((decimal)esperado, AgendamentoRegras.ValorComissao((decimal)preco, (decimal)percentual));

    [Fact]
    public void Sem_percentual_ou_com_zero_nao_ha_comissao()
    {
        Assert.Null(AgendamentoRegras.ValorComissao(100, null));
        Assert.Null(AgendamentoRegras.ValorComissao(100, 0));
        Assert.Null(AgendamentoRegras.ValorComissao(100, -5));
    }

    [Fact]
    public void Comissao_arredonda_para_duas_casas() =>
        Assert.Equal(0.33m, AgendamentoRegras.ValorComissao(3.33m, 10m));   // 0,333 -> 0,33

    // ── Preço e duração ──────────────────────────────────────────────────────

    [Fact]
    public void Preco_e_duracao_informados_valem_e_zerados_usam_o_servico()
    {
        Assert.Equal(100m, AgendamentoRegras.PrecoFinal(100, 80));
        Assert.Equal(80m, AgendamentoRegras.PrecoFinal(0, 80));
        Assert.Equal(80m, AgendamentoRegras.PrecoFinal(-1, 80));
        Assert.Equal(90, AgendamentoRegras.DuracaoFinal(90, 60));
        Assert.Equal(60, AgendamentoRegras.DuracaoFinal(0, 60));
    }

    // ── Conflito de horário ──────────────────────────────────────────────────

    [Theory]
    [InlineData(9, 0, 60, true)]     // mesmo horário
    [InlineData(9, 30, 60, true)]    // começa no meio do outro
    [InlineData(8, 30, 60, true)]    // termina no meio do outro
    [InlineData(8, 0, 180, true)]    // engloba o outro
    [InlineData(10, 0, 60, false)]   // começa quando o outro termina
    [InlineData(8, 0, 60, false)]    // termina quando o outro começa
    [InlineData(14, 0, 30, false)]
    public void Conflita_quando_os_horarios_se_sobrepoem(int hora, int minuto, int duracao, bool esperado)
    {
        var existente = new[] { new Ocupacao(Dia.AddHours(9), 60) };   // 9h às 10h
        var inicio = Dia.AddHours(hora).AddMinutes(minuto);
        Assert.Equal(esperado, AgendamentoRegras.Conflita(inicio, inicio.AddMinutes(duracao), existente));
    }

    [Fact]
    public void Sem_ocupados_nao_ha_conflito() =>
        Assert.False(AgendamentoRegras.Conflita(Dia.AddHours(9), Dia.AddHours(10), []));

    // ── Horários livres ──────────────────────────────────────────────────────

    [Fact]
    public void Gera_horarios_de_30_em_30_ate_caber_o_servico()
    {
        var livres = AgendamentoRegras.HorariosLivres(Dia, 8, 12, 60, Passado, []);
        Assert.Equal(["08:00", "08:30", "09:00", "09:30", "10:00", "10:30", "11:00"], livres);
    }

    [Fact]
    public void Servico_mais_longo_tem_menos_horarios() =>
        Assert.Equal(["08:00", "08:30", "09:00", "09:30", "10:00", "10:30"], AgendamentoRegras.HorariosLivres(Dia, 8, 12, 90, Passado, []));

    [Fact]
    public void Duracao_zerada_conta_como_30_minutos() =>
        Assert.Equal(8, AgendamentoRegras.HorariosLivres(Dia, 8, 12, 0, Passado, []).Count);   // 08:00 ... 11:30

    [Fact]
    public void Tira_os_horarios_que_batem_com_o_que_ja_esta_agendado() =>
        Assert.Equal(["08:00", "10:00", "10:30", "11:00"],
            AgendamentoRegras.HorariosLivres(Dia, 8, 12, 60, Passado, [new Ocupacao(Dia.AddHours(9), 60)]));

    [Fact]
    public void Nao_oferece_horario_que_ja_passou_nem_o_exato_agora()
    {
        var agora = Dia.AddHours(9);   // 09:00
        Assert.Equal(["09:30", "10:00", "10:30", "11:00"], AgendamentoRegras.HorariosLivres(Dia, 8, 12, 60, agora, []));
    }

    [Fact]
    public void Dia_totalmente_no_passado_nao_tem_horarios() =>
        Assert.Empty(AgendamentoRegras.HorariosLivres(Dia, 8, 12, 60, Dia.AddDays(1), []));

    [Fact]
    public void Expediente_curto_demais_para_o_servico_nao_tem_horarios() =>
        Assert.Empty(AgendamentoRegras.HorariosLivres(Dia, 8, 9, 90, Passado, []));

    // ── Agendamento online ───────────────────────────────────────────────────

    [Theory]
    [InlineData(null, "aprovacao")]
    [InlineData("", "aprovacao")]
    [InlineData("automatico", "automatico")]
    [InlineData("aprovacao", "aprovacao")]
    public void Confirmacao_padrao_e_aprovacao_manual(string? configurada, string esperado) =>
        Assert.Equal(esperado, AgendamentoRegras.ConfirmacaoOnline(configurada));

    [Theory]
    [InlineData("automatico", "agendado")]
    [InlineData("aprovacao", "pendente")]
    [InlineData("qualquer coisa", "pendente")]
    public void Status_do_pedido_online_depende_da_confirmacao(string confirmacao, string esperado) =>
        Assert.Equal(esperado, AgendamentoRegras.StatusDoAgendamentoOnline(confirmacao));

    [Fact]
    public void Mensagem_do_pedido_online()
    {
        Assert.Equal("Solicitação enviada! A loja vai confirmar seu horário em breve.", AgendamentoRegras.MensagemDoAgendamentoOnline("pendente"));
        Assert.Equal("Agendamento confirmado!", AgendamentoRegras.MensagemDoAgendamentoOnline("agendado"));
    }

    [Theory]
    [InlineData("(67) 99999-1111", "67999991111")]
    [InlineData("67 9 9999 1111", "67999991111")]
    [InlineData("+55 (67) 99999-1111", "5567999991111")]
    [InlineData("", "")]
    [InlineData(null, "")]
    [InlineData("abc", "")]
    public void Telefone_so_com_digitos(string? telefone, string esperado) =>
        Assert.Equal(esperado, AgendamentoRegras.SoDigitos(telefone));

    [Theory]
    [InlineData("Maria da Silva", "Maria")]
    [InlineData("  João   Pedro ", "João")]
    [InlineData("Ana", "Ana")]
    [InlineData("   ", "")]
    public void Mostra_so_o_primeiro_nome(string nome, string esperado) =>
        Assert.Equal(esperado, AgendamentoRegras.PrimeiroNome(nome));
}
