using LojaApi.Domain.Plataforma;

namespace LojaApi.Tests.Domain.AggregatesModel.PlataformaTeste;

// Regras puras da plataforma: atraso, situação da assinatura, link da loja, preço do cadastro e módulos (sem banco, sem HTTP).
public class PlataformaRegrasTests
{
    private static readonly DateTime Agora = new(2031, 5, 10, 15, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData("2031-05-10T12:00:00Z", 0)]
    [InlineData("2031-05-09T12:00:00Z", 1)]
    [InlineData("2031-05-12T12:00:00Z", -2)]
    public void Dias_de_atraso_contam_dias_corridos_no_horario_de_brasilia(string vencimento, int esperado) =>
        Assert.Equal(esperado, PlataformaRegras.DiasDeAtraso(DateTime.Parse(vencimento).ToUniversalTime(), Agora));

    [Fact]
    public void Meia_noite_utc_ainda_e_o_dia_anterior_em_brasilia() =>
        Assert.Equal(0, PlataformaRegras.DiasDeAtraso(new DateTime(2031, 5, 9, 23, 0, 0, DateTimeKind.Utc), new DateTime(2031, 5, 10, 1, 0, 0, DateTimeKind.Utc)));

    [Fact]
    public void Situacao_em_trial_conta_os_dias_e_depois_vira_carencia()
    {
        Assert.Equal(("trial", 3), PlataformaRegras.CalcularSituacao("Trial", Agora.AddDays(3), null, Agora));
        Assert.Equal(("carencia", 0), PlataformaRegras.CalcularSituacao("Trial", Agora.AddDays(-2), null, Agora));
    }

    [Fact]
    public void Situacao_ativa_conta_dias_ate_o_vencimento_e_cai_em_carencia_se_vencida()
    {
        Assert.Equal(("ativo", 10), PlataformaRegras.CalcularSituacao("Ativo", Agora, Agora.AddDays(10), Agora));
        Assert.Equal(("carencia", 0), PlataformaRegras.CalcularSituacao("Ativo", Agora, Agora.AddDays(-1), Agora));
        Assert.Equal(("bloqueado", 0), PlataformaRegras.CalcularSituacao("Bloqueado", Agora, null, Agora));
        Assert.Equal(("ativo", 0), PlataformaRegras.CalcularSituacao("Ativo", Agora, null, Agora));                          // ativo sem vencimento: fase pelo nome do status
    }

    [Theory]
    [InlineData("  Salão Éxito  ", "salao-exito")]
    [InlineData("Olá  Mundo__x", "ola-mundo-x")]
    [InlineData("--a--", "a")]
    public void Slug_sem_acento_minusculo_e_sem_hifens_repetidos(string texto, string esperado) =>
        Assert.Equal(esperado, PlataformaRegras.GerarSlug(texto));

    [Fact]
    public void Slug_precisa_de_3_caracteres() { Assert.False(PlataformaRegras.SlugValido("ab")); Assert.True(PlataformaRegras.SlugValido("abc")); }

    [Fact]
    public void Nome_do_schema_troca_espaco_e_hifen_por_sublinhado_e_tira_simbolos() =>
        Assert.StartsWith("loja_minha_loja_2_", PlataformaRegras.GerarSchemaNome("Minha Loja-2!", Agora));

    [Fact]
    public void Horario_da_agenda_e_limitado() { Assert.Equal((0, 24), PlataformaRegras.HorarioDaAgenda(-1, 99)); Assert.Equal((23, 1), PlataformaRegras.HorarioDaAgenda(40, 0)); }

    [Fact]
    public void Confirmacao_desconhecida_vira_aprovacao() { Assert.Equal("automatico", PlataformaRegras.ConfirmacaoDoAgendamentoOnline("automatico")); Assert.Equal("aprovacao", PlataformaRegras.ConfirmacaoDoAgendamentoOnline("x")); }

    [Fact]
    public void Cooldown_do_modulo_so_libera_depois_de_30_dias()
    {
        Assert.Equal(30, PlataformaRegras.DiasParaReativarModulo(Agora, Agora));
        Assert.Equal(1, PlataformaRegras.DiasParaReativarModulo(Agora.AddDays(-29), Agora));
        Assert.Equal(0, PlataformaRegras.DiasParaReativarModulo(Agora.AddDays(-30), Agora));
    }

    [Fact]
    public void Pausa_vale_de_hoje_em_diante_e_vai_ate_o_fim_do_dia()
    {
        Assert.False(PlataformaRegras.PausaValida(null, Agora));
        Assert.False(PlataformaRegras.PausaValida(Agora.AddDays(-1), Agora));
        Assert.True(PlataformaRegras.PausaValida(Agora.Date, Agora));
        var fim = PlataformaRegras.FimDaPausa(new DateTime(2031, 5, 12, 3, 0, 0));
        Assert.Equal(new DateTime(2031, 5, 12, 23, 59, 0, DateTimeKind.Utc), fim);
        Assert.Equal("Voltamos!", PlataformaRegras.MensagemDaPausa("  Voltamos! "));
        Assert.StartsWith("Estamos temporariamente fechados", PlataformaRegras.MensagemDaPausa(" "));
    }

    [Fact]
    public void Cadastro_preco_promocao_so_para_as_10_primeiras_e_fora_do_financeiro_puro()
    {
        Assert.True(PlataformaRegras.EhPromocional(false, 9));
        Assert.False(PlataformaRegras.EhPromocional(false, 10));
        Assert.False(PlataformaRegras.EhPromocional(true, 0));
        Assert.Equal(89.90m, PlataformaRegras.MensalidadeBase(false, true));
        Assert.Equal(119.90m, PlataformaRegras.MensalidadeBase(false, false));
        Assert.Equal(39.90m, PlataformaRegras.MensalidadeBase(true, false));
    }

    [Fact]
    public void Modulos_extras_do_cadastro_so_os_permitidos_sem_repetir()
    {
        Assert.Equal(["financeiro", "nf"], PlataformaRegras.ModulosExtrasValidos(["financeiro", "nf", "nf", "turmas", "x"]));
        Assert.Empty(PlataformaRegras.ModulosExtrasValidos(null));
    }

    [Theory]
    [InlineData("servicos", "Qualquer", "servicos")]
    [InlineData("loja_modulos", "Qualquer", "servicos")]
    [InlineData("loja", "Corretora", "corretora")]
    [InlineData("loja", "Pilates Studio", "turmas")]
    [InlineData("loja", "Moda", "")]
    public void Modulos_do_perfil(string tipoPlano, string nome, string esperado) =>
        Assert.Equal(esperado, string.Join(",", PlataformaRegras.ModulosDoPerfil(tipoPlano, nome)));
}
