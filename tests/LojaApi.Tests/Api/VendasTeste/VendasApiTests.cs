using System.Text.Json;
using LojaApi.Models;
using LojaApi.src.Models.Fiscal;
using LojaApi.src.Models.Funcionarios;
using Microsoft.EntityFrameworkCore;

namespace LojaApi.Tests.Api.VendasTeste;

// Vendas do caixa: registrar (produto, variação, serviço, plano, carteira, crédito, comissão), listar, buscar e excluir com estorno.
[Collection(ColecaoApi.Nome)]
public class VendasApiTests(ApiFixture api)
{
    private Task<LojaTeste> NovaAsync() => api.NovaLojaDeServicosAsync();

    private static Guid Id(JsonElement e) => e.GetProperty("id").GetGuid();

    // ── Permissões ───────────────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Exige_login_e_so_admin_exclui()
    {
        var loja = await NovaAsync();
        var produto = await api.ProdutoAsync(loja.Id);
        var venda = await loja.VenderAsync([CenariosVendas.ItemProduto(produto)]);

        Assert.Equal(401, (await loja.Site.Get("/api/vendas")).Codigo);
        Assert.Equal(401, (await loja.Site.Get($"/api/vendas/{venda}")).Codigo);
        Assert.Equal(401, (await loja.Site.Post("/api/vendas", new { })).Codigo);
        Assert.Equal(401, (await loja.Site.Delete($"/api/vendas/{venda}")).Codigo);

        var operador = await api.OperadorAsync(loja);
        Assert.Equal(200, (await operador.Get("/api/vendas")).Codigo);
        Assert.Equal(403, (await operador.Delete($"/api/vendas/{venda}")).Codigo);
    }

    // ── Registrar: validações ────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Registrar_valida_itens_data_produto_estoque_e_funcionario_sem_gravar_nada()
    {
        var loja = await NovaAsync();
        var produto = await api.ProdutoAsync(loja.Id, "Blusa", estoque: 2);
        var inativo = await api.ProdutoAsync(loja.Id, "Velha", ativo: false);
        var comGrade = await api.ProdutoAsync(loja.Id, "Tênis", estoque: 0);
        var variacao = await api.VariacaoAsync(comGrade, "42", "Preto", estoque: 1);

        async Task<(int, string)> Tenta(object corpo) { var r = await loja.Painel.Post("/api/vendas", corpo); return (r.Codigo, r.Erro); }

        Assert.Equal((400, "A venda deve ter ao menos um item."), await Tenta(CenariosVendas.CorpoVenda([])));
        Assert.Equal((400, "A data da venda não pode ser no futuro."), await Tenta(CenariosVendas.CorpoVenda([CenariosVendas.ItemProduto(produto)], data: DateTime.UtcNow.AddDays(2))));
        Assert.Equal((400, "Item sem produto nem serviço."), await Tenta(CenariosVendas.CorpoVenda([CenariosVendas.ItemSemProdutoNemServico()])));
        var inexistente = Guid.NewGuid();
        Assert.Equal((400, $"Produto {inexistente} não encontrado."), await Tenta(CenariosVendas.CorpoVenda([CenariosVendas.ItemProduto(inexistente)])));
        Assert.Equal((400, "Produto 'Velha' está inativo."), await Tenta(CenariosVendas.CorpoVenda([CenariosVendas.ItemProduto(inativo)])));
        Assert.Equal((400, "Estoque insuficiente para 'Blusa'."), await Tenta(CenariosVendas.CorpoVenda([CenariosVendas.ItemProduto(produto, 3)])));
        Assert.Equal((400, "Estoque insuficiente para 'Tênis' (42/Preto)."), await Tenta(CenariosVendas.CorpoVenda([CenariosVendas.ItemProduto(comGrade, 2, variacao: variacao)])));
        Assert.Equal((400, "Variação não encontrada."), await Tenta(CenariosVendas.CorpoVenda([CenariosVendas.ItemProduto(comGrade, 1, variacao: Guid.NewGuid())])));
        Assert.Equal((400, "Serviço não encontrado."), await Tenta(CenariosVendas.CorpoVenda([CenariosVendas.ItemServico(Guid.NewGuid())])));
        Assert.Equal((400, "Funcionário não encontrado."), await Tenta(CenariosVendas.CorpoVenda([CenariosVendas.ItemProduto(produto)], funcionario: Guid.NewGuid())));

        Assert.Equal(0, (await loja.Painel.Get("/api/vendas")).Json.GetArrayLength());
        Assert.Equal(2m, (await api.ProdutoDaLojaNoBancoAsync(produto)).Estoque);
        Assert.Empty(await api.MovimentosDoProdutoAsync(produto));
    }

    // ── Registrar: produto ───────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Registrar_venda_de_produto_calcula_o_total_baixa_o_estoque_e_devolve_201_com_a_venda()
    {
        var loja = await NovaAsync();
        var cliente = await loja.ClienteIdAsync("Dona Rosa");
        var camiseta = await api.ProdutoAsync(loja.Id, "Camiseta", estoque: 10);
        var meia = await api.ProdutoAsync(loja.Id, "Meia", estoque: 4);

        var r = await loja.Painel.Post("/api/vendas", CenariosVendas.CorpoVenda(
            [CenariosVendas.ItemProduto(camiseta, 2, 50m), CenariosVendas.ItemProduto(meia, 1, 15.5m)], cliente, desconto: 5m, forma: "dinheiro", troco: 10m));

        Assert.Equal(201, r.Codigo);
        var j = r.Json;
        var id = Id(j);
        Assert.EndsWith($"/api/vendas/{id}", r.Local);
        Assert.Equal((115.5m, 5m, 110.5m, "dinheiro", 10m, "Dona Rosa", cliente), (j.GetProperty("total").GetDecimal(), j.GetProperty("desconto").GetDecimal(), j.GetProperty("totalFinal").GetDecimal(),
            j.GetProperty("formaPagamento").GetString(), j.GetProperty("troco").GetDecimal(), j.GetProperty("nomeCliente").GetString(), j.GetProperty("clienteId").GetGuid()));
        var itens = j.GetProperty("itens").EnumerateArray().OrderBy(i => i.GetProperty("nomeProduto").GetString()).ToList();
        Assert.Equal([("Camiseta", 2m, 50m, 100m), ("Meia", 1m, 15.5m, 15.5m)],
            itens.Select(i => (i.GetProperty("nomeProduto").GetString()!, i.GetProperty("quantidade").GetDecimal(), i.GetProperty("precoUnitario").GetDecimal(), i.GetProperty("subtotal").GetDecimal())).ToList());

        Assert.Equal((8m, 3m), ((await api.ProdutoDaLojaNoBancoAsync(camiseta)).Estoque, (await api.ProdutoDaLojaNoBancoAsync(meia)).Estoque));
        var saida = Assert.Single(await api.MovimentosDoProdutoAsync(camiseta));
        Assert.Equal(("saida", 2m, loja.Id, $"Venda #{id.ToString()[..8]} - Camiseta"), (saida.Tipo, saida.Quantidade, saida.LojaId, saida.Observacao));
        var gravada = await api.VendaNoBancoAsync(id);
        Assert.Equal((loja.Id, 110.5m), (gravada.LojaId, gravada.TotalFinal));
    }

    [FatoIntegracao]
    public async Task Registrar_venda_com_variacao_baixa_so_a_variacao_e_leva_o_tamanho_e_a_cor_no_nome()
    {
        var loja = await NovaAsync();
        var produto = await api.ProdutoAsync(loja.Id, "Camisa", estoque: 20);
        var variacao = await api.VariacaoAsync(produto, "M", "Azul", estoque: 5);
        var semCor = await api.VariacaoAsync(produto, "G", null, estoque: 3);

        var r = await loja.Painel.Post("/api/vendas", CenariosVendas.CorpoVenda([CenariosVendas.ItemProduto(produto, 2, 80m, variacao), CenariosVendas.ItemProduto(produto, 1, 80m, semCor)]));

        Assert.Equal(201, r.Codigo);
        var nomes = r.Json.GetProperty("itens").EnumerateArray().Select(i => i.GetProperty("nomeProduto").GetString()).OrderBy(n => n).ToList();
        Assert.Equal(["Camisa (G)", "Camisa (M / Azul)"], nomes);
        Assert.Equal((3, 2), ((await api.VariacaoNoBancoAsync(variacao)).Estoque, (await api.VariacaoNoBancoAsync(semCor)).Estoque));
        Assert.Equal(20m, (await api.ProdutoDaLojaNoBancoAsync(produto)).Estoque);       // o estoque do produto pai não muda
    }

    [FatoIntegracao]
    public async Task Registrar_com_data_passada_usa_a_hora_de_agora_e_com_origem_e_funcionario_grava_os_nomes()
    {
        var loja = await NovaAsync();
        var produto = await api.ProdutoAsync(loja.Id);
        var origem = (await loja.Painel.Post("/api/origens-venda", new { nome = "Instagram", ordem = 1, ativa = true })).Json.GetProperty("id").GetGuid();
        var funcionario = await loja.FuncionarioIdAsync("Joana Vendedora");
        var dia = DateTime.UtcNow.Date.AddDays(-5);

        var r = await loja.Painel.Post("/api/vendas", CenariosVendas.CorpoVenda([CenariosVendas.ItemProduto(produto)], data: dia, origem: origem, funcionario: funcionario));

        Assert.Equal(201, r.Codigo);
        var criada = r.Json.GetProperty("criadaEm").GetDateTime();
        Assert.Equal(dia, criada.Date);
        Assert.Equal(DateTimeKind.Utc, DateTime.SpecifyKind(criada, DateTimeKind.Utc).Kind);
        Assert.Equal(("Instagram", "Joana Vendedora", funcionario), (r.Json.GetProperty("origemNome").GetString(), r.Json.GetProperty("nomeFuncionario").GetString(), r.Json.GetProperty("funcionarioId").GetGuid()));
        var gravada = await api.VendaNoBancoAsync(Id(r.Json));
        Assert.Equal((origem, "Instagram"), (gravada.OrigemVendaId, gravada.OrigemNome));
    }

    [FatoIntegracao]
    public async Task Origem_de_outra_loja_nao_vira_nome_e_a_venda_sai_sem_o_nome_da_origem()
    {
        var loja = await NovaAsync();
        var outra = await NovaAsync();
        var produto = await api.ProdutoAsync(loja.Id);
        var origemAlheia = (await outra.Painel.Post("/api/origens-venda", new { nome = "Origem alheia", ordem = 1, ativa = true })).Json.GetProperty("id").GetGuid();

        var r = await loja.Painel.Post("/api/vendas", CenariosVendas.CorpoVenda([CenariosVendas.ItemProduto(produto)], origem: origemAlheia));

        Assert.Equal(201, r.Codigo);
        Assert.Equal(JsonValueKind.Null, r.Json.GetProperty("origemNome").ValueKind);
    }

    // ── Registrar: crédito de loja ───────────────────────────────────────────

    [FatoIntegracao]
    public async Task Credito_usado_sai_do_saldo_do_cliente_sem_ficar_negativo()
    {
        var loja = await NovaAsync();
        var cliente = await loja.ClienteIdAsync("Com crédito");
        var produto = await api.ProdutoAsync(loja.Id, estoque: 20);
        await api.DarCreditoAsync(cliente, 30m);

        var primeira = await loja.Painel.Post("/api/vendas", CenariosVendas.CorpoVenda([CenariosVendas.ItemProduto(produto, 1, 50m)], cliente, credito: 20m));
        Assert.Equal(20m, primeira.Json.GetProperty("creditoUsado").GetDecimal());
        Assert.Equal(10m, await api.CreditoDoClienteAsync(cliente));

        await loja.Painel.Post("/api/vendas", CenariosVendas.CorpoVenda([CenariosVendas.ItemProduto(produto, 1, 50m)], cliente, credito: 25m));
        Assert.Equal(0m, await api.CreditoDoClienteAsync(cliente));       // piso em zero
    }

    // ── Registrar: comissão do funcionário ───────────────────────────────────

    [FatoIntegracao]
    public async Task Venda_com_funcionario_de_comissao_ativa_gera_a_comissao_so_dos_itens_que_nao_vieram_de_agendamento()
    {
        var loja = await NovaAsync();
        var produto = await api.ProdutoAsync(loja.Id, estoque: 10);
        var servico = await loja.ServicoIdAsync("Banho", 80);
        var agendamento = (await loja.AgendarAsync(servico, Datas.Segunda().Hora(9), nomeCliente: "Pet", preco: 80)).Json.GetProperty("id").GetGuid();
        var comissionado = (await loja.NovoFuncionarioAsync("Vendedor", 10m, comissaoVendas: true, percentualVendas: 10m)).Json.GetProperty("id").GetGuid();
        var semComissao = await loja.FuncionarioIdAsync("Sem comissão", 10m);

        var venda = await loja.VenderAsync([CenariosVendas.ItemProduto(produto, 2, 50m), CenariosVendas.ItemServico(servico, 1, 80m, agendamento: agendamento)], funcionario: comissionado);
        var outra = await loja.VenderAsync([CenariosVendas.ItemProduto(produto, 1, 50m)], funcionario: semComissao);

        var comissao = await api.NoBancoAsync(db => db.ComissoesFuncionario.AsNoTracking().SingleAsync(c => c.OrigemId == venda));
        Assert.Equal(("venda", comissionado, 100m, 10m, 10m, "pendente", loja.Id), (comissao.OrigemTipo, comissao.ProfissionalId, comissao.ValorServico, comissao.ComissaoPercentual, comissao.ValorComissao, comissao.Status, comissao.LojaId));
        Assert.Equal(0, await api.NoBancoAsync(db => db.ComissoesFuncionario.CountAsync(c => c.OrigemId == outra)));
    }

    // ── Registrar: serviço, agendamento e plano ──────────────────────────────

    [FatoIntegracao]
    public async Task Item_de_servico_nao_mexe_em_estoque_e_marca_o_agendamento_como_pago()
    {
        var loja = await NovaAsync();
        var outra = await NovaAsync();
        var servico = await loja.ServicoIdAsync("Tosa", 100);
        var servicoAlheio = await outra.ServicoIdAsync("Tosa alheia", 100);
        var agendamento = (await loja.AgendarAsync(servico, Datas.Segunda().Hora(10), nomeCliente: "Rex", preco: 100)).Json.GetProperty("id").GetGuid();
        var agendamentoAlheio = (await outra.AgendarAsync(servicoAlheio, Datas.Segunda().Hora(10), nomeCliente: "Alheio", preco: 100)).Json.GetProperty("id").GetGuid();

        var r = await loja.Painel.Post("/api/vendas", CenariosVendas.CorpoVenda([CenariosVendas.ItemServico(servico, 1, 100m, agendamento: agendamento)]));
        var daOutra = await loja.Painel.Post("/api/vendas", CenariosVendas.CorpoVenda([CenariosVendas.ItemServico(servico, 1, 100m, agendamento: agendamentoAlheio)]));

        Assert.Equal(201, r.Codigo);
        var item = r.Json.GetProperty("itens").EnumerateArray().Single();
        Assert.Equal(("Tosa", servico), (item.GetProperty("nomeProduto").GetString(), item.GetProperty("servicoId").GetGuid()));
        Assert.Equal(JsonValueKind.Null, item.GetProperty("produtoId").ValueKind);
        var ag = await api.AgendamentoNoBancoAsync(agendamento);
        Assert.Equal((true, Id(r.Json)), (ag.Pago, ag.VendaId!.Value));
        var alheio = await api.AgendamentoNoBancoAsync(agendamentoAlheio);
        Assert.Equal((false, (Guid?)null), (alheio.Pago, alheio.VendaId));      // agendamento de outra loja não é mexido
        Assert.Equal(201, daOutra.Codigo);
    }

    [FatoIntegracao]
    public async Task Servico_dentro_de_plano_valida_assinatura_dia_e_limite_e_grava_o_consumo()
    {
        var loja = await NovaAsync();
        var servico = await loja.ServicoIdAsync("Banho", 80);
        var cliente = await loja.ClienteIdAsync("Assinante");
        var plano = (await loja.Painel.Post("/api/planos", new { nome = "Plano Banho", valor = 150, servicosIds = servico.ToString(), limiteUsosMes = 2, diasSemana = (int[]?)null })).Json.GetProperty("id").GetGuid();
        var assinatura = (await loja.Painel.Post("/api/planos/assinantes", new { clienteId = cliente, planoId = plano, diaVencimento = 10, iniciarProximoMes = false })).Json.GetProperty("id").GetGuid();

        var semAssinatura = await loja.Painel.Post("/api/vendas", CenariosVendas.CorpoVenda([CenariosVendas.ItemServico(servico, 1, 0m, assinatura: Guid.NewGuid())], cliente));
        Assert.Equal((400, "Assinatura do plano não encontrada."), (semAssinatura.Codigo, semAssinatura.Erro));

        var uso1 = await loja.Painel.Post("/api/vendas", CenariosVendas.CorpoVenda([CenariosVendas.ItemServico(servico, 1, 0m, assinatura: assinatura)], cliente));
        Assert.Equal(201, uso1.Codigo);
        var consumo = await api.NoBancoAsync(db => db.ConsumosPlano.AsNoTracking().SingleAsync(c => c.AssinaturaId == assinatura));
        Assert.Equal(("Banho", Id(uso1.Json), servico, loja.Id), (consumo.NomeServico, consumo.VendaId!.Value, consumo.ServicoId, consumo.LojaId));

        // dois itens do plano na mesma venda contam os dois usos: o terceiro estoura o limite de 2 no mês
        var estoura = await loja.Painel.Post("/api/vendas", CenariosVendas.CorpoVenda([CenariosVendas.ItemServico(servico, 1, 0m, assinatura: assinatura), CenariosVendas.ItemServico(servico, 1, 0m, assinatura: assinatura)], cliente));
        Assert.Equal((400, "O plano 'Plano Banho' já atingiu o limite de 2 uso(s) neste mês."), (estoura.Codigo, estoura.Erro));
    }

    [FatoIntegracao]
    public async Task Servico_do_plano_so_vale_nos_dias_da_semana_permitidos()
    {
        var loja = await NovaAsync();
        var servico = await loja.ServicoIdAsync("Banho", 80);
        var cliente = await loja.ClienteIdAsync("Assinante");
        var hoje = (int)LojaApi.Domain.Servicos.PlanoUsoRegras.ParaHorarioLocal(DateTime.UtcNow).DayOfWeek;
        var outroDia = (hoje + 3) % 7;
        var plano = (await loja.Painel.Post("/api/planos", new { nome = "Só em um dia", valor = 100, servicosIds = servico.ToString(), limiteUsosMes = (int?)null, diasSemana = new[] { outroDia } })).Json.GetProperty("id").GetGuid();
        var assinatura = (await loja.Painel.Post("/api/planos/assinantes", new { clienteId = cliente, planoId = plano, diaVencimento = 10, iniciarProximoMes = false })).Json.GetProperty("id").GetGuid();

        var r = await loja.Painel.Post("/api/vendas", CenariosVendas.CorpoVenda([CenariosVendas.ItemServico(servico, 1, 0m, assinatura: assinatura)], cliente));

        Assert.Equal(400, r.Codigo);
        Assert.StartsWith("O plano 'Só em um dia' só pode ser usado em: ", r.Erro);
    }

    // ── Registrar: carteira ──────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Parte_da_venda_na_carteira_fica_registrada_e_nao_pode_passar_do_total()
    {
        var loja = await NovaAsync();
        var cliente = await loja.ClienteIdAsync("Fiado");
        var produto = await api.ProdutoAsync(loja.Id, estoque: 10);
        const string formas = "[{\"forma\":\"pix\",\"valor\":40},{\"forma\":\"carteira\",\"valor\":60}]";

        var r = await loja.Painel.Post("/api/vendas", CenariosVendas.CorpoVenda([CenariosVendas.ItemProduto(produto, 1, 100m)], cliente, forma: "pix", formas: formas));

        Assert.Equal(201, r.Codigo);
        Assert.Equal(60m, r.Json.GetProperty("valorCarteira").GetDecimal());
        Assert.Equal(60m, (await api.VendaNoBancoAsync(Id(r.Json))).ValorCarteira);

        const string demais = "[{\"forma\":\"carteira\",\"valor\":150}]";
        var estoura = await loja.Painel.Post("/api/vendas", CenariosVendas.CorpoVenda([CenariosVendas.ItemProduto(produto, 1, 100m)], cliente, forma: "pix", formas: demais));
        Assert.Equal((400, "O valor na carteira não pode ser maior que o total da venda."), (estoura.Codigo, estoura.Erro));
    }

    // ── Listar e buscar ──────────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Listar_traz_so_as_vendas_da_loja_do_mais_novo_para_o_mais_antigo_e_filtra_por_periodo_e_cliente()
    {
        var loja = await NovaAsync();
        var outra = await NovaAsync();
        var maria = await loja.ClienteIdAsync("Maria");
        var produto = await api.ProdutoAsync(loja.Id, estoque: 50);
        var produtoAlheio = await api.ProdutoAsync(outra.Id, estoque: 50);
        var antiga = await loja.VenderAsync([CenariosVendas.ItemProduto(produto)], data: DateTime.UtcNow.Date.AddDays(-20));
        var comMaria = await loja.VenderAsync([CenariosVendas.ItemProduto(produto)], cliente: maria, data: DateTime.UtcNow.Date.AddDays(-3));
        var recente = await loja.VenderAsync([CenariosVendas.ItemProduto(produto)]);
        await outra.VenderAsync([CenariosVendas.ItemProduto(produtoAlheio)]);

        async Task<List<Guid>> Ids(string query) => (await loja.Painel.Get("/api/vendas" + query)).Json.EnumerateArray().Select(Id).ToList();
        string D(int dias) => DateTime.UtcNow.Date.AddDays(dias).ToString("yyyy-MM-dd") + "T00:00:00Z";

        Assert.Equal([recente, comMaria, antiga], await Ids(""));
        Assert.Equal([comMaria], await Ids($"?clienteId={maria}"));
        Assert.Equal([recente, comMaria], await Ids($"?de={Uri.EscapeDataString(D(-5))}"));
        Assert.Equal([comMaria, antiga], await Ids($"?ate={Uri.EscapeDataString(D(-3))}"));    // o "ate" vale o dia inteiro
        Assert.Equal([comMaria], await Ids($"?de={Uri.EscapeDataString(D(-5))}&ate={Uri.EscapeDataString(D(-3))}"));
    }

    [FatoIntegracao]
    public async Task Buscar_devolve_a_venda_com_cliente_e_itens_e_404_para_inexistente_ou_de_outra_loja()
    {
        var loja = await NovaAsync();
        var outra = await NovaAsync();
        var cliente = await loja.ClienteIdAsync("Dona Lia");
        var produto = await api.ProdutoAsync(loja.Id, "Calça");
        var venda = await loja.VenderAsync([CenariosVendas.ItemProduto(produto, 2, 70m)], cliente);

        var r = await loja.Painel.Get($"/api/vendas/{venda}");

        Assert.Equal(200, r.Codigo);
        Assert.Equal(("Dona Lia", 140m), (r.Json.GetProperty("nomeCliente").GetString(), r.Json.GetProperty("totalFinal").GetDecimal()));
        Assert.Equal("Calça", r.Json.GetProperty("itens")[0].GetProperty("nomeProduto").GetString());
        Assert.Equal(404, (await loja.Painel.Get($"/api/vendas/{Guid.NewGuid()}")).Codigo);
        Assert.Equal(404, (await outra.Painel.Get($"/api/vendas/{venda}")).Codigo);
    }

    // ── Excluir ──────────────────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Excluir_estorna_estoque_agendamento_consumo_de_plano_e_credito()
    {
        var loja = await NovaAsync();
        var cliente = await loja.ClienteIdAsync("Estorno");
        var produto = await api.ProdutoAsync(loja.Id, "Bolsa", estoque: 10);
        var servico = await loja.ServicoIdAsync("Banho", 80);
        var agendamento = (await loja.AgendarAsync(servico, Datas.Segunda().Hora(9), nomeCliente: "Pet", preco: 80)).Json.GetProperty("id").GetGuid();
        var plano = (await loja.Painel.Post("/api/planos", new { nome = "P", valor = 100, servicosIds = servico.ToString(), limiteUsosMes = (int?)null, diasSemana = (int[]?)null })).Json.GetProperty("id").GetGuid();
        var assinatura = (await loja.Painel.Post("/api/planos/assinantes", new { clienteId = cliente, planoId = plano, diaVencimento = 10, iniciarProximoMes = false })).Json.GetProperty("id").GetGuid();
        await api.DarCreditoAsync(cliente, 40m);
        var venda = await loja.VenderAsync([CenariosVendas.ItemProduto(produto, 3, 50m), CenariosVendas.ItemServico(servico, 1, 80m, agendamento: agendamento, assinatura: assinatura)], cliente, credito: 40m);
        Assert.Equal((7m, 0m), ((await api.ProdutoDaLojaNoBancoAsync(produto)).Estoque, await api.CreditoDoClienteAsync(cliente)));

        var r = await loja.Painel.Delete($"/api/vendas/{venda}");

        Assert.Equal((200, "Venda excluída. Estoque, agendamento e crédito foram estornados."), (r.Codigo, r.Json.GetProperty("mensagem").GetString()));
        Assert.Equal(10m, (await api.ProdutoDaLojaNoBancoAsync(produto)).Estoque);
        var entrada = (await api.MovimentosDoProdutoAsync(produto)).Last();
        Assert.Equal(("entrada", 3m, $"Estorno da venda #{venda.ToString()[..8]} (excluída) - Bolsa"), (entrada.Tipo, entrada.Quantidade, entrada.Observacao));
        var ag = await api.AgendamentoNoBancoAsync(agendamento);
        Assert.Equal((false, (Guid?)null), (ag.Pago, ag.VendaId));
        Assert.Equal(0, await api.NoBancoAsync(db => db.ConsumosPlano.CountAsync(c => c.VendaId == venda)));
        Assert.Equal(40m, await api.CreditoDoClienteAsync(cliente));
        Assert.Equal(0, await api.NoBancoAsync(db => db.Vendas.CountAsync(v => v.Id == venda)));
        Assert.Equal(0, await api.NoBancoAsync(db => db.ItensVenda.CountAsync(i => i.VendaId == venda)));
        Assert.Equal(404, (await loja.Painel.Delete($"/api/vendas/{venda}")).Codigo);
    }

    [FatoIntegracao]
    public async Task Excluir_so_vale_para_vendas_do_mes_atual()
    {
        var loja = await NovaAsync();
        var produto = await api.ProdutoAsync(loja.Id);
        var venda = await loja.VenderAsync([CenariosVendas.ItemProduto(produto)]);
        var hoje = DateTime.UtcNow.Date;
        await api.ConfigurarDataDaVendaAsync(venda, new DateTime(hoje.Year, hoje.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(-1));   // último segundo do mês passado

        var r = await loja.Painel.Delete($"/api/vendas/{venda}");

        Assert.Equal((400, "Só é possível excluir vendas do mês atual."), (r.Codigo, r.Erro));
        Assert.Equal(1, await api.NoBancoAsync(db => db.Vendas.CountAsync(v => v.Id == venda)));
    }

    [FatoIntegracao]
    public async Task Excluir_e_recusado_com_nota_fiscal_autorizada_ou_comissao_ja_paga()
    {
        var loja = await NovaAsync();
        var produto = await api.ProdutoAsync(loja.Id, estoque: 10);
        var comNota = await loja.VenderAsync([CenariosVendas.ItemProduto(produto)]);
        await api.NoBancoAsync(async db =>
        {
            db.EmissoesFiscais.Add(new EmissaoFiscal { LojaId = loja.Id, VendaId = comNota, Status = "autorizada" });
            await db.SaveChangesAsync();
        });

        var nota = await loja.Painel.Delete($"/api/vendas/{comNota}");
        Assert.Equal((400, "Não é possível excluir: já existe uma nota fiscal autorizada para esta venda. Cancele a nota antes."), (nota.Codigo, nota.Erro));

        var vendedor = (await loja.NovoFuncionarioAsync("Vendedor", 10m, comissaoVendas: true, percentualVendas: 10m)).Json.GetProperty("id").GetGuid();
        var comComissao = await loja.VenderAsync([CenariosVendas.ItemProduto(produto)], funcionario: vendedor);
        await api.NoBancoAsync(db => db.ComissoesFuncionario.Where(c => c.OrigemId == comComissao).ExecuteUpdateAsync(s => s.SetProperty(c => c.Status, "pago")));

        var comissaoPaga = await loja.Painel.Delete($"/api/vendas/{comComissao}");
        Assert.Equal((400, "Não é possível excluir: a comissão desta venda já foi paga."), (comissaoPaga.Codigo, comissaoPaga.Erro));
        Assert.Equal(2, await api.NoBancoAsync(db => db.Vendas.CountAsync(v => v.LojaId == loja.Id)));
    }

    [FatoIntegracao]
    public async Task Excluir_remove_junto_a_comissao_pendente_da_venda()
    {
        var loja = await NovaAsync();
        var produto = await api.ProdutoAsync(loja.Id, estoque: 10);
        var vendedor = (await loja.NovoFuncionarioAsync("Vendedor", 10m, comissaoVendas: true, percentualVendas: 10m)).Json.GetProperty("id").GetGuid();
        var venda = await loja.VenderAsync([CenariosVendas.ItemProduto(produto, 1, 100m)], funcionario: vendedor);
        Assert.Equal(1, await api.NoBancoAsync(db => db.ComissoesFuncionario.CountAsync(c => c.OrigemId == venda)));

        (await loja.Painel.Delete($"/api/vendas/{venda}")).DeveSerSucesso();

        Assert.Equal(0, await api.NoBancoAsync(db => db.ComissoesFuncionario.CountAsync(c => c.OrigemId == venda)));
    }
}
