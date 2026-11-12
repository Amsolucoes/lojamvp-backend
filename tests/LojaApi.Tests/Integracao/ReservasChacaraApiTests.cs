using LojaApi.Tests.Chacara.Dominio;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Resend;

namespace LojaApi.Tests.Integracao;

// Reservas vistas pelo dono da chácara (painel): criar, confirmar, negociar, expirar, editar, contrato, dashboard.
[Collection(ColecaoApi.Nome)]
public class ReservasChacaraApiTests(ApiFixture api)
{
    // ── Reserva manual ───────────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Reserva_manual_nasce_confirmada_com_o_valor_pago_informado()
    {
        var loja = await api.PrepararParaReservasAsync();
        var r = await loja.CriarReservaManualAsync(Datas.Sexta(), Datas.Domingo(), valor: 5500, valorPago: 2000);

        Assert.Equal(200, r.Codigo);
        Assert.Equal("confirmada", r.Json.GetProperty("status").GetString());
        Assert.Equal(2000m, r.Json.GetProperty("valorPago").GetDecimal());
        Assert.Equal(5500m, r.Json.GetProperty("valor").GetDecimal());
        Assert.Equal("67999990000", r.Json.GetProperty("clienteTelefone").GetString());
        Assert.Equal($"{Datas.Texto(Datas.Sexta())}T12:00:00Z", r.Json.GetProperty("dataInicio").GetString());

        var naLista = await loja.ReservaNaListaAsync(r.Id);
        Assert.Equal("Maria Silva", naLista.GetProperty("clienteNome").GetString());
    }

    [FatoIntegracao]
    public async Task Reserva_manual_sem_valor_pago_assume_pagamento_integral()
    {
        var loja = await api.PrepararParaReservasAsync();
        var r = await loja.CriarReservaManualAsync(Datas.Sexta(), Datas.Sabado(), valor: 1400);
        Assert.Equal(1400m, r.Json.GetProperty("valorPago").GetDecimal());
    }

    [FatoIntegracao]
    public async Task Reserva_manual_nao_aceita_datas_que_conflitam()
    {
        var loja = await api.PrepararParaReservasAsync();
        (await loja.CriarReservaManualAsync(Datas.Sexta(), Datas.Domingo())).DeveSerSucesso();

        var conflito = await loja.CriarReservaManualAsync(Datas.Sabado(), Datas.Sabado(), nome: "Conflito");
        Assert.Equal(409, conflito.Codigo);
        Assert.Equal("Essas datas conflitam com outra reserva existente.", conflito.Erro);

        // encostar no último dia também conflita; o dia seguinte é livre
        Assert.Equal(409, (await loja.CriarReservaManualAsync(Datas.Domingo(), Datas.Domingo().AddDays(1))).Codigo);
        Assert.Equal(200, (await loja.CriarReservaManualAsync(Datas.Domingo().AddDays(1), Datas.Domingo().AddDays(1))).Codigo);
    }

    [FatoIntegracao]
    public async Task Data_volta_a_ficar_livre_quando_a_reserva_pendente_expira()
    {
        var loja = await api.PrepararParaReservasAsync();
        var pendente = await loja.ReservarPeloSiteAsync(Datas.Sexta(), Datas.Sabado());
        Assert.Equal(409, (await loja.CriarReservaManualAsync(Datas.Sexta(), Datas.Sexta())).Codigo);

        (await loja.Painel.Patch($"/api/chacara/reservas/{pendente.Id}/marcar-expirada")).DeveSerSucesso();

        Assert.Equal(200, (await loja.CriarReservaManualAsync(Datas.Sexta(), Datas.Sexta())).Codigo);
    }

    // ── Confirmação e pagamentos ─────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Confirmar_reserva_pendente_com_pagamento_parcial_e_depois_quitar()
    {
        var loja = await api.PrepararParaReservasAsync();
        var id = (await loja.ReservarPeloSiteAsync(Datas.Sexta(), Datas.Domingo())).Id;

        var confirmada = await loja.Painel.Patch($"/api/chacara/reservas/{id}/confirmar", new { valorPago = 500 });
        Assert.Equal(200, confirmada.Codigo);
        Assert.Equal("confirmada", confirmada.Json.GetProperty("status").GetString());
        Assert.Equal(500m, confirmada.Json.GetProperty("valorPago").GetDecimal());
        Assert.Equal(1700m, confirmada.Json.GetProperty("saldoPendente").GetDecimal());   // 2.200 - 500

        Assert.Equal("Esta reserva já está confirmada.", (await loja.Painel.Patch($"/api/chacara/reservas/{id}/confirmar", new { valorPago = 500 })).Erro);

        var parcial = await loja.Painel.Patch($"/api/chacara/reservas/{id}/registrar-pagamento", new { valor = 700 });
        Assert.Equal(1000m, parcial.Json.GetProperty("saldoPendente").GetDecimal());
        Assert.False(parcial.Json.GetProperty("quitada").GetBoolean());

        var quitada = await loja.Painel.Patch($"/api/chacara/reservas/{id}/registrar-pagamento", new { valor = 999999 });
        Assert.True(quitada.Json.GetProperty("quitada").GetBoolean());
        Assert.Equal(2200m, quitada.Json.GetProperty("valorPago").GetDecimal());           // nunca passa do valor
        Assert.Equal(0m, quitada.Json.GetProperty("saldoPendente").GetDecimal());
    }

    [FatoIntegracao]
    public async Task Confirmar_sem_informar_valor_assume_pagamento_integral()
    {
        var loja = await api.PrepararParaReservasAsync();
        var id = (await loja.ReservarPeloSiteAsync(Datas.Sexta(), Datas.Sabado())).Id;

        var r = await loja.Painel.Patch($"/api/chacara/reservas/{id}/confirmar", new { });
        Assert.Equal(1400m, r.Json.GetProperty("valorPago").GetDecimal());
    }

    [FatoIntegracao]
    public async Task Registrar_pagamento_valida_valor_e_situacao()
    {
        var loja = await api.PrepararParaReservasAsync();
        var pendente = (await loja.ReservarPeloSiteAsync(Datas.Sexta(), Datas.Sabado())).Id;
        var confirmada = (await loja.CriarReservaManualAsync(Datas.Sexta(1), Datas.Sabado(1), valor: 1000, valorPago: 100)).Id;

        Assert.Equal(400, (await loja.Painel.Patch($"/api/chacara/reservas/{pendente}/registrar-pagamento", new { valor = 10 })).Codigo);
        Assert.Equal("Informe um valor de pagamento maior que zero.", (await loja.Painel.Patch($"/api/chacara/reservas/{confirmada}/registrar-pagamento", new { valor = 0 })).Erro);
        Assert.Equal(404, (await loja.Painel.Patch("/api/chacara/reservas/999999/registrar-pagamento", new { valor = 10 })).Codigo);
    }

    // ── Negociação, expiração e reativação ───────────────────────────────────

    [FatoIntegracao]
    public async Task Negociacao_tira_e_devolve_o_prazo_de_expiracao()
    {
        var loja = await api.PrepararParaReservasAsync();
        var id = (await loja.ReservarPeloSiteAsync(Datas.Sexta(), Datas.Sabado())).Id;

        var mantida = await loja.Painel.Patch($"/api/chacara/reservas/{id}/manter-negociacao");
        Assert.Equal(JsonValueKind.Null, mantida.Json.GetProperty("expiraEm").ValueKind);
        Assert.Null((await api.ReservaNoBancoAsync(id)).ExpiraEm);

        var desfeita = await loja.Painel.Patch($"/api/chacara/reservas/{id}/desfazer-negociacao");
        Assert.NotEqual(JsonValueKind.Null, desfeita.Json.GetProperty("expiraEm").ValueKind);
        Assert.NotNull((await api.ReservaNoBancoAsync(id)).ExpiraEm);

        Assert.Equal(404, (await loja.Painel.Patch("/api/chacara/reservas/999999/manter-negociacao")).Codigo);
    }

    [FatoIntegracao]
    public async Task Reserva_em_negociacao_continua_ocupando_a_agenda()
    {
        var loja = await api.PrepararParaReservasAsync();
        var id = (await loja.ReservarPeloSiteAsync(Datas.Sexta(), Datas.Sabado())).Id;
        await loja.Painel.Patch($"/api/chacara/reservas/{id}/manter-negociacao");

        var disp = await loja.Site.Get($"{loja.Publico}/disponibilidade?dataInicio={Datas.Texto(Datas.Sexta())}&dataFim={Datas.Texto(Datas.Sexta())}");
        Assert.False(disp.Json.GetProperty("disponivel").GetBoolean());

        var dash = (await loja.Painel.Get("/api/chacara/dashboard")).Json.GetProperty("geral");
        Assert.Equal(1, dash.GetProperty("totalNegociacao").GetInt32());
    }

    [FatoIntegracao]
    public async Task Marcar_como_expirada_lista_em_perdidas_e_pode_reativar()
    {
        var loja = await api.PrepararParaReservasAsync();
        var id = (await loja.ReservarPeloSiteAsync(Datas.Sexta(), Datas.Sabado())).Id;

        var expirada = await loja.Painel.Patch($"/api/chacara/reservas/{id}/marcar-expirada");
        Assert.Equal("expirada", expirada.Json.GetProperty("status").GetString());
        Assert.Equal("Só é possível marcar como expirada uma reserva pendente de pagamento.",
            (await loja.Painel.Patch($"/api/chacara/reservas/{id}/marcar-expirada")).Erro);

        var perdidas = (await loja.Painel.Get("/api/chacara/reservas/perdidas")).Json;
        Assert.Equal(1, perdidas.GetProperty("total").GetInt32());
        Assert.Equal(id, perdidas.GetProperty("itens")[0].GetProperty("id").GetInt32());

        var reativada = await loja.Painel.Patch($"/api/chacara/reservas/{id}/reativar");
        Assert.Equal("pendente_pagamento", reativada.Json.GetProperty("status").GetString());
        Assert.Equal("Só é possível reativar uma reserva expirada.", (await loja.Painel.Patch($"/api/chacara/reservas/{id}/reativar")).Erro);
        Assert.Equal(0, (await loja.Painel.Get("/api/chacara/reservas/perdidas")).Json.GetProperty("total").GetInt32());
    }

    [FatoIntegracao]
    public async Task Perdidas_filtra_por_periodo_e_pagina()
    {
        var loja = await api.PrepararParaReservasAsync();
        for (var i = 0; i < 3; i++)
        {
            var id = (await loja.ReservarPeloSiteAsync(Datas.Sexta(i), Datas.Sabado(i), nome: $"Cliente {i}")).Id;
            await loja.Painel.Patch($"/api/chacara/reservas/{id}/marcar-expirada");
        }

        var pagina = (await loja.Painel.Get("/api/chacara/reservas/perdidas?de=2000-01-01&ate=2099-01-01&pagina=1&porPagina=2")).Json;
        Assert.Equal(3, pagina.GetProperty("total").GetInt32());
        Assert.Equal(2, pagina.GetProperty("itens").GetArrayLength());

        var vazio = (await loja.Painel.Get("/api/chacara/reservas/perdidas?de=2000-01-01&ate=2000-12-31")).Json;
        Assert.Equal(0, vazio.GetProperty("total").GetInt32());
    }

    // ── Prejuízo e avaliação do cliente ──────────────────────────────────────

    [FatoIntegracao]
    public async Task Prejuizo_e_avaliacao_do_cliente_validam_e_gravam()
    {
        var loja = await api.PrepararParaReservasAsync();
        var id = (await loja.CriarReservaManualAsync(Datas.Sexta(), Datas.Sabado())).Id;

        Assert.Equal("O valor não pode ser negativo.", (await loja.Painel.Patch($"/api/chacara/reservas/{id}/prejuizo", new { valor = -5, observacao = "x" })).Erro);
        var prejuizo = await loja.Painel.Patch($"/api/chacara/reservas/{id}/prejuizo", new { valor = 150.5, observacao = "  quebrou cadeira " });
        Assert.Equal(150.5m, prejuizo.Json.GetProperty("valorPrejuizo").GetDecimal());
        Assert.Equal("quebrou cadeira", prejuizo.Json.GetProperty("observacaoPrejuizo").GetString());

        Assert.Equal("A nota deve ser de 1 a 5.", (await loja.Painel.Patch($"/api/chacara/reservas/{id}/avaliar-cliente", new { nota = 9, comentario = "x" })).Erro);
        var nota = await loja.Painel.Patch($"/api/chacara/reservas/{id}/avaliar-cliente", new { nota = 4, comentario = " ótimo " });
        Assert.Equal(4, nota.Json.GetProperty("notaCliente").GetInt32());
        Assert.Equal("ótimo", nota.Json.GetProperty("comentarioCliente").GetString());

        var gravado = await api.ReservaNoBancoAsync(id);
        Assert.Equal(150.5m, gravado.ValorPrejuizo);
        Assert.Equal(4, gravado.NotaCliente);
    }

    // ── Edição ───────────────────────────────────────────────────────────────

    private static object Edicao(DateTime ini, DateTime fim, int pessoas = 30, string nome = "Maria Silva", decimal? valorManual = null, string? horaEntrada = null) => new
    {
        dataInicio = Datas.Texto(ini), dataFim = Datas.Texto(fim), pessoas,
        clienteNome = nome, clienteEmail = "maria@x.com", clienteTelefone = "(67) 9 1111-3333",
        horaEntrada, valorManual,
    };

    [FatoIntegracao]
    public async Task Editar_valida_datas_periodo_maximo_e_numero_de_pessoas()
    {
        var loja = await api.PrepararParaReservasAsync();
        var id = (await loja.ReservarPeloSiteAsync(Datas.Sexta(), Datas.Sabado())).Id;

        Assert.Equal("Data final não pode ser antes da data inicial.", (await loja.Painel.Put($"/api/chacara/reservas/{id}", Edicao(Datas.Sabado(), Datas.Sexta()))).Erro);
        Assert.Equal("O período máximo por reserva é de 30 dias.", (await loja.Painel.Put($"/api/chacara/reservas/{id}", Edicao(Datas.Sexta(), Datas.Sexta().AddDays(60)))).Erro);
        Assert.Equal("O mínimo é de 3 pessoas.", (await loja.Painel.Put($"/api/chacara/reservas/{id}", Edicao(Datas.Sexta(), Datas.Sabado(), pessoas: 1))).Erro);
        Assert.Equal(404, (await loja.Painel.Put("/api/chacara/reservas/999999", Edicao(Datas.Sexta(), Datas.Sabado()))).Codigo);
    }

    [FatoIntegracao]
    public async Task Editar_nao_deixa_invadir_outra_reserva_mas_pode_manter_as_proprias_datas()
    {
        var loja = await api.PrepararParaReservasAsync();
        var a = (await loja.CriarReservaManualAsync(Datas.Sexta(), Datas.Sabado(), nome: "A")).Id;
        var b = (await loja.CriarReservaManualAsync(Datas.Sexta(1), Datas.Sabado(1), nome: "B")).Id;

        Assert.Equal(409, (await loja.Painel.Put($"/api/chacara/reservas/{b}", Edicao(Datas.Sabado(), Datas.Domingo(), nome: "B"))).Codigo);
        Assert.Equal(200, (await loja.Painel.Put($"/api/chacara/reservas/{a}", Edicao(Datas.Sexta(), Datas.Sabado(), nome: "A editada"))).Codigo);
        Assert.Equal("A editada", (await loja.ReservaNaListaAsync(a)).GetProperty("clienteNome").GetString());
    }

    [FatoIntegracao]
    public async Task Editar_recalcula_o_valor_e_valor_manual_vence_o_calculo()
    {
        var loja = await api.PrepararParaReservasAsync();
        var id = (await loja.ReservarPeloSiteAsync(Datas.Sexta(), Datas.Sabado())).Id;     // 1.400

        var recalculado = await loja.Painel.Put($"/api/chacara/reservas/{id}", Edicao(Datas.Sexta(), Datas.Domingo()));
        Assert.Equal(200, recalculado.Codigo);
        Assert.Equal(2200m, recalculado.Json.GetProperty("reserva").GetProperty("valor").GetDecimal()); // pacote + domingo

        var manual = await loja.Painel.Put($"/api/chacara/reservas/{id}", Edicao(Datas.Sexta(), Datas.Domingo(), valorManual: 777));
        Assert.Equal(777m, manual.Json.GetProperty("reserva").GetProperty("valor").GetDecimal());
        Assert.Equal("67911113333", manual.Json.GetProperty("reserva").GetProperty("clienteTelefone").GetString());
    }

    // ── Exclusão ─────────────────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Excluir_remove_a_reserva_e_libera_a_data()
    {
        var loja = await api.PrepararParaReservasAsync();
        var id = (await loja.CriarReservaManualAsync(Datas.Sexta(), Datas.Sabado())).Id;

        Assert.Equal(200, (await loja.Painel.Delete($"/api/chacara/reservas/{id}")).Codigo);
        Assert.Equal(404, (await loja.Painel.Delete($"/api/chacara/reservas/{id}")).Codigo);
        Assert.Equal(0, (await loja.Painel.Get("/api/chacara/reservas")).Json.GetArrayLength());
        Assert.Equal(200, (await loja.CriarReservaManualAsync(Datas.Sexta(), Datas.Sabado())).Codigo);
    }

    // ── Contrato ─────────────────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Contrato_em_PDF_e_gerado_para_a_reserva()
    {
        var loja = await api.PrepararParaReservasAsync();
        var id = (await loja.CriarReservaManualAsync(Datas.Sexta(), Datas.Sabado())).Id;

        var pdf = await loja.Painel.Get($"/api/chacara/reservas/{id}/contrato-pdf");
        Assert.Equal(200, pdf.Codigo);
        Assert.Equal("application/pdf", pdf.TipoConteudo);
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(pdf.Bytes, 0, 4));
        Assert.True(pdf.Bytes.Length > 1000);

        Assert.Equal(404, (await loja.Painel.Get("/api/chacara/reservas/999999/contrato-pdf")).Codigo);
    }

    [FatoIntegracao]
    public async Task Enviar_contrato_por_email_dispara_o_envio()
    {
        var loja = await api.PrepararParaReservasAsync();
        var id = (await loja.CriarReservaManualAsync(Datas.Sexta(), Datas.Sabado())).Id;
        api.Resend.ClearReceivedCalls();

        var r = await loja.Painel.Post($"/api/chacara/reservas/{id}/enviar-contrato");

        Assert.Equal(200, r.Codigo);
        Assert.Equal("Contrato enviado com sucesso.", r.Json.GetProperty("mensagem").GetString());
        await api.Resend.ReceivedWithAnyArgs().EmailSendAsync(default(EmailMessage)!, default);
    }

    [FatoIntegracao]
    public async Task Enviar_contrato_exige_email_do_cliente()
    {
        var loja = await api.PrepararParaReservasAsync();
        var id = (await loja.CriarReservaManualAsync(Datas.Sexta(), Datas.Sabado(), email: null)).Id;

        var r = await loja.Painel.Post($"/api/chacara/reservas/{id}/enviar-contrato");
        Assert.Equal(400, r.Codigo);
        Assert.Equal("Esta reserva não tem e-mail de cliente cadastrado.", r.Erro);
    }

    // ── Dashboard ────────────────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Dashboard_soma_pago_e_pendente_das_reservas_confirmadas()
    {
        var loja = await api.PrepararParaReservasAsync();
        await loja.CriarReservaManualAsync(Datas.Sexta(), Datas.Domingo(), valor: 5500, valorPago: 2000);
        await loja.CriarReservaManualAsync(Datas.Sexta(1), Datas.Sabado(1), valor: 1400, valorPago: 1400);

        var geral = (await loja.Painel.Get("/api/chacara/dashboard")).Json.GetProperty("geral");
        Assert.Equal(2, geral.GetProperty("totalReservas").GetInt32());
        Assert.Equal(3400m, geral.GetProperty("totalPago").GetDecimal());
        Assert.Equal(3500m, geral.GetProperty("totalPendente").GetDecimal());
    }

    [FatoIntegracao]
    public async Task Dashboard_marca_os_dias_ocupados_do_mes()
    {
        var loja = await api.PrepararParaReservasAsync();
        var dia = Datas.Sexta();
        await loja.CriarReservaManualAsync(dia, dia.AddDays(2), valor: 2200, valorPago: 0);

        var json = (await loja.Painel.Get($"/api/chacara/dashboard?ano={dia.Year}")).Json;
        Assert.Equal(dia.Year, json.GetProperty("anoSelecionado").GetInt32());
        var meses = json.GetProperty("meses").EnumerateArray().ToList();
        Assert.Equal(3, meses.Sum(m => m.GetProperty("diasOcupados").GetInt32()));
    }
}
