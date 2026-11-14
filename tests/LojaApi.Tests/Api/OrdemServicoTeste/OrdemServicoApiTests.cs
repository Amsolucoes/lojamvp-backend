using System.Text.Json;
using LojaApi.Models;
using LojaApi.src.Models.Funcionarios;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using static LojaApi.Tests.Suporte.CenariosOrdemServico;

namespace LojaApi.Tests.Api.OrdemServicoTeste;

// Orçamentos e ordens de serviço da oficina: criar, editar, aprovar, concluir (estoque + financeiro + comissão) e desfazer.
[Collection(ColecaoApi.Nome)]
public class OrdemServicoApiTests(ApiFixture api)
{
    private const string Orc = Rota + "/orcamentos";

    private Task<LojaTeste> NovaAsync() => api.NovaLojaDeServicosAsync();

    private static async Task<(int, string)> Erro(Task<Resposta> r) { var x = await r; return (x.Codigo, x.Erro); }

    private async Task<Guid> MecanicoAsync(LojaTeste loja, string nome = "Mecânico", decimal? comissao = 10m, string baseCalculo = "total")
    {
        var r = await loja.NovoFuncionarioAsync(nome, comissao, baseCalculo: baseCalculo);
        r.DeveSerSucesso();
        return r.Json.GetProperty("id").GetGuid();
    }

    [FatoIntegracao]
    public async Task Exige_login_e_papel_de_admin_nas_acoes_de_gestao()
    {
        var loja = await NovaAsync();
        var operador = await api.OperadorAsync(loja);
        var cliente = await loja.ClienteIdAsync();
        var id = await loja.OrcamentoAsync(cliente, [MaoDeObra()]);

        foreach (var (m, u) in new[] { ("GET", Orc), ("GET", $"{Orc}/{id}"), ("POST", Orc), ("PUT", $"{Orc}/{id}"), ("GET", $"{Orc}/{id}/pdf"), ("POST", $"{Orc}/{id}/enviar-email"),
                     ("GET", $"{Orc}/cliente/{cliente}"), ("GET", $"{Rota}/resumo-clientes") })
            Assert.Equal(401, (await Enviar(loja.Site, m, u)).Codigo);

        foreach (var u in new[] { $"{Orc}/{id}/aprovar", $"{Orc}/{id}/cancelar", $"{Orc}/{id}/reabrir", $"{Orc}/{id}/desfazer-conclusao", $"{Orc}/{id}/concluir", $"{Orc}/{id}/datas" })
            Assert.Equal(403, (await operador.Patch(u, new { })).Codigo);
        Assert.Equal(403, (await operador.Delete($"{Orc}/{id}")).Codigo);
        Assert.Equal(200, (await operador.Get(Orc)).Codigo);                       // operador cria e consulta
        Assert.Equal(200, (await operador.Post(Orc, CorpoOrcamento(cliente, [MaoDeObra()]))).Codigo);
    }

    private static Task<Resposta> Enviar(ClienteApi c, string metodo, string url) => metodo switch
    {
        "GET" => c.Get(url), "POST" => c.Post(url, new { }), _ => c.Put(url, new { }),
    };

    // ── Criar ────────────────────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Criar_valida_loja_itens_cliente_tipo_e_mecanico_sem_gravar_nada()
    {
        var loja = await NovaAsync();
        var cliente = await loja.ClienteIdAsync();

        Assert.Equal((400, "O orçamento precisa ter ao menos um item."), await Erro(loja.Painel.Post(Orc, CorpoOrcamento(cliente, []))));
        Assert.Equal((400, "Cliente não encontrado."), await Erro(loja.Painel.Post(Orc, CorpoOrcamento(Guid.NewGuid(), [MaoDeObra()]))));
        Assert.Equal((400, "Tipo de item inválido — use 'peca' ou 'servico'."),
            await Erro(loja.Painel.Post(Orc, CorpoOrcamento(cliente, [new { tipo = "outro", descricao = "x", quantidade = 1, valorUnitario = 1m }]))));
        Assert.Equal((400, "Mecânico/profissional não encontrado."),
            await Erro(loja.Painel.Post(Orc, CorpoOrcamento(cliente, [MaoDeObra()], [Mecanico(Guid.NewGuid())]))));
        Assert.Empty((await loja.Painel.Get(Orc)).Json.EnumerateArray());
    }

    [FatoIntegracao]
    public async Task Criar_soma_os_itens_normaliza_a_placa_e_usa_a_comissao_padrao_do_mecanico()
    {
        var loja = await NovaAsync();
        var cliente = await loja.ClienteIdAsync();
        var produto = await api.ProdutoAsync(loja.Id, "Pastilha", estoque: 10);
        var padrao = await MecanicoAsync(loja, "Padrão", 12m);
        var proprio = await MecanicoAsync(loja, "Próprio", 12m);
        var item = await loja.ChecklistItemAsync(await loja.ChecklistCategoriaAsync());

        var r = await loja.Painel.Post(Orc, CorpoOrcamento(cliente, [Peca(produto, "  Pastilha ", 2, 100m), MaoDeObra("Alinhamento", 1, 80m)],
            [Mecanico(padrao, 0m), Mecanico(proprio, 25m)], [Resposta(item, "ruim", "gasto")], placa: " abc1d23 ", observacoes: "urgente"));

        Assert.Equal(200, r.Codigo);
        Assert.Equal(("pendente", 280m), (r.Json.GetProperty("status").GetString(), r.Json.GetProperty("valorTotal").GetDecimal()));
        var d = (await loja.Painel.Get($"{Orc}/{r.Json.GetProperty("id").GetGuid()}")).Json;
        Assert.Equal(("ABC1D23", "Gol 2015", "urgente", "pendente"), (d.GetProperty("placa").GetString(), d.GetProperty("veiculoDescricao").GetString(), d.GetProperty("observacoes").GetString(), d.GetProperty("status").GetString()));
        Assert.Equal(["Alinhamento", "Pastilha"], d.GetProperty("itens").EnumerateArray().Select(i => i.GetProperty("descricao").GetString()).OrderBy(x => x));
        Assert.Equal([80m, 200m], d.GetProperty("itens").EnumerateArray().Select(i => i.GetProperty("valorTotal").GetDecimal()).OrderBy(x => x));
        Assert.Equal([12m, 25m], d.GetProperty("mecanicos").EnumerateArray().Select(m => m.GetProperty("comissaoPercentual").GetDecimal()).OrderBy(x => x == 12m ? 0 : 1));
        var resposta = Assert.Single(d.GetProperty("checklist").EnumerateArray());
        Assert.Equal(("Pastilha", "ruim", "gasto"), (resposta.GetProperty("nomeItem").GetString(), resposta.GetProperty("estado").GetString(), resposta.GetProperty("observacao").GetString()));
    }

    [FatoIntegracao]
    public async Task Criar_com_placa_em_branco_grava_nulo()
    {
        var loja = await NovaAsync();
        var cliente = await loja.ClienteIdAsync();

        var id = await loja.OrcamentoAsync(cliente, [MaoDeObra()], placa: "   ");

        Assert.Null((await api.OrcamentoNoBancoAsync(id)).Placa);
    }

    // ── Consultas ────────────────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Listar_filtra_por_status_e_placa_mostra_mecanicos_e_isola_por_loja()
    {
        var loja = await NovaAsync();
        var outra = await NovaAsync();
        var cliente = await loja.ClienteIdAsync();
        var mecanico = await MecanicoAsync(loja, "Zé");
        var a = await loja.OrcamentoAsync(cliente, [MaoDeObra(valor: 50m)], [Mecanico(mecanico)], placa: "AAA1111");
        var b = await loja.OrcamentoAsync(cliente, [MaoDeObra(valor: 70m)], placa: "BBB2222");
        (await loja.Painel.Patch($"{Orc}/{b}/aprovar")).DeveSerSucesso();
        await outra.OrcamentoAsync(await outra.ClienteIdAsync(), [MaoDeObra()]);

        var todos = (await loja.Painel.Get(Orc)).Json.EnumerateArray().ToList();
        var pendentes = (await loja.Painel.Get($"{Orc}?status=pendente")).Json.EnumerateArray().ToList();
        var porPlaca = (await loja.Painel.Get($"{Orc}?placa=bbb")).Json.EnumerateArray().ToList();

        Assert.Equal([b, a], todos.Select(o => o.GetProperty("id").GetGuid()));      // mais novo primeiro
        Assert.Equal(a, Assert.Single(pendentes).GetProperty("id").GetGuid());
        Assert.Equal(b, Assert.Single(porPlaca).GetProperty("id").GetGuid());
        Assert.Equal((1, "Zé"), (pendentes[0].GetProperty("qtdMecanicos").GetInt32(), pendentes[0].GetProperty("nomesMecanicos")[0].GetString()));
        Assert.Equal(50m, pendentes[0].GetProperty("valorTotal").GetDecimal());
    }

    [FatoIntegracao]
    public async Task Buscar_devolve_o_detalhe_e_404_para_outra_loja_ou_inexistente()
    {
        var loja = await NovaAsync();
        var outra = await NovaAsync();
        var cliente = await loja.ClienteIdAsync();
        var mecanico = await MecanicoAsync(loja, "Zé");
        var id = await loja.OrcamentoAsync(cliente, [Peca(null, "Peça avulsa", 3, 10m)], [Mecanico(mecanico)]);

        var d = (await loja.Painel.Get($"{Orc}/{id}")).Json;

        Assert.Equal((cliente, 30m), (d.GetProperty("clienteId").GetGuid(), d.GetProperty("valorTotal").GetDecimal()));
        var item = d.GetProperty("itens")[0];
        Assert.Equal(("peca", "Peça avulsa", 3, 10m, 30m), (item.GetProperty("tipo").GetString(), item.GetProperty("descricao").GetString(), item.GetProperty("quantidade").GetInt32(),
            item.GetProperty("valorUnitario").GetDecimal(), item.GetProperty("valorTotal").GetDecimal()));
        Assert.Equal(("Zé", 10m), (d.GetProperty("mecanicos")[0].GetProperty("nomeProfissional").GetString(), d.GetProperty("mecanicos")[0].GetProperty("comissaoPercentual").GetDecimal()));
        Assert.Equal(404, (await outra.Painel.Get($"{Orc}/{id}")).Codigo);
        Assert.Equal(404, (await loja.Painel.Get($"{Orc}/{Guid.NewGuid()}")).Codigo);
    }

    [FatoIntegracao]
    public async Task Historico_e_resumo_por_cliente_ignoram_cancelados_e_marcam_o_que_esta_em_andamento()
    {
        var loja = await NovaAsync();
        var maria = await loja.ClienteIdAsync("Maria");
        var joao = await loja.ClienteIdAsync("João", "67999991111");
        var conta = await loja.ContaBancariaIdAsync();
        var feita = await loja.OrcamentoEmAndamentoAsync(maria, [MaoDeObra(valor: 100m)]);
        (await loja.ConcluirAsync(feita, conta)).DeveSerSucesso();
        await loja.OrcamentoAsync(maria, [MaoDeObra(valor: 50m)]);
        var cancelada = await loja.OrcamentoAsync(maria, [MaoDeObra(valor: 999m)]);
        (await loja.Painel.Patch($"{Orc}/{cancelada}/cancelar")).DeveSerSucesso();
        await loja.OrcamentoAsync(joao, [MaoDeObra(valor: 30m)]);

        var historico = (await loja.Painel.Get($"{Orc}/cliente/{maria}")).Json.EnumerateArray().ToList();
        var resumo = (await loja.Painel.Get($"{Rota}/resumo-clientes")).Json.EnumerateArray().ToDictionary(r => r.GetProperty("clienteId").GetGuid());

        Assert.Equal(3, historico.Count);
        Assert.Equal("Alinhamento", historico.First(h => h.GetProperty("id").GetGuid() == feita).GetProperty("itens")[0].GetProperty("descricao").GetString());
        Assert.Equal((2, 150m, true), (resumo[maria].GetProperty("qtd").GetInt32(), resumo[maria].GetProperty("total").GetDecimal(), resumo[maria].GetProperty("temAndamento").GetBoolean()));
        Assert.Equal((1, 30m), (resumo[joao].GetProperty("qtd").GetInt32(), resumo[joao].GetProperty("total").GetDecimal()));
        Assert.Empty((await (await NovaAsync()).Painel.Get($"{Orc}/cliente/{maria}")).Json.EnumerateArray());
    }

    // ── Editar / transições ──────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Editar_recria_itens_mecanicos_e_checklist_e_so_vale_antes_de_concluir()
    {
        var loja = await NovaAsync();
        var cliente = await loja.ClienteIdAsync();
        var outroCliente = await loja.ClienteIdAsync("Outro", "67999992222");
        var m1 = await MecanicoAsync(loja, "Um");
        var m2 = await MecanicoAsync(loja, "Dois", 5m);
        var id = await loja.OrcamentoAsync(cliente, [MaoDeObra("Velho", 1, 10m)], [Mecanico(m1)]);

        var r = await loja.Painel.Put($"{Orc}/{id}", CorpoOrcamento(outroCliente, [Peca(null, " Nova ", 2, 40m)], [Mecanico(m2)], placa: "xyz9k88", veiculo: "Uno", observacoes: "ok"));

        Assert.Equal(("pendente", 80m), (r.Json.GetProperty("status").GetString(), r.Json.GetProperty("valorTotal").GetDecimal()));
        var d = (await loja.Painel.Get($"{Orc}/{id}")).Json;
        Assert.Equal((outroCliente, "XYZ9K88", "Uno"), (d.GetProperty("clienteId").GetGuid(), d.GetProperty("placa").GetString(), d.GetProperty("veiculoDescricao").GetString()));
        Assert.Equal("Nova", Assert.Single(d.GetProperty("itens").EnumerateArray()).GetProperty("descricao").GetString());
        Assert.Equal(("Dois", 5m), (d.GetProperty("mecanicos")[0].GetProperty("nomeProfissional").GetString(), d.GetProperty("mecanicos")[0].GetProperty("comissaoPercentual").GetDecimal()));

        Assert.Equal((400, "O orçamento precisa ter ao menos um item."), await Erro(loja.Painel.Put($"{Orc}/{id}", CorpoOrcamento(cliente, []))));
        Assert.Equal((400, "Cliente não encontrado."), await Erro(loja.Painel.Put($"{Orc}/{id}", CorpoOrcamento(Guid.NewGuid(), [MaoDeObra()]))));
        Assert.Equal((400, "Tipo de item inválido — use 'peca' ou 'servico'."), await Erro(loja.Painel.Put($"{Orc}/{id}", CorpoOrcamento(cliente, [new { tipo = "x", descricao = "x", quantidade = 1, valorUnitario = 1m }]))));
        Assert.Equal((400, "Mecânico/profissional não encontrado."), await Erro(loja.Painel.Put($"{Orc}/{id}", CorpoOrcamento(cliente, [MaoDeObra()], [Mecanico(Guid.NewGuid())]))));
        Assert.Equal(404, (await loja.Painel.Put($"{Orc}/{Guid.NewGuid()}", CorpoOrcamento(cliente, [MaoDeObra()]))).Codigo);

        foreach (var status in new[] { "concluido", "cancelado" })
        {
            await api.DefinirStatusAsync(id, status);
            Assert.Equal((400, "Só é possível editar uma ordem pendente ou em andamento. Ordens concluídas já geraram financeiro, comissão e baixa de estoque."),
                await Erro(loja.Painel.Put($"{Orc}/{id}", CorpoOrcamento(cliente, [MaoDeObra()]))));
        }
    }

    [FatoIntegracao]
    public async Task Aprovar_cancelar_e_reabrir_seguem_as_transicoes_de_status()
    {
        var loja = await NovaAsync();
        var outra = await NovaAsync();
        var cliente = await loja.ClienteIdAsync();
        var id = await loja.OrcamentoAsync(cliente, [MaoDeObra()]);

        Assert.Equal((400, "Só é possível reabrir uma ordem cancelada."), await Erro(loja.Painel.Patch($"{Orc}/{id}/reabrir")));
        var aprovada = await loja.Painel.Patch($"{Orc}/{id}/aprovar");
        Assert.Equal("em_andamento", aprovada.Json.GetProperty("status").GetString());
        Assert.NotNull((await api.OrcamentoNoBancoAsync(id)).AprovadoEm);
        Assert.Equal((400, "Só é possível aprovar um orçamento pendente."), await Erro(loja.Painel.Patch($"{Orc}/{id}/aprovar")));

        Assert.Equal("cancelado", (await loja.Painel.Patch($"{Orc}/{id}/cancelar")).Json.GetProperty("status").GetString());
        Assert.Equal("em_andamento", (await loja.Painel.Patch($"{Orc}/{id}/reabrir")).Json.GetProperty("status").GetString());

        await api.DefinirStatusAsync(id, "concluido");
        Assert.Equal((400, "Não é possível cancelar uma ordem já concluída."), await Erro(loja.Painel.Patch($"{Orc}/{id}/cancelar")));

        foreach (var acao in new[] { "aprovar", "cancelar", "reabrir", "desfazer-conclusao" })
            Assert.Equal(404, (await outra.Painel.Patch($"{Orc}/{id}/{acao}")).Codigo);
        Assert.Equal(404, (await loja.Painel.Patch($"{Orc}/{Guid.NewGuid()}/aprovar")).Codigo);
    }

    // ── Concluir ─────────────────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Concluir_valida_status_conta_estoque_e_pagamento_sem_gravar_nada()
    {
        var loja = await NovaAsync();
        var outra = await NovaAsync();
        var cliente = await loja.ClienteIdAsync();
        var conta = await loja.ContaBancariaIdAsync();
        var contaDela = await outra.ContaBancariaIdAsync();
        var pouco = await api.ProdutoAsync(loja.Id, "Pastilha", estoque: 1);
        var pendente = await loja.OrcamentoAsync(cliente, [MaoDeObra()]);
        var semEstoque = await loja.OrcamentoEmAndamentoAsync(cliente, [Peca(pouco, "Pastilha", 3, 10m)]);
        var simples = await loja.OrcamentoEmAndamentoAsync(cliente, [MaoDeObra()]);

        Assert.Equal((400, "Só é possível concluir uma ordem em andamento (aprovada)."), await Erro(loja.ConcluirAsync(pendente, conta)));
        Assert.Equal((400, "Conta bancária não encontrada."), await Erro(loja.ConcluirAsync(simples, contaDela)));
        Assert.Equal((400, "Conta bancária não encontrada."), await Erro(loja.ConcluirAsync(simples, Guid.NewGuid())));
        Assert.Equal((400, "Estoque insuficiente para 'Pastilha' — disponível: 1."), await Erro(loja.ConcluirAsync(semEstoque, conta)));
        Assert.Equal((400, "Informe a forma de pagamento."), await Erro(loja.ConcluirAsync(simples, conta, jaPago: true)));
        Assert.Equal("em_andamento", (await api.OrcamentoNoBancoAsync(simples)).Status);
        Assert.Equal(1m, (await api.ProdutoDaLojaNoBancoAsync(pouco)).Estoque);
        Assert.Null((await api.OrcamentoNoBancoAsync(simples)).LancamentoFinanceiroId);
    }

    [FatoIntegracao]
    public async Task Concluir_baixa_o_estoque_gera_o_lancamento_a_receber_e_cria_a_categoria_uma_vez()
    {
        var loja = await NovaAsync();
        var cliente = await loja.ClienteIdAsync();
        var conta = await loja.ContaBancariaIdAsync();
        var produto = await api.ProdutoAsync(loja.Id, "Pastilha", estoque: 5);
        var id = await loja.OrcamentoEmAndamentoAsync(cliente, [Peca(produto, "Pastilha", 2, 100m), Peca(null, "Avulsa", 1, 20m), MaoDeObra("Alinhamento", 1, 80m)]);
        var vencimento = new DateTime(2031, 3, 20, 0, 0, 0, DateTimeKind.Utc);

        var r = await loja.ConcluirAsync(id, conta, vencimento);

        Assert.Equal(200, r.Codigo);
        Assert.Equal("concluido", r.Json.GetProperty("status").GetString());
        var o = await api.OrcamentoNoBancoAsync(id);
        Assert.NotNull(o.ConcluidoEm);
        Assert.Equal(o.LancamentoFinanceiroId, r.Json.GetProperty("lancamentoFinanceiroId").GetGuid());
        Assert.Equal(3m, (await api.ProdutoDaLojaNoBancoAsync(produto)).Estoque);
        var mov = Assert.Single(await api.MovimentosDoProdutoAsync(produto));
        Assert.Equal(("saida", 2m), (mov.Tipo, mov.Quantidade));
        Assert.StartsWith($"Ordem de Serviço #{id.ToString()[..8]} - Pastilha", mov.Observacao);
        var l = await api.LancamentoNoBancoAsync(o.LancamentoFinanceiroId!.Value);
        Assert.Equal(("receber", "avulsa", "pendente", 300m, conta), (l.Tipo, l.Modo, l.Status, l.Valor, l.ContaBancariaId));
        Assert.Equal("Ordem de Serviço — Gol 2015", l.Descricao);
        Assert.Equal(new DateTime(2031, 3, 20, 12, 0, 0, DateTimeKind.Utc), l.Vencimento);
        Assert.Null(l.PagoEm);
        Assert.Null(l.Observacao);

        // segunda ordem reaproveita a categoria "Ordem de Serviço"
        var id2 = await loja.OrcamentoEmAndamentoAsync(cliente, [MaoDeObra()]);
        (await loja.ConcluirAsync(id2, conta)).DeveSerSucesso();
        var categorias = await api.NoBancoAsync(db => db.CategoriasFinanceiras.Where(c => c.LojaId == loja.Id && c.Nome == "Ordem de Serviço").ToListAsync());
        var unica = Assert.Single(categorias);
        Assert.Equal(("receber", "🔧"), (unica.Tipo, unica.Icone));
        Assert.Equal(unica.Id, l.CategoriaId);
    }

    [FatoIntegracao]
    public async Task Concluir_ja_pago_registra_a_forma_de_pagamento_e_as_parcelas()
    {
        var loja = await NovaAsync();
        var cliente = await loja.ClienteIdAsync();
        var conta = await loja.ContaBancariaIdAsync();
        var avista = await loja.OrcamentoEmAndamentoAsync(cliente, [MaoDeObra()], null);
        var parcelado = await loja.OrcamentoEmAndamentoAsync(cliente, [MaoDeObra()], null);

        (await loja.ConcluirAsync(avista, conta, jaPago: true, forma: "pix")).DeveSerSucesso();
        (await loja.ConcluirAsync(parcelado, conta, jaPago: true, forma: "cartao", parcelas: 3)).DeveSerSucesso();

        var a = await api.LancamentoNoBancoAsync((await api.OrcamentoNoBancoAsync(avista)).LancamentoFinanceiroId!.Value);
        var p = await api.LancamentoNoBancoAsync((await api.OrcamentoNoBancoAsync(parcelado)).LancamentoFinanceiroId!.Value);
        Assert.Equal(("pago", "Pago via pix"), (a.Status, a.Observacao));
        Assert.NotNull(a.PagoEm);
        Assert.Equal(("pago", "Pago via cartao em 3x"), (p.Status, p.Observacao));
    }

    [FatoIntegracao]
    public async Task Concluir_gera_comissao_por_mecanico_conforme_a_base_de_calculo_e_as_excecoes_por_servico()
    {
        var loja = await NovaAsync();
        var cliente = await loja.ClienteIdAsync();
        var conta = await loja.ContaBancariaIdAsync();
        var servico = await loja.ServicoIdAsync("Alinhamento", 80m);
        var total = await MecanicoAsync(loja, "Total", 10m, "total");
        var soServico = await MecanicoAsync(loja, "SoServico", 10m, "servico");
        var excecao = await MecanicoAsync(loja, "Excecao", 10m, "total");
        var semComissao = await MecanicoAsync(loja, "Zerado", 0m, "total");
        (await loja.Painel.Post($"/api/funcionarios/{excecao}/comissoes-servico", new { servicoId = servico, comissaoPercentual = 0m })).DeveSerSucesso();
        var produto = await api.ProdutoAsync(loja.Id, "Pastilha", estoque: 5);
        var id = await loja.OrcamentoEmAndamentoAsync(cliente, [Peca(produto, "Pastilha", 2, 100m), MaoDeObra("Alinhamento", 1, 80m, servico)],
            [Mecanico(total), Mecanico(soServico), Mecanico(excecao), Mecanico(semComissao)]);

        (await loja.ConcluirAsync(id, conta)).DeveSerSucesso();

        var porMecanico = (await api.ComissoesDaOrdemAsync(id)).ToDictionary(c => c.ProfissionalId);
        Assert.Equal(3, porMecanico.Count);                                           // percentual 0 não gera comissão
        Assert.Equal((280m, 10m, 28m, "pendente"), (porMecanico[total].ValorServico, porMecanico[total].ComissaoPercentual, porMecanico[total].ValorComissao, porMecanico[total].Status));
        Assert.Equal((80m, 10m, 8m), (porMecanico[soServico].ValorServico, porMecanico[soServico].ComissaoPercentual, porMecanico[soServico].ValorComissao));
        Assert.Equal((280m, 7.14m, 20m), (porMecanico[excecao].ValorServico, porMecanico[excecao].ComissaoPercentual, porMecanico[excecao].ValorComissao));   // serviço com exceção de 0%
    }

    // ── Desfazer conclusão ───────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Desfazer_devolve_o_estoque_remove_o_lancamento_e_as_comissoes()
    {
        var loja = await NovaAsync();
        var cliente = await loja.ClienteIdAsync();
        var conta = await loja.ContaBancariaIdAsync();
        var mecanico = await MecanicoAsync(loja);
        var produto = await api.ProdutoAsync(loja.Id, "Pastilha", estoque: 5);
        var id = await loja.OrcamentoEmAndamentoAsync(cliente, [Peca(produto, "Pastilha", 2, 100m), MaoDeObra()], [Mecanico(mecanico)]);
        (await loja.ConcluirAsync(id, conta)).DeveSerSucesso();
        var lancamento = (await api.OrcamentoNoBancoAsync(id)).LancamentoFinanceiroId!.Value;

        var r = await loja.Painel.Patch($"{Orc}/{id}/desfazer-conclusao");

        Assert.Equal(("em_andamento", "Conclusão desfeita — estoque, financeiro e comissão foram revertidos."), (r.Json.GetProperty("status").GetString(), r.Json.GetProperty("mensagem").GetString()));
        var o = await api.OrcamentoNoBancoAsync(id);
        Assert.Equal(("em_andamento", null, null), (o.Status, o.ConcluidoEm, o.LancamentoFinanceiroId));
        Assert.Equal(5m, (await api.ProdutoDaLojaNoBancoAsync(produto)).Estoque);
        Assert.Equal(["saida", "entrada"], (await api.MovimentosDoProdutoAsync(produto)).Select(m => m.Tipo));
        Assert.Empty(await api.ComissoesDaOrdemAsync(id));
        Assert.False(await api.NoBancoAsync(db => db.LancamentosFinanceiros.AnyAsync(l => l.Id == lancamento)));
    }

    [FatoIntegracao]
    public async Task Desfazer_recusa_ordem_nao_concluida_lancamento_pago_e_comissao_paga()
    {
        var loja = await NovaAsync();
        var cliente = await loja.ClienteIdAsync();
        var conta = await loja.ContaBancariaIdAsync();
        var mecanico = await MecanicoAsync(loja);
        var emAndamento = await loja.OrcamentoEmAndamentoAsync(cliente, [MaoDeObra()]);
        var pago = await loja.OrcamentoEmAndamentoAsync(cliente, [MaoDeObra()]);
        var comissaoPaga = await loja.OrcamentoEmAndamentoAsync(cliente, [MaoDeObra()], [Mecanico(mecanico)]);
        (await loja.ConcluirAsync(pago, conta, jaPago: true, forma: "pix")).DeveSerSucesso();
        (await loja.ConcluirAsync(comissaoPaga, conta)).DeveSerSucesso();
        await api.NoBancoAsync(db => db.ComissoesFuncionario.Where(c => c.OrigemId == comissaoPaga).ExecuteUpdateAsync(s => s.SetProperty(c => c.Status, "pago")));

        Assert.Equal((400, "Só é possível desfazer uma ordem concluída."), await Erro(loja.Painel.Patch($"{Orc}/{emAndamento}/desfazer-conclusao")));
        Assert.Equal((400, "O lançamento financeiro desta ordem já foi marcado como pago. Reverta o pagamento no Financeiro antes de desfazer a conclusão."),
            await Erro(loja.Painel.Patch($"{Orc}/{pago}/desfazer-conclusao")));
        Assert.Equal((400, "Uma ou mais comissões geradas por esta ordem já foram pagas. Reverta o pagamento em Comissões antes de desfazer a conclusão."),
            await Erro(loja.Painel.Patch($"{Orc}/{comissaoPaga}/desfazer-conclusao")));
        Assert.Equal("concluido", (await api.OrcamentoNoBancoAsync(pago)).Status);
        Assert.Equal("concluido", (await api.OrcamentoNoBancoAsync(comissaoPaga)).Status);
    }

    // ── Datas e exclusão ─────────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Ajustar_datas_valida_o_status_a_ordem_das_datas_e_grava_em_utc()
    {
        var loja = await NovaAsync();
        var cliente = await loja.ClienteIdAsync();
        var conta = await loja.ContaBancariaIdAsync();
        var pendente = await loja.OrcamentoAsync(cliente, [MaoDeObra()]);
        var id = await loja.OrcamentoEmAndamentoAsync(cliente, [MaoDeObra()]);
        var entrada = new DateTime(2031, 5, 10, 8, 0, 0);
        var saida = new DateTime(2031, 5, 12, 17, 30, 0);

        Assert.Equal((400, "Só é possível ajustar entrada/saída de uma ordem aprovada ou concluída."), await Erro(loja.Painel.Patch($"{Orc}/{pendente}/datas", new { aprovadoEm = entrada })));
        Assert.Equal((400, "A data/hora de saída não pode ser antes da entrada."), await Erro(loja.Painel.Patch($"{Orc}/{id}/datas", new { aprovadoEm = saida, concluidoEm = entrada })));

        var r = await loja.Painel.Patch($"{Orc}/{id}/datas", new { aprovadoEm = entrada, concluidoEm = saida });

        Assert.Equal(200, r.Codigo);
        var o = await api.OrcamentoNoBancoAsync(id);
        Assert.Equal((new DateTime(2031, 5, 10, 8, 0, 0, DateTimeKind.Utc), new DateTime(2031, 5, 12, 17, 30, 0, DateTimeKind.Utc)), (o.AprovadoEm, o.ConcluidoEm));

        // só a entrada: a saída continua como estava
        (await loja.Painel.Patch($"{Orc}/{id}/datas", new { aprovadoEm = new DateTime(2031, 5, 11, 9, 0, 0) })).DeveSerSucesso();
        var depois = await api.OrcamentoNoBancoAsync(id);
        Assert.Equal((new DateTime(2031, 5, 11, 9, 0, 0, DateTimeKind.Utc), new DateTime(2031, 5, 12, 17, 30, 0, DateTimeKind.Utc)), (depois.AprovadoEm, depois.ConcluidoEm));
        Assert.Equal(404, (await loja.Painel.Patch($"{Orc}/{Guid.NewGuid()}/datas", new { aprovadoEm = entrada })).Codigo);
    }

    [FatoIntegracao]
    public async Task Excluir_so_vale_para_pendente_e_so_da_propria_loja()
    {
        var loja = await NovaAsync();
        var outra = await NovaAsync();
        var cliente = await loja.ClienteIdAsync();
        var pendente = await loja.OrcamentoAsync(cliente, [MaoDeObra()]);
        var aprovada = await loja.OrcamentoEmAndamentoAsync(cliente, [MaoDeObra()]);

        Assert.Equal(404, (await outra.Painel.Delete($"{Orc}/{pendente}")).Codigo);
        Assert.Equal((400, "Só é possível excluir um orçamento ainda pendente. Cancele em vez de excluir."), await Erro(loja.Painel.Delete($"{Orc}/{aprovada}")));
        Assert.Equal("Orçamento excluído.", (await loja.Painel.Delete($"{Orc}/{pendente}")).Json.GetProperty("mensagem").GetString());
        Assert.Equal(404, (await loja.Painel.Get($"{Orc}/{pendente}")).Codigo);
        Assert.Equal(404, (await loja.Painel.Delete($"{Orc}/{Guid.NewGuid()}")).Codigo);
    }

    // ── E-mail e PDF ─────────────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Enviar_email_exige_orcamento_da_loja_e_cliente_com_email()
    {
        var loja = await NovaAsync();
        var outra = await NovaAsync();
        var semEmail = await loja.ClienteIdAsync("Sem email");
        var comEmail = (await loja.Painel.Post("/api/clientes", new { nome = "Com email", telefone = "67999993333", email = "cliente@teste.com" })).Json.GetProperty("id").GetGuid();
        var a = await loja.OrcamentoAsync(semEmail, [MaoDeObra()]);
        var b = await loja.OrcamentoAsync(comEmail, [Peca(null, "Peça", 2, 50m), MaoDeObra()]);
        api.Resend.ClearReceivedCalls();

        Assert.Equal((400, "Orçamento não encontrado."), await Erro(outra.Painel.Post($"{Orc}/{b}/enviar-email")));
        Assert.Equal((400, "Cliente não tem e-mail cadastrado."), await Erro(loja.Painel.Post($"{Orc}/{a}/enviar-email")));
        Assert.Equal(0, EnviosDeEmail());

        var ok = await loja.Painel.Post($"{Orc}/{b}/enviar-email");

        Assert.Equal(("Orçamento enviado por e-mail."), ok.Json.GetProperty("mensagem").GetString());
        Assert.Equal(1, EnviosDeEmail());
    }

    private int EnviosDeEmail() => api.Resend.ReceivedCalls().Count(c => c.GetMethodInfo().Name == "EmailSendAsync");

    [FatoIntegracao]
    public async Task Baixar_pdf_devolve_o_arquivo_e_404_para_orcamento_de_outra_loja()
    {
        var loja = await NovaAsync();
        var outra = await NovaAsync();
        var id = await loja.OrcamentoAsync(await loja.ClienteIdAsync(), [Peca(null, "Peça", 2, 50m), MaoDeObra()]);

        var pdf = await loja.Painel.Get($"{Orc}/{id}/pdf");

        Assert.Equal(200, pdf.Codigo);
        Assert.Equal("application/pdf", pdf.TipoConteudo);
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(pdf.Bytes, 0, 4));
        Assert.Equal(404, (await outra.Painel.Get($"{Orc}/{id}/pdf")).Codigo);
    }
}
