using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using static LojaApi.Tests.Suporte.CenariosNfImportacao;

namespace LojaApi.Tests.Api.NfImportacaoTeste;

// Nota fiscal lançada à mão (sem XML): lançar, ver o detalhe, editar quantidade/custo, histórico e desfazer.
[Collection(ColecaoApi.Nome)]
public class NfImportacaoManualApiTests(ApiFixture api)
{
    private Task<LojaTeste> NovaAsync() => api.NovaLojaDeServicosAsync();

    private static async Task<(int, string)> Erro(Task<Resposta> r) { var x = await r; return (x.Codigo, x.Erro); }

    [FatoIntegracao]
    public async Task Exige_login_e_so_admin_lanca_edita_e_desfaz_mas_qualquer_logado_ve_historico_e_detalhe()
    {
        var loja = await NovaAsync();
        var operador = await api.OperadorAsync(loja);
        var fornecedor = await loja.FornecedorIdAsync();
        var produto = await loja.ProdutoPelaApiAsync("Camisa", 1m);
        var nf = await loja.NfManualAsync(fornecedor, "10", ItemManual(produto, 1m));

        Assert.Equal(401, (await loja.Site.Get($"{Rota}/historico")).Codigo);
        Assert.Equal(401, (await loja.Site.Get($"{Rota}/{nf}")).Codigo);
        Assert.Equal(200, (await operador.Get($"{Rota}/historico")).Codigo);
        Assert.Equal(200, (await operador.Get($"{Rota}/{nf}")).Codigo);
        Assert.Equal(403, (await operador.Post($"{Rota}/manual", new { })).Codigo);
        Assert.Equal(403, (await operador.Put($"{Rota}/{nf}/editar", new { })).Codigo);
        Assert.Equal(403, (await operador.Post($"{Rota}/{nf}/desfazer")).Codigo);
    }

    // ── Lançar ───────────────────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Lancar_valida_numero_itens_fornecedor_duplicada_e_os_itens_sem_deixar_nada_para_tras()
    {
        var loja = await NovaAsync();
        var outra = await NovaAsync();
        var fornecedor = await loja.FornecedorIdAsync("Alfa");
        var fornecedorDeOutra = await outra.FornecedorIdAsync("Dela");
        var produto = await loja.ProdutoPelaApiAsync("Camisa", 5m);
        var deOutra = await outra.ProdutoPelaApiAsync("Dela", 5m);
        var comVariacao = await loja.ProdutoPelaApiAsync("Vestido", 0m);
        await loja.NfManualAsync(fornecedor, "100", ItemManual(produto, 1m));

        Assert.Equal((400, "Informe o número da nota fiscal."), await Erro(loja.LancarManualAsync(fornecedor, "  ", null, null, ItemManual(produto, 1m))));
        Assert.Equal((400, "Adicione ao menos um item."), await Erro(loja.LancarManualAsync(fornecedor, "1", null, null)));
        Assert.Equal((400, "Fornecedor não encontrado."), await Erro(loja.LancarManualAsync(fornecedorDeOutra, "1", null, null, ItemManual(produto, 1m))));
        Assert.Equal((409, "Já existe uma nota 100 lançada para o fornecedor Alfa."), await Erro(loja.LancarManualAsync(fornecedor, " 100 ", null, null, ItemManual(produto, 1m))));
        Assert.Equal((400, "Informe o nome do novo produto."), await Erro(loja.LancarManualAsync(fornecedor, "2", null, null, ItemManual(null, 1m, acao: "novo", nomeBase: " "))));
        Assert.Equal((400, "Produto não informado."), await Erro(loja.LancarManualAsync(fornecedor, "3", null, null, ItemManual(null, 1m))));
        Assert.Equal((400, "Produto não encontrado."), await Erro(loja.LancarManualAsync(fornecedor, "4", null, null, ItemManual(deOutra, 1m))));
        Assert.Equal((400, "Produto não encontrado."), await Erro(loja.LancarManualAsync(fornecedor, "5", null, null, ItemManual(Guid.NewGuid(), 1m))));
        Assert.Equal((400, "Variação não encontrada para 'Vestido'."), await Erro(loja.LancarManualAsync(fornecedor, "6", null, null, ItemManual(comVariacao, 1m, variacao: Guid.NewGuid()))));

        Assert.Equal(6m, (await api.ProdutoDaLojaNoBancoAsync(produto)).Estoque);      // 5 + 1 da nota 100; as recusadas não mexeram
        Assert.Equal(1, (await loja.Painel.Get($"{Rota}/historico")).Json.GetArrayLength());                     // só a nota 100
    }

    [FatoIntegracao]
    public async Task Lancar_com_item_invalido_depois_de_um_produto_novo_nao_deixa_o_produto_novo_nem_estoque_alterado()
    {
        var loja = await NovaAsync();
        var fornecedor = await loja.FornecedorIdAsync("Alfa");
        var produto = await loja.ProdutoPelaApiAsync("Camisa", 5m);

        var r = await loja.LancarManualAsync(fornecedor, "150", null, null,
            ItemManual(produto, 2m),
            ItemManual(null, 1m, 3m, acao: "novo", nomeBase: "Fantasma", categoria: "CatFantasma"),
            ItemManual(Guid.NewGuid(), 1m));

        Assert.Equal((400, "Produto não encontrado."), (r.Codigo, r.Erro));
        Assert.Empty(await api.ProdutosPorNomeNoBancoAsync(loja.Id, "Fantasma"));      // antes ficava criado, com a categoria
        Assert.DoesNotContain(await api.CategoriasNoBancoAsync(loja.Id), c => c.Nome == "CatFantasma");
        Assert.Equal(5m, (await api.ProdutoDaLojaNoBancoAsync(produto)).Estoque);      // e o estoque do 1º item era gravado junto
        Assert.Empty((await loja.Painel.Get($"{Rota}/historico")).Json.EnumerateArray());
    }

    [FatoIntegracao]
    public async Task Lancar_item_existente_soma_o_estoque_troca_o_custo_e_registra_a_nota_e_o_movimento()
    {
        var loja = await NovaAsync();
        var fornecedor = await loja.FornecedorIdAsync("Alfa");
        var produto = await loja.ProdutoPelaApiAsync("Camisa", 5m, custo: 10m, venda: 30m);
        var comVariacao = await loja.ProdutoPelaApiAsync("Vestido", 0m, custo: 20m, venda: 50m);
        var variacao = await loja.VariacaoPelaApiAsync(comVariacao, "M", "Azul", 3);

        var r = await loja.LancarManualAsync(fornecedor, " 200 ", new DateTime(2031, 3, 15, 23, 30, 0), null,
            ItemManual(produto, 4m, custo: 12m), ItemManual(comVariacao, 2m, variacao: variacao));

        Assert.Equal(("Nota fiscal lançada.", 2), (r.Json.GetProperty("mensagem").GetString(), r.Json.GetProperty("produtosAtualizados").GetInt32()));
        var camisa = await api.ProdutoDaLojaNoBancoAsync(produto);
        Assert.Equal((9m, 12m, 30m), (camisa.Estoque, camisa.PrecoCusto, camisa.PrecoVenda));
        Assert.Equal(5, (await api.VariacaoNoBancoDoProdutoAsync(variacao)).Estoque);
        Assert.Equal(0m, (await api.ProdutoDaLojaNoBancoAsync(comVariacao)).Estoque);
        var nf = await api.NfPorNumeroNoBancoAsync(loja.Id, "200");
        Assert.Equal(("manual", null, fornecedor, "Alfa", 2, 88m, 88m, 220m, 6m), (nf.Origem, nf.ChaveAcesso, nf.FornecedorId, nf.NomeFornecedor, nf.QtdItens, nf.ValorTotal,
            nf.ValorCustoTotal, nf.ValorVendaTotal, nf.QuantidadeTotal));          // custo: 4×12 + 2×20 = 88; venda: 4×30 + 2×50 = 220
        Assert.Equal(new DateTime(2031, 3, 15, 0, 0, 0, DateTimeKind.Utc), nf.DataEmissao);
        var movs = (await api.MovimentosDaLojaNoBancoAsync(loja.Id)).Where(m => m.Observacao == "Nota fiscal manual 200").ToList();
        Assert.Equal([4m, 2m], movs.Select(m => m.Quantidade).OrderByDescending(x => x));
    }

    [FatoIntegracao]
    public async Task Lancar_aceita_valor_total_informado_e_cria_produto_novo_com_fracionado_e_categoria()
    {
        var loja = await NovaAsync();
        var fornecedor = await loja.FornecedorIdAsync("Alfa");

        (await loja.LancarManualAsync(fornecedor, "300", null, 999.99m,
            ItemManual(null, 2.5m, 10m, acao: "novo", nomeBase: " Granel ", categoria: "Mercearia", venda: 18m, gtin: "7890000000099", tipoVenda: "fracionado"),
            ItemManual(null, 3m, 4m, acao: "novo", nomeBase: "Unidade simples", cor: "Azul", tamanho: "M", tipoVenda: "qualquer", unidade: "g"))).DeveSerSucesso();

        var granel = Assert.Single(await api.ProdutosPorNomeNoBancoAsync(loja.Id, "Granel"));
        Assert.Equal(("Mercearia", "fracionado", "kg", 2.5m, 10m, 18m, "7890000000099"), (granel.Categoria, granel.TipoVenda, granel.UnidadeMedida, granel.Estoque, granel.PrecoCusto, granel.PrecoVenda, granel.CodigoBarras));
        var simples = Assert.Single(await api.ProdutosPorNomeNoBancoAsync(loja.Id, "Unidade simples"));
        Assert.Equal(("Outro", "unidade", "un", 0m), (simples.Categoria, simples.TipoVenda, simples.UnidadeMedida, simples.Estoque));
        Assert.Equal(3, Assert.Single(await api.VariacoesDoProdutoNoBancoAsync(simples.Id)).Estoque);
        var nf = await api.NfPorNumeroNoBancoAsync(loja.Id, "300");
        Assert.Equal((999.99m, 37m, 5.5m), (nf.ValorTotal, nf.ValorCustoTotal, nf.QuantidadeTotal));      // custo: 2,5×10 + 3×4 = 37; quantidade 2,5 + 3 = 5,5
        Assert.Equal(["Mercearia", "Outro"], (await api.CategoriasNoBancoAsync(loja.Id)).Select(c => c.Nome));
    }

    // ── Detalhe ──────────────────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Detalhe_mostra_os_itens_com_o_custo_atual_e_pula_produto_excluido()
    {
        var loja = await NovaAsync();
        var outra = await NovaAsync();
        var fornecedor = await loja.FornecedorIdAsync("Alfa");
        var produto = await loja.ProdutoPelaApiAsync("Camisa", 5m, custo: 10m);
        var comVariacao = await loja.ProdutoPelaApiAsync("Vestido", 0m, custo: 20m);
        var variacao = await loja.VariacaoPelaApiAsync(comVariacao, "M", "Azul", 3);
        var apagado = await loja.ProdutoPelaApiAsync("Apagado", 1m);
        var nf = await loja.NfManualAsync(fornecedor, "400", ItemManual(produto, 4m, custo: 12m), ItemManual(comVariacao, 2m, variacao: variacao), ItemManual(apagado, 1m));
        (await loja.Painel.Delete($"/api/produtos/{apagado}")).DeveSerSucesso();

        var d = (await loja.Painel.Get($"{Rota}/{nf}")).Json;

        Assert.Equal((nf, fornecedor, "400"), (d.GetProperty("id").GetGuid(), d.GetProperty("fornecedorId").GetGuid(), d.GetProperty("numeroNf").GetString()));
        var itens = d.GetProperty("itens").EnumerateArray().ToList();
        Assert.Equal(2, itens.Count);                                                                          // o produto excluído some da lista
        Assert.Equal(("Camisa", 4m, 12m, JsonValueKind.Null), (itens[0].GetProperty("nomeProduto").GetString()!, itens[0].GetProperty("quantidade").GetDecimal(),
            itens[0].GetProperty("precoCusto").GetDecimal(), itens[0].GetProperty("variacaoLabel").ValueKind));
        Assert.Equal(("Azul / M", variacao), (itens[1].GetProperty("variacaoLabel").GetString()!, itens[1].GetProperty("variacaoId").GetGuid()));
        Assert.Equal(404, (await outra.Painel.Get($"{Rota}/{nf}")).Codigo);
        Assert.Equal(404, (await loja.Painel.Get($"{Rota}/{Guid.NewGuid()}")).Codigo);
    }

    [FatoIntegracao]
    public async Task Detalhe_de_nota_importada_por_xml_e_recusado()
    {
        var loja = await NovaAsync();
        var chave = NovaChave();
        (await loja.ConfirmarAsync(chave, "500", ItemConfirmacao("A", "Tênis Novo", 1m, custo: 10m))).DeveSerSucesso();
        var nf = await api.NfPorNumeroNoBancoAsync(loja.Id, "500");

        Assert.Equal((400, "Apenas notas lançadas manualmente têm detalhe editável."), await Erro(loja.Painel.Get($"{Rota}/{nf.Id}")));
    }

    // ── Editar ───────────────────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Editar_valida_origem_estado_numero_fornecedor_duplicada_e_itens()
    {
        var loja = await NovaAsync();
        var outra = await NovaAsync();
        var fornecedor = await loja.FornecedorIdAsync("Alfa");
        var fornecedorDeOutra = await outra.FornecedorIdAsync("Dela");
        var a = await loja.ProdutoPelaApiAsync("A", 5m);
        var b = await loja.ProdutoPelaApiAsync("B", 5m);
        var nf1 = await loja.NfManualAsync(fornecedor, "601", ItemManual(a, 2m), ItemManual(b, 3m));
        await loja.NfManualAsync(fornecedor, "602", ItemManual(a, 1m));
        (await loja.ConfirmarAsync(NovaChave(), "603", ItemConfirmacao("X", "Xml", 1m, custo: 1m))).DeveSerSucesso();
        var xml = (await api.NfPorNumeroNoBancoAsync(loja.Id, "603")).Id;
        object Edit(Guid f, string numero, params object[] itens) => new { fornecedorId = f, numeroNf = numero, dataEmissao = (DateTime?)null, valorTotal = (decimal?)null, itens };
        object It(Guid p, decimal q, decimal? c = null) => new { produtoId = p, variacaoId = (Guid?)null, quantidade = q, precoCusto = c };

        Assert.Equal(404, (await loja.Painel.Put($"{Rota}/{Guid.NewGuid()}/editar", Edit(fornecedor, "1", It(a, 1m)))).Codigo);
        Assert.Equal(404, (await outra.Painel.Put($"{Rota}/{nf1}/editar", Edit(fornecedorDeOutra, "1", It(a, 1m)))).Codigo);
        Assert.Equal((400, "Apenas notas lançadas manualmente podem ser editadas."), await Erro(loja.Painel.Put($"{Rota}/{xml}/editar", Edit(fornecedor, "1", It(a, 1m)))));
        Assert.Equal((400, "Informe o número da nota fiscal."), await Erro(loja.Painel.Put($"{Rota}/{nf1}/editar", Edit(fornecedor, " ", It(a, 2m), It(b, 3m)))));
        Assert.Equal((400, "Fornecedor não encontrado."), await Erro(loja.Painel.Put($"{Rota}/{nf1}/editar", Edit(fornecedorDeOutra, "601", It(a, 2m), It(b, 3m)))));
        Assert.Equal((409, "Já existe uma nota 602 lançada para o fornecedor Alfa."), await Erro(loja.Painel.Put($"{Rota}/{nf1}/editar", Edit(fornecedor, "602", It(a, 2m), It(b, 3m)))));
        Assert.Equal((400, "Não é possível adicionar ou remover itens ao editar — desfaça e lance novamente."), await Erro(loja.Painel.Put($"{Rota}/{nf1}/editar", Edit(fornecedor, "601", It(a, 2m)))));
        Assert.Equal((400, "Os itens editados não correspondem aos itens originais desta nota."), await Erro(loja.Painel.Put($"{Rota}/{nf1}/editar", Edit(fornecedor, "601", It(a, 2m), It(Guid.NewGuid(), 3m)))));

        Assert.Equal(200, (await loja.Painel.Put($"{Rota}/{nf1}/editar", Edit(fornecedor, "601", It(a, 2m), It(b, 3m)))).Codigo);    // o próprio número é aceito
        (await loja.Painel.Post($"{Rota}/{nf1}/desfazer")).DeveSerSucesso();
        Assert.Equal((400, "Esta importação foi desfeita e não pode ser editada."), await Erro(loja.Painel.Put($"{Rota}/{nf1}/editar", Edit(fornecedor, "601", It(a, 2m), It(b, 3m)))));
    }

    [FatoIntegracao]
    public async Task Editar_ajusta_o_estoque_pela_diferenca_troca_custo_numero_e_movimentos_e_refaz_os_totais()
    {
        var loja = await NovaAsync();
        var alfa = await loja.FornecedorIdAsync("Alfa");
        var beta = await loja.FornecedorIdAsync("Beta");
        var a = await loja.ProdutoPelaApiAsync("A", 5m, custo: 10m, venda: 30m);
        var comVariacao = await loja.ProdutoPelaApiAsync("Vestido", 0m, custo: 20m, venda: 50m);
        var variacao = await loja.VariacaoPelaApiAsync(comVariacao, "M", "Azul", 3);
        var nf = await loja.NfManualAsync(alfa, "700", ItemManual(a, 4m), ItemManual(comVariacao, 2m, variacao: variacao));
        // depois do lançamento: A = 9, variação = 5

        var r = await loja.Painel.Put($"{Rota}/{nf}/editar", new
        {
            fornecedorId = beta, numeroNf = " 701 ", dataEmissao = new DateTime(2031, 6, 1, 15, 0, 0), valorTotal = (decimal?)null,
            itens = new object[]
            {
                new { produtoId = a, variacaoId = (Guid?)null, quantidade = 10m, precoCusto = (decimal?)11m },
                new { produtoId = comVariacao, variacaoId = (Guid?)variacao, quantidade = 1m, precoCusto = (decimal?)null },
            },
        });

        Assert.Equal("Nota fiscal atualizada.", r.Json.GetProperty("mensagem").GetString());
        var produto = await api.ProdutoDaLojaNoBancoAsync(a);
        Assert.Equal((15m, 11m), (produto.Estoque, produto.PrecoCusto));                                   // 9 + (10 - 4)
        Assert.Equal(4, (await api.VariacaoNoBancoDoProdutoAsync(variacao)).Estoque);                      // 5 + (1 - 2)
        var n = await api.NfNoBancoAsync(nf);
        Assert.Equal(("701", beta, "Beta", 130m, 130m, 350m, 11m), (n.NumeroNf, n.FornecedorId, n.NomeFornecedor, n.ValorTotal, n.ValorCustoTotal, n.ValorVendaTotal, n.QuantidadeTotal));
        Assert.Equal(new DateTime(2031, 6, 1, 0, 0, 0, DateTimeKind.Utc), n.DataEmissao);
        var movs = (await api.MovimentosDaLojaNoBancoAsync(loja.Id)).Where(m => m.Observacao == "Nota fiscal manual 701").ToList();
        Assert.Equal(2, movs.Count);
        Assert.Equal(10m, movs.Single(m => m.ProdutoId == a).Quantidade);
        Assert.DoesNotContain(await api.MovimentosDaLojaNoBancoAsync(loja.Id), m => m.Observacao == "Nota fiscal manual 700");
        // o detalhe passa a mostrar as quantidades novas
        Assert.Equal([10m, 1m], (await loja.Painel.Get($"{Rota}/{nf}")).Json.GetProperty("itens").EnumerateArray().Select(i => i.GetProperty("quantidade").GetDecimal()));
    }

    [FatoIntegracao]
    public async Task Editar_nao_deixa_o_estoque_negativo_e_o_valor_total_informado_vale()
    {
        var loja = await NovaAsync();
        var fornecedor = await loja.FornecedorIdAsync("Alfa");
        var a = await loja.ProdutoPelaApiAsync("A", 0m, custo: 10m);
        var nf = await loja.NfManualAsync(fornecedor, "800", ItemManual(a, 6m));
        await loja.Painel.Post("/api/estoque/ajuste", new { produtoId = a, tipo = "ajuste", quantidade = 2m });      // sobrou menos do que a nota trouxe

        (await loja.Painel.Put($"{Rota}/{nf}/editar", new { fornecedorId = fornecedor, numeroNf = "800", dataEmissao = (DateTime?)null, valorTotal = 77m,
            itens = new object[] { new { produtoId = a, variacaoId = (Guid?)null, quantidade = 1m, precoCusto = (decimal?)null } } })).DeveSerSucesso();

        Assert.Equal(0m, (await api.ProdutoDaLojaNoBancoAsync(a)).Estoque);                                  // 2 + (1 - 6) = -3, limitado a zero
        var nfBanco = await api.NfNoBancoAsync(nf);
        Assert.Equal((77m, 10m), (nfBanco.ValorTotal, nfBanco.ValorCustoTotal));
        Assert.Null(nfBanco.DataEmissao);
    }

    // ── Histórico e desfazer manual ──────────────────────────────────────────

    [FatoIntegracao]
    public async Task Historico_lista_xml_e_manual_do_mais_novo_ao_mais_antigo_com_desfeitas_e_isola_por_loja()
    {
        var loja = await NovaAsync();
        var outra = await NovaAsync();
        var fornecedor = await loja.FornecedorIdAsync("Alfa");
        var produto = await loja.ProdutoPelaApiAsync("A", 1m);
        (await loja.ConfirmarAsync(NovaChave(), "900", ItemConfirmacao("A", "Tênis Novo", 2m, custo: 10m, venda: 20m))).DeveSerSucesso();
        var manual = await loja.NfManualAsync(fornecedor, "901", ItemManual(produto, 3m));
        (await loja.Painel.Post($"{Rota}/{manual}/desfazer")).DeveSerSucesso();
        await outra.NfManualAsync(await outra.FornecedorIdAsync("Dela"), "902", ItemManual(await outra.ProdutoPelaApiAsync("D", 1m), 1m));

        var lista = (await loja.Painel.Get($"{Rota}/historico")).Json.EnumerateArray().ToList();

        Assert.Equal(["901", "900"], lista.Select(n => n.GetProperty("numeroNf").GetString()));
        Assert.Equal(("manual", true, fornecedor), (lista[0].GetProperty("origem").GetString()!, lista[0].GetProperty("desfeita").GetBoolean(), lista[0].GetProperty("fornecedorId").GetGuid()));
        Assert.Equal(("xml", false, JsonValueKind.Null, 20m, 20m, 40m, 2m, 1), (lista[1].GetProperty("origem").GetString()!, lista[1].GetProperty("desfeita").GetBoolean(),
            lista[1].GetProperty("fornecedorId").ValueKind, lista[1].GetProperty("valorTotal").GetDecimal(), lista[1].GetProperty("valorCustoTotal").GetDecimal(),
            lista[1].GetProperty("valorVendaTotal").GetDecimal(), lista[1].GetProperty("quantidadeTotal").GetDecimal(), lista[1].GetProperty("qtdItens").GetInt32()));
        Assert.Equal("Distribuidora Alfa", lista[1].GetProperty("nomeFornecedor").GetString());
        Assert.Empty((await (await NovaAsync()).Painel.Get($"{Rota}/historico")).Json.EnumerateArray());
    }

    [FatoIntegracao]
    public async Task Desfazer_nota_manual_devolve_o_estoque_apaga_os_movimentos_dela_e_libera_o_numero()
    {
        var loja = await NovaAsync();
        var fornecedor = await loja.FornecedorIdAsync("Alfa");
        var produto = await loja.ProdutoPelaApiAsync("A", 5m);
        var nf = await loja.NfManualAsync(fornecedor, "950", ItemManual(produto, 4m), ItemManual(null, 2m, 5m, acao: "novo", nomeBase: "Novo da nota", categoria: "Cat"));
        Assert.Equal(9m, (await api.ProdutoDaLojaNoBancoAsync(produto)).Estoque);

        (await loja.Painel.Post($"{Rota}/{nf}/desfazer")).DeveSerSucesso();

        Assert.Equal(5m, (await api.ProdutoDaLojaNoBancoAsync(produto)).Estoque);
        Assert.Empty(await api.ProdutosPorNomeNoBancoAsync(loja.Id, "Novo da nota"));
        Assert.DoesNotContain(await api.MovimentosDaLojaNoBancoAsync(loja.Id), m => m.Observacao == "Nota fiscal manual 950");
        Assert.Equal(200, (await loja.LancarManualAsync(fornecedor, "950", null, null, ItemManual(produto, 1m))).Codigo);      // o número volta a ficar livre
    }

    [FatoIntegracao]
    public async Task Numero_da_nota_duplicada_e_comparado_sem_diferenciar_maiuscula()
    {
        var loja = await NovaAsync();
        var fornecedor = await loja.FornecedorIdAsync("Alfa");
        var produto = await loja.ProdutoPelaApiAsync("Camisa", 5m);
        await loja.NfManualAsync(fornecedor, "a1", ItemManual(produto, 1m));

        Assert.Equal((409, "Já existe uma nota A1 lançada para o fornecedor Alfa."), await Erro(loja.LancarManualAsync(fornecedor, "A1", null, null, ItemManual(produto, 1m))));
    }

    private Task DefinirEstoqueDaVariacaoAsync(Guid variacao, int estoque) =>
        api.NoBancoAsync(db => db.ProdutoVariacoes.Where(v => v.Id == variacao).ExecuteUpdateAsync(s => s.SetProperty(v => v.Estoque, estoque)));

    [FatoIntegracao]
    public async Task Editar_e_desfazer_nao_deixam_o_estoque_da_variacao_negativo()
    {
        var loja = await NovaAsync();
        var fornecedor = await loja.FornecedorIdAsync("Alfa");
        var produto = await loja.ProdutoPelaApiAsync("Vestido", 0m);
        var variacao = await loja.VariacaoPelaApiAsync(produto, "M", "Azul", 0);
        var nf = await loja.NfManualAsync(fornecedor, "31", ItemManual(produto, 6m, variacao: variacao));
        await DefinirEstoqueDaVariacaoAsync(variacao, 2);                  // sobrou menos do que a nota trouxe

        (await loja.Painel.Put($"{Rota}/{nf}/editar", new { fornecedorId = fornecedor, numeroNf = "31", dataEmissao = (DateTime?)null, valorTotal = (decimal?)null,
            itens = new object[] { new { produtoId = produto, variacaoId = (Guid?)variacao, quantidade = 1m, precoCusto = (decimal?)null } } })).DeveSerSucesso();
        Assert.Equal(0, (await api.VariacaoNoBancoDoProdutoAsync(variacao)).Estoque);          // 2 + (1 - 6) = -3, limitado a zero

        await DefinirEstoqueDaVariacaoAsync(variacao, 0);
        (await loja.Painel.Post($"{Rota}/{nf}/desfazer")).DeveSerSucesso();
        Assert.Equal(0, (await api.VariacaoNoBancoDoProdutoAsync(variacao)).Estoque);          // 0 - 1 = -1, limitado a zero
    }

    [FatoIntegracao]
    public async Task Desfazer_nao_apaga_os_movimentos_de_outra_loja_com_o_mesmo_numero_de_nota()
    {
        var loja = await NovaAsync();
        var outra = await NovaAsync();
        var nf = await loja.NfManualAsync(await loja.FornecedorIdAsync("Alfa"), "777", ItemManual(await loja.ProdutoPelaApiAsync("A", 1m), 1m));
        await outra.NfManualAsync(await outra.FornecedorIdAsync("Beta"), "777", ItemManual(await outra.ProdutoPelaApiAsync("B", 1m), 1m));

        (await loja.Painel.Post($"{Rota}/{nf}/desfazer")).DeveSerSucesso();

        Assert.Single(await api.MovimentosDaLojaNoBancoAsync(outra.Id), m => m.Observacao == "Nota fiscal manual 777");
        Assert.DoesNotContain(await api.MovimentosDaLojaNoBancoAsync(loja.Id), m => m.Observacao == "Nota fiscal manual 777");
    }
}
