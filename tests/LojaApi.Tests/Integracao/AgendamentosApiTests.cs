using LojaApi.Models;
using LojaApi.src.Models.Funcionarios;
using LojaApi.Tests.Chacara.Dominio;
using Microsoft.EntityFrameworkCore;

namespace LojaApi.Tests.Integracao;

// Agenda da loja (painel): criar, editar, listar, mudar status, comissão do profissional e regras do plano de assinatura.
[Collection(ColecaoApi.Nome)]
public class AgendamentosApiTests(ApiFixture api)
{
    private static string Seg(int h, int min = 0) => Datas.Segunda().Hora(h, min);

    // ── Criar ────────────────────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Exige_login()
    {
        var loja = await api.NovaLojaDeServicosAsync();
        Assert.Equal(401, (await loja.Site.Get("/api/agendamentos")).Codigo);
    }

    [FatoIntegracao]
    public async Task Criar_sem_preco_e_duracao_usa_os_do_servico()
    {
        var loja = await api.NovaLojaDeServicosAsync();
        var servico = await loja.ServicoIdAsync("Banho", 80, 45);

        var r = await loja.AgendarAsync(servico, Seg(9), nomeCliente: "Cliente avulso");

        Assert.Equal(200, r.Codigo);
        Assert.Equal(80m, r.Json.GetProperty("preco").GetDecimal());
        Assert.Equal(45, r.Json.GetProperty("duracaoMin").GetInt32());
        Assert.Equal("agendado", r.Json.GetProperty("status").GetString());
        Assert.False(r.Json.GetProperty("pago").GetBoolean());
        Assert.Equal("Banho", r.Json.GetProperty("nomeServico").GetString());
        Assert.Equal("Cliente avulso", r.Json.GetProperty("nomeCliente").GetString());
    }

    [FatoIntegracao]
    public async Task Criar_com_preco_e_duracao_informados_usa_os_informados()
    {
        var loja = await api.NovaLojaDeServicosAsync();
        var servico = await loja.ServicoIdAsync("Banho", 80, 45);

        var r = await loja.AgendarAsync(servico, Seg(9), preco: 100, duracaoMin: 90);

        Assert.Equal(100m, r.Json.GetProperty("preco").GetDecimal());
        Assert.Equal(90, r.Json.GetProperty("duracaoMin").GetInt32());
    }

    [FatoIntegracao]
    public async Task Criar_com_cliente_cadastrado_usa_o_nome_do_cadastro()
    {
        var loja = await api.NovaLojaDeServicosAsync();
        var servico = await loja.ServicoIdAsync();
        var cliente = await loja.ClienteIdAsync("Maria Cadastrada");

        var r = await loja.AgendarAsync(servico, Seg(9), clienteId: cliente, nomeCliente: "Outro nome digitado");

        Assert.Equal("Maria Cadastrada", r.Json.GetProperty("nomeCliente").GetString());
        Assert.Equal(cliente, r.Json.GetProperty("clienteId").GetGuid());
    }

    [FatoIntegracao]
    public async Task Criar_com_servico_inexistente_e_recusado()
    {
        var loja = await api.NovaLojaDeServicosAsync();
        var r = await loja.AgendarAsync(Guid.NewGuid(), Seg(9));
        Assert.Equal(400, r.Codigo);
        Assert.Equal("Serviço não encontrado.", r.Erro);
    }

    // ── Listar ───────────────────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Listar_por_dia_por_periodo_e_tudo_em_ordem_de_horario()
    {
        var loja = await api.NovaLojaDeServicosAsync();
        var servico = await loja.ServicoIdAsync();
        await loja.AgendarAsync(servico, Datas.Segunda().Hora(15), nomeCliente: "Tarde");
        await loja.AgendarAsync(servico, Datas.Segunda().Hora(9), nomeCliente: "Manhã");
        await loja.AgendarAsync(servico, Datas.Segunda().AddDays(2).Hora(10), nomeCliente: "Quarta");

        async Task<List<string?>> Nomes(string query) =>
            (await loja.Painel.Get("/api/agendamentos" + query)).Json.EnumerateArray().Select(a => a.GetProperty("nomeCliente").GetString()).ToList();

        Assert.Equal(["Manhã", "Tarde"], await Nomes($"?data={Datas.Texto(Datas.Segunda())}"));
        Assert.Equal(["Manhã", "Tarde", "Quarta"], await Nomes($"?de={Datas.Texto(Datas.Segunda())}&ate={Datas.Texto(Datas.Segunda().AddDays(2))}"));
        Assert.Equal(["Quarta"], await Nomes($"?de={Datas.Texto(Datas.Segunda().AddDays(2))}&ate={Datas.Texto(Datas.Segunda().AddDays(2))}"));
        Assert.Equal(["Manhã", "Tarde", "Quarta"], await Nomes(""));
    }

    [FatoIntegracao]
    public async Task Listar_mostra_o_nome_do_profissional()
    {
        var loja = await api.NovaLojaDeServicosAsync();
        var servico = await loja.ServicoIdAsync();
        var profissional = await api.ProfissionalAsync(loja, nome: "Joana");
        await loja.AgendarAsync(servico, Seg(9), profissionalId: profissional);

        var a = (await loja.Painel.Get("/api/agendamentos")).Json[0];
        Assert.Equal("Joana", a.GetProperty("nomeProfissional").GetString());
        Assert.Equal(profissional, a.GetProperty("profissionalId").GetGuid());
    }

    // ── Editar ───────────────────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Atualizar_troca_os_dados_e_valida_servico_e_existencia()
    {
        var loja = await api.NovaLojaDeServicosAsync();
        var banho = await loja.ServicoIdAsync("Banho", 80, 60);
        var tosa = await loja.ServicoIdAsync("Tosa", 50, 30);
        var id = (await loja.AgendarAsync(banho, Seg(9), nomeCliente: "Ana")).Id2();

        var r = await loja.Painel.Put($"/api/agendamentos/{id}", Agendamento(tosa, Seg(14), nomeCliente: "Ana Maria", observacao: "alérgico"));

        Assert.Equal(200, r.Codigo);
        Assert.Equal("Tosa", r.Json.GetProperty("nomeServico").GetString());
        Assert.Equal(50m, r.Json.GetProperty("preco").GetDecimal());       // preço do novo serviço
        Assert.Equal(30, r.Json.GetProperty("duracaoMin").GetInt32());
        Assert.Equal("Ana Maria", r.Json.GetProperty("nomeCliente").GetString());
        Assert.Equal("agendado", r.Json.GetProperty("status").GetString()); // o status não muda na edição

        Assert.Equal("Serviço não encontrado.", (await loja.Painel.Put($"/api/agendamentos/{id}", Agendamento(Guid.NewGuid(), Seg(14)))).Erro);
        Assert.Equal(404, (await loja.Painel.Put($"/api/agendamentos/{Guid.NewGuid()}", Agendamento(banho, Seg(14)))).Codigo);
    }

    private static object Agendamento(Guid servicoId, string dataHora, string? nomeCliente = null, string? observacao = null) =>
        CenariosServicos.Agendamento(servicoId, dataHora, nomeCliente: nomeCliente, observacao: observacao);

    // ── Status ───────────────────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Mudar_status_valida_o_valor_e_aceita_concluir_cancelar_e_reabrir()
    {
        var loja = await api.NovaLojaDeServicosAsync();
        var id = (await loja.AgendarAsync(await loja.ServicoIdAsync(), Seg(9))).Id2();

        Assert.Equal("Status inválido.", (await loja.Painel.Patch($"/api/agendamentos/{id}/status", new { status = "voando" })).Erro);

        foreach (var status in new[] { "concluido", "cancelado", "agendado" })
        {
            var r = await loja.Painel.Patch($"/api/agendamentos/{id}/status", new { status });
            Assert.Equal(200, r.Codigo);
            Assert.Equal(status, r.Json.GetProperty("status").GetString());
        }
        Assert.Equal(404, (await loja.Painel.Patch($"/api/agendamentos/{Guid.NewGuid()}/status", new { status = "concluido" })).Codigo);
    }

    [FatoIntegracao]
    public async Task Concluido_e_nao_pago_aparece_nos_pendentes_de_pagamento()
    {
        var loja = await api.NovaLojaDeServicosAsync();
        var servico = await loja.ServicoIdAsync("Banho", 80, 60);
        var id = (await loja.AgendarAsync(servico, Seg(9), nomeCliente: "Ana")).Id2();
        Assert.Equal(0, (await loja.Painel.Get("/api/agendamentos/pendentes-pagamento")).Json.GetArrayLength());   // ainda "agendado"

        await loja.Painel.Patch($"/api/agendamentos/{id}/status", new { status = "concluido" });
        var lista = (await loja.Painel.Get("/api/agendamentos/pendentes-pagamento")).Json;
        Assert.Equal(id, lista[0].GetProperty("id").GetGuid());
        Assert.Equal(80m, lista[0].GetProperty("preco").GetDecimal());

        await api.MarcarComoPagoAsync(id);
        Assert.Equal(0, (await loja.Painel.Get("/api/agendamentos/pendentes-pagamento")).Json.GetArrayLength());
    }

    [FatoIntegracao]
    public async Task Agendamento_pago_nao_pode_ser_cancelado_nem_excluido()
    {
        var loja = await api.NovaLojaDeServicosAsync();
        var id = (await loja.AgendarAsync(await loja.ServicoIdAsync(), Seg(9))).Id2();
        await api.MarcarComoPagoAsync(id);

        var cancelar = await loja.Painel.Patch($"/api/agendamentos/{id}/status", new { status = "cancelado" });
        Assert.Equal(400, cancelar.Codigo);
        Assert.Equal("Não é possível cancelar: este agendamento já foi pago. Para estornar, faça o processo pelo caixa/vendas.", cancelar.Erro);

        var excluir = await loja.Painel.Delete($"/api/agendamentos/{id}");
        Assert.Equal(400, excluir.Codigo);
        Assert.Equal("Não é possível excluir: este agendamento já foi pago e gerou uma venda. Você pode cancelá-lo em vez de excluir.", excluir.Erro);
        Assert.Equal("agendado", (await api.AgendamentoNoBancoAsync(id)).Status);
    }

    [FatoIntegracao]
    public async Task Excluir_remove_e_devolve_404_na_segunda_vez()
    {
        var loja = await api.NovaLojaDeServicosAsync();
        var id = (await loja.AgendarAsync(await loja.ServicoIdAsync(), Seg(9))).Id2();

        Assert.Equal("Agendamento excluído.", (await loja.Painel.Delete($"/api/agendamentos/{id}")).Json.GetProperty("mensagem").GetString());
        Assert.Equal(404, (await loja.Painel.Delete($"/api/agendamentos/{id}")).Codigo);
    }

    // ── Histórico e resumo por cliente ───────────────────────────────────────

    [FatoIntegracao]
    public async Task Historico_do_cliente_vem_do_mais_novo_para_o_mais_antigo_e_resumo_ignora_cancelados()
    {
        var loja = await api.NovaLojaDeServicosAsync();
        var servico = await loja.ServicoIdAsync("Banho", 80, 60);
        var ana = await loja.ClienteIdAsync("Ana", "67911110000");
        var bia = await loja.ClienteIdAsync("Bia", "67922220000");
        await loja.AgendarAsync(servico, Datas.Segunda().Hora(9), clienteId: ana);
        var segundo = (await loja.AgendarAsync(servico, Datas.Segunda().AddDays(1).Hora(9), clienteId: ana)).Id2();
        await loja.AgendarAsync(servico, Datas.Segunda().Hora(11), clienteId: bia);
        await loja.Painel.Patch($"/api/agendamentos/{segundo}/status", new { status = "cancelado" });

        var historico = (await loja.Painel.Get($"/api/agendamentos/cliente/{ana}")).Json;
        Assert.Equal(2, historico.GetArrayLength());
        Assert.Equal(segundo, historico[0].GetProperty("id").GetGuid());       // o mais novo primeiro

        var resumo = (await loja.Painel.Get("/api/agendamentos/resumo-clientes")).Json.EnumerateArray()
            .ToDictionary(x => x.GetProperty("clienteId").GetGuid(), x => (x.GetProperty("qtd").GetInt32(), x.GetProperty("total").GetDecimal()));
        Assert.Equal((1, 80m), resumo[ana]);   // o cancelado não conta
        Assert.Equal((1, 80m), resumo[bia]);
    }

    // ── Comissão do profissional ─────────────────────────────────────────────

    private Task<List<ComissaoFuncionario>> ComissoesDe(Guid agendamentoId) =>
        api.NoBancoAsync(db => db.ComissoesFuncionario.AsNoTracking().Where(c => c.OrigemId == agendamentoId).ToListAsync());

    [FatoIntegracao]
    public async Task Concluir_gera_a_comissao_padrao_do_profissional_uma_unica_vez()
    {
        var loja = await api.NovaLojaDeServicosAsync();
        var servico = await loja.ServicoIdAsync("Banho", 80, 60);
        var profissional = await api.ProfissionalAsync(loja, comissaoPadrao: 10m);
        var id = (await loja.AgendarAsync(servico, Seg(9), profissionalId: profissional)).Id2();

        await loja.Painel.Patch($"/api/agendamentos/{id}/status", new { status = "concluido" });

        var c = Assert.Single(await ComissoesDe(id));
        Assert.Equal(("agendamento", 80m, 10m, 8m), (c.OrigemTipo, c.ValorServico, c.ComissaoPercentual, c.ValorComissao));
        Assert.Equal(profissional, c.ProfissionalId);
        Assert.Equal(loja.Id, c.LojaId);

        // reabrir e concluir de novo não duplica
        await loja.Painel.Patch($"/api/agendamentos/{id}/status", new { status = "agendado" });
        await loja.Painel.Patch($"/api/agendamentos/{id}/status", new { status = "concluido" });
        Assert.Single(await ComissoesDe(id));
    }

    [FatoIntegracao]
    public async Task Comissao_especifica_do_servico_vale_mais_que_a_padrao()
    {
        var loja = await api.NovaLojaDeServicosAsync();
        var servico = await loja.ServicoIdAsync("Banho", 200, 60);
        var profissional = await api.ProfissionalAsync(loja, comissaoPadrao: 10m);
        await api.NoBancoAsync(async db =>
        {
            db.ComissoesServicoProfissional.Add(new ComissaoServicoProfissional { ProfissionalId = profissional, ServicoId = servico, ComissaoPercentual = 25m });
            await db.SaveChangesAsync();
        });
        var id = (await loja.AgendarAsync(servico, Seg(9), profissionalId: profissional)).Id2();

        await loja.Painel.Patch($"/api/agendamentos/{id}/status", new { status = "concluido" });

        var c = Assert.Single(await ComissoesDe(id));
        Assert.Equal((25m, 50m), (c.ComissaoPercentual, c.ValorComissao));
    }

    [FatoIntegracao]
    public async Task Sem_percentual_ou_sem_profissional_nao_gera_comissao()
    {
        var loja = await api.NovaLojaDeServicosAsync();
        var servico = await loja.ServicoIdAsync();
        var semPercentual = await api.ProfissionalAsync(loja, comissaoPadrao: null);
        var a = (await loja.AgendarAsync(servico, Seg(9), profissionalId: semPercentual)).Id2();
        var b = (await loja.AgendarAsync(servico, Seg(11))).Id2();

        await loja.Painel.Patch($"/api/agendamentos/{a}/status", new { status = "concluido" });
        await loja.Painel.Patch($"/api/agendamentos/{b}/status", new { status = "concluido" });

        Assert.Empty(await ComissoesDe(a));
        Assert.Empty(await ComissoesDe(b));
    }

    // ── Regras do plano de assinatura ao agendar ─────────────────────────────

    private async Task<(LojaTeste Loja, Guid Servico, Guid Cliente, Guid Assinatura)> LojaComPlanoAsync(int? limiteUsos, int[]? dias)
    {
        var loja = await api.NovaLojaDeServicosAsync();
        var servico = await loja.ServicoIdAsync("Banho", 80, 60);
        var cliente = await loja.ClienteIdAsync("Assinante");
        var plano = await loja.Painel.Post("/api/planos", new { nome = "Plano Banho", valor = 150, servicosIds = servico.ToString(), limiteUsosMes = limiteUsos, diasSemana = dias });
        plano.DeveSerSucesso();
        var assinatura = await loja.Painel.Post("/api/planos/assinantes", new { clienteId = cliente, planoId = plano.Id2(), diaVencimento = 10, iniciarProximoMes = false });
        assinatura.DeveSerSucesso();
        return (loja, servico, cliente, assinatura.Id2());
    }

    [FatoIntegracao]
    public async Task Plano_com_dias_restritos_recusa_agendar_em_dia_nao_permitido()
    {
        var (loja, servico, cliente, _) = await LojaComPlanoAsync(limiteUsos: null, dias: [1]);   // só segundas

        var sexta = await loja.AgendarAsync(servico, Datas.Sexta().Hora(9), clienteId: cliente);
        Assert.Equal(400, sexta.Codigo);
        Assert.Equal("O plano 'Plano Banho' só pode ser usado em: segunda. Escolha outro dia.", sexta.Erro);

        Assert.Equal(200, (await loja.AgendarAsync(servico, Seg(9), clienteId: cliente)).Codigo);
    }

    [FatoIntegracao]
    public async Task Plano_com_limite_mensal_conta_o_que_ja_esta_agendado()
    {
        var (loja, servico, cliente, _) = await LojaComPlanoAsync(limiteUsos: 1, dias: null);
        var primeiro = (await loja.AgendarAsync(servico, Seg(9), clienteId: cliente)).Id2();

        var segundo = await loja.AgendarAsync(servico, Seg(11), clienteId: cliente);

        Assert.Equal(400, segundo.Codigo);
        Assert.Equal("O plano 'Plano Banho' permite 1 uso(s) por mês e já há 1 usado(s) ou agendado(s) neste mês.", segundo.Erro);

        // editar o próprio agendamento não conta contra si mesmo
        Assert.Equal(200, (await loja.Painel.Put($"/api/agendamentos/{primeiro}", CenariosServicos.Agendamento(servico, Seg(10), clienteId: cliente))).Codigo);

        // cancelado libera a vaga
        await loja.Painel.Patch($"/api/agendamentos/{primeiro}/status", new { status = "cancelado" });
        Assert.Equal(200, (await loja.AgendarAsync(servico, Seg(11), clienteId: cliente)).Codigo);
    }

    [FatoIntegracao]
    public async Task Plano_com_limite_nao_conta_agendamento_ja_pago()
    {
        var (loja, servico, cliente, _) = await LojaComPlanoAsync(limiteUsos: 1, dias: null);
        var primeiro = (await loja.AgendarAsync(servico, Seg(9), clienteId: cliente)).Id2();
        await api.MarcarComoPagoAsync(primeiro);   // já foi cobrado no caixa: não ocupa mais a cota

        Assert.Equal(200, (await loja.AgendarAsync(servico, Seg(11), clienteId: cliente)).Codigo);
    }

    [FatoIntegracao]
    public async Task Plano_com_limite_conta_os_usos_ja_consumidos_so_do_mes_do_agendamento()
    {
        var (loja, servico, cliente, assinatura) = await LojaComPlanoAsync(limiteUsos: 1, dias: null);
        var diaDoUso = Datas.Segunda();
        await api.NoBancoAsync(async db =>
        {
            db.ConsumosPlano.Add(new ConsumoPlano { AssinaturaId = assinatura, LojaId = loja.Id, ServicoId = servico, NomeServico = "Banho", CriadoEm = DateTime.SpecifyKind(diaDoUso.AddHours(15), DateTimeKind.Utc) });
            await db.SaveChangesAsync();
        });

        var estourou = await loja.AgendarAsync(servico, diaDoUso.Hora(9), clienteId: cliente);
        Assert.Equal(400, estourou.Codigo);
        Assert.Equal("O plano 'Plano Banho' permite 1 uso(s) por mês e já há 1 usado(s) ou agendado(s) neste mês.", estourou.Erro);

        // o consumo é deste mês: em outro mês a cota está livre
        Assert.Equal(200, (await loja.AgendarAsync(servico, diaDoUso.AddMonths(2).Hora(9), clienteId: cliente)).Codigo);
    }

    [FatoIntegracao]
    public async Task Atualizar_troca_o_profissional_e_a_resposta_nao_traz_pago_nem_profissional()
    {
        var loja = await api.NovaLojaDeServicosAsync();
        var servico = await loja.ServicoIdAsync();
        var joana = await api.ProfissionalAsync(loja, nome: "Joana");
        var id = (await loja.AgendarAsync(servico, Seg(9))).Id2();

        var r = await loja.Painel.Put($"/api/agendamentos/{id}", CenariosServicos.Agendamento(servico, Seg(9), profissionalId: joana));

        Assert.Equal(200, r.Codigo);
        Assert.Equal(joana, (await api.AgendamentoNoBancoAsync(id)).ProfissionalId);
        Assert.False(r.Json.TryGetProperty("pago", out _));             // contrato atual do PUT: resposta enxuta
        Assert.False(r.Json.TryGetProperty("profissionalId", out _));
        Assert.True(r.Json.TryGetProperty("observacao", out _));
    }

    [FatoIntegracao]
    public async Task Mensalidade_em_atraso_faz_o_plano_deixar_de_restringir()
    {
        var (loja, servico, cliente, assinatura) = await LojaComPlanoAsync(limiteUsos: 1, dias: [1]);
        var mesPassado = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-1);
        await api.NoBancoAsync(async db =>
        {
            db.PagamentosPlano.Add(new PagamentoPlano { AssinaturaId = assinatura, LojaId = loja.Id, MesReferencia = mesPassado, Valor = 150, Status = "pendente" });
            await db.SaveChangesAsync();
        });

        // sexta-feira e já usado o limite: nada disso vale, porque a mensalidade está atrasada (o serviço é cobrado normalmente)
        Assert.Equal(200, (await loja.AgendarAsync(servico, Datas.Sexta().Hora(9), clienteId: cliente)).Codigo);
        Assert.Equal(200, (await loja.AgendarAsync(servico, Datas.Sexta().Hora(11), clienteId: cliente)).Codigo);
    }

    [FatoIntegracao]
    public async Task Plano_so_restringe_os_servicos_que_inclui()
    {
        var (loja, _, cliente, _) = await LojaComPlanoAsync(limiteUsos: 1, dias: [1]);
        var outro = await loja.ServicoIdAsync("Tosa", 50, 30);

        Assert.Equal(200, (await loja.AgendarAsync(outro, Datas.Sexta().Hora(9), clienteId: cliente)).Codigo);
    }

    // ── Isolamento entre lojas ───────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Uma_loja_nao_ve_nem_altera_agendamentos_de_outra()
    {
        var a = await api.NovaLojaDeServicosAsync();
        var b = await api.NovaLojaDeServicosAsync();
        var servicoA = await a.ServicoIdAsync();
        var idA = (await a.AgendarAsync(servicoA, Seg(9), nomeCliente: "Cliente da A")).Id2();
        var servicoB = await b.ServicoIdAsync();

        Assert.Equal(0, (await b.Painel.Get("/api/agendamentos")).Json.GetArrayLength());
        Assert.Equal(404, (await b.Painel.Put($"/api/agendamentos/{idA}", CenariosServicos.Agendamento(servicoB, Seg(9)))).Codigo);
        Assert.Equal(404, (await b.Painel.Patch($"/api/agendamentos/{idA}/status", new { status = "cancelado" })).Codigo);
        Assert.Equal(404, (await b.Painel.Delete($"/api/agendamentos/{idA}")).Codigo);

        // nem usar o serviço de outra loja
        Assert.Equal("Serviço não encontrado.", (await b.AgendarAsync(servicoA, Seg(9))).Erro);
        Assert.Equal("agendado", (await api.AgendamentoNoBancoAsync(idA)).Status);
    }

    // Correção desta migração: antes, cliente e profissional de OUTRA loja eram aceitos no agendamento
    // (o nome do cliente alheio ia parar na agenda) e a comissão buscava o profissional sem filtrar a loja.

    [FatoIntegracao]
    public async Task Nao_aceita_cliente_de_outra_loja_ao_criar_nem_ao_editar()
    {
        var a = await api.NovaLojaDeServicosAsync();
        var b = await api.NovaLojaDeServicosAsync();
        var clienteDeA = await a.ClienteIdAsync("Cliente Secreto da A");
        var servicoB = await b.ServicoIdAsync();

        var criar = await b.AgendarAsync(servicoB, Seg(9), clienteId: clienteDeA, nomeCliente: "Digitado");
        Assert.Equal((400, "Cliente não encontrado."), (criar.Codigo, criar.Erro));
        Assert.Equal(0, (await b.Painel.Get("/api/agendamentos")).Json.GetArrayLength());

        var proprio = (await b.AgendarAsync(servicoB, Seg(9), nomeCliente: "Avulso")).Id2();
        var editar = await b.Painel.Put($"/api/agendamentos/{proprio}", CenariosServicos.Agendamento(servicoB, Seg(9), clienteId: clienteDeA));
        Assert.Equal((400, "Cliente não encontrado."), (editar.Codigo, editar.Erro));
        var noBanco = await api.AgendamentoNoBancoAsync(proprio);
        Assert.Equal(("Avulso", null), (noBanco.NomeCliente, noBanco.ClienteId?.ToString()));   // nada do cliente da A vazou
    }

    [FatoIntegracao]
    public async Task Nao_aceita_profissional_de_outra_loja_nem_inexistente()
    {
        var a = await api.NovaLojaDeServicosAsync();
        var b = await api.NovaLojaDeServicosAsync();
        var profissionalDeA = await api.ProfissionalAsync(a);
        var servicoB = await b.ServicoIdAsync();

        foreach (var estranho in new[] { profissionalDeA, Guid.NewGuid() })
        {
            var r = await b.AgendarAsync(servicoB, Seg(9), profissionalId: estranho);
            Assert.Equal((400, "Profissional não encontrado."), (r.Codigo, r.Erro));
        }
        Assert.Equal(0, (await b.Painel.Get("/api/agendamentos")).Json.GetArrayLength());

        var proprio = (await b.AgendarAsync(servicoB, Seg(9))).Id2();
        Assert.Equal("Profissional não encontrado.", (await b.Painel.Put($"/api/agendamentos/{proprio}", CenariosServicos.Agendamento(servicoB, Seg(9), profissionalId: profissionalDeA))).Erro);
        Assert.Null((await api.AgendamentoNoBancoAsync(proprio)).ProfissionalId);
    }

    [FatoIntegracao]
    public async Task Comissao_nao_e_gerada_para_profissional_de_outra_loja()
    {
        var a = await api.NovaLojaDeServicosAsync();
        var b = await api.NovaLojaDeServicosAsync();
        var profissionalDeA = await api.ProfissionalAsync(a, comissaoPadrao: 50m);
        var id = (await b.AgendarAsync(await b.ServicoIdAsync(), Seg(9))).Id2();
        // dado inconsistente que já exista no banco: agendamento da B apontando para o profissional da A
        await api.NoBancoAsync(db => db.Agendamentos.Where(x => x.Id == id).ExecuteUpdateAsync(s => s.SetProperty(x => x.ProfissionalId, profissionalDeA)));

        var r = await b.Painel.Patch($"/api/agendamentos/{id}/status", new { status = "concluido" });

        Assert.Equal(200, r.Codigo);
        Assert.Empty(await ComissoesDe(id));
        Assert.Equal(0, await api.NoBancoAsync(db => db.ComissoesFuncionario.CountAsync(c => c.LojaId == b.Id)));
    }
}

internal static class RespostaExtensoes
{
    // O id volta como "id" em todas as respostas de criação
    public static Guid Id2(this Resposta r) => r.Json.GetProperty("id").GetGuid();
}
