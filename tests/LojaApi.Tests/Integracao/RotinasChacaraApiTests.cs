using LojaApi.Modules.Chacara.Application;
using LojaApi.Tests.Chacara.Dominio;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace LojaApi.Tests.Integracao;

// As rotinas automáticas da chácara (rodam de hora em hora em produção). Aqui são chamadas direto,
// porque os serviços em segundo plano ficam desligados durante os testes.
[Collection(ColecaoApi.Nome)]
public class RotinasChacaraApiTests(ApiFixture api)
{
    private Task<int> Expirar() => api.ServicoAsync<ReservaChacaraNotificacaoService, int>(s => s.ExpirarReservasVencidasAsync());
    private Task<int> Avisar() => api.ServicoAsync<ReservaChacaraNotificacaoService, int>(s => s.EnviarAvisosReservasSemPagamentoAsync());

    private Task DefinirPrazo(int id, DateTime? expiraEm) =>
        api.NoBancoAsync(db => db.Reservas.Where(r => r.Id == id).ExecuteUpdateAsync(s => s.SetProperty(r => r.ExpiraEm, expiraEm)));

    [FatoIntegracao]
    public async Task Rotina_expira_reservas_pendentes_com_prazo_vencido_e_libera_a_data()
    {
        var loja = await api.PrepararParaReservasAsync();
        var id = (await loja.ReservarPeloSiteAsync(Datas.Sexta(), Datas.Sabado())).Id;
        await DefinirPrazo(id, DateTime.UtcNow.AddMinutes(-5));

        var expiradas = await Expirar();

        Assert.True(expiradas >= 1);
        Assert.Equal("expirada", (await api.ReservaNoBancoAsync(id)).Status);
        Assert.Equal(1, (await loja.Painel.Get("/api/chacara/reservas/perdidas")).Json.GetProperty("total").GetInt32());
        Assert.Equal(200, (await loja.CriarReservaManualAsync(Datas.Sexta(), Datas.Sabado())).Codigo);
    }

    [FatoIntegracao]
    public async Task Rotina_nao_mexe_em_reserva_no_prazo_em_negociacao_ou_confirmada()
    {
        var loja = await api.PrepararParaReservasAsync();
        var noPrazo = (await loja.ReservarPeloSiteAsync(Datas.Sexta(), Datas.Sabado(), nome: "No prazo")).Id;
        var negociacao = (await loja.ReservarPeloSiteAsync(Datas.Sexta(1), Datas.Sabado(1), nome: "Negociação")).Id;
        await loja.Painel.Patch($"/api/chacara/reservas/{negociacao}/manter-negociacao");
        var confirmada = (await loja.CriarReservaManualAsync(Datas.Sexta(2), Datas.Sabado(2))).Id;
        await DefinirPrazo(confirmada, DateTime.UtcNow.AddDays(-30));   // mesmo com prazo velho, confirmada não expira

        await Expirar();

        Assert.Equal("pendente_pagamento", (await api.ReservaNoBancoAsync(noPrazo)).Status);
        Assert.Equal("pendente_pagamento", (await api.ReservaNoBancoAsync(negociacao)).Status);
        Assert.Equal("confirmada", (await api.ReservaNoBancoAsync(confirmada)).Status);
    }

    [FatoIntegracao]
    public async Task Rotina_e_idempotente()
    {
        var loja = await api.PrepararParaReservasAsync();
        var id = (await loja.ReservarPeloSiteAsync(Datas.Sexta(), Datas.Sabado())).Id;
        await DefinirPrazo(id, DateTime.UtcNow.AddMinutes(-5));

        await Expirar();
        Assert.Equal(0, await Expirar());
    }

    [FatoIntegracao]
    public async Task Aviso_de_24h_so_vai_para_reserva_sem_pagamento_online_antiga_e_so_uma_vez()
    {
        var loja = await api.PrepararParaReservasAsync();   // sem Mercado Pago: reserva nasce "sem_pagamento_online" (48h)
        var antiga = (await loja.ReservarPeloSiteAsync(Datas.Sexta(), Datas.Sabado(), nome: "Antiga")).Id;
        var recente = (await loja.ReservarPeloSiteAsync(Datas.Sexta(1), Datas.Sabado(1), nome: "Recente")).Id;
        await api.NoBancoAsync(db => db.Reservas.Where(r => r.Id == antiga).ExecuteUpdateAsync(s => s.SetProperty(r => r.CriadoEm, DateTime.UtcNow.AddHours(-25))));
        api.Resend.ClearReceivedCalls();

        var enviados = await Avisar();

        Assert.True(enviados >= 1);
        Assert.True((await api.ReservaNoBancoAsync(antiga)).AvisoExpiracaoEnviado);
        Assert.False((await api.ReservaNoBancoAsync(recente)).AvisoExpiracaoEnviado);
        Assert.True(api.Resend.ReceivedCalls().Count(c => c.GetMethodInfo().Name == "EmailSendAsync") >= 2);   // dono e cliente

        // segunda passada: já avisou, não repete
        api.Resend.ClearReceivedCalls();
        await Avisar();
        Assert.Equal(0, api.Resend.ReceivedCalls().Count(c => c.GetMethodInfo().Name == "EmailSendAsync"));
    }

    [FatoIntegracao]
    public async Task Aviso_de_24h_nao_vai_para_reserva_que_ja_expirou_ou_foi_confirmada()
    {
        var loja = await api.PrepararParaReservasAsync();
        var expirou = (await loja.ReservarPeloSiteAsync(Datas.Sexta(), Datas.Sabado(), nome: "Expirou")).Id;
        var confirmou = (await loja.ReservarPeloSiteAsync(Datas.Sexta(1), Datas.Sabado(1), nome: "Confirmou")).Id;
        await loja.Painel.Patch($"/api/chacara/reservas/{confirmou}/confirmar", new { });
        foreach (var id in new[] { expirou, confirmou })
            await api.NoBancoAsync(db => db.Reservas.Where(r => r.Id == id).ExecuteUpdateAsync(s => s.SetProperty(r => r.CriadoEm, DateTime.UtcNow.AddHours(-30))));
        await DefinirPrazo(expirou, DateTime.UtcNow.AddMinutes(-1));

        await Avisar();

        Assert.False((await api.ReservaNoBancoAsync(expirou)).AvisoExpiracaoEnviado);
        Assert.False((await api.ReservaNoBancoAsync(confirmou)).AvisoExpiracaoEnviado);
    }
}
