using System.Text.Json;
using LojaApi.Models;
using LojaApi.Tests.Chacara.Dominio;
using Microsoft.EntityFrameworkCore;

namespace LojaApi.Tests.Integracao;

// Planos de assinatura (mensalidade) e os clientes assinantes.
[Collection(ColecaoApi.Nome)]
public class PlanosApiTests(ApiFixture api)
{
    private static DateTime MesAtual() => new(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);

    private static Task<Resposta> NovoPlano(LojaTeste loja, string nome = "Plano Banho", decimal valor = 150, string? servicos = null,
        int? limite = null, int[]? dias = null) =>
        loja.Painel.Post("/api/planos", new { nome, valor, servicosIds = servicos, limiteUsosMes = limite, diasSemana = dias });

    private async Task<(Guid Plano, Guid Cliente, Guid Assinatura)> AssinanteAsync(LojaTeste loja, decimal valor = 150, string nomeCliente = "Assinante",
        string telefone = "67999990000", bool proximoMes = false, string? servicos = null, int? limite = null, int[]? dias = null, string nomePlano = "Plano Banho")
    {
        var plano = await NovoPlano(loja, nomePlano, valor, servicos, limite, dias);
        plano.DeveSerSucesso();
        var cliente = await loja.ClienteIdAsync(nomeCliente, telefone);
        var assinatura = await loja.Painel.Post("/api/planos/assinantes", new { clienteId = cliente, planoId = plano.Json.GetProperty("id").GetGuid(), diaVencimento = 10, iniciarProximoMes = proximoMes });
        assinatura.DeveSerSucesso();
        return (plano.Json.GetProperty("id").GetGuid(), cliente, assinatura.Json.GetProperty("id").GetGuid());
    }

    private Task<Guid> PendenteAsync(LojaTeste loja, Guid assinatura, int mesesAtras, decimal valor = 150, string status = "pendente") =>
        api.NoBancoAsync(async db =>
        {
            var pg = new PagamentoPlano { AssinaturaId = assinatura, LojaId = loja.Id, MesReferencia = MesAtual().AddMonths(-mesesAtras), Valor = valor, Status = status };
            db.PagamentosPlano.Add(pg);
            await db.SaveChangesAsync();
            return pg.Id;
        });

    private Task<PagamentoPlano> PagamentoNoBanco(Guid id) => api.NoBancoAsync(db => db.PagamentosPlano.AsNoTracking().FirstAsync(p => p.Id == id));

    private async Task<(Venda Venda, List<ItemVenda> Itens)> VendaDe(Guid vendaId)
    {
        var venda = await api.NoBancoAsync(db => db.Vendas.AsNoTracking().FirstAsync(v => v.Id == vendaId));
        var itens = await api.NoBancoAsync(db => db.ItensVenda.AsNoTracking().Where(i => i.VendaId == vendaId).OrderBy(i => i.PrecoUnitario).ToListAsync());
        return (venda, itens);
    }

    // ── Planos ───────────────────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Exige_login()
    {
        var loja = await api.NovaLojaDeServicosAsync();
        Assert.Equal(401, (await loja.Site.Get("/api/planos")).Codigo);
        Assert.Equal(401, (await loja.Site.Get("/api/planos/assinantes")).Codigo);
    }

    [FatoIntegracao]
    public async Task Criar_valida_nome_e_valor_e_normaliza_limite_e_dias()
    {
        var loja = await api.NovaLojaDeServicosAsync();

        Assert.Equal("Informe nome e valor válidos.", (await NovoPlano(loja, nome: "  ")).Erro);
        Assert.Equal("Informe nome e valor válidos.", (await NovoPlano(loja, valor: 0)).Erro);

        var r = await NovoPlano(loja, nome: "  Plano Ouro  ", valor: 200, servicos: "a,b", limite: 0, dias: [3, 1, 1, 9]);
        Assert.Equal(200, r.Codigo);
        Assert.Equal("Plano Ouro", r.Json.GetProperty("nome").GetString());
        Assert.Equal(200m, r.Json.GetProperty("valor").GetDecimal());
        Assert.Equal("a,b", r.Json.GetProperty("servicosIds").GetString());
        Assert.True(r.Json.GetProperty("ativo").GetBoolean());
        Assert.Equal(JsonValueKind.Null, r.Json.GetProperty("limiteUsosMes").ValueKind);        // 0 = sem limite
        Assert.Equal("1,3", r.Json.GetProperty("diasSemanaPermitidos").GetString());             // ordenado, sem repetir, ignora fora de 0-6

        var todosOsDias = await NovoPlano(loja, nome: "Todos", dias: [0, 1, 2, 3, 4, 5, 6], limite: 4);
        Assert.Equal(JsonValueKind.Null, todosOsDias.Json.GetProperty("diasSemanaPermitidos").ValueKind);   // 7 dias = sem restrição
        Assert.Equal(4, todosOsDias.Json.GetProperty("limiteUsosMes").GetInt32());
    }

    [FatoIntegracao]
    public async Task Listar_mostra_ativos_primeiro_e_em_ordem_de_nome()
    {
        var loja = await api.NovaLojaDeServicosAsync();
        await NovoPlano(loja, "Zeta");
        var inativo = await NovoPlano(loja, "Alfa");
        await NovoPlano(loja, "Beta");
        await loja.Painel.Patch($"/api/planos/{inativo.Json.GetProperty("id").GetGuid()}/ativo");

        var nomes = (await loja.Painel.Get("/api/planos")).Json.EnumerateArray().Select(p => p.GetProperty("nome").GetString()).ToList();

        Assert.Equal(["Beta", "Zeta", "Alfa"], nomes);
    }

    [FatoIntegracao]
    public async Task Atualizar_alterna_ativo_e_404_para_inexistente()
    {
        var loja = await api.NovaLojaDeServicosAsync();
        var id = (await NovoPlano(loja, "Plano", 100)).Json.GetProperty("id").GetGuid();

        var r = await loja.Painel.Put($"/api/planos/{id}", new { nome = " Novo ", valor = 120, servicosIds = "x", limiteUsosMes = 3, diasSemana = new[] { 2 } });
        Assert.Equal(("Novo", 120m, 3, "2"), (r.Json.GetProperty("nome").GetString(), r.Json.GetProperty("valor").GetDecimal(),
            r.Json.GetProperty("limiteUsosMes").GetInt32(), r.Json.GetProperty("diasSemanaPermitidos").GetString()));

        Assert.False((await loja.Painel.Patch($"/api/planos/{id}/ativo")).Json.GetProperty("ativo").GetBoolean());
        Assert.True((await loja.Painel.Patch($"/api/planos/{id}/ativo")).Json.GetProperty("ativo").GetBoolean());

        Assert.Equal(404, (await loja.Painel.Put($"/api/planos/{Guid.NewGuid()}", new { nome = "x", valor = 1 })).Codigo);
        Assert.Equal(404, (await loja.Painel.Patch($"/api/planos/{Guid.NewGuid()}/ativo")).Codigo);
        Assert.Equal(404, (await loja.Painel.Delete($"/api/planos/{Guid.NewGuid()}")).Codigo);
    }

    [FatoIntegracao]
    public async Task Plano_com_assinante_ativo_nao_pode_ser_excluido_ate_cancelar()
    {
        var loja = await api.NovaLojaDeServicosAsync();
        var (plano, _, assinatura) = await AssinanteAsync(loja);

        var bloqueado = await loja.Painel.Delete($"/api/planos/{plano}");
        Assert.Equal(400, bloqueado.Codigo);
        Assert.Equal("Este plano tem clientes ativos. Desative-o em vez de excluir.", bloqueado.Erro);

        await loja.Painel.Patch($"/api/planos/assinantes/{assinatura}/cancelar");
        Assert.Equal("Plano excluído.", (await loja.Painel.Delete($"/api/planos/{plano}")).Json.GetProperty("mensagem").GetString());
        Assert.Equal(0, (await loja.Painel.Get("/api/planos")).Json.GetArrayLength());
    }

    // ── Assinaturas ──────────────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Vincular_valida_plano_cliente_e_duplicidade()
    {
        var loja = await api.NovaLojaDeServicosAsync();
        var plano = (await NovoPlano(loja)).Json.GetProperty("id").GetGuid();
        var cliente = await loja.ClienteIdAsync();

        Assert.Equal("Plano não encontrado.", (await loja.Painel.Post("/api/planos/assinantes", new { clienteId = cliente, planoId = Guid.NewGuid(), diaVencimento = 5 })).Erro);
        Assert.Equal("Cliente não encontrado.", (await loja.Painel.Post("/api/planos/assinantes", new { clienteId = Guid.NewGuid(), planoId = plano, diaVencimento = 5 })).Erro);

        (await loja.Painel.Post("/api/planos/assinantes", new { clienteId = cliente, planoId = plano, diaVencimento = 5 })).DeveSerSucesso();
        var duplicada = await loja.Painel.Post("/api/planos/assinantes", new { clienteId = cliente, planoId = plano, diaVencimento = 5 });
        Assert.Equal(409, duplicada.Codigo);
        Assert.Equal("Este cliente já tem um plano ativo.", duplicada.Erro);
    }

    [FatoIntegracao]
    public async Task Vincular_usa_dia_10_quando_o_vencimento_e_invalido_e_pode_iniciar_no_proximo_mes()
    {
        var loja = await api.NovaLojaDeServicosAsync();
        var plano = (await NovoPlano(loja)).Json.GetProperty("id").GetGuid();
        var a = await loja.ClienteIdAsync("Ana", "67911110000");
        var b = await loja.ClienteIdAsync("Bia", "67922220000");
        var da = await loja.Painel.Post("/api/planos/assinantes", new { clienteId = a, planoId = plano, diaVencimento = 31 });
        var db_ = await loja.Painel.Post("/api/planos/assinantes", new { clienteId = b, planoId = plano, diaVencimento = 28, iniciarProximoMes = true });

        var lista = (await loja.Painel.Get("/api/planos/assinantes")).Json.EnumerateArray().ToDictionary(x => x.GetProperty("clienteNome").GetString()!);

        Assert.Equal(10, lista["Ana"].GetProperty("diaVencimento").GetInt32());
        Assert.False(lista["Ana"].GetProperty("aindaNaoIniciou").GetBoolean());
        Assert.Equal(28, lista["Bia"].GetProperty("diaVencimento").GetInt32());
        Assert.True(lista["Bia"].GetProperty("aindaNaoIniciou").GetBoolean());
        Assert.Equal(da.Json.GetProperty("id").GetGuid(), lista["Ana"].GetProperty("assinaturaId").GetGuid());
        Assert.NotEqual(db_.Json.GetProperty("id").GetGuid(), Guid.Empty);
    }

    [FatoIntegracao]
    public async Task Assinantes_traz_atraso_usos_e_vem_ordenado_por_atraso_e_nome()
    {
        var loja = await api.NovaLojaDeServicosAsync();
        var (_, _, emDia) = await AssinanteAsync(loja, nomeCliente: "Ana em dia", telefone: "67911110000", nomePlano: "P1", limite: 4);
        var (_, _, atrasada) = await AssinanteAsync(loja, nomeCliente: "Zoe atrasada", telefone: "67922220000", nomePlano: "P2");
        await PendenteAsync(loja, atrasada, 1);
        await PendenteAsync(loja, atrasada, 2);
        await PendenteAsync(loja, atrasada, 3, status: "pago");     // pago não conta
        await api.NoBancoAsync(async db =>
        {
            db.ConsumosPlano.Add(new ConsumoPlano { AssinaturaId = emDia, LojaId = loja.Id, ServicoId = Guid.NewGuid(), NomeServico = "Banho" });
            db.ConsumosPlano.Add(new ConsumoPlano { AssinaturaId = emDia, LojaId = loja.Id, ServicoId = Guid.NewGuid(), NomeServico = "Banho", CriadoEm = DateTime.UtcNow.AddMonths(-3) });
            await db.SaveChangesAsync();
        });

        var lista = (await loja.Painel.Get("/api/planos/assinantes")).Json;

        Assert.Equal(["Zoe atrasada", "Ana em dia"], lista.EnumerateArray().Select(x => x.GetProperty("clienteNome").GetString()).ToList());
        var zoe = lista[0];
        Assert.Equal((2, 300m), (zoe.GetProperty("mesesEmAtraso").GetInt32(), zoe.GetProperty("valorTotalAtraso").GetDecimal()));
        Assert.False(zoe.GetProperty("pagoNoMes").GetBoolean());
        var ana = lista[1];
        Assert.Equal((2, 1, 4m, "P1"), (ana.GetProperty("totalConsumos").GetInt32(), ana.GetProperty("usosNoMes").GetInt32(),
            ana.GetProperty("limiteUsosMes").GetDecimal(), ana.GetProperty("planoNome").GetString()));
        Assert.Equal(("67911110000", 150m), (ana.GetProperty("clienteTelefone").GetString(), ana.GetProperty("valor").GetDecimal()));
    }

    [FatoIntegracao]
    public async Task Atualizar_assinatura_troca_plano_e_dia_e_valida()
    {
        var loja = await api.NovaLojaDeServicosAsync();
        var (_, _, assinatura) = await AssinanteAsync(loja);
        var outro = (await NovoPlano(loja, "Outro", 90)).Json.GetProperty("id").GetGuid();

        var r = await loja.Painel.Put($"/api/planos/assinantes/{assinatura}", new { planoId = outro, diaVencimento = 20 });
        Assert.Equal((outro, 20), (r.Json.GetProperty("planoId").GetGuid(), r.Json.GetProperty("diaVencimento").GetInt32()));

        Assert.Equal(20, (await loja.Painel.Put($"/api/planos/assinantes/{assinatura}", new { diaVencimento = 29 })).Json.GetProperty("diaVencimento").GetInt32());   // inválido: mantém
        Assert.Equal("Plano não encontrado.", (await loja.Painel.Put($"/api/planos/assinantes/{assinatura}", new { planoId = Guid.NewGuid() })).Erro);
        Assert.Equal(404, (await loja.Painel.Put($"/api/planos/assinantes/{Guid.NewGuid()}", new { diaVencimento = 5 })).Codigo);
    }

    [FatoIntegracao]
    public async Task Cancelar_assinatura_tira_da_lista_e_libera_novo_vinculo()
    {
        var loja = await api.NovaLojaDeServicosAsync();
        var (plano, cliente, assinatura) = await AssinanteAsync(loja);

        var r = await loja.Painel.Patch($"/api/planos/assinantes/{assinatura}/cancelar");
        Assert.Equal("cancelada", r.Json.GetProperty("status").GetString());
        Assert.Equal(0, (await loja.Painel.Get("/api/planos/assinantes")).Json.GetArrayLength());
        Assert.Equal(404, (await loja.Painel.Put($"/api/planos/assinantes/{assinatura}", new { diaVencimento = 5 })).Codigo);   // só ativa pode ser editada
        Assert.False((await loja.Painel.Get($"/api/planos/cliente/{cliente}")).Json.GetProperty("temPlano").GetBoolean());
        (await loja.Painel.Post("/api/planos/assinantes", new { clienteId = cliente, planoId = plano, diaVencimento = 5 })).DeveSerSucesso();
        Assert.Equal(404, (await loja.Painel.Patch($"/api/planos/assinantes/{Guid.NewGuid()}/cancelar")).Codigo);
    }

    // ── Pagamento da mensalidade ─────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Marcar_pagamento_do_mes_gera_a_venda_e_desmarcar_remove()
    {
        var loja = await api.NovaLojaDeServicosAsync();
        var (_, _, assinatura) = await AssinanteAsync(loja, valor: 150);

        var pago = await loja.Painel.Post($"/api/planos/assinantes/{assinatura}/pagamento", new { pago = true });
        Assert.Equal("pago", pago.Json.GetProperty("status").GetString());
        var pg = await PagamentoNoBanco(pago.Json.GetProperty("id").GetGuid());
        Assert.Equal((MesAtual(), 150m), (pg.MesReferencia, pg.Valor));
        Assert.NotNull(pg.PagoEm);
        var (venda, itens) = await VendaDe(pg.VendaId!.Value);
        Assert.Equal(("pix", 150m, loja.Id), (venda.FormaPagamento, venda.TotalFinal, venda.LojaId));
        var item = Assert.Single(itens);
        Assert.Equal(("Mensalidade - Plano Banho", 150m), (item.NomeProduto, item.Subtotal));

        // marcar de novo não duplica a venda
        await loja.Painel.Post($"/api/planos/assinantes/{assinatura}/pagamento", new { pago = true });
        Assert.Equal(1, await api.NoBancoAsync(db => db.Vendas.CountAsync(v => v.LojaId == loja.Id)));

        var pendente = await loja.Painel.Post($"/api/planos/assinantes/{assinatura}/pagamento", new { pago = false });
        Assert.Equal("pendente", pendente.Json.GetProperty("status").GetString());
        var depois = await PagamentoNoBanco(pg.Id);
        Assert.Equal(("pendente", null), (depois.Status, depois.PagoEm == null ? null : "x"));
        Assert.Null(depois.VendaId);
        Assert.Equal(0, await api.NoBancoAsync(db => db.Vendas.CountAsync(v => v.LojaId == loja.Id)));
        Assert.Equal(404, (await loja.Painel.Post($"/api/planos/assinantes/{Guid.NewGuid()}/pagamento", new { pago = true })).Codigo);
    }

    [FatoIntegracao]
    public async Task Pagar_meses_com_valor_acima_separa_juros_e_abaixo_registra_o_recebido()
    {
        var loja = await api.NovaLojaDeServicosAsync();
        var (_, _, assinatura) = await AssinanteAsync(loja, valor: 150);
        var com = await PendenteAsync(loja, assinatura, 1);
        var abaixo = await PendenteAsync(loja, assinatura, 2);
        var padrao = await PendenteAsync(loja, assinatura, 3);

        var r = await loja.Painel.Post($"/api/planos/assinantes/{assinatura}/pagar", new
        {
            meses = new object[] { new { pagamentoId = com, valorPago = 180 }, new { pagamentoId = abaixo, valorPago = 100 }, new { pagamentoId = padrao, valorPago = (decimal?)null } },
        });

        Assert.Equal((3, 430m), (r.Json.GetProperty("pagos").GetInt32(), r.Json.GetProperty("valorTotal").GetDecimal()));

        var pgCom = await PagamentoNoBanco(com);
        Assert.Equal(("pago", 180m), (pgCom.Status, pgCom.ValorPago));
        var (vendaCom, itensCom) = await VendaDe(pgCom.VendaId!.Value);
        Assert.Equal(180m, vendaCom.TotalFinal);
        Assert.Equal(2, itensCom.Count);
        Assert.Equal((30m, 150m), (itensCom[0].Subtotal, itensCom[1].Subtotal));
        Assert.StartsWith("Juros/multa - Plano Banho (", itensCom[0].NomeProduto);
        Assert.StartsWith("Mensalidade - Plano Banho (", itensCom[1].NomeProduto);

        var (vendaAbaixo, itensAbaixo) = await VendaDe((await PagamentoNoBanco(abaixo)).VendaId!.Value);
        Assert.Equal(100m, vendaAbaixo.TotalFinal);
        Assert.Equal(100m, Assert.Single(itensAbaixo).Subtotal);

        Assert.Equal(150m, (await PagamentoNoBanco(padrao)).ValorPago);
    }

    [FatoIntegracao]
    public async Task Pagar_meses_valida_a_selecao()
    {
        var loja = await api.NovaLojaDeServicosAsync();
        var (_, _, assinatura) = await AssinanteAsync(loja);
        var aberto = await PendenteAsync(loja, assinatura, 1);
        var quitado = await PendenteAsync(loja, assinatura, 2, status: "pago");
        string Url() => $"/api/planos/assinantes/{assinatura}/pagar";

        Assert.Equal("Selecione ao menos um mês para pagar.", (await loja.Painel.Post(Url(), new { meses = Array.Empty<object>() })).Erro);
        Assert.Equal("Algum mês selecionado não foi encontrado.", (await loja.Painel.Post(Url(), new { meses = new[] { new { pagamentoId = Guid.NewGuid(), valorPago = (decimal?)10 } } })).Erro);
        Assert.Equal("Algum mês selecionado já está pago.", (await loja.Painel.Post(Url(), new { meses = new[] { new { pagamentoId = quitado, valorPago = (decimal?)10 } } })).Erro);
        Assert.Equal("Informe um valor pago maior que zero.", (await loja.Painel.Post(Url(), new { meses = new[] { new { pagamentoId = aberto, valorPago = (decimal?)0 } } })).Erro);
        Assert.Equal("pendente", (await PagamentoNoBanco(aberto)).Status);
        Assert.Equal(404, (await loja.Painel.Post($"/api/planos/assinantes/{Guid.NewGuid()}/pagar", new { meses = new[] { new { pagamentoId = aberto, valorPago = (decimal?)10 } } })).Codigo);
    }

    [FatoIntegracao]
    public async Task Quitar_atraso_paga_todos_os_pendentes_ou_avisa_que_nao_ha()
    {
        var loja = await api.NovaLojaDeServicosAsync();
        var (_, _, assinatura) = await AssinanteAsync(loja, valor: 150);
        string Url() => $"/api/planos/assinantes/{assinatura}/quitar-atraso";
        var vazio = await loja.Painel.Post(Url());
        Assert.Equal(400, vazio.Codigo);
        Assert.Equal("Não há meses pendentes para essa assinatura.", vazio.Erro);

        var a = await PendenteAsync(loja, assinatura, 1);
        var b = await PendenteAsync(loja, assinatura, 2);

        var r = await loja.Painel.Post(Url());

        Assert.Equal((2, 300m), (r.Json.GetProperty("quitados").GetInt32(), r.Json.GetProperty("valorTotal").GetDecimal()));
        foreach (var id in new[] { a, b })
        {
            var pg = await PagamentoNoBanco(id);
            Assert.Equal("pago", pg.Status);
            Assert.Equal(150m, (await VendaDe(pg.VendaId!.Value)).Venda.TotalFinal);
        }
        Assert.Equal("Não há meses pendentes para essa assinatura.", (await loja.Painel.Post(Url())).Erro);
        Assert.Equal(404, (await loja.Painel.Post($"/api/planos/assinantes/{Guid.NewGuid()}/quitar-atraso")).Codigo);
    }

    [FatoIntegracao]
    public async Task Historico_vem_do_mes_mais_novo_para_o_mais_antigo_e_consumos_do_mais_recente()
    {
        var loja = await api.NovaLojaDeServicosAsync();
        var (_, _, assinatura) = await AssinanteAsync(loja);
        await PendenteAsync(loja, assinatura, 3);
        await PendenteAsync(loja, assinatura, 1, status: "pago");
        await PendenteAsync(loja, assinatura, 2);
        await api.NoBancoAsync(async db =>
        {
            db.ConsumosPlano.Add(new ConsumoPlano { AssinaturaId = assinatura, LojaId = loja.Id, ServicoId = Guid.NewGuid(), NomeServico = "Antigo", CriadoEm = DateTime.UtcNow.AddDays(-9) });
            db.ConsumosPlano.Add(new ConsumoPlano { AssinaturaId = assinatura, LojaId = loja.Id, ServicoId = Guid.NewGuid(), NomeServico = "Novo", CriadoEm = DateTime.UtcNow.AddDays(-1) });
            await db.SaveChangesAsync();
        });

        var historico = (await loja.Painel.Get($"/api/planos/assinantes/{assinatura}/historico")).Json;
        Assert.Equal(["pago", "pendente", "pendente"], historico.EnumerateArray().Select(p => p.GetProperty("status").GetString()).ToList());
        Assert.Equal(150m, historico[0].GetProperty("valor").GetDecimal());

        var consumos = (await loja.Painel.Get($"/api/planos/assinantes/{assinatura}/consumos")).Json;
        Assert.Equal(["Novo", "Antigo"], consumos.EnumerateArray().Select(c => c.GetProperty("nomeServico").GetString()).ToList());

        Assert.Equal(404, (await loja.Painel.Get($"/api/planos/assinantes/{Guid.NewGuid()}/historico")).Codigo);
        Assert.Equal(404, (await loja.Painel.Get($"/api/planos/assinantes/{Guid.NewGuid()}/consumos")).Codigo);
    }

    // ── Plano do cliente (usado no Caixa) ────────────────────────────────────

    [FatoIntegracao]
    public async Task Plano_do_cliente_sem_assinatura_diz_que_nao_tem()
    {
        var loja = await api.NovaLojaDeServicosAsync();
        var cliente = await loja.ClienteIdAsync();
        var r = await loja.Painel.Get($"/api/planos/cliente/{cliente}");
        Assert.Equal(200, r.Codigo);
        Assert.False(r.Json.GetProperty("temPlano").GetBoolean());
    }

    [FatoIntegracao]
    public async Task Plano_do_cliente_traz_regras_de_uso_na_data_de_referencia()
    {
        var loja = await api.NovaLojaDeServicosAsync();
        var servico = await loja.ServicoIdAsync("Banho", 80, 60);
        var (plano, cliente, assinatura) = await AssinanteAsync(loja, servicos: servico.ToString(), limite: 4, dias: [1]);
        await api.NoBancoAsync(async db =>
        {
            db.ConsumosPlano.Add(new ConsumoPlano { AssinaturaId = assinatura, LojaId = loja.Id, ServicoId = servico, NomeServico = "Banho" });
            await db.SaveChangesAsync();
        });
        var segunda = Datas.Segunda();
        var agendado = await loja.AgendarAsync(servico, segunda.Hora(9), clienteId: cliente);
        agendado.DeveSerSucesso();

        var seg = (await loja.Painel.Get($"/api/planos/cliente/{cliente}?data={Datas.Texto(segunda)}")).Json;
        var sex = (await loja.Painel.Get($"/api/planos/cliente/{cliente}?data={Datas.Texto(Datas.Sexta())}")).Json;

        Assert.True(seg.GetProperty("temPlano").GetBoolean());
        Assert.Equal((plano, assinatura, "Plano Banho"), (seg.GetProperty("planoId").GetGuid(), seg.GetProperty("assinaturaId").GetGuid(), seg.GetProperty("planoNome").GetString()));
        Assert.Equal([servico.ToString()], seg.GetProperty("servicosIncluidos").EnumerateArray().Select(s => s.GetString()).ToList());
        Assert.True(seg.GetProperty("emDia").GetBoolean());
        Assert.Equal("1", seg.GetProperty("diasSemanaPermitidos").GetString());
        Assert.Equal("segunda", seg.GetProperty("diasPermitidosTexto").GetString());
        Assert.True(seg.GetProperty("diaPermitido").GetBoolean());
        Assert.False(sex.GetProperty("diaPermitido").GetBoolean());
        Assert.Equal(4, seg.GetProperty("limiteUsosMes").GetInt32());
        Assert.Equal(1, seg.GetProperty("usosAgendados").GetInt32());   // o agendamento da segunda ainda não foi pago

        // usos consumidos e restantes contam o mês da data de referência (o consumo é de hoje)
        var hoje = (await loja.Painel.Get($"/api/planos/cliente/{cliente}")).Json;
        Assert.Equal((1, 3), (hoje.GetProperty("usosNoMes").GetInt32(), hoje.GetProperty("usosRestantes").GetInt32()));
    }

    [FatoIntegracao]
    public async Task Plano_do_cliente_fica_fora_de_dia_com_mensalidade_atrasada_e_sem_limite_nao_tem_restantes()
    {
        var loja = await api.NovaLojaDeServicosAsync();
        var (_, cliente, assinatura) = await AssinanteAsync(loja);
        Assert.Equal(JsonValueKind.Null, (await loja.Painel.Get($"/api/planos/cliente/{cliente}")).Json.GetProperty("usosRestantes").ValueKind);
        Assert.True((await loja.Painel.Get($"/api/planos/cliente/{cliente}")).Json.GetProperty("emDia").GetBoolean());

        await PendenteAsync(loja, assinatura, 1);

        Assert.False((await loja.Painel.Get($"/api/planos/cliente/{cliente}")).Json.GetProperty("emDia").GetBoolean());
    }

    // ── Isolamento entre lojas ───────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Uma_loja_nao_ve_nem_mexe_nos_planos_e_assinantes_de_outra()
    {
        var a = await api.NovaLojaDeServicosAsync();
        var b = await api.NovaLojaDeServicosAsync();
        var (planoA, clienteA, assinaturaA) = await AssinanteAsync(a);
        var pgA = await PendenteAsync(a, assinaturaA, 1);
        var clienteB = await b.ClienteIdAsync();
        var planoB = (await NovoPlano(b)).Json.GetProperty("id").GetGuid();

        Assert.Equal(1, (await b.Painel.Get("/api/planos")).Json.GetArrayLength());
        Assert.Equal(0, (await b.Painel.Get("/api/planos/assinantes")).Json.GetArrayLength());
        Assert.Equal(404, (await b.Painel.Put($"/api/planos/{planoA}", new { nome = "x", valor = 1 })).Codigo);
        Assert.Equal(404, (await b.Painel.Patch($"/api/planos/{planoA}/ativo")).Codigo);
        Assert.Equal(404, (await b.Painel.Delete($"/api/planos/{planoA}")).Codigo);
        Assert.Equal(404, (await b.Painel.Put($"/api/planos/assinantes/{assinaturaA}", new { diaVencimento = 5 })).Codigo);
        Assert.Equal(404, (await b.Painel.Patch($"/api/planos/assinantes/{assinaturaA}/cancelar")).Codigo);
        Assert.Equal(404, (await b.Painel.Post($"/api/planos/assinantes/{assinaturaA}/pagamento", new { pago = true })).Codigo);
        Assert.Equal(404, (await b.Painel.Post($"/api/planos/assinantes/{assinaturaA}/quitar-atraso")).Codigo);
        Assert.Equal(404, (await b.Painel.Post($"/api/planos/assinantes/{assinaturaA}/pagar", new { meses = new[] { new { pagamentoId = pgA, valorPago = (decimal?)10 } } })).Codigo);
        Assert.Equal(404, (await b.Painel.Get($"/api/planos/assinantes/{assinaturaA}/historico")).Codigo);
        Assert.Equal(404, (await b.Painel.Get($"/api/planos/assinantes/{assinaturaA}/consumos")).Codigo);
        Assert.False((await b.Painel.Get($"/api/planos/cliente/{clienteA}")).Json.GetProperty("temPlano").GetBoolean());
        Assert.Equal("Plano não encontrado.", (await b.Painel.Post("/api/planos/assinantes", new { clienteId = clienteB, planoId = planoA, diaVencimento = 5 })).Erro);
        Assert.Equal("Cliente não encontrado.", (await b.Painel.Post("/api/planos/assinantes", new { clienteId = clienteA, planoId = planoB, diaVencimento = 5 })).Erro);
        Assert.Equal("pendente", (await PagamentoNoBanco(pgA)).Status);
    }
}
