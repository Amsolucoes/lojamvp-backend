using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using static LojaApi.Tests.Suporte.CenariosNfImportacao;

namespace LojaApi.Tests.Api.NfImportacaoTeste;

// Importação de NF-e por XML: preview (casamento com os produtos da loja) e confirmação (cria/atualiza produtos, estoque, categoria e mapeamento).
[Collection(ColecaoApi.Nome)]
public class NfImportacaoXmlApiTests(ApiFixture api)
{
    private Task<LojaTeste> NovaAsync() => api.NovaLojaDeServicosAsync();

    private static async Task<(int, string)> Erro(Task<Resposta> r) { var x = await r; return (x.Codigo, x.Erro); }

    [FatoIntegracao]
    public async Task Exige_login_e_so_admin_importa()
    {
        var loja = await NovaAsync();
        var operador = await api.OperadorAsync(loja);

        Assert.Equal(401, (await loja.Site.PostArquivo($"{Rota}/preview", "arquivo", "n.xml", "<a/>")).Codigo);
        Assert.Equal(401, (await loja.Site.Post($"{Rota}/confirmar", new { })).Codigo);
        Assert.Equal(403, (await operador.PostArquivo($"{Rota}/preview", "arquivo", "n.xml", "<a/>")).Codigo);
        Assert.Equal(403, (await operador.Post($"{Rota}/confirmar", new { })).Codigo);
    }

    // ── Preview: validações do arquivo ───────────────────────────────────────

    [FatoIntegracao]
    public async Task Preview_recusa_arquivo_vazio_xml_invalido_sem_nfe_e_chave_fora_do_padrao()
    {
        var loja = await NovaAsync();

        Assert.Equal((400, "Envie o arquivo XML da nota fiscal."), await Erro(loja.PreviewAsync("")));
        Assert.Equal((400, "Arquivo XML inválido."), await Erro(loja.PreviewAsync("isto não é xml")));
        Assert.Equal((400, "XML não parece ser uma NF-e válida."), await Erro(loja.PreviewAsync("<nfeProc xmlns=\"http://www.portalfiscal.inf.br/nfe\"><NFe/></nfeProc>")));
        Assert.Equal((400, "Não foi possível identificar a chave de acesso da nota."), await Erro(loja.PreviewAsync(Xml("123", itens: new ItemXml("A", null, "Peça")))));
    }

    [FatoIntegracao]
    public async Task Preview_le_fornecedor_numero_e_itens_e_trata_sem_gtin()
    {
        var loja = await NovaAsync();
        var chave = NovaChave();

        var r = await loja.PreviewAsync(Xml(chave, "4567", "99887766000155", "Fornecedor Beta",
            new ItemXml("C1", "SEM GTIN", "Tênis Esportivo", "3.0000", "25.50", "76.50"), new ItemXml("C2", "7891000100103", "Meia Soquete", "2", "5", "10")));

        Assert.Equal(200, r.Codigo);
        var j = r.Json;
        Assert.Equal(("99887766000155", "Fornecedor Beta", "4567", chave), (j.GetProperty("cnpjFornecedor").GetString(), j.GetProperty("nomeFornecedor").GetString(),
            j.GetProperty("numeroNf").GetString(), j.GetProperty("chaveAcesso").GetString()));
        var itens = j.GetProperty("itens").EnumerateArray().ToList();
        Assert.Equal(2, itens.Count);
        Assert.Equal(("C1", JsonValueKind.Null, "Tênis Esportivo", 3m, 25.5m, 76.5m), (itens[0].GetProperty("codigoFornecedor").GetString()!, itens[0].GetProperty("gtin").ValueKind,
            itens[0].GetProperty("descricao").GetString()!, itens[0].GetProperty("quantidade").GetDecimal(), itens[0].GetProperty("valorUnitario").GetDecimal(), itens[0].GetProperty("valorTotal").GetDecimal()));
        Assert.Equal("7891000100103", itens[1].GetProperty("gtin").GetString());
        Assert.Equal("novo", itens[0].GetProperty("statusMatch").GetString());
    }

    [FatoIntegracao]
    public async Task Preview_separa_cor_e_tamanho_do_nome_e_sugere_a_categoria_pela_primeira_palavra()
    {
        var loja = await NovaAsync();
        (await loja.Painel.Post("/api/categorias", new { nome = "Blusa", tipoTamanho = "letra", usaTamanho = true, usaCor = true, tamanhosPersonalizados = (string?)null })).DeveSerSucesso();

        var r = await loja.PreviewAsync(Xml(NovaChave(), itens:
        [
            new ItemXml("A", null, "Blusa de Renda Manga Longa Cor:Azul;Tamanho:M"),
            new ItemXml("B", null, "calça jeans Tamanho: 42"),
            new ItemXml("C", null, "MEIA SOQUETE"),
        ]));

        var itens = r.Json.GetProperty("itens").EnumerateArray().ToList();
        Assert.Equal(("Blusa de Renda Manga Longa", "Azul", "M", "Blusa", true), (itens[0].GetProperty("nomeBase").GetString()!, itens[0].GetProperty("cor").GetString()!,
            itens[0].GetProperty("tamanho").GetString()!, itens[0].GetProperty("categoriaSugerida").GetString()!, itens[0].GetProperty("categoriaJaExiste").GetBoolean()));
        Assert.Equal(("calça jeans", JsonValueKind.Null, "42", "Calça", false), (itens[1].GetProperty("nomeBase").GetString()!, itens[1].GetProperty("cor").ValueKind,
            itens[1].GetProperty("tamanho").GetString()!, itens[1].GetProperty("categoriaSugerida").GetString()!, itens[1].GetProperty("categoriaJaExiste").GetBoolean()));
        Assert.Equal(("MEIA SOQUETE", "Meia"), (itens[2].GetProperty("nomeBase").GetString()!, itens[2].GetProperty("categoriaSugerida").GetString()!));
    }

    // ── Preview: casamento com os produtos da loja ───────────────────────────

    [FatoIntegracao]
    public async Task Preview_casa_por_gtin_mapeamento_nome_exato_e_sugestao_nessa_ordem()
    {
        var loja = await NovaAsync();
        var outra = await NovaAsync();
        var porGtin = await loja.ProdutoPelaApiAsync("Blusa Cloe", 0m, "7891234567895", "roupas");
        var porMapa = await loja.ProdutoPelaApiAsync("Camisa Polo", 0m, null, "camisas");
        var exato = await loja.ProdutoPelaApiAsync("Vestido Longo Floral");
        var parecido = await loja.ProdutoPelaApiAsync("Calça Jeans Slim Azul");
        await outra.ProdutoPelaApiAsync("Tênis Esportivo", 0m, "7890000000001");
        await api.MapearAsync(loja.Id, CnpjFornecedor, "ABC", porMapa);

        var r = await loja.PreviewAsync(Xml(NovaChave(), itens:
        [
            new ItemXml("X1", "7891234567895", "Qualquer nome"),
            new ItemXml("ABC", null, "Outro nome do fornecedor"),
            new ItemXml("X3", null, "vestido longo floral"),
            new ItemXml("X4", null, "Calça Jeans Reta"),
            new ItemXml("X5", "7890000000001", "Tênis Esportivo"),
        ]));

        var itens = r.Json.GetProperty("itens").EnumerateArray().ToList();
        string St(int i) => itens[i].GetProperty("statusMatch").GetString()!;
        Guid? Id(int i) => itens[i].GetProperty("produtoSugeridoId").ValueKind == JsonValueKind.Null ? null : itens[i].GetProperty("produtoSugeridoId").GetGuid();
        Assert.Equal(["gtin", "mapeamento", "nome_exato", "sugestao", "novo"], new[] { St(0), St(1), St(2), St(3), St(4) });
        Assert.Equal([porGtin, porMapa, exato, parecido, null], new[] { Id(0), Id(1), Id(2), Id(3), Id(4) });
        Assert.Equal("Blusa Cloe", itens[0].GetProperty("produtoSugeridoNome").GetString());
        Assert.Equal(("roupas", "camisas"), (itens[0].GetProperty("categoriaSugerida").GetString(), itens[1].GetProperty("categoriaSugerida").GetString()));    // casado: usa a categoria do produto
    }

    [FatoIntegracao]
    public async Task Preview_informa_se_a_variacao_ja_existe_e_o_estoque_dela()
    {
        var loja = await NovaAsync();
        var produto = await loja.ProdutoPelaApiAsync("Vestido Longo Floral");
        await loja.VariacaoPelaApiAsync(produto, "M", "Azul", 4);
        var inativa = await loja.VariacaoPelaApiAsync(produto, "G", "Azul", 9);
        (await loja.Painel.Delete($"/api/produtos/{produto}/variacoes/{inativa}")).DeveSerSucesso();

        var r = await loja.PreviewAsync(Xml(NovaChave(), itens:
        [
            new ItemXml("A", null, "Vestido Longo Floral Cor:azul;Tamanho:m"),
            new ItemXml("B", null, "Vestido Longo Floral Cor:Azul;Tamanho:G"),
            new ItemXml("C", null, "Vestido Longo Floral"),
        ]));

        var itens = r.Json.GetProperty("itens").EnumerateArray().ToList();
        Assert.Equal((true, 4), (itens[0].GetProperty("variacaoJaExiste").GetBoolean(), itens[0].GetProperty("estoqueVariacaoAtual").GetInt32()));      // compara sem diferenciar maiúscula
        Assert.Equal((false, JsonValueKind.Null), (itens[1].GetProperty("variacaoJaExiste").GetBoolean(), itens[1].GetProperty("estoqueVariacaoAtual").ValueKind));   // variação inativa não conta
        Assert.False(itens[2].GetProperty("variacaoJaExiste").GetBoolean());
    }

    [FatoIntegracao]
    public async Task Preview_so_considera_produtos_ativos_e_mapeamentos_da_propria_loja()
    {
        var loja = await NovaAsync();
        var outra = await NovaAsync();
        var inativo = await loja.ProdutoPelaApiAsync("Produto Inativo", 0m, "7891111111111", ativo: false);
        var dela = await outra.ProdutoPelaApiAsync("Produto Dela");
        await api.MapearAsync(outra.Id, CnpjFornecedor, "ZZ", dela);

        var r = await loja.PreviewAsync(Xml(NovaChave(), itens: [new ItemXml("A", "7891111111111", "Produto Inativo"), new ItemXml("ZZ", null, "Produto Dela")]));

        Assert.Equal(["novo", "novo"], r.Json.GetProperty("itens").EnumerateArray().Select(i => i.GetProperty("statusMatch").GetString()));
        Assert.Equal(404, (await loja.Painel.Get($"/api/produtos/{dela}")).Codigo);
        Assert.NotEqual(Guid.Empty, inativo);
    }

    [FatoIntegracao]
    public async Task Preview_de_nota_ja_importada_da_409_com_a_data_e_a_quantidade_mas_nota_desfeita_pode_voltar()
    {
        var loja = await NovaAsync();
        var chave = NovaChave();
        var xml = Xml(chave, "777", itens: [new ItemXml("A", null, "Tênis Esportivo")]);
        (await loja.ConfirmarAsync(chave, "777", ItemConfirmacao("A", "Tênis Esportivo", 2m, custo: 10m))).DeveSerSucesso();

        var repetida = await loja.PreviewAsync(xml);

        Assert.Equal(409, repetida.Codigo);
        Assert.StartsWith("Esta nota já foi importada em ", repetida.Erro);
        Assert.EndsWith("(1 item(ns)).", repetida.Erro);

        var nf = await api.NfPorNumeroNoBancoAsync(loja.Id, "777");
        (await loja.Painel.Post($"{Rota}/{nf.Id}/desfazer")).DeveSerSucesso();
        Assert.Equal(200, (await loja.PreviewAsync(xml)).Codigo);
        Assert.Equal(200, (await (await NovaAsync()).PreviewAsync(xml)).Codigo);        // outra loja pode importar a mesma nota
    }

    // ── Confirmar ────────────────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Confirmar_item_novo_cria_categoria_produto_movimento_mapeamento_e_registro_da_nota()
    {
        var loja = await NovaAsync();
        var chave = NovaChave();

        var r = await loja.ConfirmarAsync(chave, "123", ItemConfirmacao("T1", "Tênis Esportivo", 5m, custo: 30m, venda: 60m, categoria: "Calçados", gtin: "7891000100103"));

        Assert.Equal(200, r.Codigo);
        Assert.Equal(("Importação concluída.", 1, 0, 1), (r.Json.GetProperty("mensagem").GetString(), r.Json.GetProperty("produtosNovos").GetInt32(),
            r.Json.GetProperty("produtosAtualizados").GetInt32(), r.Json.GetProperty("categoriasCriadas").GetInt32()));
        var categoria = Assert.Single(await api.CategoriasNoBancoAsync(loja.Id));
        Assert.Equal(("Calçados", 0, false, false), (categoria.Nome, categoria.Ordem, categoria.UsaTamanho, categoria.UsaCor));
        var produto = Assert.Single(await api.ProdutosPorNomeNoBancoAsync(loja.Id, "Tênis Esportivo"));
        Assert.Equal(("Calçados", 5m, 30m, 60m, "7891000100103"), (produto.Categoria, produto.Estoque, produto.PrecoCusto, produto.PrecoVenda, produto.CodigoBarras));
        var mov = Assert.Single(await api.MovimentosDaLojaNoBancoAsync(loja.Id));
        Assert.Equal((produto.Id, "entrada", 5m, "Importação NF 123"), (mov.ProdutoId, mov.Tipo, mov.Quantidade, mov.Observacao));
        var mapa = Assert.Single(await api.MapeamentosNoBancoAsync(loja.Id));
        Assert.Equal((CnpjFornecedor, "T1", produto.Id), (mapa.CnpjFornecedor, mapa.CodigoFornecedor, mapa.ProdutoId));
        var nf = await api.NfPorNumeroNoBancoAsync(loja.Id, "123");
        Assert.Equal((chave, "xml", 1, 150m, 150m, 300m, 5m, false), (nf.ChaveAcesso, nf.Origem, nf.QtdItens, nf.ValorTotal, nf.ValorCustoTotal, nf.ValorVendaTotal, nf.QuantidadeTotal, nf.Desfeita));
        Assert.Equal("Distribuidora Alfa", nf.NomeFornecedor);
    }

    [FatoIntegracao]
    public async Task Confirmar_item_novo_sem_categoria_ou_preco_usa_outro_e_o_custo_como_venda()
    {
        var loja = await NovaAsync();

        (await loja.ConfirmarAsync(NovaChave(), "1", ItemConfirmacao("A", "Sem preço", 2m), ItemConfirmacao("B", "Só custo", 3m, custo: 8m, categoria: "  "))).DeveSerSucesso();

        var semPreco = Assert.Single(await api.ProdutosPorNomeNoBancoAsync(loja.Id, "Sem preço"));
        var soCusto = Assert.Single(await api.ProdutosPorNomeNoBancoAsync(loja.Id, "Só custo"));
        Assert.Equal(("Outro", 0m, 0m), (semPreco.Categoria, semPreco.PrecoCusto, semPreco.PrecoVenda));
        Assert.Equal(("Outro", 8m, 8m), (soCusto.Categoria, soCusto.PrecoCusto, soCusto.PrecoVenda));
        Assert.Equal(["Outro"], (await api.CategoriasNoBancoAsync(loja.Id)).Select(c => c.Nome));                 // uma só categoria, mesmo com dois itens
    }

    [FatoIntegracao]
    public async Task Confirmar_item_novo_com_cor_ou_tamanho_cria_a_variacao_com_o_estoque_e_marca_a_grade_na_categoria()
    {
        var loja = await NovaAsync();

        (await loja.ConfirmarAsync(NovaChave(), "2", ItemConfirmacao("A", "Blusa Cloe", 6m, custo: 20m, venda: 50m, categoria: "Blusa", cor: "Azul", tamanho: "M"))).DeveSerSucesso();

        var produto = Assert.Single(await api.ProdutosPorNomeNoBancoAsync(loja.Id, "Blusa Cloe"));
        var variacao = Assert.Single(await api.VariacoesDoProdutoNoBancoAsync(produto.Id));
        Assert.Equal((0m, "Azul", "M", 6), (produto.Estoque, variacao.Cor, variacao.Tamanho, variacao.Estoque));
        var categoria = Assert.Single(await api.CategoriasNoBancoAsync(loja.Id));
        Assert.Equal((true, true), (categoria.UsaTamanho, categoria.UsaCor));
        Assert.Equal(6m, Assert.Single(await api.MovimentosDaLojaNoBancoAsync(loja.Id)).Quantidade);
    }

    [FatoIntegracao]
    public async Task Confirmar_item_existente_soma_o_estoque_usa_o_custo_do_cadastro_e_atualiza_o_mapeamento()
    {
        var loja = await NovaAsync();
        var produto = await loja.ProdutoPelaApiAsync("Camisa Polo", 4m, custo: 15m, venda: 40m);
        var outro = await loja.ProdutoPelaApiAsync("Outro produto", 0m);
        await api.MapearAsync(loja.Id, CnpjFornecedor, "P1", outro);

        var r = await loja.ConfirmarAsync(NovaChave(), "55", ItemConfirmacao("P1", "Camisa Polo", 3m, "existente", produto, custo: 999m));

        Assert.Equal((0, 1, 0), (r.Json.GetProperty("produtosNovos").GetInt32(), r.Json.GetProperty("produtosAtualizados").GetInt32(), r.Json.GetProperty("categoriasCriadas").GetInt32()));
        var depois = await api.ProdutoDaLojaNoBancoAsync(produto);
        Assert.Equal((7m, 15m, 40m), (depois.Estoque, depois.PrecoCusto, depois.PrecoVenda));                    // o preço da nota não muda o cadastro
        var mapa = Assert.Single(await api.MapeamentosNoBancoAsync(loja.Id));
        Assert.Equal(produto, mapa.ProdutoId);                                                                     // o mapeamento passa para o produto escolhido agora
        var nf = await api.NfPorNumeroNoBancoAsync(loja.Id, "55");
        Assert.Equal((45m, 120m, 3m), (nf.ValorCustoTotal, nf.ValorVendaTotal, nf.QuantidadeTotal));
        var movimentos = await api.MovimentosDoProdutoNoBancoAsync(produto);
        Assert.Equal(["Estoque inicial", "Importação NF 55"], movimentos.Select(m => m.Observacao));
    }

    [FatoIntegracao]
    public async Task Confirmar_existente_com_variacao_soma_na_que_existe_ou_cria_uma_nova()
    {
        var loja = await NovaAsync();
        var produto = await loja.ProdutoPelaApiAsync("Vestido");
        var existente = await loja.VariacaoPelaApiAsync(produto, "M", "Azul", 4);

        (await loja.ConfirmarAsync(NovaChave(), "9", ItemConfirmacao("A", "Vestido", 2m, "existente", produto, cor: "azul", tamanho: "m"), ItemConfirmacao("B", "Vestido", 5m, "existente", produto, cor: "Preto", tamanho: "G"))).DeveSerSucesso();

        var variacoes = await api.VariacoesDoProdutoNoBancoAsync(produto);
        Assert.Equal(6, variacoes.Single(v => v.Id == existente).Estoque);
        var nova = variacoes.Single(v => v.Id != existente);
        Assert.Equal(("Preto", "G", 5), (nova.Cor, nova.Tamanho, nova.Estoque));
        Assert.Equal(0m, (await api.ProdutoDaLojaNoBancoAsync(produto)).Estoque);                                  // o estoque do produto não muda quando entra por variação
    }

    [FatoIntegracao]
    public async Task Confirmar_recusa_nota_repetida_e_ignora_item_existente_de_outra_loja_ou_sem_produto()
    {
        var loja = await NovaAsync();
        var outra = await NovaAsync();
        var dela = await outra.ProdutoPelaApiAsync("Dela", 5m);
        var chave = NovaChave();

        var r = await loja.ConfirmarAsync(chave, "1", ItemConfirmacao("A", "Dela", 3m, "existente", dela), ItemConfirmacao("B", "Sem produto", 1m, "existente", null));
        Assert.Equal(200, r.Codigo);
        Assert.Equal(0, r.Json.GetProperty("produtosAtualizados").GetInt32());
        Assert.Equal(5m, (await api.ProdutoDaLojaNoBancoAsync(dela)).Estoque);
        Assert.Empty(await api.MovimentosDaLojaNoBancoAsync(loja.Id));
        Assert.Equal(2, (await api.NfPorNumeroNoBancoAsync(loja.Id, "1")).QtdItens);                                // a nota é registrada mesmo assim, com a quantidade de itens do pedido

        var repetida = await loja.ConfirmarAsync(chave, "1", ItemConfirmacao("A", "Qualquer", 1m));
        Assert.Equal((409, "Esta nota já foi importada anteriormente."), (repetida.Codigo, repetida.Erro));
    }

    // ── Desfazer ─────────────────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Desfazer_remove_o_que_a_nota_criou_e_devolve_o_estoque_do_que_ja_existia()
    {
        var loja = await NovaAsync();
        var existente = await loja.ProdutoPelaApiAsync("Camisa Polo", 4m);
        var comVariacao = await loja.ProdutoPelaApiAsync("Vestido");
        var variacao = await loja.VariacaoPelaApiAsync(comVariacao, "M", "Azul", 4);
        (await loja.ConfirmarAsync(NovaChave(), "88",
            ItemConfirmacao("A", "Tênis Novo", 5m, custo: 10m, categoria: "Calçados"),
            ItemConfirmacao("B", "Camisa Polo", 3m, "existente", existente),
            ItemConfirmacao("C", "Vestido", 2m, "existente", comVariacao, cor: "Azul", tamanho: "M"),
            ItemConfirmacao("D", "Vestido", 1m, "existente", comVariacao, cor: "Preto", tamanho: "G"))).DeveSerSucesso();
        var nf = await api.NfPorNumeroNoBancoAsync(loja.Id, "88");

        var r = await loja.Painel.Post($"{Rota}/{nf.Id}/desfazer");

        Assert.Equal("Importação desfeita. Você já pode reimportar esta nota se quiser.", r.Json.GetProperty("mensagem").GetString());
        Assert.Empty(await api.ProdutosPorNomeNoBancoAsync(loja.Id, "Tênis Novo"));
        Assert.Equal(4m, (await api.ProdutoDaLojaNoBancoAsync(existente)).Estoque);
        var variacoes = await api.VariacoesDoProdutoNoBancoAsync(comVariacao);
        Assert.Equal(4, Assert.Single(variacoes).Estoque);                                                         // a variação criada foi removida e a antiga voltou a 4
        Assert.True((await api.CategoriasNoBancoAsync(loja.Id)).Single(c => c.Nome == "Calçados").Ativo);          // comportamento atual: a categoria criada pela nota não é desativada ao desfazer
        Assert.DoesNotContain(await api.MovimentosDaLojaNoBancoAsync(loja.Id), m => m.Observacao == "Importação NF 88");
        Assert.True((await api.NfNoBancoAsync(nf.Id)).Desfeita);
        Assert.Equal(["B", "C", "D"], (await api.MapeamentosNoBancoAsync(loja.Id)).Select(m => m.CodigoFornecedor).OrderBy(x => x));     // ficam os dos produtos que já existiam (o do produto excluído vai junto, em cascata)
        Assert.Equal(variacao, variacoes[0].Id);
    }

    [FatoIntegracao]
    public async Task Desfazer_nao_apaga_produto_criado_pela_nota_se_ja_foi_vendido_e_nao_baixa_estoque_abaixo_de_zero()
    {
        var loja = await NovaAsync();
        var existente = await loja.ProdutoPelaApiAsync("Camisa Polo", 1m);
        (await loja.ConfirmarAsync(NovaChave(), "90", ItemConfirmacao("A", "Tênis Novo", 5m, custo: 10m), ItemConfirmacao("B", "Camisa Polo", 3m, "existente", existente))).DeveSerSucesso();
        var novo = Assert.Single(await api.ProdutosPorNomeNoBancoAsync(loja.Id, "Tênis Novo")).Id;
        await loja.VenderAsync([CenariosVendas.ItemProduto(novo, 1m)]);
        await loja.Painel.Post("/api/estoque/ajuste", new { produtoId = existente, tipo = "ajuste", quantidade = 2m });      // o estoque agora é menor que a quantidade da nota
        var nf = await api.NfPorNumeroNoBancoAsync(loja.Id, "90");

        (await loja.Painel.Post($"{Rota}/{nf.Id}/desfazer")).DeveSerSucesso();

        Assert.Single(await api.ProdutosPorNomeNoBancoAsync(loja.Id, "Tênis Novo"));                                // vendido: continua
        Assert.Equal(0m, (await api.ProdutoDaLojaNoBancoAsync(existente)).Estoque);                                 // 2 - 3 não passa de zero
    }

    [FatoIntegracao]
    public async Task Desfazer_valida_nota_ja_desfeita_e_outra_loja_e_permite_reimportar()
    {
        var loja = await NovaAsync();
        var outra = await NovaAsync();
        var chave = NovaChave();
        (await loja.ConfirmarAsync(chave, "91", ItemConfirmacao("A", "Tênis Novo", 5m, custo: 10m))).DeveSerSucesso();
        var nf = await api.NfPorNumeroNoBancoAsync(loja.Id, "91");

        Assert.Equal(404, (await outra.Painel.Post($"{Rota}/{nf.Id}/desfazer")).Codigo);
        Assert.Equal(404, (await loja.Painel.Post($"{Rota}/{Guid.NewGuid()}/desfazer")).Codigo);
        (await loja.Painel.Post($"{Rota}/{nf.Id}/desfazer")).DeveSerSucesso();
        Assert.Equal((400, "Esta importação já foi desfeita."), await Erro(loja.Painel.Post($"{Rota}/{nf.Id}/desfazer")));

        Assert.Equal(200, (await loja.ConfirmarAsync(chave, "91", ItemConfirmacao("A", "Tênis Novo", 5m, custo: 10m))).Codigo);       // a mesma chave pode voltar depois de desfeita
        Assert.Single(await api.ProdutosPorNomeNoBancoAsync(loja.Id, "Tênis Novo"));
    }

    [FatoIntegracao]
    public async Task A_mesma_chave_de_acesso_pode_ser_importada_por_outra_loja()
    {
        var loja = await NovaAsync();
        var outra = await NovaAsync();
        var chave = NovaChave();
        (await loja.ConfirmarAsync(chave, "1", ItemConfirmacao("A", "Tênis Novo", 1m, custo: 10m))).DeveSerSucesso();

        Assert.Equal(409, (await loja.ConfirmarAsync(chave, "1", ItemConfirmacao("A", "Tênis Novo", 1m, custo: 10m))).Codigo);
        Assert.Equal(200, (await outra.PreviewAsync(Xml(chave, "1", itens: new ItemXml("A", null, "Tênis Novo")))).Codigo);
        (await outra.ConfirmarAsync(chave, "1", ItemConfirmacao("A", "Tênis Novo", 1m, custo: 10m))).DeveSerSucesso();
    }

    [FatoIntegracao]
    public async Task Categoria_nova_reaproveita_a_existente_sem_diferenciar_maiuscula()
    {
        var loja = await NovaAsync();
        (await loja.ConfirmarAsync(NovaChave(), "1", ItemConfirmacao("A", "Produto A", 1m, custo: 1m, categoria: "Mercearia"))).DeveSerSucesso();

        var r = await loja.ConfirmarAsync(NovaChave(), "2", ItemConfirmacao("B", "Produto B", 1m, custo: 1m, categoria: "mercearia"));

        Assert.Equal(0, r.Json.GetProperty("categoriasCriadas").GetInt32());
        Assert.Equal(["Mercearia"], (await api.CategoriasNoBancoAsync(loja.Id)).Select(c => c.Nome));
        Assert.Equal("Mercearia", Assert.Single(await api.ProdutosPorNomeNoBancoAsync(loja.Id, "Produto B")).Categoria);
    }
}
