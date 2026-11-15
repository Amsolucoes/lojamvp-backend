using Microsoft.EntityFrameworkCore;

namespace LojaApi.Tests.Api.PlataformaTeste;

// Área do assinante (dashboard, configurações, troca de e-mail, faturas) e configurações da loja (situação, agenda, agendamento online, módulos, pausa e link).
[Collection(ColecaoApi.Nome)]
public class ClienteLojaApiTests(ApiFixture api)
{
    private Task<LojaTeste> NovaAsync() => api.NovaLojaAsync();

    private static async Task<(int, string)> Erro(Task<Resposta> r) { var x = await r; return (x.Codigo, x.Erro); }

    private Task AlterarLojaAsync(Guid id, Action<LojaApi.Models.Loja> alterar) => api.NoBancoAsync(async db =>
    {
        var l = await db.Lojas.FirstAsync(x => x.Id == id);
        alterar(l);
        await db.SaveChangesAsync();
    });

    private Task<LojaApi.Models.Loja> LojaNoBancoAsync(Guid id) => api.NoBancoAsync(db => db.Lojas.AsNoTracking().FirstAsync(x => x.Id == id));

    // ── Área do assinante ────────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Cliente_exige_login()
    {
        var loja = await NovaAsync();
        foreach (var url in new[] { "dashboard", "config", "faturas" })
            Assert.Equal(401, (await loja.Site.Get($"/api/cliente/{url}")).Codigo);
        Assert.Equal(401, (await loja.Site.Patch("/api/cliente/config", new { })).Codigo);
        Assert.Equal(401, (await loja.Site.Patch("/api/cliente/email", new { })).Codigo);
    }

    [FatoIntegracao]
    public async Task Dashboard_mostra_trial_primeira_fatura_pendente_e_sem_atraso()
    {
        var loja = await NovaAsync();

        var r = await loja.Painel.Get("/api/cliente/dashboard");

        Assert.Equal(200, r.Codigo);
        var j = r.Json;
        Assert.Equal(["nomeLoja", "status", "trialAte", "proximoVencimento", "mensalidadeValor", "emAtraso", "diasAtraso", "faturaPendente", "historicoFaturas", "assinaturaStatus", "assinaturaCartaoFinal"],
            j.EnumerateObject().Select(x => x.Name));
        Assert.Equal(("Trial", false, 0), (j.GetProperty("status").GetString()!, j.GetProperty("emAtraso").GetBoolean(), j.GetProperty("diasAtraso").GetInt32()));
        Assert.NotEqual(System.Text.Json.JsonValueKind.Null, j.GetProperty("trialAte").ValueKind);
        var fatura = j.GetProperty("faturaPendente");
        Assert.Equal("pendente", fatura.GetProperty("status").GetString());
        Assert.Equal(1, j.GetProperty("historicoFaturas").GetArrayLength());
    }

    [FatoIntegracao]
    public async Task Dashboard_com_assinatura_ativa_vencida_ha_dias_bloqueia_a_loja_e_conta_atraso()
    {
        var loja = await NovaAsync();
        await AlterarLojaAsync(loja.Id, l => { l.Status = LojaApi.Models.StatusLoja.Ativo; l.ProximoVencimento = DateTime.UtcNow.AddDays(-5); l.TrialAte = DateTime.UtcNow.AddDays(-20); });

        var j = (await loja.Painel.Get("/api/cliente/dashboard")).Json;

        Assert.Equal(("Bloqueado", true), (j.GetProperty("status").GetString()!, j.GetProperty("emAtraso").GetBoolean()));
        Assert.InRange(j.GetProperty("diasAtraso").GetInt32(), 4, 6);
        Assert.Equal(LojaApi.Models.StatusLoja.Bloqueado, (await LojaNoBancoAsync(loja.Id)).Status);                       // o dashboard grava o bloqueio
        Assert.Equal(System.Text.Json.JsonValueKind.Null, j.GetProperty("trialAte").ValueKind);                            // trialAte só aparece em Trial
    }

    [FatoIntegracao]
    public async Task Dashboard_limita_o_historico_a_12_faturas_das_mais_novas()
    {
        var loja = await NovaAsync();
        await api.NoBancoAsync(async db =>
        {
            for (var i = 1; i <= 14; i++)
                db.Pagamentos.Add(new LojaApi.Models.Pagamento { LojaId = loja.Id, Valor = i, Status = "pago", Vencimento = DateTime.UtcNow.AddDays(-30 * i) });
            await db.SaveChangesAsync();
        });

        var j = (await loja.Painel.Get("/api/cliente/dashboard")).Json;
        var historico = j.GetProperty("historicoFaturas").EnumerateArray().ToList();

        Assert.Equal(12, historico.Count);
        var vencimentos = historico.Select(h => h.GetProperty("vencimento").GetDateTime()).ToList();
        Assert.Equal(vencimentos.OrderByDescending(v => v).ToList(), vencimentos);
        Assert.Equal(15, (await loja.Painel.Get("/api/cliente/faturas")).Json.GetArrayLength());                          // a lista completa traz todas (14 + a do trial)
    }

    [FatoIntegracao]
    public async Task Config_mostra_a_loja_e_o_motivo_so_quando_bloqueada()
    {
        var loja = await NovaAsync();

        var j = (await loja.Painel.Get("/api/cliente/config")).Json;
        Assert.Equal(["id", "nome", "corPrimaria", "logoUrl", "status", "motivoBloqueo", "endereco", "telefone"], j.EnumerateObject().Select(x => x.Name));
        Assert.Equal((loja.Id, "#c38228", "Trial", System.Text.Json.JsonValueKind.Null), (j.GetProperty("id").GetGuid(), j.GetProperty("corPrimaria").GetString()!, j.GetProperty("status").GetString()!, j.GetProperty("motivoBloqueo").ValueKind));

        await AlterarLojaAsync(loja.Id, l => l.Status = LojaApi.Models.StatusLoja.Bloqueado);
        Assert.Equal("Acesso bloqueado por inadimplência.", (await loja.Painel.Get("/api/cliente/config")).Json.GetProperty("motivoBloqueo").GetString());
    }

    [FatoIntegracao]
    public async Task Atualizar_config_so_troca_o_que_veio_e_endereco_e_telefone_so_se_enviados()
    {
        var loja = await NovaAsync();
        await loja.Painel.Patch("/api/cliente/config", new { endereco = "Rua A, 1", telefone = "6711111111" });

        var r = await loja.Painel.Patch("/api/cliente/config", new { nome = "Nome Novo", corPrimaria = "#112233", logoUrl = "https://x/logo.png" });

        Assert.Equal("Configurações atualizadas.", r.Json.GetProperty("mensagem").GetString());
        var l = await LojaNoBancoAsync(loja.Id);
        Assert.Equal(("Nome Novo", "#112233", "https://x/logo.png", "Rua A, 1", "6711111111"), (l.Nome, l.CorPrimaria, l.LogoUrl, l.Endereco, l.Telefone));

        await loja.Painel.Patch("/api/cliente/config", new { endereco = "", telefone = "" });                              // vazio é aceito (só nulo é ignorado)
        var limpa = await LojaNoBancoAsync(loja.Id);
        Assert.Equal(("", ""), (limpa.Endereco, limpa.Telefone));
        Assert.Equal("Nome Novo", limpa.Nome);
    }

    [FatoIntegracao]
    public async Task Trocar_email_valida_senha_formato_duplicidade_e_aceita_caixa_diferente()
    {
        var loja = await NovaAsync();
        var outra = await NovaAsync();
        var emailOutra = $"dono-{outra.Slug[2..]}@teste.com";
        var emailNovo = $"novo-{Guid.NewGuid():N}@teste.com";

        Assert.Equal((400, "Senha atual incorreta."), await Erro(loja.Painel.Patch("/api/cliente/email", new { novoEmail = emailNovo, senhaAtual = "errada" })));
        Assert.Equal((400, "E-mail inválido."), await Erro(loja.Painel.Patch("/api/cliente/email", new { novoEmail = "semarroba", senhaAtual = "123456" })));
        Assert.Equal((400, "E-mail inválido."), await Erro(loja.Painel.Patch("/api/cliente/email", new { novoEmail = "  ", senhaAtual = "123456" })));
        Assert.Equal((409, "Este e-mail já está em uso."), await Erro(loja.Painel.Patch("/api/cliente/email", new { novoEmail = emailOutra.ToUpper(), senhaAtual = "123456" })));

        var ok = await loja.Painel.Patch("/api/cliente/email", new { novoEmail = $"  {emailNovo.ToUpper()} ", senhaAtual = "123456" });
        Assert.Equal("E-mail atualizado. Use o novo e-mail no próximo login.", ok.Json.GetProperty("mensagem").GetString());
        Assert.Equal(emailNovo, await api.NoBancoAsync(db => db.Usuarios.Where(u => u.Email == emailNovo).Select(u => u.Email).SingleAsync()));      // gravado minúsculo e sem espaços
        Assert.Equal(200, (await loja.Site.Post("/api/auth/login", new { email = emailNovo, senha = "123456" })).Codigo);
    }

    // ── Loja ────────────────────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Situacao_exige_login_e_mostra_trial_com_dias_restantes_e_modulos()
    {
        var loja = await NovaAsync();
        Assert.Equal(401, (await loja.Site.Get("/api/loja/situacao")).Codigo);

        var j = (await loja.Painel.Get("/api/loja/situacao")).Json;

        Assert.Equal(["fase", "diasRestantes", "status", "mensalidadeValor", "trialAte", "proximoVencimento", "nomeLoja", "enderecoLoja", "telefoneLoja", "logoUrlLoja", "tipoPlano",
            "modulosAtivos", "agendaHoraInicio", "agendaHoraFim", "assinaturaStatus", "assinaturaCartaoFinal", "agendamentoOnlineAtivo", "agendamentoOnlineConfirmacao", "slug",
            "modulosAlteradoEm", "pausaAte", "pausaMensagem"], j.EnumerateObject().Select(x => x.Name));
        Assert.Equal(("trial", "Trial", loja.Slug, "aprovacao"), (j.GetProperty("fase").GetString()!, j.GetProperty("status").GetString()!, j.GetProperty("slug").GetString()!, j.GetProperty("agendamentoOnlineConfirmacao").GetString()!));
        Assert.InRange(j.GetProperty("diasRestantes").GetInt32(), 6, 7);
        Assert.Contains(j.GetProperty("modulosAtivos").EnumerateArray(), m => m.GetString() == "chacara_reservas");
    }

    [FatoIntegracao]
    public async Task Situacao_do_superadmin_sem_loja_e_fixa_e_as_fases_seguem_o_status()
    {
        var loja = await NovaAsync();
        await api.NoBancoAsync(db => db.UsuariosLoja.Where(ul => ul.LojaId == loja.Id).ExecuteUpdateAsync(s => s.SetProperty(ul => ul.Ativo, false)));
        var j = (await loja.Painel.Get("/api/loja/situacao")).Json;
        Assert.Equal(("superadmin", 0), (j.GetProperty("fase").GetString()!, j.GetProperty("diasRestantes").GetInt32()));
        Assert.Equal(2, j.EnumerateObject().Count());
        await api.NoBancoAsync(db => db.UsuariosLoja.Where(ul => ul.LojaId == loja.Id).ExecuteUpdateAsync(s => s.SetProperty(ul => ul.Ativo, true)));

        await AlterarLojaAsync(loja.Id, l => { l.Status = LojaApi.Models.StatusLoja.Ativo; l.ProximoVencimento = DateTime.UtcNow.AddDays(10); });
        var ativo = (await loja.Painel.Get("/api/loja/situacao")).Json;
        Assert.Equal("ativo", ativo.GetProperty("fase").GetString());
        Assert.InRange(ativo.GetProperty("diasRestantes").GetInt32(), 9, 10);

        await AlterarLojaAsync(loja.Id, l => l.Status = LojaApi.Models.StatusLoja.Bloqueado);
        var bloqueado = (await loja.Painel.Get("/api/loja/situacao")).Json;
        Assert.Equal(("bloqueado", 0), (bloqueado.GetProperty("fase").GetString()!, bloqueado.GetProperty("diasRestantes").GetInt32()));
    }

    [FatoIntegracao]
    public async Task Agenda_horario_limita_inicio_a_0_23_e_fim_a_1_24()
    {
        var loja = await NovaAsync();

        var j = (await loja.Painel.Patch("/api/loja/agenda-horario", new { horaInicio = -5, horaFim = 40 })).Json;
        Assert.Equal((0, 24), (j.GetProperty("agendaHoraInicio").GetInt32(), j.GetProperty("agendaHoraFim").GetInt32()));

        var k = (await loja.Painel.Patch("/api/loja/agenda-horario", new { horaInicio = 30, horaFim = -1 })).Json;
        Assert.Equal((23, 1), (k.GetProperty("agendaHoraInicio").GetInt32(), k.GetProperty("agendaHoraFim").GetInt32()));

        var l = await LojaNoBancoAsync(loja.Id);
        Assert.Equal((23, 1), (l.AgendaHoraInicio, l.AgendaHoraFim));
        Assert.Equal(401, (await loja.Site.Patch("/api/loja/agenda-horario", new { horaInicio = 8, horaFim = 18 })).Codigo);
    }

    [FatoIntegracao]
    public async Task Agendamento_online_normaliza_o_slug_valida_duplicidade_e_exige_slug_para_ativar()
    {
        var loja = await NovaAsync();
        var outra = await NovaAsync();
        await AlterarLojaAsync(loja.Id, l => l.Slug = null);

        Assert.Equal((400, "Defina um link (slug) antes de ativar o agendamento online."), await Erro(loja.Painel.Patch("/api/loja/agendamento-online", new { ativo = true })));
        Assert.Equal((400, "O link deve ter ao menos 3 caracteres válidos."), await Erro(loja.Painel.Patch("/api/loja/agendamento-online", new { ativo = true, slug = "a!" })));
        Assert.Equal((409, "Este link já está em uso. Escolha outro."), await Erro(loja.Painel.Patch("/api/loja/agendamento-online", new { ativo = true, slug = outra.Slug.ToUpper() })));

        var sufixo = Guid.NewGuid().ToString("N")[..8];
        var ok = (await loja.Painel.Patch("/api/loja/agendamento-online", new { ativo = true, slug = $"  Salão Éxito {sufixo} ", confirmacao = "automatico" })).Json;
        Assert.Equal(($"salao-exito-{sufixo}", true, "automatico"), (ok.GetProperty("slug").GetString()!, ok.GetProperty("agendamentoOnlineAtivo").GetBoolean(), ok.GetProperty("agendamentoOnlineConfirmacao").GetString()!));

        var outro = (await loja.Painel.Patch("/api/loja/agendamento-online", new { ativo = false, confirmacao = "qualquer coisa" })).Json;           // confirmação desconhecida vira "aprovacao"; slug fica
        Assert.Equal((false, "aprovacao", $"salao-exito-{sufixo}"), (outro.GetProperty("agendamentoOnlineAtivo").GetBoolean(), outro.GetProperty("agendamentoOnlineConfirmacao").GetString()!, outro.GetProperty("slug").GetString()!));
    }

    [FatoIntegracao]
    public async Task Definir_slug_valida_tamanho_duplicidade_e_aceita_o_proprio()
    {
        var loja = await NovaAsync();
        var outra = await NovaAsync();

        Assert.Equal((400, "O link deve ter ao menos 3 caracteres válidos."), await Erro(loja.Painel.Patch("/api/loja/slug", new { slug = "--a--" })));
        Assert.Equal((409, "Este link já está em uso. Escolha outro."), await Erro(loja.Painel.Patch("/api/loja/slug", new { slug = outra.Slug })));
        Assert.Equal(loja.Slug, (await loja.Painel.Patch("/api/loja/slug", new { slug = loja.Slug })).Json.GetProperty("slug").GetString());       // o próprio link não conta como "em uso"

        var sufixo = Guid.NewGuid().ToString("N")[..6];
        Assert.Equal($"ola-mundo-{sufixo}", (await loja.Painel.Patch("/api/loja/slug", new { slug = $"Olá  Mundo__{sufixo}" })).Json.GetProperty("slug").GetString());
        Assert.Equal(401, (await loja.Site.Patch("/api/loja/slug", new { slug = "abc" })).Codigo);
    }

    [FatoIntegracao]
    public async Task Alternar_modulo_valida_disponibilidade_soma_e_subtrai_a_mensalidade_e_aplica_o_cooldown_de_30_dias()
    {
        var loja = await NovaAsync();
        var modulo = await api.NoBancoAsync(db => db.ModulosPreco.AsNoTracking().FirstAsync(m => m.Chave == "financeiro"));
        await api.NoBancoAsync(db => db.ModulosPreco.Where(m => m.Id == modulo.Id).ExecuteUpdateAsync(s => s.SetProperty(m => m.DisponivelParaAtivar, true).SetProperty(m => m.Valor, 20m)));
        try
        {
            await AlterarLojaAsync(loja.Id, l => { l.ModulosAtivos = ""; l.MensalidadeValor = 100m; l.ModulosAlteradoEm = new(); });

            Assert.Equal((400, "Módulo indisponível."), await Erro(loja.Painel.Patch("/api/loja/modulos", new { chave = "inexistente", ativar = true })));

            var ativou = (await loja.Painel.Patch("/api/loja/modulos", new { chave = "financeiro", ativar = true })).Json;
            Assert.Equal(("financeiro", 120m), (ativou.GetProperty("modulosAtivos").GetString()!, ativou.GetProperty("mensalidadeValor").GetDecimal()));
            // defeito antigo mantido: ativar de novo um módulo que já está ativo soma o valor outra vez (a lista não duplica, a mensalidade sim)
            var denovo = (await loja.Painel.Patch("/api/loja/modulos", new { chave = "financeiro", ativar = true })).Json;
            Assert.Equal(("financeiro", 140m), (denovo.GetProperty("modulosAtivos").GetString()!, denovo.GetProperty("mensalidadeValor").GetDecimal()));

            await AlterarLojaAsync(loja.Id, l => { l.ModulosAtivos = "servicos,financeiro"; l.MensalidadeValor = 100m; });
            var desativou = (await loja.Painel.Patch("/api/loja/modulos", new { chave = "financeiro", ativar = false })).Json;
            Assert.Equal(("servicos", 80m), (desativou.GetProperty("modulosAtivos").GetString()!, desativou.GetProperty("mensalidadeValor").GetDecimal()));
            Assert.True((await LojaNoBancoAsync(loja.Id)).ModulosAlteradoEm.ContainsKey("financeiro"));                         // a desativação grava a data

            var barrado = await loja.Painel.Patch("/api/loja/modulos", new { chave = "financeiro", ativar = true });            // reativar dentro de 30 dias é barrado
            Assert.Equal(400, barrado.Codigo);
            Assert.Equal((true, true), (barrado.Json.GetProperty("cooldown").GetBoolean(), barrado.Json.GetProperty("diasRestantes").GetInt32() is >= 29 and <= 30));
            Assert.StartsWith("Este módulo foi desativado recentemente. Aguarde ", barrado.Erro);

            await AlterarLojaAsync(loja.Id, l => l.ModulosAlteradoEm = new() { ["financeiro"] = DateTime.UtcNow.AddDays(-31) });
            Assert.Equal(200, (await loja.Painel.Patch("/api/loja/modulos", new { chave = "financeiro", ativar = true })).Codigo);        // passados os 30 dias, libera

            // desativar sem estar ativo não mexe na mensalidade (mas grava a data)
            await AlterarLojaAsync(loja.Id, l => { l.ModulosAtivos = "servicos"; l.MensalidadeValor = 100m; l.ModulosAlteradoEm = new(); });
            Assert.Equal(100m, (await loja.Painel.Patch("/api/loja/modulos", new { chave = "financeiro", ativar = false })).Json.GetProperty("mensalidadeValor").GetDecimal());

            // a mensalidade não fica negativa
            await AlterarLojaAsync(loja.Id, l => { l.ModulosAtivos = "financeiro"; l.MensalidadeValor = 5m; l.ModulosAlteradoEm = new(); });
            Assert.Equal(0m, (await loja.Painel.Patch("/api/loja/modulos", new { chave = "financeiro", ativar = false })).Json.GetProperty("mensalidadeValor").GetDecimal());

            // módulo "em breve" (indisponível) não ativa
            await api.NoBancoAsync(db => db.ModulosPreco.Where(m => m.Id == modulo.Id).ExecuteUpdateAsync(s => s.SetProperty(m => m.DisponivelParaAtivar, false)));
            Assert.Equal((400, "Módulo indisponível."), await Erro(loja.Painel.Patch("/api/loja/modulos", new { chave = "financeiro", ativar = true })));
        }
        finally
        {
            await api.NoBancoAsync(db => db.ModulosPreco.Where(m => m.Id == modulo.Id).ExecuteUpdateAsync(s => s.SetProperty(m => m.DisponivelParaAtivar, modulo.DisponivelParaAtivar).SetProperty(m => m.Valor, modulo.Valor)));
        }
    }

    [FatoIntegracao]
    public async Task Pausa_do_agendamento_exige_data_de_retorno_valida_aplica_mensagem_padrao_e_limpa()
    {
        var loja = await NovaAsync();

        Assert.Equal((400, "Informe uma data de retorno válida (hoje ou futura)."), await Erro(loja.Painel.Patch("/api/loja/pausa-agendamento", new { ativar = true })));
        Assert.Equal((400, "Informe uma data de retorno válida (hoje ou futura)."), await Erro(loja.Painel.Patch("/api/loja/pausa-agendamento", new { ativar = true, pausaAte = DateTime.UtcNow.AddDays(-2).ToString("o") })));

        var volta = DateTime.UtcNow.AddDays(3);
        var ok = (await loja.Painel.Patch("/api/loja/pausa-agendamento", new { ativar = true, pausaAte = volta.ToString("o") })).Json;
        Assert.Equal("Estamos temporariamente fechados. Voltamos em breve!", ok.GetProperty("pausaMensagem").GetString());
        var pausaAte = ok.GetProperty("pausaAte").GetDateTime();
        Assert.Equal((volta.Date, 23, 59), (pausaAte.Date, pausaAte.Hour, pausaAte.Minute));                                   // até o fim do dia de retorno

        var comMensagem = (await loja.Painel.Patch("/api/loja/pausa-agendamento", new { ativar = true, pausaAte = DateTime.UtcNow.ToString("o"), mensagem = "  Férias!  " })).Json;
        Assert.Equal("Férias!", comMensagem.GetProperty("pausaMensagem").GetString());                                          // hoje vale

        var limpa = (await loja.Painel.Patch("/api/loja/pausa-agendamento", new { ativar = false })).Json;
        Assert.Equal((System.Text.Json.JsonValueKind.Null, System.Text.Json.JsonValueKind.Null), (limpa.GetProperty("pausaAte").ValueKind, limpa.GetProperty("pausaMensagem").ValueKind));
        Assert.Equal(401, (await loja.Site.Patch("/api/loja/pausa-agendamento", new { ativar = false })).Codigo);
    }
}
