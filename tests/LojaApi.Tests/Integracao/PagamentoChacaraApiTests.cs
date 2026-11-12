using LojaApi.Tests.Chacara.Dominio;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace LojaApi.Tests.Integracao;

// Pagamento online da reserva (Pix, cartão e combinado) com o Mercado Pago de mentira (MercadoPagoFalso).
[Collection(ColecaoApi.Nome)]
public class PagamentoChacaraApiTests(ApiFixture api)
{
    // Pix e cartão cobram em cima do valor da reserva: sexta a domingo, 30 pessoas = R$ 2.200.
    private const decimal Valor = 2200m;

    private async Task<(LojaTeste Loja, int Id)> ReservaPendenteAsync(bool comPagamentoOnline = true, string? documento = "111.222.333-44")
    {
        var loja = await api.PrepararParaReservasAsync(comPagamentoOnline);
        var r = await loja.ReservarPeloSiteAsync(Datas.Sexta(), Datas.Domingo(), documento: documento);
        r.DeveSerSucesso();
        return (loja, r.Id);
    }

    private static string Pag(LojaTeste loja, int id, string rota) => $"{loja.Publico}/reservas/{id}/pagamento/{rota}";

    // ── Configurar o Mercado Pago da chácara ─────────────────────────────────

    [FatoIntegracao]
    public async Task Access_token_recusado_pelo_Mercado_Pago_nao_e_salvo()
    {
        var loja = await api.NovaLojaAsync();
        var r = await loja.Painel.Put("/api/chacara/info/pagamento", new { accessToken = "token-invalido", publicKey = "pk" });

        Assert.Equal(400, r.Codigo);
        Assert.Equal("O Mercado Pago não aceitou esse Access Token. Confira se copiou o de produção.", r.Erro);
        Assert.False((await loja.Painel.Get("/api/chacara/info/pagamento")).Json.GetProperty("configurado").GetBoolean());
    }

    [FatoIntegracao]
    public async Task Com_pagamento_online_configurado_a_reserva_segura_a_data_por_15_minutos()
    {
        var loja = await api.PrepararParaReservasAsync(comPagamentoOnline: true);
        var r = await loja.ReservarPeloSiteAsync(Datas.Sexta(), Datas.Sabado());

        Assert.False(r.Json.GetProperty("semPagamentoOnline").GetBoolean());
        Assert.Equal("Reserva criada. Prossiga para o pagamento.", r.Json.GetProperty("mensagem").GetString());
        var salva = await api.ReservaNoBancoAsync(r.Id);
        Assert.Null(salva.FormaPagamento);
        Assert.InRange(salva.ExpiraEm!.Value, DateTime.UtcNow.AddMinutes(14), DateTime.UtcNow.AddMinutes(16));
    }

    // ── Escolher a forma de pagamento ────────────────────────────────────────

    [FatoIntegracao]
    public async Task Escolher_pix_informa_a_metade_do_valor_e_a_chave_publica()
    {
        var (loja, id) = await ReservaPendenteAsync();
        var r = await loja.Site.Post(Pag(loja, id, "escolher"), new { formaPagamento = "pix" });

        Assert.Equal(200, r.Codigo);
        Assert.Equal(Valor / 2, r.Json.GetProperty("valorPix").GetDecimal());
        Assert.Equal(0m, r.Json.GetProperty("valorCartao").GetDecimal());
        Assert.Equal("APP_USR-chave-publica", r.Json.GetProperty("mpPublicKey").GetString());
        Assert.Equal("pix", (await api.ReservaNoBancoAsync(id)).FormaPagamento);
    }

    [FatoIntegracao]
    public async Task Escolher_cartao_soma_a_taxa_e_limita_as_parcelas()
    {
        var (loja, id) = await ReservaPendenteAsync();
        var r = await loja.Site.Post(Pag(loja, id, "escolher"), new { formaPagamento = "cartao" });

        Assert.Equal(2310m, r.Json.GetProperty("valorCartao").GetDecimal());   // 2.200 + 5% de taxa
        Assert.Equal(3, r.Json.GetProperty("parcelasMax").GetInt32());          // acima de R$ 800: até 3x
        Assert.Equal(0m, r.Json.GetProperty("valorPix").GetDecimal());
    }

    [FatoIntegracao]
    public async Task Escolher_combinado_divide_meio_a_meio()
    {
        var (loja, id) = await ReservaPendenteAsync();
        var r = await loja.Site.Post(Pag(loja, id, "escolher"), new { formaPagamento = "combinado" });

        Assert.Equal(1100m, r.Json.GetProperty("valorPix").GetDecimal());
        Assert.Equal(1155m, r.Json.GetProperty("valorCartao").GetDecimal());    // 1.100 + 5%
        Assert.Equal(1, r.Json.GetProperty("parcelasMax").GetInt32());          // combinado: cartão à vista
    }

    [FatoIntegracao]
    public async Task Escolher_recusa_forma_invalida_reserva_inexistente_e_chacara_sem_pagamento_online()
    {
        var (loja, id) = await ReservaPendenteAsync();
        Assert.Equal("Forma de pagamento inválida.", (await loja.Site.Post(Pag(loja, id, "escolher"), new { formaPagamento = "boleto" })).Erro);
        Assert.Equal(404, (await loja.Site.Post(Pag(loja, 999999, "escolher"), new { formaPagamento = "pix" })).Codigo);

        var (semMp, idSemMp) = await ReservaPendenteAsync(comPagamentoOnline: false);
        var r = await semMp.Site.Post(Pag(semMp, idSemMp, "escolher"), new { formaPagamento = "pix" });
        Assert.Equal(400, r.Codigo);
        Assert.Contains("pagamento online ainda não está configurado", r.Erro);
    }

    // ── Pix ──────────────────────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Pix_aprovado_confirma_a_reserva_pela_metade_do_valor()
    {
        var (loja, id) = await ReservaPendenteAsync();
        await loja.Site.Post(Pag(loja, id, "escolher"), new { formaPagamento = "pix" });

        var pix = await loja.Site.Post(Pag(loja, id, "pix"));
        Assert.Equal(200, pix.Codigo);
        Assert.Equal(Valor / 2, pix.Json.GetProperty("valor").GetDecimal());
        Assert.Equal("000201-qr-de-teste", pix.Json.GetProperty("qrCode").GetString());

        // ainda não pagou: continua pendente
        var aguardando = await loja.Site.Get(Pag(loja, id, "status"));
        Assert.Equal("pendente_pagamento", aguardando.Json.GetProperty("status").GetString());
        Assert.Equal("pending", aguardando.Json.GetProperty("mpStatusPix").GetString());

        // o Mercado Pago aprova: a próxima consulta confirma
        api.MercadoPago.Aprovar((await api.ReservaNoBancoAsync(id)).MpPaymentId!);
        var confirmada = await loja.Site.Get(Pag(loja, id, "status"));
        Assert.Equal("confirmada_parcial", confirmada.Json.GetProperty("status").GetString());

        var salva = await api.ReservaNoBancoAsync(id);
        Assert.Equal(Valor / 2, salva.ValorPago);
        Assert.Equal(Valor / 2, salva.SaldoPendente());
        Assert.Null(salva.ExpiraEm);
        Assert.NotNull(salva.DataConfirmacao);
    }

    [FatoIntegracao]
    public async Task Pix_usa_o_access_token_da_propria_chacara()
    {
        var (loja, id) = await ReservaPendenteAsync();
        await loja.Site.Post(Pag(loja, id, "escolher"), new { formaPagamento = "pix" });
        await loja.Site.Post(Pag(loja, id, "pix"));

        Assert.Contains(api.MercadoPago.PixCriados, p => p.TokenDaLoja == "APP_USR-token-de-teste" && p.Valor == Valor / 2);
    }

    [FatoIntegracao]
    public async Task Pix_exige_forma_escolhida_e_cpf()
    {
        var (loja, id) = await ReservaPendenteAsync();
        Assert.Equal("Escolha a forma de pagamento antes de gerar o Pix.", (await loja.Site.Post(Pag(loja, id, "pix"))).Erro);

        var (loja2, id2) = await ReservaPendenteAsync(documento: null);
        await loja2.Site.Post(Pag(loja2, id2, "escolher"), new { formaPagamento = "pix" });
        Assert.Equal("CPF é obrigatório para gerar o Pix.", (await loja2.Site.Post(Pag(loja2, id2, "pix"))).Erro);
    }

    // ── O Mercado Pago certo para cada loja ──────────────────────────────────
    // O dinheiro tem que cair na conta da PRÓPRIA chácara: o token dela tem que chegar ao Mercado Pago em toda chamada.

    private async Task<(LojaTeste Loja, int Id, string Token)> ReservaComTokenProprioAsync()
    {
        var loja = await api.PrepararParaReservasAsync();
        var token = "APP_USR-so-desta-loja-" + Guid.NewGuid().ToString("N")[..10];
        (await loja.Painel.Put("/api/chacara/info/pagamento", new { accessToken = token, publicKey = "APP_USR-pk" })).DeveSerSucesso();
        var id = (await loja.ReservarPeloSiteAsync(Datas.Sexta(), Datas.Domingo())).Id;
        return (loja, id, token);
    }

    [FatoIntegracao]
    public async Task Cartao_e_cobrado_com_o_access_token_da_propria_chacara()
    {
        var (loja, id, token) = await ReservaComTokenProprioAsync();
        await loja.Site.Post(Pag(loja, id, "escolher"), new { formaPagamento = "cartao" });

        (await loja.Site.Post(Pag(loja, id, "cartao"), new { token = "tok-ok", parcelas = 1 })).DeveSerSucesso();

        Assert.Contains(api.MercadoPago.CobrancasDeCartao, c => c.TokenDaLoja == token);
    }

    [FatoIntegracao]
    public async Task Consulta_de_status_do_pagamento_usa_o_access_token_da_propria_chacara()
    {
        var (loja, id, token) = await ReservaComTokenProprioAsync();
        await loja.Site.Post(Pag(loja, id, "escolher"), new { formaPagamento = "pix" });
        await loja.Site.Post(Pag(loja, id, "pix"));
        var pagamentoId = (await api.ReservaNoBancoAsync(id)).MpPaymentId!;

        await loja.Site.Get(Pag(loja, id, "status"));

        Assert.Contains(api.MercadoPago.StatusConsultados, s => s.PagamentoId == pagamentoId && s.TokenDaLoja == token);
    }

    [FatoIntegracao]
    public async Task Duas_chacaras_cobram_cada_uma_com_o_seu_token()
    {
        var (lojaA, idA, tokenA) = await ReservaComTokenProprioAsync();
        var (lojaB, idB, tokenB) = await ReservaComTokenProprioAsync();
        foreach (var (loja, id) in new[] { (lojaA, idA), (lojaB, idB) })
        {
            await loja.Site.Post(Pag(loja, id, "escolher"), new { formaPagamento = "pix" });
            await loja.Site.Post(Pag(loja, id, "pix"));
        }

        Assert.Contains(api.MercadoPago.PixCriados, p => p.TokenDaLoja == tokenA);
        Assert.Contains(api.MercadoPago.PixCriados, p => p.TokenDaLoja == tokenB);
        Assert.NotEqual(tokenA, tokenB);
    }

    [FatoIntegracao]
    public async Task Access_token_fica_cifrado_no_banco_e_so_volta_em_claro_para_o_Mercado_Pago()
    {
        var loja = await api.PrepararParaReservasAsync();
        var token = "APP_USR-segredo-" + Guid.NewGuid().ToString("N")[..10];
        (await loja.Painel.Put("/api/chacara/info/pagamento", new { accessToken = token, publicKey = "APP_USR-pk" })).DeveSerSucesso();

        var gravado = await api.NoBancoAsync(db => db.InfosChacara.AsNoTracking().Where(i => i.LojaId == loja.Id).Select(i => i.MpAccessTokenCifrado).SingleAsync());

        Assert.False(string.IsNullOrWhiteSpace(gravado));
        Assert.NotEqual(token, gravado);                 // não está em texto puro
        Assert.DoesNotContain(token, gravado!);
        Assert.DoesNotContain("APP_USR", gravado!);

        // e mesmo cifrado, o Mercado Pago recebe o original (prova de que decifrar funciona)
        var id = (await loja.ReservarPeloSiteAsync(Datas.Sexta(), Datas.Domingo())).Id;
        await loja.Site.Post(Pag(loja, id, "escolher"), new { formaPagamento = "pix" });
        await loja.Site.Post(Pag(loja, id, "pix"));
        Assert.Contains(api.MercadoPago.PixCriados, p => p.TokenDaLoja == token);
    }

    // ── Cartão ───────────────────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Cartao_aprovado_quita_a_reserva_cobrando_a_taxa()
    {
        var (loja, id) = await ReservaPendenteAsync();
        await loja.Site.Post(Pag(loja, id, "escolher"), new { formaPagamento = "cartao" });

        var r = await loja.Site.Post(Pag(loja, id, "cartao"), new { token = "tok-ok", parcelas = 3 });

        Assert.Equal(200, r.Codigo);
        Assert.Equal("approved", r.Json.GetProperty("status").GetString());
        Assert.Equal(2310m, r.Json.GetProperty("valor").GetDecimal());
        Assert.Contains(api.MercadoPago.CobrancasDeCartao, c => c.Valor == 2310m && c.Parcelas == 3);

        var salva = await api.ReservaNoBancoAsync(id);
        Assert.Equal("confirmada", salva.Status);
        Assert.Equal(Valor, salva.ValorPago);               // a taxa do cartão não entra no valor pago da reserva
        Assert.Equal(0m, salva.SaldoPendente());
    }

    [FatoIntegracao]
    public async Task Cartao_recusado_mantem_a_reserva_pendente_e_mostra_o_motivo()
    {
        var (loja, id) = await ReservaPendenteAsync();
        await loja.Site.Post(Pag(loja, id, "escolher"), new { formaPagamento = "cartao" });

        var r = await loja.Site.Post(Pag(loja, id, "cartao"), new { token = "cartao-recusado", parcelas = 1 });

        Assert.Equal(400, r.Codigo);
        Assert.Equal("Cartão recusado pelo banco emissor.", r.Erro);
        Assert.Equal("pendente_pagamento", (await api.ReservaNoBancoAsync(id)).Status);
    }

    [FatoIntegracao]
    public async Task Cartao_valida_parcelas_forma_escolhida_e_cpf()
    {
        var (loja, id) = await ReservaPendenteAsync();
        Assert.Equal("Escolha a forma de pagamento antes de cobrar o cartão.", (await loja.Site.Post(Pag(loja, id, "cartao"), new { token = "t", parcelas = 1 })).Erro);

        await loja.Site.Post(Pag(loja, id, "escolher"), new { formaPagamento = "cartao" });
        Assert.Equal("Para esse valor, o máximo é 3x.", (await loja.Site.Post(Pag(loja, id, "cartao"), new { token = "t", parcelas = 4 })).Erro);
        Assert.Equal("Para esse valor, o máximo é 3x.", (await loja.Site.Post(Pag(loja, id, "cartao"), new { token = "t", parcelas = 0 })).Erro);

        var (loja2, id2) = await ReservaPendenteAsync(documento: null);
        await loja2.Site.Post(Pag(loja2, id2, "escolher"), new { formaPagamento = "cartao" });
        Assert.Equal("CPF é obrigatório para pagar no cartão.", (await loja2.Site.Post(Pag(loja2, id2, "cartao"), new { token = "t", parcelas = 1 })).Erro);
    }

    // ── Combinado (metade Pix, metade cartão) ────────────────────────────────

    [FatoIntegracao]
    public async Task Combinado_so_confirma_quando_Pix_e_cartao_foram_aprovados()
    {
        var (loja, id) = await ReservaPendenteAsync();
        await loja.Site.Post(Pag(loja, id, "escolher"), new { formaPagamento = "combinado" });
        await loja.Site.Post(Pag(loja, id, "pix"));

        // cartão aprova primeiro, mas o Pix ainda não: continua pendente
        var cartao = await loja.Site.Post(Pag(loja, id, "cartao"), new { token = "tok", parcelas = 1 });
        Assert.Equal(1155m, cartao.Json.GetProperty("valor").GetDecimal());
        Assert.Equal("pendente_pagamento", (await api.ReservaNoBancoAsync(id)).Status);

        // parcelar o combinado não é permitido
        Assert.Equal(400, (await loja.Site.Post(Pag(loja, id, "cartao"), new { token = "tok", parcelas = 2 })).Codigo);

        api.MercadoPago.Aprovar((await api.ReservaNoBancoAsync(id)).MpPaymentId!);
        var status = await loja.Site.Get(Pag(loja, id, "status"));

        Assert.Equal("confirmada", status.Json.GetProperty("status").GetString());
        Assert.Equal(Valor, (await api.ReservaNoBancoAsync(id)).ValorPago);
    }

    // ── Status ───────────────────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Status_confirma_apenas_uma_vez_e_avisa_o_cliente()
    {
        var (loja, id) = await ReservaPendenteAsync();
        await loja.Site.Post(Pag(loja, id, "escolher"), new { formaPagamento = "pix" });
        await loja.Site.Post(Pag(loja, id, "pix"));
        api.MercadoPago.Aprovar((await api.ReservaNoBancoAsync(id)).MpPaymentId!);
        api.Resend.ClearReceivedCalls();

        await loja.Site.Get(Pag(loja, id, "status"));
        var confirmadaEm = (await api.ReservaNoBancoAsync(id)).DataConfirmacao;
        var envios = api.Resend.ReceivedCalls().Count(c => c.GetMethodInfo().Name == "EmailSendAsync");
        Assert.True(envios >= 1, "Esperava e-mail de confirmação.");

        // consultar de novo não confirma outra vez nem manda outro e-mail
        api.Resend.ClearReceivedCalls();
        await loja.Site.Get(Pag(loja, id, "status"));
        Assert.Equal(confirmadaEm, (await api.ReservaNoBancoAsync(id)).DataConfirmacao);
        Assert.Equal(0, api.Resend.ReceivedCalls().Count(c => c.GetMethodInfo().Name == "EmailSendAsync"));
    }

    [FatoIntegracao]
    public async Task Status_de_reserva_inexistente_da_404_e_sem_pagamento_nao_muda_nada()
    {
        var (loja, id) = await ReservaPendenteAsync();
        Assert.Equal(404, (await loja.Site.Get(Pag(loja, 999999, "status"))).Codigo);

        var r = await loja.Site.Get(Pag(loja, id, "status"));
        Assert.Equal("pendente_pagamento", r.Json.GetProperty("status").GetString());
        Assert.Equal(JsonValueKind.Null, r.Json.GetProperty("mpStatusPix").ValueKind);
    }

    [FatoIntegracao]
    public async Task Depois_de_cancelada_a_reserva_nao_aceita_nova_forma_de_pagamento()
    {
        var (loja, id) = await ReservaPendenteAsync();
        await loja.Site.Post($"{loja.Publico}/reservas/{id}/cancelar");

        var r = await loja.Site.Post(Pag(loja, id, "escolher"), new { formaPagamento = "pix" });
        Assert.Equal("Essa reserva não está mais aguardando pagamento.", r.Erro);
    }
}
