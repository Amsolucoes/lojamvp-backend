using LojaApi.Modules.Chacara.Domain;

namespace LojaApi.Tests.Chacara.Dominio;

public class ReservaTests
{
    private static readonly DateTime Agora = new(2030, 5, 10, 15, 0, 0, DateTimeKind.Utc);

    private static Reserva Pendente(decimal valor = 1000m, string? forma = null) => new()
    {
        Valor = valor,
        Status = StatusReserva.PendentePagamento,
        FormaPagamento = forma,
        ExpiraEm = Agora.AddMinutes(15),
    };

    private static Reserva ComStatus(string status, decimal valor = 1000m, decimal pago = 0m) =>
        new() { Valor = valor, ValorPago = pago, Status = status };

    // ── Criação ──────────────────────────────────────────────────────────────

    [Fact]
    public void CriarManual_nasce_confirmada_e_limpa_os_dados()
    {
        var r = Reserva.CriarManual(Guid.NewGuid(), new DateTime(2030, 6, 1, 3, 0, 0), new DateTime(2030, 6, 2),
            20, "  Maria Silva ", " maria@x.com ", "(67) 99999-0000", " 123 ", "  ", " Rua A ", null, " Campo Grande ",
            " 15:00 ", null, 1500m, null, Agora);

        Assert.Equal(StatusReserva.Confirmada, r.Status);
        Assert.Equal(Agora, r.DataConfirmacao);
        Assert.Equal("Maria Silva", r.ClienteNome);
        Assert.Equal("maria@x.com", r.ClienteEmail);
        Assert.Equal("67999990000", r.ClienteTelefone);   // só dígitos
        Assert.Equal("123", r.ClienteDocumento);
        Assert.Null(r.ClienteCep);                        // em branco vira nulo
        Assert.Equal("Rua A", r.ClienteEndereco);
        Assert.Equal("15:00", r.HoraEntrada);
        Assert.Null(r.HoraSaida);
        Assert.Equal(1500m, r.ValorPago);                 // sem informar, assume pago por inteiro
        Assert.Equal(0m, r.SaldoPendente());
    }

    [Fact]
    public void CriarManual_guarda_as_datas_ao_meio_dia_UTC()
    {
        var r = Reserva.CriarManual(Guid.NewGuid(), new DateTime(2030, 6, 1, 23, 30, 0), new DateTime(2030, 6, 2, 0, 5, 0),
            10, "A", null, null, null, null, null, null, null, null, null, 100m, null, Agora);

        Assert.Equal(new DateTime(2030, 6, 1, 12, 0, 0, DateTimeKind.Utc), r.DataInicio);
        Assert.Equal(new DateTime(2030, 6, 2, 12, 0, 0, DateTimeKind.Utc), r.DataFim);
        Assert.Equal(DateTimeKind.Utc, r.DataInicio.Kind);
        Assert.Equal("", r.ClienteEmail);                 // e-mail ausente vira texto vazio (coluna obrigatória)
    }

    [Fact]
    public void CriarManual_com_valor_pago_parcial_deixa_saldo()
    {
        var r = Reserva.CriarManual(Guid.NewGuid(), Agora, Agora, 10, "A", null, null, null, null, null, null, null,
            null, null, 1000m, 400m, Agora);
        Assert.Equal(600m, r.SaldoPendente());
    }

    [Fact]
    public void CriarPeloSite_com_pagamento_online_segura_a_data_por_15_minutos()
    {
        var r = Reserva.CriarPeloSite(Guid.NewGuid(), Agora, Agora, 10, " João ", " j@x.com ", "(67) 98888-7777",
            null, null, null, null, null, 800m, semPagamentoOnline: false, Agora);

        Assert.Equal(StatusReserva.PendentePagamento, r.Status);
        Assert.Equal(Agora.AddMinutes(15), r.ExpiraEm);
        Assert.Null(r.FormaPagamento);
        Assert.Equal("João", r.ClienteNome);
        Assert.Equal("67988887777", r.ClienteTelefone);
        Assert.Equal(0m, r.ValorPago);
    }

    [Fact]
    public void CriarPeloSite_sem_pagamento_online_espera_o_dono_por_48_horas()
    {
        var r = Reserva.CriarPeloSite(Guid.NewGuid(), Agora, Agora, 10, "João", "j@x.com", "1",
            null, null, null, null, null, 800m, semPagamentoOnline: true, Agora);

        Assert.Equal(Agora.AddHours(48), r.ExpiraEm);
        Assert.Equal(FormaPagamentoReserva.SemPagamentoOnline, r.FormaPagamento);
    }

    // ── Cancelamento pelo cliente ────────────────────────────────────────────

    [Fact]
    public void CancelarPeloCliente_so_vale_para_reserva_pendente()
    {
        var r = Pendente();
        Assert.Null(r.CancelarPeloCliente());
        Assert.Equal(StatusReserva.Cancelada, r.Status);
        Assert.Equal("Cliente desistiu", r.MotivoCancelamento);
        Assert.Null(r.ExpiraEm);

        Assert.NotNull(r.CancelarPeloCliente()); // já cancelada
        Assert.NotNull(ComStatus(StatusReserva.Confirmada).CancelarPeloCliente());
        Assert.NotNull(ComStatus(StatusReserva.Expirada).CancelarPeloCliente());
    }

    // ── Escolha da forma de pagamento ────────────────────────────────────────

    [Theory]
    [InlineData("pix")]
    [InlineData("cartao")]
    [InlineData("combinado")]
    public void ValidarEscolhaDePagamento_aceita_as_formas_conhecidas(string forma) =>
        Assert.Null(Pendente().ValidarEscolhaDePagamento(forma));

    [Theory]
    [InlineData("boleto")]
    [InlineData("sem_pagamento_online")]
    [InlineData("")]
    [InlineData(null)]
    public void ValidarEscolhaDePagamento_recusa_forma_desconhecida(string? forma) =>
        Assert.Equal("Forma de pagamento inválida.", Pendente().ValidarEscolhaDePagamento(forma));

    [Fact]
    public void ValidarEscolhaDePagamento_recusa_reserva_que_nao_esta_pendente() =>
        Assert.NotNull(ComStatus(StatusReserva.Cancelada).ValidarEscolhaDePagamento("pix"));

    [Theory]
    [InlineData("pix", "approved", null, true)]
    [InlineData("pix", null, "approved", false)]
    [InlineData("cartao", null, "approved", true)]
    [InlineData("cartao", "approved", null, false)]
    [InlineData("combinado", "approved", "approved", true)]
    [InlineData("combinado", "approved", "pending", false)]
    [InlineData("combinado", "pending", "approved", false)]
    [InlineData(null, "approved", "approved", false)]
    [InlineData("sem_pagamento_online", "approved", "approved", false)]
    public void PagamentoOnlineAprovado_exige_o_que_a_forma_escolhida_pede(string? forma, string? pix, string? cartao, bool esperado)
    {
        var r = Pendente(forma: forma);
        r.MpStatusPix = pix;
        r.MpStatusCartao = cartao;
        Assert.Equal(esperado, r.PagamentoOnlineAprovado());
    }

    [Fact]
    public void ConfirmarPorPagamentoOnline_com_so_Pix_fica_parcial_com_50_por_cento()
    {
        var r = Pendente(1000m, FormaPagamentoReserva.Pix);
        r.ConfirmarPorPagamentoOnline(Agora);

        Assert.Equal(StatusReserva.ConfirmadaParcial, r.Status);
        Assert.Equal(500m, r.ValorPago);
        Assert.Equal(Agora, r.DataConfirmacao);
        Assert.Null(r.ExpiraEm);
    }

    [Theory]
    [InlineData("cartao")]
    [InlineData("combinado")]
    public void ConfirmarPorPagamentoOnline_com_cartao_ou_combinado_quita_tudo(string forma)
    {
        var r = Pendente(1000m, forma);
        r.ConfirmarPorPagamentoOnline(Agora);

        Assert.Equal(StatusReserva.Confirmada, r.Status);
        Assert.Equal(1000m, r.ValorPago);
    }

    [Fact]
    public void PrazoVencido_so_quando_ha_prazo_e_ele_passou()
    {
        var r = Pendente();
        Assert.False(r.PrazoVencido(Agora));
        Assert.True(r.PrazoVencido(Agora.AddMinutes(16)));
        r.ExpiraEm = null; // em negociação
        Assert.False(r.PrazoVencido(Agora.AddYears(1)));
    }

    [Fact]
    public void EstadiaTerminou_depois_da_data_final()
    {
        var r = new Reserva { DataFim = Agora };
        Assert.True(r.EstadiaTerminou(Agora));
        Assert.True(r.EstadiaTerminou(Agora.AddHours(1)));
        Assert.False(r.EstadiaTerminou(Agora.AddHours(-1)));
    }

    // ── Expirar / reativar ───────────────────────────────────────────────────

    [Fact]
    public void MarcarComoExpirada_so_para_pendente()
    {
        var r = Pendente();
        Assert.Null(r.MarcarComoExpirada());
        Assert.Equal(StatusReserva.Expirada, r.Status);
        Assert.NotNull(r.MarcarComoExpirada());
        Assert.NotNull(ComStatus(StatusReserva.Confirmada).MarcarComoExpirada());
    }

    [Fact]
    public void Reativar_volta_para_pendente_com_15_minutos()
    {
        var r = ComStatus(StatusReserva.Expirada);
        Assert.Null(r.PodeReativar());
        r.Reativar(Agora);

        Assert.Equal(StatusReserva.PendentePagamento, r.Status);
        Assert.Equal(Agora.AddMinutes(15), r.ExpiraEm);
    }

    [Theory]
    [InlineData(StatusReserva.PendentePagamento)]
    [InlineData(StatusReserva.Confirmada)]
    [InlineData(StatusReserva.Cancelada)]
    public void PodeReativar_recusa_o_que_nao_esta_expirado(string status) =>
        Assert.NotNull(ComStatus(status).PodeReativar());

    // ── Confirmação pelo dono ────────────────────────────────────────────────

    [Fact]
    public void Confirmar_sem_valor_assume_pagamento_integral()
    {
        var r = Pendente(1000m);
        Assert.Null(r.Confirmar(null, Agora));

        Assert.Equal(StatusReserva.Confirmada, r.Status);
        Assert.Equal(1000m, r.ValorPago);
        Assert.Equal(Agora, r.DataConfirmacao);
    }

    [Fact]
    public void Confirmar_com_valor_parcial_guarda_o_que_foi_pago()
    {
        var r = Pendente(1000m);
        r.Confirmar(300m, Agora);
        Assert.Equal(300m, r.ValorPago);
        Assert.Equal(700m, r.SaldoPendente());
    }

    [Fact]
    public void Confirmar_de_novo_e_recusado_e_nao_altera_nada()
    {
        var r = Pendente(1000m);
        r.Confirmar(300m, Agora);

        Assert.Equal("Esta reserva já está confirmada.", r.Confirmar(1000m, Agora.AddDays(1)));
        Assert.Equal(300m, r.ValorPago);
        Assert.Equal(Agora, r.DataConfirmacao);
    }

    // ── Negociação ───────────────────────────────────────────────────────────

    [Fact]
    public void ManterEmNegociacao_remove_o_prazo_e_DesfazerNegociacao_devolve_15_minutos()
    {
        var r = Pendente();
        Assert.Null(r.ManterEmNegociacao());
        Assert.Null(r.ExpiraEm);

        Assert.Null(r.DesfazerNegociacao(Agora));
        Assert.Equal(Agora.AddMinutes(15), r.ExpiraEm);
    }

    [Fact]
    public void Negociacao_so_existe_para_reserva_pendente()
    {
        var r = ComStatus(StatusReserva.Confirmada);
        Assert.NotNull(r.ManterEmNegociacao());
        Assert.NotNull(r.DesfazerNegociacao(Agora));
    }

    // ── Pagamento adicional ──────────────────────────────────────────────────

    [Fact]
    public void RegistrarPagamento_soma_ate_quitar_e_nunca_passa_do_valor()
    {
        var r = ComStatus(StatusReserva.ConfirmadaParcial, valor: 1000m, pago: 500m);

        Assert.Null(r.RegistrarPagamento(100m));
        Assert.Equal(600m, r.ValorPago);
        Assert.Equal(StatusReserva.ConfirmadaParcial, r.Status); // ainda falta

        Assert.Null(r.RegistrarPagamento(999999m));
        Assert.Equal(1000m, r.ValorPago);                        // limitado ao valor da reserva
        Assert.Equal(StatusReserva.Confirmada, r.Status);        // quitada sai do "parcial"
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void RegistrarPagamento_exige_valor_positivo(int valor) =>
        Assert.Equal("Informe um valor de pagamento maior que zero.",
            ComStatus(StatusReserva.Confirmada, 1000m, 100m).RegistrarPagamento(valor));

    [Theory]
    [InlineData(StatusReserva.PendentePagamento)]
    [InlineData(StatusReserva.Cancelada)]
    [InlineData(StatusReserva.Expirada)]
    public void RegistrarPagamento_so_em_reserva_confirmada(string status) =>
        Assert.NotNull(ComStatus(status).RegistrarPagamento(10m));

    // ── Prejuízo e avaliação do cliente ──────────────────────────────────────

    [Fact]
    public void RegistrarPrejuizo_valida_e_limpa_a_observacao()
    {
        var r = new Reserva();
        Assert.NotNull(r.RegistrarPrejuizo(-1m, "x"));
        Assert.Null(r.ValorPrejuizo);

        Assert.Null(r.RegistrarPrejuizo(150.5m, "  quebrou cadeira "));
        Assert.Equal(150.5m, r.ValorPrejuizo);
        Assert.Equal("quebrou cadeira", r.ObservacaoPrejuizo);

        Assert.Null(r.RegistrarPrejuizo(null, "   ")); // limpar o registro
        Assert.Null(r.ValorPrejuizo);
        Assert.Null(r.ObservacaoPrejuizo);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    [InlineData(-1)]
    public void AvaliarCliente_recusa_nota_fora_de_1_a_5(int nota)
    {
        var r = new Reserva();
        Assert.Equal("A nota deve ser de 1 a 5.", r.AvaliarCliente(nota, "x"));
        Assert.Null(r.NotaCliente);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    public void AvaliarCliente_aceita_os_limites(int nota)
    {
        var r = new Reserva();
        Assert.Null(r.AvaliarCliente(nota, " ótimo "));
        Assert.Equal(nota, r.NotaCliente);
        Assert.Equal("ótimo", r.ComentarioCliente);
    }

    // ── Edição ───────────────────────────────────────────────────────────────

    [Fact]
    public void AtualizarDados_campos_opcionais_em_branco_mantem_o_valor_atual()
    {
        var r = new Reserva
        {
            ClienteDocumento = "111", ClienteCep = "79000-000", ClienteEndereco = "Rua A", ClienteNumero = "10",
            ClienteCidade = "CG - MS", HoraEntrada = "08:00", HoraSaida = "22:00",
        };

        r.AtualizarDados(Agora, Agora.AddDays(1), 25, 900m, " Novo Nome ", " n@x.com ", "(67) 9 1111-2222",
            documento: "  ", cep: null, endereco: "Rua B", numero: "", cidade: null, horaEntrada: " 15:00 ", horaSaida: null);

        Assert.Equal("Novo Nome", r.ClienteNome);
        Assert.Equal("n@x.com", r.ClienteEmail);
        Assert.Equal("6791111" + "2222", r.ClienteTelefone);
        Assert.Equal(25, r.Pessoas);
        Assert.Equal(900m, r.Valor);
        Assert.Equal("111", r.ClienteDocumento);          // mantido
        Assert.Equal("79000-000", r.ClienteCep);          // mantido
        Assert.Equal("Rua B", r.ClienteEndereco);         // trocado
        Assert.Equal("10", r.ClienteNumero);              // mantido
        Assert.Equal("CG - MS", r.ClienteCidade);         // mantido
        Assert.Equal("15:00", r.HoraEntrada);             // trocado
        Assert.Equal("22:00", r.HoraSaida);               // mantido
    }
}
