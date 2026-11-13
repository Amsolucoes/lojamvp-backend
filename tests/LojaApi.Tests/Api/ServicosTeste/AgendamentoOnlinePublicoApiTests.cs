using System.Text.Json;
using LojaApi.Models;
using Microsoft.EntityFrameworkCore;

namespace LojaApi.Tests.Api.ServicosTeste;

// Página pública de agendamento online (sem login): dados da loja, horários livres, agendar e reconhecer cliente pelo telefone.
[Collection(ColecaoApi.Nome)]
public class AgendamentoOnlinePublicoApiTests(ApiFixture api)
{
    private static string Horarios(LojaTeste loja, DateTime dia, Guid servico) =>
        $"/api/publico/{loja.Slug}/horarios?data={Datas.Texto(dia)}&servicoId={servico}";

    private static async Task<List<string>> LivresAsync(LojaTeste loja, DateTime dia, Guid servico)
    {
        var r = await loja.Site.Get(Horarios(loja, dia, servico));
        r.DeveSerSucesso();
        return r.Json.GetProperty("horarios").EnumerateArray().Select(h => h.GetString()!).ToList();
    }

    private static Task<Resposta> AgendarOnline(LojaTeste loja, Guid servico, string dataHora, string nome = "Cliente Online", string telefone = "(67) 99999-1111") =>
        loja.Site.Post($"/api/publico/{loja.Slug}/agendar", new { servicoId = servico, nomeCliente = nome, telefone, dataHora });

    // ── Dados da loja ────────────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Dados_da_loja_so_com_a_pagina_ligada_e_lista_servicos_ativos_em_ordem()
    {
        var loja = await api.NovaLojaComAgendamentoOnlineAsync();
        await loja.CriarServicoAsync("Tosa", 50, 30, "Tosa");
        await loja.CriarServicoAsync("Banho", 80, 60, "Banho");
        await loja.CriarServicoAsync("Escondido", 10, 30, "Banho", ativo: false);

        var r = await loja.Site.Get($"/api/publico/{loja.Slug}");

        Assert.Equal(200, r.Codigo);
        var nomeDaLoja = await api.NoBancoAsync(db => db.Lojas.AsNoTracking().Where(l => l.Id == loja.Id).Select(l => l.Nome).FirstAsync());
        Assert.Equal(nomeDaLoja, r.Json.GetProperty("nome").GetString());
        Assert.Equal("aprovacao", r.Json.GetProperty("confirmacao").GetString());
        Assert.False(r.Json.GetProperty("pausado").GetBoolean());
        Assert.Equal(JsonValueKind.Null, r.Json.GetProperty("pausaMensagem").ValueKind);
        var servicos = r.Json.GetProperty("servicos").EnumerateArray().ToList();
        Assert.Equal(["Banho", "Tosa"], servicos.Select(s => s.GetProperty("nome").GetString()).ToList());   // por categoria; inativo fica de fora
        Assert.Equal((80m, 60), (servicos[0].GetProperty("preco").GetDecimal(), servicos[0].GetProperty("duracaoMin").GetInt32()));
    }

    [FatoIntegracao]
    public async Task Dados_da_loja_dao_404_para_slug_desconhecido_ou_pagina_desligada()
    {
        var desligada = await api.NovaLojaDeServicosAsync();
        var r = await desligada.Site.Get($"/api/publico/{desligada.Slug}");
        Assert.Equal(404, r.Codigo);
        Assert.Equal("Página de agendamento não encontrada.", r.Erro);

        Assert.Equal(404, (await desligada.Site.Get($"/api/publico/slug-que-nao-existe-{Guid.NewGuid():N}")).Codigo);
    }

    [FatoIntegracao]
    public async Task Dados_da_loja_respeita_a_confirmacao_automatica_e_a_pausa()
    {
        var automatica = await api.NovaLojaComAgendamentoOnlineAsync(confirmacao: "automatico");
        Assert.Equal("automatico", (await automatica.Site.Get($"/api/publico/{automatica.Slug}")).Json.GetProperty("confirmacao").GetString());

        var vazia = await api.NovaLojaComAgendamentoOnlineAsync(confirmacao: "");
        Assert.Equal("aprovacao", (await vazia.Site.Get($"/api/publico/{vazia.Slug}")).Json.GetProperty("confirmacao").GetString());

        var pausada = await api.NovaLojaComAgendamentoOnlineAsync(pausaAte: DateTime.UtcNow.AddDays(2));
        await pausada.CriarServicoAsync();
        var r = (await pausada.Site.Get($"/api/publico/{pausada.Slug}")).Json;
        Assert.True(r.GetProperty("pausado").GetBoolean());
        Assert.Equal(0, r.GetProperty("servicos").GetArrayLength());
        Assert.Equal("Estamos temporariamente fechados. Voltamos em breve!", r.GetProperty("pausaMensagem").GetString());

        var comMensagem = await api.NovaLojaComAgendamentoOnlineAsync(pausaAte: DateTime.UtcNow.AddDays(2), pausaMensagem: "Férias até dia 20");
        Assert.Equal("Férias até dia 20", (await comMensagem.Site.Get($"/api/publico/{comMensagem.Slug}")).Json.GetProperty("pausaMensagem").GetString());

        var vencida = await api.NovaLojaComAgendamentoOnlineAsync(pausaAte: DateTime.UtcNow.AddDays(-1));
        Assert.False((await vencida.Site.Get($"/api/publico/{vencida.Slug}")).Json.GetProperty("pausado").GetBoolean());
    }

    // ── Horários livres ──────────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Horarios_gera_faixas_de_30_min_dentro_do_expediente_pela_duracao_do_servico()
    {
        var loja = await api.NovaLojaComAgendamentoOnlineAsync(horaInicio: 8, horaFim: 12);
        var hora = await loja.ServicoIdAsync("Banho", 80, 60);
        var noventa = await loja.ServicoIdAsync("Completo", 120, 90);

        Assert.Equal(["08:00", "08:30", "09:00", "09:30", "10:00", "10:30", "11:00"], await LivresAsync(loja, Datas.Segunda(), hora));
        Assert.Equal(["08:00", "08:30", "09:00", "09:30", "10:00", "10:30"], await LivresAsync(loja, Datas.Segunda(), noventa));   // tem que terminar até as 12h
    }

    [FatoIntegracao]
    public async Task Horarios_tira_os_ocupados_mas_nao_os_cancelados()
    {
        var loja = await api.NovaLojaComAgendamentoOnlineAsync();
        var servico = await loja.ServicoIdAsync("Banho", 80, 60);
        var ocupado = (await loja.AgendarAsync(servico, Datas.Segunda().Hora(9), nomeCliente: "Ocupa")).Id2();

        Assert.Equal(["08:00", "10:00", "10:30", "11:00"], await LivresAsync(loja, Datas.Segunda(), servico));   // 08:00 termina quando o outro começa

        await loja.Painel.Patch($"/api/agendamentos/{ocupado}/status", new { status = "cancelado" });
        Assert.Equal(7, (await LivresAsync(loja, Datas.Segunda(), servico)).Count);
        Assert.Equal(7, (await LivresAsync(loja, Datas.Segunda(1), servico)).Count);   // outro dia não é afetado
    }

    [FatoIntegracao]
    public async Task Horarios_nao_oferece_dia_passado()
    {
        var loja = await api.NovaLojaComAgendamentoOnlineAsync();
        var servico = await loja.ServicoIdAsync();
        Assert.Empty(await LivresAsync(loja, DateTime.UtcNow.AddDays(-3), servico));
    }

    [FatoIntegracao]
    public async Task Horarios_recusa_pagina_desligada_pausada_e_servico_invalido()
    {
        var pausada = await api.NovaLojaComAgendamentoOnlineAsync(pausaAte: DateTime.UtcNow.AddDays(1), pausaMensagem: "Fechado");
        var servicoPausada = await pausada.ServicoIdAsync();
        var r = await pausada.Site.Get(Horarios(pausada, Datas.Segunda(), servicoPausada));
        Assert.Equal((400, "Fechado"), (r.Codigo, r.Erro));

        var padrao = await api.NovaLojaComAgendamentoOnlineAsync(pausaAte: DateTime.UtcNow.AddDays(1));
        Assert.Equal("Agendamentos temporariamente pausados.", (await padrao.Site.Get(Horarios(padrao, Datas.Segunda(), Guid.NewGuid()))).Erro);

        var desligada = await api.NovaLojaDeServicosAsync();
        Assert.Equal(404, (await desligada.Site.Get(Horarios(desligada, Datas.Segunda(), Guid.NewGuid()))).Codigo);

        var loja = await api.NovaLojaComAgendamentoOnlineAsync();
        var inexistente = await loja.Site.Get(Horarios(loja, Datas.Segunda(), Guid.NewGuid()));
        Assert.Equal((400, "Serviço não encontrado."), (inexistente.Codigo, inexistente.Erro));
        // serviço de outra loja
        var deOutra = await pausada.ServicoIdAsync("Outro");
        Assert.Equal("Serviço não encontrado.", (await loja.Site.Get(Horarios(loja, Datas.Segunda(), deOutra))).Erro);
    }

    // ── Agendar ──────────────────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Agendar_com_aprovacao_cria_pendente_e_cliente_novo_so_com_digitos()
    {
        var loja = await api.NovaLojaComAgendamentoOnlineAsync(confirmacao: "aprovacao");
        var servico = await loja.ServicoIdAsync("Banho", 80, 60);

        var r = await AgendarOnline(loja, servico, Datas.Segunda().Hora(9), nome: "  Carla Souza ", telefone: "(67) 99999-1111");

        Assert.Equal(200, r.Codigo);
        Assert.Equal(("pendente", "Solicitação enviada! A loja vai confirmar seu horário em breve."),
            (r.Json.GetProperty("status").GetString(), r.Json.GetProperty("mensagem").GetString()));
        var ag = (await loja.Painel.Get("/api/agendamentos")).Json.EnumerateArray().Single();
        Assert.Equal(("pendente", "Carla Souza", "Banho", 80m, 60), (ag.GetProperty("status").GetString(), ag.GetProperty("nomeCliente").GetString(),
            ag.GetProperty("nomeServico").GetString(), ag.GetProperty("preco").GetDecimal(), ag.GetProperty("duracaoMin").GetInt32()));
        var noBanco = await api.AgendamentoNoBancoAsync(ag.GetProperty("id").GetGuid());
        Assert.Equal("online", noBanco.Origem);
        var cliente = await api.NoBancoAsync(db => db.Clientes.AsNoTracking().SingleAsync(c => c.LojaId == loja.Id));
        Assert.Equal(("Carla Souza", "67999991111", cliente.Id), (cliente.Nome, cliente.Telefone, noBanco.ClienteId));
    }

    [FatoIntegracao]
    public async Task Agendar_com_confirmacao_automatica_ja_entra_como_agendado()
    {
        var loja = await api.NovaLojaComAgendamentoOnlineAsync(confirmacao: "automatico");
        var servico = await loja.ServicoIdAsync();

        var r = await AgendarOnline(loja, servico, Datas.Segunda().Hora(9));

        Assert.Equal(("agendado", "Agendamento confirmado!"), (r.Json.GetProperty("status").GetString(), r.Json.GetProperty("mensagem").GetString()));
    }

    [FatoIntegracao]
    public async Task Agendar_reaproveita_o_cliente_pelo_telefone_mesmo_formatado_diferente()
    {
        var loja = await api.NovaLojaComAgendamentoOnlineAsync(confirmacao: "automatico");
        var servico = await loja.ServicoIdAsync();
        var existente = await loja.ClienteIdAsync("Maria Antiga", "(67) 98888-7777");

        (await AgendarOnline(loja, servico, Datas.Segunda().Hora(9), nome: "Nome novo", telefone: "67 98888 7777")).DeveSerSucesso();

        Assert.Equal(1, await api.NoBancoAsync(db => db.Clientes.CountAsync(c => c.LojaId == loja.Id)));
        var ag = (await loja.Painel.Get("/api/agendamentos")).Json[0];
        Assert.Equal((existente, "Maria Antiga"), (ag.GetProperty("clienteId").GetGuid(), ag.GetProperty("nomeCliente").GetString()));   // usa o nome do cadastro
    }

    [FatoIntegracao]
    public async Task Agendar_recusa_horario_ocupado_e_aceita_o_que_encosta()
    {
        var loja = await api.NovaLojaComAgendamentoOnlineAsync(confirmacao: "automatico");
        var servico = await loja.ServicoIdAsync("Banho", 80, 60);
        (await AgendarOnline(loja, servico, Datas.Segunda().Hora(9))).DeveSerSucesso();

        var conflito = await AgendarOnline(loja, servico, Datas.Segunda().Hora(9, 30), telefone: "67922223333");
        Assert.Equal((409, "Este horário acabou de ser reservado. Escolha outro."), (conflito.Codigo, conflito.Erro));

        (await AgendarOnline(loja, servico, Datas.Segunda().Hora(10), telefone: "67922223333")).DeveSerSucesso();   // começa quando o outro termina
        (await AgendarOnline(loja, servico, Datas.Segunda().Hora(8), telefone: "67944445555")).DeveSerSucesso();    // termina quando o outro começa
        Assert.Equal(3, (await loja.Painel.Get("/api/agendamentos")).Json.GetArrayLength());
    }

    [FatoIntegracao]
    public async Task Agendar_valida_dados_pagina_pausa_e_servico()
    {
        var loja = await api.NovaLojaComAgendamentoOnlineAsync();
        var servico = await loja.ServicoIdAsync();

        Assert.Equal("Informe nome e telefone.", (await AgendarOnline(loja, servico, Datas.Segunda().Hora(9), nome: " ")).Erro);
        Assert.Equal("Informe nome e telefone.", (await AgendarOnline(loja, servico, Datas.Segunda().Hora(9), telefone: "")).Erro);
        Assert.Equal("Serviço não encontrado.", (await AgendarOnline(loja, Guid.NewGuid(), Datas.Segunda().Hora(9))).Erro);
        Assert.Equal(0, (await loja.Painel.Get("/api/agendamentos")).Json.GetArrayLength());
        Assert.Equal(0, await api.NoBancoAsync(db => db.Clientes.CountAsync(c => c.LojaId == loja.Id)));   // nada foi criado

        var pausada = await api.NovaLojaComAgendamentoOnlineAsync(pausaAte: DateTime.UtcNow.AddDays(1));
        var servicoPausada = await pausada.ServicoIdAsync();
        Assert.Equal((400, "Agendamentos temporariamente pausados."), ((await AgendarOnline(pausada, servicoPausada, Datas.Segunda().Hora(9))) is var p ? (p.Codigo, p.Erro) : default));

        var desligada = await api.NovaLojaDeServicosAsync();
        Assert.Equal(404, (await AgendarOnline(desligada, await desligada.ServicoIdAsync(), Datas.Segunda().Hora(9))).Codigo);
    }

    // ── Reconhecer cliente pelo telefone ─────────────────────────────────────

    [FatoIntegracao]
    public async Task Verificar_cliente_devolve_so_o_primeiro_nome()
    {
        var loja = await api.NovaLojaComAgendamentoOnlineAsync();
        await loja.ClienteIdAsync("Maria da Silva Santos", "(67) 98888-7777");
        string Url(string tel) => $"/api/publico/{loja.Slug}/cliente?telefone={Uri.EscapeDataString(tel)}";

        var achou = (await loja.Site.Get(Url("67 98888-7777"))).Json;
        Assert.True(achou.GetProperty("existe").GetBoolean());
        Assert.Equal("Maria", achou.GetProperty("primeiroNome").GetString());

        Assert.False((await loja.Site.Get(Url("67911112222"))).Json.GetProperty("existe").GetBoolean());
        Assert.False((await loja.Site.Get(Url("1234567"))).Json.GetProperty("existe").GetBoolean());   // menos de 8 dígitos
        Assert.Equal(400, (await loja.Site.Get($"/api/publico/{loja.Slug}/cliente")).Codigo);   // telefone é obrigatório na consulta
    }

    [FatoIntegracao]
    public async Task Verificar_cliente_nao_enxerga_clientes_de_outra_loja_nem_de_pagina_desligada()
    {
        var a = await api.NovaLojaComAgendamentoOnlineAsync();
        var b = await api.NovaLojaComAgendamentoOnlineAsync();
        await a.ClienteIdAsync("Maria", "67988887777");
        Assert.False((await b.Site.Get($"/api/publico/{b.Slug}/cliente?telefone=67988887777")).Json.GetProperty("existe").GetBoolean());

        var desligada = await api.NovaLojaDeServicosAsync();
        Assert.Equal(404, (await desligada.Site.Get($"/api/publico/{desligada.Slug}/cliente?telefone=67988887777")).Codigo);
    }

    [FatoIntegracao]
    public async Task Agendar_so_enxerga_servicos_e_horarios_da_propria_loja()
    {
        var a = await api.NovaLojaComAgendamentoOnlineAsync(confirmacao: "automatico");
        var b = await api.NovaLojaComAgendamentoOnlineAsync(confirmacao: "automatico");
        var servicoA = await a.ServicoIdAsync();
        var servicoB = await b.ServicoIdAsync();
        (await AgendarOnline(a, servicoA, Datas.Segunda().Hora(9))).DeveSerSucesso();

        Assert.Equal("Serviço não encontrado.", (await AgendarOnline(b, servicoA, Datas.Segunda().Hora(9))).Erro);
        (await AgendarOnline(b, servicoB, Datas.Segunda().Hora(9))).DeveSerSucesso();      // o horário da loja A não bloqueia o da B
        Assert.Equal(1, (await b.Painel.Get("/api/agendamentos")).Json.GetArrayLength());
    }
}
