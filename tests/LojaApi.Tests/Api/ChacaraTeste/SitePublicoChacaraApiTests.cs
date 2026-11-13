using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace LojaApi.Tests.Api.ChacaraTeste;

// O que o cliente vê e faz no site público da chácara (sem login).
[Collection(ColecaoApi.Nome)]
public class SitePublicoChacaraApiTests(ApiFixture api)
{
    private static string Periodo(DateTime ini, DateTime fim) => $"dataInicio={Datas.Texto(ini)}&dataFim={Datas.Texto(fim)}";

    // ── Página ───────────────────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Endereco_inexistente_devolve_pagina_nao_encontrada()
    {
        var loja = await api.NovaLojaAsync();
        foreach (var url in new[] { "dados", $"disponibilidade?{Periodo(Datas.Sexta(), Datas.Sabado())}", "datas-ocupadas?meses=12", $"valor?{Periodo(Datas.Sexta(), Datas.Sabado())}&pessoas=10" })
        {
            var r = await loja.Site.Get($"/api/publico/nao-existe-{Guid.NewGuid():N}/chacara/{url}");
            Assert.Equal(404, r.Codigo);
            Assert.Equal("Página não encontrada.", r.Erro);
        }
    }

    [FatoIntegracao]
    public async Task Loja_sem_o_modulo_de_reservas_nao_mostra_a_pagina()
    {
        var loja = await api.NovaLojaAsync(moduloReservasAtivo: false);
        Assert.Equal(404, (await loja.Site.Get($"{loja.Publico}/dados")).Codigo);
    }

    [FatoIntegracao]
    public async Task Dados_mostram_a_chacara_com_comodidades_conhecidas_e_avisos_em_lista()
    {
        var loja = await api.PrepararParaReservasAsync();
        await loja.Painel.Put("/api/chacara/info", new
        {
            descricao = "Chácara linda", endereco = "Rua A, 10", comodidades = "piscina,wifi,inventada",
            comodidadesExtras = "Rede\nSauna", horaEntrada = "08:00", horaSaida = "22:00", avisosUso = "Sem som alto\nSem animais",
            locadorNome = "Fulano de Tal", locadorCpf = "123.456.789-00", locadorEndereco = "Rua do Locador, 5",
        });
        await loja.Painel.Post("/api/chacara/fotos", new { url = "https://img.test/1.jpg" });
        await loja.Painel.Post("/api/chacara/horarios", new { tipo = "entrada", hora = "15:00", ajuste = -40, ordem = 0 });

        var r = await loja.Site.Get($"{loja.Publico}/dados");

        Assert.Equal(200, r.Codigo);
        var j = r.Json;
        Assert.Equal("Chácara linda", j.GetProperty("descricao").GetString());
        Assert.Equal(["piscina", "wifi"], j.GetProperty("comodidades").EnumerateArray().Select(c => c.GetProperty("chave").GetString()));  // "inventada" some
        Assert.Equal(["Piscina", "Wi-Fi"], j.GetProperty("comodidades").EnumerateArray().Select(c => c.GetProperty("label").GetString()));
        Assert.Equal(["Rede", "Sauna"], j.GetProperty("comodidadesExtras").EnumerateArray().Select(c => c.GetString()));
        Assert.Equal(["Sem som alto", "Sem animais"], j.GetProperty("avisosUso").EnumerateArray().Select(c => c.GetString()));
        Assert.Single(j.GetProperty("fotos").EnumerateArray());
        Assert.Equal(-40m, j.GetProperty("horarios").GetProperty("entrada")[0].GetProperty("ajuste").GetDecimal());
        Assert.False(j.GetProperty("pagamentoOnline").GetBoolean());
        Assert.True(j.GetProperty("reservasDisponiveis").GetBoolean());
    }

    [FatoIntegracao]
    public async Task Reservas_so_ficam_disponiveis_com_os_dados_do_locador_completos()
    {
        var loja = await api.NovaLojaAsync();
        await loja.Painel.Put("/api/chacara/info", new { descricao = "d", endereco = "e", comodidades = "", locadorNome = "Fulano", locadorCpf = "123" /* CPF incompleto */, locadorEndereco = "Rua" });
        Assert.False((await loja.Site.Get($"{loja.Publico}/dados")).Json.GetProperty("reservasDisponiveis").GetBoolean());

        await loja.Painel.Put("/api/chacara/info", new { descricao = "d", endereco = "e", comodidades = "", locadorNome = "Fulano", locadorCpf = "123.456.789-00", locadorEndereco = "Rua" });
        Assert.True((await loja.Site.Get($"{loja.Publico}/dados")).Json.GetProperty("reservasDisponiveis").GetBoolean());
    }

    [FatoIntegracao]
    public async Task Dados_nao_expoem_credenciais_nem_dados_internos()
    {
        var loja = await api.PrepararParaReservasAsync(comPagamentoOnline: true);
        var corpo = (await loja.Site.Get($"{loja.Publico}/dados")).Corpo;

        Assert.DoesNotContain("APP_USR-token-de-teste", corpo);
        Assert.DoesNotContain("123.456.789-00", corpo);   // CPF do locador não aparece no site
        Assert.True((await loja.Site.Get($"{loja.Publico}/dados")).Json.GetProperty("pagamentoOnline").GetBoolean());
    }

    // ── Disponibilidade e datas ocupadas ─────────────────────────────────────

    [FatoIntegracao]
    public async Task Disponibilidade_reflete_as_reservas_da_loja()
    {
        var loja = await api.PrepararParaReservasAsync();
        await loja.CriarReservaManualAsync(Datas.Sexta(), Datas.Domingo());

        async Task<bool> Disponivel(DateTime ini, DateTime fim) =>
            (await loja.Site.Get($"{loja.Publico}/disponibilidade?{Periodo(ini, fim)}")).Json.GetProperty("disponivel").GetBoolean();

        Assert.False(await Disponivel(Datas.Sabado(), Datas.Sabado()));
        Assert.False(await Disponivel(Datas.Domingo(), Datas.Domingo().AddDays(3)));   // encosta no último dia
        Assert.True(await Disponivel(Datas.Sexta(1), Datas.Domingo(1)));
        Assert.True(await Disponivel(Datas.Sexta().AddDays(-3), Datas.Sexta().AddDays(-1)));
    }

    [FatoIntegracao]
    public async Task Datas_ocupadas_lista_confirmadas_e_pendentes_no_prazo_mas_nao_as_expiradas()
    {
        var loja = await api.PrepararParaReservasAsync();
        await loja.CriarReservaManualAsync(Datas.Sexta(), Datas.Sabado());
        var pendente = (await loja.ReservarPeloSiteAsync(Datas.Sexta(2), Datas.Sabado(2))).Id;

        async Task<List<string?>> Ocupadas() =>
            (await loja.Site.Get($"{loja.Publico}/datas-ocupadas?meses=24")).Json.EnumerateArray()
                .Select(d => d.GetProperty("dataInicio").GetString()).ToList();

        Assert.Equal([$"{Datas.Texto(Datas.Sexta())}T12:00:00Z", $"{Datas.Texto(Datas.Sexta(2))}T12:00:00Z"], await Ocupadas());

        await loja.Painel.Patch($"/api/chacara/reservas/{pendente}/marcar-expirada");
        Assert.Equal([$"{Datas.Texto(Datas.Sexta())}T12:00:00Z"], await Ocupadas());
    }

    // ── Cálculo do valor ─────────────────────────────────────────────────────

    private async Task<JsonElement> Valor(LojaTeste loja, DateTime ini, DateTime fim, int pessoas, string extra = "")
    {
        var r = await loja.Site.Get($"{loja.Publico}/valor?{Periodo(ini, fim)}&pessoas={pessoas}{extra}");
        r.DeveSerSucesso();
        return r.Json;
    }

    [FatoIntegracao]
    public async Task Valor_de_fim_de_semana_usa_pacote_mais_diaria()
    {
        var loja = await api.PrepararParaReservasAsync();
        var v = await Valor(loja, Datas.Sexta(), Datas.Domingo(), 30);

        Assert.Equal(2200m, v.GetProperty("valorTotal").GetDecimal());
        Assert.Equal(0m, v.GetProperty("valorTaxaLimpeza").GetDecimal());
        Assert.False(v.GetProperty("pacoteFechado").GetBoolean());
        Assert.Equal(2, v.GetProperty("detalhamento").GetArrayLength());
    }

    [FatoIntegracao]
    public async Task Valor_acima_do_limite_de_pessoas_cobra_taxa_de_limpeza_e_usa_a_faixa_maior()
    {
        var loja = await api.PrepararParaReservasAsync();
        var v = await Valor(loja, Datas.Segunda(), Datas.Segunda().AddDays(1), 80);

        Assert.Equal(2 * 700m + 250m, v.GetProperty("valorTotal").GetDecimal());
        Assert.Equal(250m, v.GetProperty("valorTaxaLimpeza").GetDecimal());
    }

    [FatoIntegracao]
    public async Task Valor_aplica_ajuste_dos_horarios_escolhidos()
    {
        var loja = await api.PrepararParaReservasAsync();
        await loja.Painel.Post("/api/chacara/horarios", new { tipo = "entrada", hora = "15:00", ajuste = -40, ordem = 0 });
        await loja.Painel.Post("/api/chacara/horarios", new { tipo = "saida", hora = "22:00", ajuste = 100, ordem = 0 });

        var v = await Valor(loja, Datas.Sexta(), Datas.Sabado(), 30, "&horaEntrada=15:00&horaSaida=22:00");
        Assert.Equal(1400m - 40m + 100m, v.GetProperty("valorTotal").GetDecimal());
    }

    [FatoIntegracao]
    public async Task Valor_de_periodo_especial_e_pacote_fechado()
    {
        var loja = await api.PrepararParaReservasAsync();
        var ini = Datas.Sexta(8);
        await loja.Painel.Post("/api/chacara/periodos-especiais", new { nome = "Natal", dataInicio = Datas.Texto(ini), dataFim = Datas.Texto(ini.AddDays(2)), valorTotal = 5500 });

        var v = await Valor(loja, ini, ini.AddDays(2), 30);
        Assert.True(v.GetProperty("pacoteFechado").GetBoolean());
        Assert.Equal(5500m, v.GetProperty("valorTotal").GetDecimal());
    }

    [FatoIntegracao]
    public async Task Valor_recusa_poucas_pessoas_e_periodo_maior_que_30_dias()
    {
        var loja = await api.PrepararParaReservasAsync();

        var poucas = await loja.Site.Get($"{loja.Publico}/valor?{Periodo(Datas.Sexta(), Datas.Sabado())}&pessoas=2");
        Assert.Equal(400, poucas.Codigo);
        Assert.Equal("O mínimo é de 3 pessoas.", poucas.Erro);

        var longo = await loja.Site.Get($"{loja.Publico}/valor?{Periodo(Datas.Sexta(), Datas.Sexta().AddDays(60))}&pessoas=30");
        Assert.Equal("O período máximo por reserva é de 30 dias.", longo.Erro);
    }

    // ── Reservar ─────────────────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Reservar_sem_pagamento_online_fica_pendente_e_aguarda_o_dono()
    {
        var loja = await api.PrepararParaReservasAsync();

        var r = await loja.Site.Post($"{loja.Publico}/reservar", new
        {
            dataInicio = Datas.Texto(Datas.Sexta()), dataFim = Datas.Texto(Datas.Domingo()), pessoas = 30,
            clienteNome = "  João Cliente ", clienteEmail = " joao@x.com ", clienteTelefone = "(67) 98888-7777",
            clienteDocumento = " 111.222.333-44 ", clienteCep = "", clienteEndereco = " Rua B ",
        });

        Assert.Equal(200, r.Codigo);
        Assert.True(r.Json.GetProperty("semPagamentoOnline").GetBoolean());
        Assert.Equal(2200m, r.Json.GetProperty("valor").GetDecimal());
        Assert.Equal("Solicitação enviada. A chácara vai confirmar sua reserva e entrar em contato.", r.Json.GetProperty("mensagem").GetString());

        var salva = await api.ReservaNoBancoAsync(r.Id);
        Assert.Equal("pendente_pagamento", salva.Status);
        Assert.Equal("sem_pagamento_online", salva.FormaPagamento);
        Assert.Equal("João Cliente", salva.ClienteNome);
        Assert.Equal("67988887777", salva.ClienteTelefone);
        Assert.Equal("111.222.333-44", salva.ClienteDocumento);
        Assert.Null(salva.ClienteCep);
        Assert.InRange(salva.ExpiraEm!.Value, DateTime.UtcNow.AddHours(47), DateTime.UtcNow.AddHours(49));   // 48h
    }

    [FatoIntegracao]
    public async Task Reservar_avisa_o_dono_e_o_cliente_por_email()
    {
        var loja = await api.PrepararParaReservasAsync();
        api.Resend.ClearReceivedCalls();

        (await loja.ReservarPeloSiteAsync(Datas.Sexta(), Datas.Sabado())).DeveSerSucesso();

        var envios = api.Resend.ReceivedCalls().Count(c => c.GetMethodInfo().Name == "EmailSendAsync");
        Assert.True(envios >= 2, $"Esperava e-mail para o dono e para o cliente, mas houve {envios} envio(s).");
    }

    [FatoIntegracao]
    public async Task Reservar_valida_os_dados_do_cliente_e_o_periodo()
    {
        var loja = await api.PrepararParaReservasAsync();
        object Req(DateTime ini, DateTime fim, int pessoas = 30, string nome = "X") => new
        {
            dataInicio = Datas.Texto(ini), dataFim = Datas.Texto(fim), pessoas, clienteNome = nome, clienteEmail = "a@a.com", clienteTelefone = "1",
        };
        string Url() => $"{loja.Publico}/reservar";

        Assert.Equal("Informe nome, e-mail e telefone.", (await loja.Site.Post(Url(), Req(Datas.Sexta(), Datas.Sabado(), nome: ""))).Erro);
        Assert.Equal("Data final não pode ser antes da data inicial.", (await loja.Site.Post(Url(), Req(Datas.Sabado(), Datas.Sexta()))).Erro);
        Assert.Equal("O período máximo por reserva é de 30 dias.", (await loja.Site.Post(Url(), Req(Datas.Sexta(), Datas.Sexta().AddDays(60)))).Erro);
        Assert.Equal("O mínimo é de 3 pessoas.", (await loja.Site.Post(Url(), Req(Datas.Sexta(), Datas.Sabado(), pessoas: 1))).Erro);
    }

    [FatoIntegracao]
    public async Task Reservar_exige_chacara_habilitada_com_locador_e_precos()
    {
        var semLocador = await api.NovaLojaAsync();
        var r1 = await semLocador.ReservarPeloSiteAsync(Datas.Sexta(), Datas.Sabado());
        Assert.Equal(400, r1.Codigo);
        Assert.Contains("ainda não está habilitada para reservas online", r1.Erro);

        await semLocador.Painel.Put("/api/chacara/info", new { descricao = "d", endereco = "e", comodidades = "", locadorNome = "Fulano", locadorCpf = "12345678900", locadorEndereco = "Rua" });
        var r2 = await semLocador.ReservarPeloSiteAsync(Datas.Sexta(), Datas.Sabado());
        Assert.Equal("Esta chácara ainda não configurou os valores de reserva.", r2.Erro);
    }

    [FatoIntegracao]
    public async Task Reservar_datas_ocupadas_por_confirmada_ou_pendente_da_conflito()
    {
        var loja = await api.PrepararParaReservasAsync();
        await loja.CriarReservaManualAsync(Datas.Sexta(), Datas.Sabado());
        await loja.ReservarPeloSiteAsync(Datas.Sexta(1), Datas.Sabado(1));

        foreach (var semana in new[] { 0, 1 })
        {
            var r = await loja.ReservarPeloSiteAsync(Datas.Sexta(semana), Datas.Sexta(semana), nome: "Outro");
            Assert.Equal(409, r.Codigo);
            Assert.Equal("Essas datas acabaram de ser reservadas. Escolha outro período.", r.Erro);
        }
    }

    [FatoIntegracao]
    public async Task Reserva_pendente_vencida_nao_bloqueia_mais_as_datas()
    {
        var loja = await api.PrepararParaReservasAsync();
        var id = (await loja.ReservarPeloSiteAsync(Datas.Sexta(), Datas.Sabado())).Id;

        // o prazo passou, mas a rotina horária ainda não rodou: a agenda já não pode considerar a reserva
        await api.NoBancoAsync(db => db.Reservas.Where(r => r.Id == id).ExecuteUpdateAsync(s => s.SetProperty(r => r.ExpiraEm, DateTime.UtcNow.AddMinutes(-1))));

        var disp = await loja.Site.Get($"{loja.Publico}/disponibilidade?{Periodo(Datas.Sexta(), Datas.Sabado())}");
        Assert.True(disp.Json.GetProperty("disponivel").GetBoolean());
        Assert.Equal(200, (await loja.ReservarPeloSiteAsync(Datas.Sexta(), Datas.Sabado(), nome: "Outro")).Codigo);
    }

    // ── Cancelar pelo cliente ────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Cliente_cancela_a_propria_reserva_pendente_uma_unica_vez()
    {
        var loja = await api.PrepararParaReservasAsync();
        var id = (await loja.ReservarPeloSiteAsync(Datas.Sexta(), Datas.Sabado())).Id;

        var r = await loja.Site.Post($"{loja.Publico}/reservas/{id}/cancelar");
        Assert.Equal(200, r.Codigo);
        Assert.Equal("Reserva cancelada.", r.Json.GetProperty("mensagem").GetString());

        var salva = await api.ReservaNoBancoAsync(id);
        Assert.Equal("cancelada", salva.Status);
        Assert.Equal("Cliente desistiu", salva.MotivoCancelamento);

        Assert.Equal("Essa reserva não pode mais ser cancelada.", (await loja.Site.Post($"{loja.Publico}/reservas/{id}/cancelar")).Erro);
        Assert.Equal(200, (await loja.CriarReservaManualAsync(Datas.Sexta(), Datas.Sabado())).Codigo);   // a data foi liberada
    }

    [FatoIntegracao]
    public async Task Cliente_nao_cancela_reserva_ja_confirmada()
    {
        var loja = await api.PrepararParaReservasAsync();
        var id = (await loja.CriarReservaManualAsync(Datas.Sexta(), Datas.Sabado())).Id;
        Assert.Equal(400, (await loja.Site.Post($"{loja.Publico}/reservas/{id}/cancelar")).Codigo);
        Assert.Equal("confirmada", (await api.ReservaNoBancoAsync(id)).Status);
    }

    // ── Avaliação da estadia ─────────────────────────────────────────────────

    private async Task<int> ReservaDoPassadoAsync(LojaTeste loja) =>
        (await loja.Painel.Post("/api/chacara/reservas", new
        {
            dataInicio = "2020-01-10", dataFim = "2020-01-11", pessoas = 10, clienteNome = "Passado",
            clienteEmail = "", clienteTelefone = "", valor = 1000, valorPago = 1000,
        })).Id;

    [FatoIntegracao]
    public async Task Avaliacao_so_abre_depois_que_a_estadia_terminou_e_a_reserva_foi_confirmada()
    {
        var loja = await api.PrepararParaReservasAsync();
        var pendente = (await loja.ReservarPeloSiteAsync(Datas.Sexta(), Datas.Sabado())).Id;
        var futura = (await loja.CriarReservaManualAsync(Datas.Sexta(1), Datas.Sabado(1))).Id;

        Assert.Equal("Essa reserva ainda não foi confirmada.", (await loja.Site.Get($"{loja.Publico}/avaliacao/{pendente}")).Erro);
        Assert.Equal("Sua estadia ainda não terminou — volte aqui depois do check-out para avaliar.", (await loja.Site.Get($"{loja.Publico}/avaliacao/{futura}")).Erro);
    }

    [FatoIntegracao]
    public async Task Cliente_avalia_a_estadia_e_pode_mudar_a_nota()
    {
        var loja = await api.PrepararParaReservasAsync();
        var id = await ReservaDoPassadoAsync(loja);

        var abrir = (await loja.Site.Get($"{loja.Publico}/avaliacao/{id}")).Json;
        Assert.False(abrir.GetProperty("jaAvaliado").GetBoolean());
        Assert.Equal("Passado", abrir.GetProperty("clienteNome").GetString());

        Assert.Equal("Escolha uma nota de 1 a 5 estrelas.", (await loja.Site.Post($"{loja.Publico}/avaliacao/{id}", new { nota = 9, comentario = "x" })).Erro);

        Assert.Equal(200, (await loja.Site.Post($"{loja.Publico}/avaliacao/{id}", new { nota = 5, comentario = "  Maravilhoso  " })).Codigo);
        Assert.Equal(200, (await loja.Site.Post($"{loja.Publico}/avaliacao/{id}", new { nota = 3, comentario = "" })).Codigo);   // atualiza

        var depois = (await loja.Site.Get($"{loja.Publico}/avaliacao/{id}")).Json;
        Assert.True(depois.GetProperty("jaAvaliado").GetBoolean());
        Assert.Equal(3, depois.GetProperty("notaAtual").GetInt32());

        var avaliacoes = await api.NoBancoAsync(db => db.AvaliacoesChacara.AsNoTracking().Where(a => a.ReservaId == id).ToListAsync());
        Assert.Single(avaliacoes);   // a segunda envio atualiza, não duplica
    }

    [FatoIntegracao]
    public async Task Avaliacao_de_reserva_inexistente_da_404()
    {
        var loja = await api.PrepararParaReservasAsync();
        Assert.Equal(404, (await loja.Site.Get($"{loja.Publico}/avaliacao/999999")).Codigo);
    }
}
