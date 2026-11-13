using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace LojaApi.Tests.Integracao;

// Corretora de seguros: seguradoras, funil de vendas (oportunidades), apólices, resumo e dashboard.
[Collection(ColecaoApi.Nome)]
public class CorretoraApiTests(ApiFixture api)
{
    private Task<LojaTeste> NovaAsync() => api.NovaLojaAsync(moduloReservasAtivo: false);

    private static async Task<Guid> SeguradoraAsync(LojaTeste loja, string nome = "Porto")
    {
        var r = await loja.Painel.Post("/api/corretora/seguradoras", new { nome });
        r.DeveSerSucesso();
        return r.Json.GetProperty("id").GetGuid();
    }

    private static async Task<Guid> ContaAsync(LojaTeste loja)
    {
        var r = await loja.Painel.Post("/api/financeiro/contas", new { nome = "Conta", saldoInicial = 0, banco = "Banco", limite = 0 });
        r.DeveSerSucesso();
        return r.Json.GetProperty("id").GetGuid();
    }

    private static async Task<Guid> OportunidadeAsync(LojaTeste loja, Guid cliente, Guid? seguradora = null, string plano = "Plano Saúde", decimal? valor = 1000, int? vidas = 2)
    {
        var r = await loja.Painel.Post("/api/corretora/oportunidades", new { clienteId = cliente, seguradoraId = seguradora, planoDesejado = plano, valorEstimado = valor, observacao = "obs", quantidadeVidas = vidas });
        r.DeveSerSucesso();
        return r.Json.GetProperty("id").GetGuid();
    }

    private static Task<Resposta> Mover(LojaTeste loja, Guid id, string etapa, int ordem = 1, string? motivo = null, decimal? valor = null, Guid? conta = null) =>
        loja.Painel.Patch($"/api/corretora/oportunidades/{id}/etapa", new { etapa, ordem, motivoPerda = motivo, valor, contaBancariaId = conta });

    private static object Apolice(Guid cliente, Guid seguradora, string plano = "Plano Saúde", decimal premio = 500, decimal comissao = 50, int inicioEmDias = -10, int fimEmDias = 355, Guid? oportunidade = null, Guid? conta = null, bool gerar = false) =>
        new
        {
            clienteId = cliente, oportunidadeId = oportunidade, seguradoraId = seguradora, nomePlano = plano, numeroApolice = "AP-1",
            valorPremio = premio, valorComissao = comissao, percentualComissao = (decimal?)10m,
            vigenciaInicio = DateTime.UtcNow.Date.AddDays(inicioEmDias).ToString("yyyy-MM-dd"),
            vigenciaFim = DateTime.UtcNow.Date.AddDays(fimEmDias).ToString("yyyy-MM-dd"),
            contaBancariaId = conta, gerarNoFinanceiro = gerar,
        };

    private static async Task<Guid> ApoliceAsync(LojaTeste loja, object corpo)
    {
        var r = await loja.Painel.Post("/api/corretora/apolices", corpo);
        r.DeveSerSucesso();
        return r.Json.GetProperty("id").GetGuid();
    }

    // ── Seguradoras ──────────────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Exige_login()
    {
        var loja = await NovaAsync();
        Assert.Equal(401, (await loja.Site.Get("/api/corretora/seguradoras")).Codigo);
    }

    [FatoIntegracao]
    public async Task Seguradoras_criar_listar_ativas_alternar_e_renomear()
    {
        var a = await NovaAsync();
        var b = await NovaAsync();
        var zurich = await SeguradoraAsync(a, "  Zurich ");
        var porto = await SeguradoraAsync(a, "Porto");
        await SeguradoraAsync(b, "Da B");

        async Task<List<string?>> Nomes(string q = "") => (await a.Painel.Get("/api/corretora/seguradoras" + q)).Json.EnumerateArray().Select(s => s.GetProperty("nome").GetString()).ToList();
        Assert.Equal(["Porto", "Zurich"], await Nomes());

        Assert.False((await a.Painel.Patch($"/api/corretora/seguradoras/{porto}/ativo")).Json.GetProperty("ativa").GetBoolean());
        Assert.Equal(["Zurich"], await Nomes());
        Assert.Equal(["Porto", "Zurich"], await Nomes("?todas=true"));

        var renomeada = await a.Painel.Put($"/api/corretora/seguradoras/{zurich}", new { nome = "  Zurich Brasil " });
        Assert.Equal("Zurich Brasil", renomeada.Json.GetProperty("nome").GetString());

        Assert.Equal(404, (await b.Painel.Patch($"/api/corretora/seguradoras/{porto}/ativo")).Codigo);
        Assert.Equal(404, (await b.Painel.Put($"/api/corretora/seguradoras/{porto}", new { nome = "Invasor" })).Codigo);
        Assert.Equal(404, (await b.Painel.Delete($"/api/corretora/seguradoras/{porto}")).Codigo);
    }

    [FatoIntegracao]
    public async Task Excluir_seguradora_em_uso_so_desativa_e_sem_uso_remove()
    {
        var loja = await NovaAsync();
        var cliente = await loja.ClienteIdAsync();
        var usada = await SeguradoraAsync(loja, "Usada");
        var livre = await SeguradoraAsync(loja, "Livre");
        await OportunidadeAsync(loja, cliente, usada);

        Assert.Equal("Seguradora em uso — foi desativada em vez de excluída.", (await loja.Painel.Delete($"/api/corretora/seguradoras/{usada}")).Json.GetProperty("mensagem").GetString());
        Assert.False((await api.NoBancoAsync(db => db.Seguradoras.AsNoTracking().FirstAsync(s => s.Id == usada))).Ativa);

        Assert.Equal("Seguradora excluída.", (await loja.Painel.Delete($"/api/corretora/seguradoras/{livre}")).Json.GetProperty("mensagem").GetString());
        Assert.Equal(0, await api.NoBancoAsync(db => db.Seguradoras.CountAsync(s => s.Id == livre)));
    }

    [FatoIntegracao]
    public async Task Seguradora_usada_so_em_apolice_tambem_e_so_desativada()
    {
        var loja = await NovaAsync();
        var cliente = await loja.ClienteIdAsync();
        var seg = await SeguradoraAsync(loja);
        await ApoliceAsync(loja, Apolice(cliente, seg));

        Assert.Equal("Seguradora em uso — foi desativada em vez de excluída.", (await loja.Painel.Delete($"/api/corretora/seguradoras/{seg}")).Json.GetProperty("mensagem").GetString());
    }

    // ── Funil de vendas ──────────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Oportunidade_nasce_no_fim_da_coluna_de_leads_e_a_lista_traz_cliente_e_seguradora()
    {
        var loja = await NovaAsync();
        var ana = await loja.ClienteIdAsync("Ana", "67911110000");
        var bia = await loja.ClienteIdAsync("Bia", "67922220000");
        var seg = await SeguradoraAsync(loja, "Porto");
        var primeira = await OportunidadeAsync(loja, ana, seg, "Saúde", 900, 3);
        await OportunidadeAsync(loja, bia, null, "Auto", null, null);

        var lista = (await loja.Painel.Get("/api/corretora/oportunidades")).Json.EnumerateArray().ToList();

        Assert.Equal([1, 2], lista.Select(o => o.GetProperty("ordem").GetInt32()).ToList());
        var o1 = lista[0];
        Assert.Equal((primeira, "Ana", "67911110000", "Porto", "Saúde", 900m, "lead", 3), (o1.GetProperty("id").GetGuid(), o1.GetProperty("clienteNome").GetString(),
            o1.GetProperty("clienteTelefone").GetString(), o1.GetProperty("seguradoraNome").GetString(), o1.GetProperty("planoDesejado").GetString(),
            o1.GetProperty("valorEstimado").GetDecimal(), o1.GetProperty("etapa").GetString(), o1.GetProperty("quantidadeVidas").GetInt32()));
        Assert.Equal(JsonValueKind.Null, lista[1].GetProperty("seguradoraNome").ValueKind);
    }

    [FatoIntegracao]
    public async Task Atualizar_e_excluir_oportunidade_com_isolamento()
    {
        var a = await NovaAsync();
        var b = await NovaAsync();
        var cliente = await a.ClienteIdAsync();
        var id = await OportunidadeAsync(a, cliente);

        var r = await a.Painel.Put($"/api/corretora/oportunidades/{id}", new { clienteId = cliente, seguradoraId = (Guid?)null, planoDesejado = "Novo", valorEstimado = 2000m, observacao = "x", quantidadeVidas = 5 });
        Assert.Equal(200, r.Codigo);
        var op = await api.NoBancoAsync(db => db.Oportunidades.AsNoTracking().FirstAsync(o => o.Id == id));
        Assert.Equal(("Novo", 2000m, 5), (op.PlanoDesejado, op.ValorEstimado, op.QuantidadeVidas));

        Assert.Equal(404, (await b.Painel.Put($"/api/corretora/oportunidades/{id}", new { clienteId = cliente, planoDesejado = "Invasor" })).Codigo);
        Assert.Equal(404, (await b.Painel.Delete($"/api/corretora/oportunidades/{id}")).Codigo);
        Assert.Equal(404, (await Mover(b, id, "contato")).Codigo);

        Assert.Equal("Removido.", (await a.Painel.Delete($"/api/corretora/oportunidades/{id}")).Json.GetProperty("mensagem").GetString());
        Assert.Equal(404, (await a.Painel.Delete($"/api/corretora/oportunidades/{id}")).Codigo);
    }

    [FatoIntegracao]
    public async Task Mover_etapa_valida_e_guarda_o_motivo_da_perda()
    {
        var loja = await NovaAsync();
        var id = await OportunidadeAsync(loja, await loja.ClienteIdAsync());

        var invalida = await Mover(loja, id, "voando");
        Assert.Equal((400, "Etapa inválida."), (invalida.Codigo, invalida.Erro));

        var contato = await Mover(loja, id, "contato", 7);
        Assert.Equal(("contato", false), (contato.Json.GetProperty("etapa").GetString(), contato.Json.GetProperty("comissaoLancada").GetBoolean()));

        await Mover(loja, id, "perdido", 3, motivo: "Achou mais barato");
        var op = await api.NoBancoAsync(db => db.Oportunidades.AsNoTracking().FirstAsync(o => o.Id == id));
        Assert.Equal(("perdido", 3, "Achou mais barato"), (op.Etapa, op.Ordem, op.MotivoPerda));
    }

    [FatoIntegracao]
    public async Task Motivo_da_perda_so_e_gravado_na_etapa_perdido()
    {
        var loja = await NovaAsync();
        var id = await OportunidadeAsync(loja, await loja.ClienteIdAsync());

        await Mover(loja, id, "contato", motivo: "não deveria gravar");

        Assert.Null((await api.NoBancoAsync(db => db.Oportunidades.AsNoTracking().FirstAsync(o => o.Id == id))).MotivoPerda);
    }

    [FatoIntegracao]
    public async Task Atualizar_oportunidade_troca_o_cliente()
    {
        var loja = await NovaAsync();
        var ana = await loja.ClienteIdAsync("Ana", "67911110000");
        var bia = await loja.ClienteIdAsync("Bia", "67922220000");
        var id = await OportunidadeAsync(loja, ana);

        (await loja.Painel.Put($"/api/corretora/oportunidades/{id}", new { clienteId = bia, seguradoraId = (Guid?)null, planoDesejado = "x", valorEstimado = 1m, observacao = "o", quantidadeVidas = 1 })).DeveSerSucesso();

        Assert.Equal(bia, (await api.NoBancoAsync(db => db.Oportunidades.AsNoTracking().FirstAsync(o => o.Id == id))).ClienteId);
    }

    [FatoIntegracao]
    public async Task Nova_oportunidade_usa_a_maior_ordem_so_da_coluna_de_leads()
    {
        var loja = await NovaAsync();
        var cliente = await loja.ClienteIdAsync();
        var emContato = await OportunidadeAsync(loja, cliente, null, "A");
        await Mover(loja, emContato, "contato", ordem: 50);   // ordem alta em OUTRA coluna não conta

        var nova = await OportunidadeAsync(loja, cliente, null, "B");

        Assert.Equal(1, (await api.NoBancoAsync(db => db.Oportunidades.AsNoTracking().FirstAsync(o => o.Id == nova))).Ordem);
        var segundoLead = await OportunidadeAsync(loja, cliente, null, "C");
        Assert.Equal(2, (await api.NoBancoAsync(db => db.Oportunidades.AsNoTracking().FirstAsync(o => o.Id == segundoLead))).Ordem);
    }

    [FatoIntegracao]
    public async Task Ganhar_com_conta_lanca_a_comissao_no_financeiro_uma_unica_vez()
    {
        var loja = await NovaAsync();
        var conta = await ContaAsync(loja);
        var cliente = await loja.ClienteIdAsync("Ana");
        var id = await OportunidadeAsync(loja, cliente, null, "Saúde", 1000);

        var r = await Mover(loja, id, "ganho", 1, valor: 120m, conta: conta);

        Assert.True(r.Json.GetProperty("comissaoLancada").GetBoolean());
        var lancamento = await api.NoBancoAsync(db => db.LancamentosFinanceiros.AsNoTracking().SingleAsync(l => l.LojaId == loja.Id));
        Assert.Equal(("receber", "avulsa", "Comissão — Saúde (Ana)", 120m, conta, true), (lancamento.Tipo, lancamento.Modo, lancamento.Descricao, lancamento.Valor, lancamento.ContaBancariaId, lancamento.Avisar));
        Assert.Equal(lancamento.Id, (await api.NoBancoAsync(db => db.Oportunidades.AsNoTracking().FirstAsync(o => o.Id == id))).LancamentoFinanceiroId);

        var denovo = await Mover(loja, id, "ganho", 2, conta: conta);
        Assert.True(denovo.Json.GetProperty("comissaoLancada").GetBoolean());
        Assert.Equal(1, await api.NoBancoAsync(db => db.LancamentosFinanceiros.CountAsync(l => l.LojaId == loja.Id)));
    }

    [FatoIntegracao]
    public async Task Ganhar_usa_o_valor_estimado_quando_nao_informam_e_sem_conta_nao_lanca()
    {
        var loja = await NovaAsync();
        var conta = await ContaAsync(loja);
        var comValor = await OportunidadeAsync(loja, await loja.ClienteIdAsync("Ana", "67911110000"), null, "Saúde", 800);
        var semConta = await OportunidadeAsync(loja, await loja.ClienteIdAsync("Bia", "67922220000"), null, "Auto", 300);

        await Mover(loja, comValor, "ganho", conta: conta);
        var r = await Mover(loja, semConta, "ganho");

        Assert.Equal(800m, (await api.NoBancoAsync(db => db.LancamentosFinanceiros.AsNoTracking().SingleAsync(l => l.LojaId == loja.Id))).Valor);
        Assert.False(r.Json.GetProperty("comissaoLancada").GetBoolean());
    }

    [FatoIntegracao]
    public async Task Verificar_cliente_so_conta_oportunidades_em_aberto()
    {
        var loja = await NovaAsync();
        var cliente = await loja.ClienteIdAsync();
        Assert.False((await loja.Painel.Get($"/api/corretora/oportunidades/verificar-cliente/{cliente}")).Json.GetProperty("existe").GetBoolean());

        var aberta = await OportunidadeAsync(loja, cliente, null, "Saúde");
        var encerrada = await OportunidadeAsync(loja, cliente, null, "Auto");
        await Mover(loja, encerrada, "perdido", motivo: "x");

        var r = (await loja.Painel.Get($"/api/corretora/oportunidades/verificar-cliente/{cliente}")).Json;
        Assert.True(r.GetProperty("existe").GetBoolean());
        Assert.Equal(1, r.GetProperty("quantidade").GetInt32());
        Assert.Equal(aberta, r.GetProperty("oportunidades")[0].GetProperty("id").GetGuid());
    }

    // ── Apólices ─────────────────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Criar_apolice_normaliza_as_vigencias_e_a_lista_traz_dias_para_vencer()
    {
        var loja = await NovaAsync();
        var cliente = await loja.ClienteIdAsync("Ana");
        var seg = await SeguradoraAsync(loja, "Porto");
        var id = await ApoliceAsync(loja, Apolice(cliente, seg, "  Saúde  ", 500, 50, -10, 20));

        var a = (await loja.Painel.Get("/api/corretora/apolices")).Json[0];

        Assert.Equal((id, "Ana", "Porto", "Saúde", 500m, 50m, "ativa", 20), (a.GetProperty("id").GetGuid(), a.GetProperty("clienteNome").GetString(), a.GetProperty("seguradoraNome").GetString(),
            a.GetProperty("nomePlano").GetString(), a.GetProperty("valorPremio").GetDecimal(), a.GetProperty("valorComissao").GetDecimal(), a.GetProperty("status").GetString(), a.GetProperty("diasParaVencer").GetInt32()));
        var gravada = await api.NoBancoAsync(db => db.Apolices.AsNoTracking().FirstAsync(x => x.Id == id));
        Assert.Equal(new TimeSpan(12, 0, 0), gravada.VigenciaFim.TimeOfDay);
        Assert.Equal(DateTime.UtcNow.Date.AddDays(-10), gravada.VigenciaInicio.Date);
    }

    [FatoIntegracao]
    public async Task Lista_de_apolices_vem_da_que_vence_primeiro()
    {
        var loja = await NovaAsync();
        var cliente = await loja.ClienteIdAsync();
        var seg = await SeguradoraAsync(loja);
        await ApoliceAsync(loja, Apolice(cliente, seg, "Longe", fimEmDias: 300));
        await ApoliceAsync(loja, Apolice(cliente, seg, "Perto", fimEmDias: 5));

        var nomes = (await loja.Painel.Get("/api/corretora/apolices")).Json.EnumerateArray().Select(x => x.GetProperty("nomePlano").GetString()).ToList();

        Assert.Equal(["Perto", "Longe"], nomes);
    }

    [FatoIntegracao]
    public async Task Apolice_vinda_de_oportunidade_marca_ela_como_ganha()
    {
        var loja = await NovaAsync();
        var cliente = await loja.ClienteIdAsync();
        var seg = await SeguradoraAsync(loja);
        var op = await OportunidadeAsync(loja, cliente, seg);

        await ApoliceAsync(loja, Apolice(cliente, seg, oportunidade: op));

        Assert.Equal("ganho", (await api.NoBancoAsync(db => db.Oportunidades.AsNoTracking().FirstAsync(o => o.Id == op))).Etapa);
    }

    [FatoIntegracao]
    public async Task Apolice_pode_gerar_a_comissao_a_receber_no_financeiro()
    {
        var loja = await NovaAsync();
        var conta = await ContaAsync(loja);
        var cliente = await loja.ClienteIdAsync("Ana");
        var seg = await SeguradoraAsync(loja);

        var id = await ApoliceAsync(loja, Apolice(cliente, seg, "Saúde", 500, 75, conta: conta, gerar: true));
        await ApoliceAsync(loja, Apolice(cliente, seg, "Sem gerar", 500, 99, conta: conta, gerar: false));
        await ApoliceAsync(loja, Apolice(cliente, seg, "Sem conta", 500, 98, gerar: true));

        var lancamento = await api.NoBancoAsync(db => db.LancamentosFinanceiros.AsNoTracking().SingleAsync(l => l.LojaId == loja.Id));
        Assert.Equal(("receber", "Comissão — Saúde (Ana)", 75m, conta), (lancamento.Tipo, lancamento.Descricao, lancamento.Valor, lancamento.ContaBancariaId));
        Assert.Equal(lancamento.Id, (await api.NoBancoAsync(db => db.Apolices.AsNoTracking().FirstAsync(a => a.Id == id))).LancamentoFinanceiroId);
        Assert.Equal(lancamento.Vencimento.Date, DateTime.UtcNow.Date.AddDays(-10));   // vence na vigência inicial
    }

    [FatoIntegracao]
    public async Task Atualizar_apolice_e_alterar_status_com_isolamento()
    {
        var a = await NovaAsync();
        var b = await NovaAsync();
        var cliente = await a.ClienteIdAsync();
        var seg = await SeguradoraAsync(a);
        var id = await ApoliceAsync(a, Apolice(cliente, seg));

        var r = await a.Painel.Put($"/api/corretora/apolices/{id}", Apolice(cliente, seg, " Novo plano ", 700, 70));
        Assert.Equal(200, r.Codigo);
        var apolice = await api.NoBancoAsync(db => db.Apolices.AsNoTracking().FirstAsync(x => x.Id == id));
        Assert.Equal(("Novo plano", 700m, 70m), (apolice.NomePlano, apolice.ValorPremio, apolice.ValorComissao));

        var invalido = await a.Painel.Patch($"/api/corretora/apolices/{id}/status", new { nome = "sumiu" });
        Assert.Equal((400, "Status inválido."), (invalido.Codigo, invalido.Erro));
        foreach (var status in new[] { "renovada", "vencida", "cancelada", "ativa" })
            Assert.Equal(status, (await a.Painel.Patch($"/api/corretora/apolices/{id}/status", new { nome = status })).Json.GetProperty("status").GetString());

        Assert.Equal(404, (await b.Painel.Put($"/api/corretora/apolices/{id}", Apolice(cliente, seg))).Codigo);
        Assert.Equal(404, (await b.Painel.Patch($"/api/corretora/apolices/{id}/status", new { nome = "cancelada" })).Codigo);
        Assert.Equal(0, (await b.Painel.Get("/api/corretora/apolices")).Json.GetArrayLength());
    }

    // ── Resumo e dashboard ───────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Resumo_soma_apolices_nao_canceladas_conta_vencendo_em_30_dias_e_o_funil()
    {
        var loja = await NovaAsync();
        var cliente = await loja.ClienteIdAsync();
        var seg = await SeguradoraAsync(loja);
        await ApoliceAsync(loja, Apolice(cliente, seg, "Vence logo", 500, 50, fimEmDias: 10));
        await ApoliceAsync(loja, Apolice(cliente, seg, "Vence longe", 300, 30, fimEmDias: 200));
        var cancelada = await ApoliceAsync(loja, Apolice(cliente, seg, "Cancelada", 1000, 100, fimEmDias: 5));
        await loja.Painel.Patch($"/api/corretora/apolices/{cancelada}/status", new { nome = "cancelada" });
        await OportunidadeAsync(loja, cliente);
        await OportunidadeAsync(loja, cliente);
        var ganha = await OportunidadeAsync(loja, cliente);
        await Mover(loja, ganha, "ganho");

        var r = (await loja.Painel.Get("/api/corretora/resumo")).Json;

        Assert.Equal((2, 800m, 80m, 1), (r.GetProperty("totalApolices").GetInt32(), r.GetProperty("totalPremios").GetDecimal(), r.GetProperty("totalComissoes").GetDecimal(), r.GetProperty("vencendoEm30Dias").GetInt32()));
        var funil = r.GetProperty("funil").EnumerateArray().ToDictionary(x => x.GetProperty("etapa").GetString()!, x => x.GetProperty("qtd").GetInt32());
        Assert.Equal((2, 1), (funil["lead"], funil["ganho"]));
    }

    [FatoIntegracao]
    public async Task Dashboard_separa_em_andamento_de_implantadas_dentro_da_janela_de_dias()
    {
        var loja = await NovaAsync();
        var cliente = await loja.ClienteIdAsync();
        await OportunidadeAsync(loja, cliente, null, "A", 100, 2);
        await OportunidadeAsync(loja, cliente, null, "B", 200, 3);
        var ganha = await OportunidadeAsync(loja, cliente, null, "C", 400, 4);
        var perdida = await OportunidadeAsync(loja, cliente, null, "D", 800, 8);
        var antiga = await OportunidadeAsync(loja, cliente, null, "E", 1600, 16);
        await Mover(loja, ganha, "ganho");
        await Mover(loja, perdida, "perdido");
        await api.NoBancoAsync(db => db.Oportunidades.Where(o => o.Id == antiga).ExecuteUpdateAsync(s => s.SetProperty(o => o.CriadoEm, DateTime.UtcNow.AddDays(-40))));

        var r = (await loja.Painel.Get("/api/corretora/dashboard")).Json;
        var andamento = r.GetProperty("emAndamento");
        var implantadas = r.GetProperty("implantadas");

        Assert.Equal((2, 5, 300m), (andamento.GetProperty("propostas").GetInt32(), andamento.GetProperty("vidas").GetInt32(), andamento.GetProperty("valorTotal").GetDecimal()));
        Assert.Equal((1, 4, 400m), (implantadas.GetProperty("propostas").GetInt32(), implantadas.GetProperty("vidas").GetInt32(), implantadas.GetProperty("valorTotal").GetDecimal()));

        var larga = (await loja.Painel.Get("/api/corretora/dashboard?dias=90")).Json;
        Assert.Equal(3, larga.GetProperty("emAndamento").GetProperty("propostas").GetInt32());   // agora entra a antiga
    }

    // Correção desta migração: antes, cliente, seguradora, oportunidade e conta bancária de OUTRA loja eram aceitos
    // (o nome do cliente alheio aparecia na lista, a oportunidade alheia virava "ganho" e o lançamento ia para a conta alheia).

    [FatoIntegracao]
    public async Task Oportunidade_nao_aceita_cliente_nem_seguradora_de_outra_loja()
    {
        var a = await NovaAsync();
        var b = await NovaAsync();
        var clienteDeA = await a.ClienteIdAsync("Cliente Secreto da A");
        var seguradoraDeA = await SeguradoraAsync(a, "Seguradora da A");
        var clienteB = await b.ClienteIdAsync();
        var seguradoraB = await SeguradoraAsync(b);
        object Corpo(Guid cliente, Guid? seguradora) => new { clienteId = cliente, seguradoraId = seguradora, planoDesejado = "x", valorEstimado = 1m, observacao = "o", quantidadeVidas = 1 };

        var clienteAlheio = await b.Painel.Post("/api/corretora/oportunidades", Corpo(clienteDeA, null));
        Assert.Equal((400, "Cliente não encontrado."), (clienteAlheio.Codigo, clienteAlheio.Erro));
        var seguradoraAlheia = await b.Painel.Post("/api/corretora/oportunidades", Corpo(clienteB, seguradoraDeA));
        Assert.Equal((400, "Seguradora não encontrada."), (seguradoraAlheia.Codigo, seguradoraAlheia.Erro));
        Assert.Equal(0, (await b.Painel.Get("/api/corretora/oportunidades")).Json.GetArrayLength());

        var propria = await OportunidadeAsync(b, clienteB, seguradoraB);
        Assert.Equal("Cliente não encontrado.", (await b.Painel.Put($"/api/corretora/oportunidades/{propria}", Corpo(clienteDeA, seguradoraB))).Erro);
        Assert.Equal("Seguradora não encontrada.", (await b.Painel.Put($"/api/corretora/oportunidades/{propria}", Corpo(clienteB, seguradoraDeA))).Erro);
        var gravada = await api.NoBancoAsync(db => db.Oportunidades.AsNoTracking().FirstAsync(o => o.Id == propria));
        Assert.Equal((clienteB, seguradoraB), (gravada.ClienteId, gravada.SeguradoraId));
    }

    [FatoIntegracao]
    public async Task Ganhar_nao_lanca_comissao_em_conta_bancaria_de_outra_loja()
    {
        var a = await NovaAsync();
        var b = await NovaAsync();
        var contaDeA = await ContaAsync(a);
        var id = await OportunidadeAsync(b, await b.ClienteIdAsync());

        var r = await Mover(b, id, "ganho", conta: contaDeA);

        Assert.Equal((400, "Conta bancária não encontrada."), (r.Codigo, r.Erro));
        Assert.Equal("lead", (await api.NoBancoAsync(db => db.Oportunidades.AsNoTracking().FirstAsync(o => o.Id == id))).Etapa);
        Assert.Equal(0, await api.NoBancoAsync(db => db.LancamentosFinanceiros.CountAsync(l => l.ContaBancariaId == contaDeA)));
    }

    [FatoIntegracao]
    public async Task Apolice_nao_aceita_cliente_seguradora_oportunidade_nem_conta_de_outra_loja()
    {
        var a = await NovaAsync();
        var b = await NovaAsync();
        var clienteDeA = await a.ClienteIdAsync("Cliente Secreto da A");
        var seguradoraDeA = await SeguradoraAsync(a);
        var oportunidadeDeA = await OportunidadeAsync(a, clienteDeA);
        var contaDeA = await ContaAsync(a);
        var clienteB = await b.ClienteIdAsync();
        var seguradoraB = await SeguradoraAsync(b);

        async Task<Resposta> Criar(object corpo) => await b.Painel.Post("/api/corretora/apolices", corpo);

        var r1 = await Criar(Apolice(clienteDeA, seguradoraB));
        Assert.Equal((400, "Cliente não encontrado."), (r1.Codigo, r1.Erro));
        var r2 = await Criar(Apolice(clienteB, seguradoraDeA));
        Assert.Equal((400, "Seguradora não encontrada."), (r2.Codigo, r2.Erro));
        var r3 = await Criar(Apolice(clienteB, seguradoraB, oportunidade: oportunidadeDeA));
        Assert.Equal((400, "Oportunidade não encontrada."), (r3.Codigo, r3.Erro));
        var r4 = await Criar(Apolice(clienteB, seguradoraB, conta: contaDeA, gerar: true));
        Assert.Equal((400, "Conta bancária não encontrada."), (r4.Codigo, r4.Erro));

        Assert.Equal(0, (await b.Painel.Get("/api/corretora/apolices")).Json.GetArrayLength());
        Assert.Equal("lead", (await api.NoBancoAsync(db => db.Oportunidades.AsNoTracking().FirstAsync(o => o.Id == oportunidadeDeA))).Etapa);   // a oportunidade alheia não virou "ganho"
        Assert.Equal(0, await api.NoBancoAsync(db => db.LancamentosFinanceiros.CountAsync(l => l.ContaBancariaId == contaDeA)));

        var propria = await ApoliceAsync(b, Apolice(clienteB, seguradoraB));
        Assert.Equal("Seguradora não encontrada.", (await b.Painel.Put($"/api/corretora/apolices/{propria}", Apolice(clienteB, seguradoraDeA))).Erro);
        Assert.Equal(seguradoraB, (await api.NoBancoAsync(db => db.Apolices.AsNoTracking().FirstAsync(x => x.Id == propria))).SeguradoraId);
    }

    [FatoIntegracao]
    public async Task Uma_loja_nao_ve_dados_de_corretora_de_outra()
    {
        var a = await NovaAsync();
        var b = await NovaAsync();
        var cliente = await a.ClienteIdAsync();
        var seg = await SeguradoraAsync(a);
        await OportunidadeAsync(a, cliente, seg);
        await ApoliceAsync(a, Apolice(cliente, seg));

        Assert.Equal(0, (await b.Painel.Get("/api/corretora/seguradoras?todas=true")).Json.GetArrayLength());
        Assert.Equal(0, (await b.Painel.Get("/api/corretora/oportunidades")).Json.GetArrayLength());
        Assert.Equal(0, (await b.Painel.Get("/api/corretora/apolices")).Json.GetArrayLength());
        Assert.Equal(0, (await b.Painel.Get("/api/corretora/resumo")).Json.GetProperty("totalApolices").GetInt32());
        Assert.False((await b.Painel.Get($"/api/corretora/oportunidades/verificar-cliente/{cliente}")).Json.GetProperty("existe").GetBoolean());
    }
}
