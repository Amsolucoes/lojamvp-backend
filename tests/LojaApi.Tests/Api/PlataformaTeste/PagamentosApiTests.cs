using Microsoft.EntityFrameworkCore;
using static LojaApi.Tests.Suporte.CenariosPlataforma;

namespace LojaApi.Tests.Api.PlataformaTeste;

// Cobrança da mensalidade pelo Mercado Pago: criar cobrança (Pix, boleto, cartão), assinatura recorrente, webhook e status da fatura.
[Collection(ColecaoApi.Nome)]
public class PagamentosApiTests(ApiFixture api)
{
    private Task<LojaTeste> NovaAsync() => api.NovaLojaAsync();

    private static async Task<(int, string)> Erro(Task<Resposta> r) { var x = await r; return (x.Codigo, x.Erro); }

    // ── Criar cobrança ───────────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Exige_login_menos_o_webhook_que_e_publico()
    {
        var loja = await NovaAsync();
        var id = (await api.FaturaPendenteAsync(loja.Id)).Id;

        Assert.Equal(401, (await loja.Site.Post("/api/pagamentos/criar", CriarFatura(id, "pix"))).Codigo);
        Assert.Equal(401, (await loja.Site.Post("/api/pagamentos/assinatura", new { cardToken = "t" })).Codigo);
        Assert.Equal(401, (await loja.Site.Post("/api/pagamentos/assinatura/cancelar")).Codigo);
        Assert.Equal(401, (await loja.Site.Get($"/api/pagamentos/{id}/status")).Codigo);
        Assert.Equal(200, (int)(await api.WebhookAsync("{}")).StatusCode);
    }

    [FatoIntegracao]
    public async Task Criar_cobranca_valida_fatura_e_forma_de_pagamento()
    {
        var loja = await NovaAsync();
        var fatura = await api.FaturaPendenteAsync(loja.Id);

        Assert.Equal((404, "Fatura não encontrada."), await Erro(loja.Painel.Post("/api/pagamentos/criar", CriarFatura(Guid.NewGuid(), "pix"))));
        Assert.Equal((400, "Forma de pagamento inválida."), await Erro(loja.Painel.Post("/api/pagamentos/criar", CriarFatura(fatura.Id, "dinheiro"))));

        await api.NoBancoAsync(db => db.Pagamentos.Where(p => p.Id == fatura.Id).ExecuteUpdateAsync(s => s.SetProperty(p => p.Status, "pago")));
        Assert.Equal((400, "Esta fatura já foi paga."), await Erro(loja.Painel.Post("/api/pagamentos/criar", CriarFatura(fatura.Id, "pix"))));
    }

    [FatoIntegracao]
    public async Task Criar_pix_guarda_o_qr_code_na_fatura_e_devolve_os_dados_da_cobranca()
    {
        var loja = await NovaAsync();
        var fatura = await api.FaturaPendenteAsync(loja.Id);

        var r = await loja.Painel.Post("/api/pagamentos/criar", CriarFatura(fatura.Id, "pix"));

        Assert.Equal(200, r.Codigo);
        Assert.Equal(["status", "qrCode", "qrCodeBase64", "boletoUrl", "boletoBarcode", "mpPaymentId"], r.Json.EnumerateObject().Select(x => x.Name));
        Assert.Equal(("pending", "000201-qr-de-teste"), (r.Json.GetProperty("status").GetString()!, r.Json.GetProperty("qrCode").GetString()!));
        var banco = await api.PagamentoNoBancoAsync(fatura.Id);
        Assert.Equal(("pendente", "pix", "000201-qr-de-teste", r.Json.GetProperty("mpPaymentId").GetString()), (banco.Status, banco.FormaPagamento, banco.MpQrCode, banco.MpPaymentId));
        Assert.Contains(api.MercadoPago.PixCriados, p => p.Valor == fatura.Valor);
    }

    [FatoIntegracao]
    public async Task Criar_boleto_guarda_url_e_codigo_de_barras_e_pix_com_erro_do_mp_volta_400()
    {
        var loja = await NovaAsync();
        var fatura = await api.FaturaPendenteAsync(loja.Id);

        var boleto = await loja.Painel.Post("/api/pagamentos/criar", CriarFatura(fatura.Id, "boleto"));
        Assert.Equal(200, boleto.Codigo);
        var banco = await api.PagamentoNoBancoAsync(fatura.Id);
        Assert.Equal(("boleto", boleto.Json.GetProperty("boletoUrl").GetString(), "23790.00000 00000.000000"), (banco.FormaPagamento, banco.MpBoletoUrl, banco.MpBoletoBarcode));

        api.MercadoPago.FalharPix = true;
        try { Assert.Equal((400, "Mercado Pago fora do ar."), await Erro(loja.Painel.Post("/api/pagamentos/criar", CriarFatura(fatura.Id, "pix")))); }
        finally { api.MercadoPago.FalharPix = false; }
    }

    [FatoIntegracao]
    public async Task Cartao_aprovado_na_hora_paga_a_fatura_reativa_a_loja_e_cria_a_proxima_fatura()
    {
        var loja = await NovaAsync();
        var fatura = await api.FaturaPendenteAsync(loja.Id);

        var r = await loja.Painel.Post("/api/pagamentos/criar", CriarFatura(fatura.Id, "cartao", cardToken: "tok", parcelas: 1));

        Assert.Equal(("approved", 200), (r.Json.GetProperty("status").GetString()!, r.Codigo));
        var faturas = await api.PagamentosDaLojaNoBancoAsync(loja.Id);
        Assert.Equal(["pago", "pendente"], faturas.Select(p => p.Status));                                                 // a paga e a próxima, já pendente
        Assert.Equal(("cartao", r.Json.GetProperty("mpPaymentId").GetString()), (faturas[0].FormaPagamento, faturas[0].MpPaymentId));
        var l = await api.LojaNoBancoAsync(loja.Id);
        Assert.Equal(LojaApi.Models.StatusLoja.Ativo, l.Status);
        Assert.NotNull(l.ProximoVencimento);
    }

    [FatoIntegracao]
    public async Task Cartao_recusado_volta_400_com_o_motivo_e_nao_mexe_na_fatura()
    {
        var loja = await NovaAsync();
        var fatura = await api.FaturaPendenteAsync(loja.Id);

        Assert.Equal((400, "Cartão recusado pelo banco emissor."), await Erro(loja.Painel.Post("/api/pagamentos/criar", CriarFatura(fatura.Id, "cartao", cardToken: "cartao-recusado"))));

        var banco = await api.PagamentoNoBancoAsync(fatura.Id);
        Assert.Equal(("pendente", null), (banco.Status, banco.MpPaymentId));
    }

    // ── Assinatura recorrente ────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Assinatura_cria_guarda_o_cartao_final_e_recusa_uma_segunda()
    {
        var loja = await NovaAsync();

        var r = await loja.Painel.Post("/api/pagamentos/assinatura", new { cardToken = "tok", emailPagador = "pagador@teste.com", cartaoFinal = "4242" });

        Assert.Equal(200, r.Codigo);
        Assert.Equal(["status", "preapprovalId", "cartaoFinal"], r.Json.EnumerateObject().Select(x => x.Name));
        Assert.Equal(("authorized", "4242"), (r.Json.GetProperty("status").GetString()!, r.Json.GetProperty("cartaoFinal").GetString()!));
        var l = await api.LojaNoBancoAsync(loja.Id);
        Assert.Equal(("authorized", "4242", r.Json.GetProperty("preapprovalId").GetString()), (l.AssinaturaStatus, l.AssinaturaCartaoFinal, l.MpPreapprovalId));
        Assert.Contains(api.MercadoPago.AssinaturasCriadas, a => a.Email == "pagador@teste.com" && a.Valor == l.MensalidadeValor);

        Assert.Equal((400, "Esta loja já tem uma assinatura ativa."), await Erro(loja.Painel.Post("/api/pagamentos/assinatura", new { cardToken = "tok" })));
    }

    [FatoIntegracao]
    public async Task Assinatura_usa_o_email_da_loja_quando_nao_vem_pagador_e_erro_do_mp_volta_400()
    {
        var loja = await NovaAsync();
        var emailLoja = (await api.LojaNoBancoAsync(loja.Id)).Email;

        api.MercadoPago.FalharAssinatura = true;
        try { Assert.Equal((400, "Cartão recusado para a assinatura."), await Erro(loja.Painel.Post("/api/pagamentos/assinatura", new { cardToken = "tok" }))); }
        finally { api.MercadoPago.FalharAssinatura = false; }
        Assert.Null((await api.LojaNoBancoAsync(loja.Id)).MpPreapprovalId);

        (await loja.Painel.Post("/api/pagamentos/assinatura", new { cardToken = "tok" })).DeveSerSucesso();
        Assert.Contains(api.MercadoPago.AssinaturasCriadas, a => a.Email == emailLoja);
    }

    [FatoIntegracao]
    public async Task Cancelar_assinatura_exige_uma_ativa_e_marca_cancelada_no_banco()
    {
        var loja = await NovaAsync();
        Assert.Equal((400, "Nenhuma assinatura ativa."), await Erro(loja.Painel.Post("/api/pagamentos/assinatura/cancelar")));

        var preapproval = (await loja.Painel.Post("/api/pagamentos/assinatura", new { cardToken = "tok" })).Json.GetProperty("preapprovalId").GetString()!;

        api.MercadoPago.FalharCancelamento = true;
        try { Assert.Equal((400, "Erro ao cancelar no Mercado Pago."), await Erro(loja.Painel.Post("/api/pagamentos/assinatura/cancelar"))); }
        finally { api.MercadoPago.FalharCancelamento = false; }
        Assert.Equal("authorized", (await api.LojaNoBancoAsync(loja.Id)).AssinaturaStatus);

        var ok = await loja.Painel.Post("/api/pagamentos/assinatura/cancelar");
        Assert.Equal("cancelled", ok.Json.GetProperty("status").GetString());
        Assert.Equal("cancelled", (await api.LojaNoBancoAsync(loja.Id)).AssinaturaStatus);
        Assert.Contains(preapproval, api.MercadoPago.AssinaturasCanceladas);
    }

    // ── Webhook ──────────────────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Webhook_de_pagamento_aprovado_paga_a_fatura_e_nao_paga_duas_vezes()
    {
        var loja = await NovaAsync();
        var fatura = await api.FaturaPendenteAsync(loja.Id);
        var mpId = (await loja.Painel.Post("/api/pagamentos/criar", CriarFatura(fatura.Id, "pix"))).Json.GetProperty("mpPaymentId").GetString()!;

        Assert.Equal(200, (int)(await api.WebhookAsync(EventoMp("payment", mpId))).StatusCode);                          // ainda pendente no MP: nada muda
        Assert.Equal("pendente", (await api.PagamentoNoBancoAsync(fatura.Id)).Status);

        api.MercadoPago.Aprovar(mpId);
        Assert.Equal(200, (int)(await api.WebhookAsync(EventoMp("payment", mpId))).StatusCode);
        var paga = await api.PagamentoNoBancoAsync(fatura.Id);
        Assert.Equal(("pago", "pix", "Pagamento confirmado via Mercado Pago"), (paga.Status, paga.FormaPagamento, paga.Observacao));
        var qtd = (await api.PagamentosDaLojaNoBancoAsync(loja.Id)).Count;
        Assert.Equal(LojaApi.Models.StatusLoja.Ativo, (await api.LojaNoBancoAsync(loja.Id)).Status);

        await api.WebhookAsync(EventoMp("payment", mpId));                                                                  // repetido: não gera outra fatura
        Assert.Equal(qtd, (await api.PagamentosDaLojaNoBancoAsync(loja.Id)).Count);
    }

    [FatoIntegracao]
    public async Task Webhook_responde_200_para_tipo_desconhecido_json_invalido_e_pagamento_inexistente()
    {
        Assert.Equal(200, (int)(await api.WebhookAsync(EventoMp("outra_coisa", "1"))).StatusCode);
        Assert.Equal(200, (int)(await api.WebhookAsync("isto não é json")).StatusCode);
        Assert.Equal(200, (int)(await api.WebhookAsync("""{"type":"payment"}""")).StatusCode);
        Assert.Equal(200, (int)(await api.WebhookAsync(EventoMp("payment", "nao-existe-" + Guid.NewGuid().ToString("N")))).StatusCode);
    }

    [FatoIntegracao]
    public async Task Webhook_de_cobranca_recorrente_registra_o_pagamento_da_loja_da_assinatura()
    {
        var loja = await NovaAsync();
        var preapproval = (await loja.Painel.Post("/api/pagamentos/assinatura", new { cardToken = "tok" })).Json.GetProperty("preapprovalId").GetString()!;
        var auth = "auth-" + Guid.NewGuid().ToString("N");
        api.MercadoPago.DefinirCobrancaDeAssinatura(auth, preapproval, "pending");

        await api.WebhookAsync(EventoMp("subscription_authorized_payment", auth));
        Assert.DoesNotContain(await api.PagamentosDaLojaNoBancoAsync(loja.Id), p => p.Status == "pago");                  // ainda não processada

        api.MercadoPago.DefinirCobrancaDeAssinatura(auth, preapproval, "processed");
        await api.WebhookAsync(EventoMp("subscription_authorized_payment", auth));

        var paga = Assert.Single(await api.PagamentosDaLojaNoBancoAsync(loja.Id), p => p.Status == "pago");
        Assert.Equal(("cartao", "Cobrança recorrente automática (assinatura)", auth), (paga.FormaPagamento, paga.Observacao, paga.MpPaymentId));
        Assert.Equal(LojaApi.Models.StatusLoja.Ativo, (await api.LojaNoBancoAsync(loja.Id)).Status);

        api.MercadoPago.DefinirCobrancaDeAssinatura("auth-sem-loja", "pre-que-nao-existe", "processed");
        Assert.Equal(200, (int)(await api.WebhookAsync(EventoMp("subscription_authorized_payment", "auth-sem-loja"))).StatusCode);
    }

    [FatoIntegracao]
    public async Task Webhook_de_status_da_assinatura_atualiza_a_loja_e_ignora_desconhecidas()
    {
        var loja = await NovaAsync();
        var preapproval = (await loja.Painel.Post("/api/pagamentos/assinatura", new { cardToken = "tok" })).Json.GetProperty("preapprovalId").GetString()!;

        await api.WebhookAsync(EventoMp("subscription_preapproval", preapproval));                                         // MP não sabe o status: não muda
        Assert.Equal("authorized", (await api.LojaNoBancoAsync(loja.Id)).AssinaturaStatus);

        api.MercadoPago.DefinirStatusDaAssinatura(preapproval, "paused");
        await api.WebhookAsync(EventoMp("subscription_preapproval", preapproval));
        Assert.Equal("paused", (await api.LojaNoBancoAsync(loja.Id)).AssinaturaStatus);
    }

    // ── Status de uma fatura ─────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Status_da_fatura_404_se_nao_existe_e_confere_no_mp_quando_ha_cobranca_pendente()
    {
        var loja = await NovaAsync();
        Assert.Equal(404, (await loja.Painel.Get($"/api/pagamentos/{Guid.NewGuid()}/status")).Codigo);

        var fatura = await api.FaturaPendenteAsync(loja.Id);
        Assert.Equal("pendente", (await loja.Painel.Get($"/api/pagamentos/{fatura.Id}/status")).Json.GetProperty("status").GetString());      // sem cobrança no MP

        var mpId = (await loja.Painel.Post("/api/pagamentos/criar", CriarFatura(fatura.Id, "pix"))).Json.GetProperty("mpPaymentId").GetString()!;
        Assert.Equal("pendente", (await loja.Painel.Get($"/api/pagamentos/{fatura.Id}/status")).Json.GetProperty("status").GetString());

        api.MercadoPago.Aprovar(mpId);
        Assert.Equal("pago", (await loja.Painel.Get($"/api/pagamentos/{fatura.Id}/status")).Json.GetProperty("status").GetString());          // consultar já registra o pagamento
        Assert.Equal(LojaApi.Models.StatusLoja.Ativo, (await api.LojaNoBancoAsync(loja.Id)).Status);
    }

    // ── Isolamento entre lojas (correção: antes bastava saber o id da fatura) ──

    [FatoIntegracao]
    public async Task Assinante_nao_cria_cobranca_nem_consulta_a_fatura_de_outra_loja_mas_o_superadmin_pode()
    {
        var loja = await NovaAsync();
        var outra = await NovaAsync();
        var faturaDaOutra = await api.FaturaPendenteAsync(outra.Id);
        var super = await api.SuperadminAsync(loja);

        Assert.Equal((404, "Fatura não encontrada."), await Erro(loja.Painel.Post("/api/pagamentos/criar", CriarFatura(faturaDaOutra.Id, "pix"))));
        Assert.Equal(404, (await loja.Painel.Get($"/api/pagamentos/{faturaDaOutra.Id}/status")).Codigo);
        var banco = await api.PagamentoNoBancoAsync(faturaDaOutra.Id);
        Assert.Null(banco.MpPaymentId);                                                                                       // nada foi gerado na fatura alheia

        Assert.Equal(200, (await super.Post("/api/pagamentos/criar", CriarFatura(faturaDaOutra.Id, "pix"))).Codigo);
        Assert.Equal(200, (await super.Get($"/api/pagamentos/{faturaDaOutra.Id}/status")).Codigo);
        Assert.Equal(200, (await outra.Painel.Get($"/api/pagamentos/{faturaDaOutra.Id}/status")).Codigo);                       // a dona continua vendo
    }
}
