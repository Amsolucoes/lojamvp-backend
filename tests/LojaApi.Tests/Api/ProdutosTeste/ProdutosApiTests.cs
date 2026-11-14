using System.Text.Json;
using LojaApi.Models;
using Microsoft.EntityFrameworkCore;
using static LojaApi.Tests.Suporte.CenariosProdutos;

namespace LojaApi.Tests.Api.ProdutosTeste;

// Cadastro de produtos: listar, buscar, criar, editar, preços e status em lote, excluir, códigos de barras e variações.
[Collection(ColecaoApi.Nome)]
public class ProdutosApiTests(ApiFixture api)
{
    private Task<LojaTeste> NovaAsync() => api.NovaLojaDeServicosAsync();

    private static async Task<(int, string)> Erro(Task<Resposta> r) { var x = await r; return (x.Codigo, x.Erro); }

    [FatoIntegracao]
    public async Task Exige_login_e_so_admin_altera()
    {
        var loja = await NovaAsync();
        var operador = await api.OperadorAsync(loja);
        var id = await loja.ProdutoPelaApiAsync();

        foreach (var u in new[] { Rota, $"{Rota}/{id}", $"{Rota}/gerar-codigos", $"{Rota}/{id}/variacoes" })
            Assert.Equal(401, (await loja.Site.Get(u)).Codigo);
        Assert.Equal(401, (await loja.Site.Post(Rota, CorpoProduto())).Codigo);

        Assert.Equal(200, (await operador.Get(Rota)).Codigo);
        Assert.Equal(200, (await operador.Get($"{Rota}/gerar-codigos")).Codigo);
        Assert.Equal(403, (await operador.Post(Rota, CorpoProduto())).Codigo);
        Assert.Equal(403, (await operador.Put($"{Rota}/{id}", CorpoProduto())).Codigo);
        Assert.Equal(403, (await operador.Put($"{Rota}/precos-em-lote", new { produtos = Array.Empty<object>() })).Codigo);
        Assert.Equal(403, (await operador.Put($"{Rota}/status-em-lote", new { ids = Array.Empty<Guid>(), ativo = true })).Codigo);
        Assert.Equal(403, (await operador.Delete($"{Rota}/{id}")).Codigo);
        Assert.Equal(403, (await operador.Post($"{Rota}/{id}/variacoes", CorpoVariacao())).Codigo);
    }

    // ── Criar / buscar ───────────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Criar_devolve_201_com_o_produto_e_registra_o_estoque_inicial()
    {
        var loja = await NovaAsync();
        var marca = await loja.MarcaIdAsync("Bosch");
        var fornecedor = await loja.FornecedorIdAsync("Alfa");

        var r = await loja.Painel.Post(Rota, CorpoProduto("Pastilha", "freios", 30m, 90m, 7m, 2m, "789", true, "fracionado", "kg", marca, fornecedor, "dianteira"));

        Assert.Equal(201, r.Codigo);
        var j = r.Json;
        Assert.Equal(("Pastilha", "dianteira", "freios", 30m, 90m, 7m, 2m, "789", true), (j.GetProperty("nome").GetString(), j.GetProperty("descricao").GetString(),
            j.GetProperty("categoria").GetString(), j.GetProperty("precoCusto").GetDecimal(), j.GetProperty("precoVenda").GetDecimal(), j.GetProperty("estoque").GetDecimal(),
            j.GetProperty("estoqueMinimo").GetDecimal(), j.GetProperty("codigoBarras").GetString(), j.GetProperty("ativo").GetBoolean()));
        Assert.Equal(("fracionado", "kg", marca, "Bosch", fornecedor, "Alfa"), (j.GetProperty("tipoVenda").GetString(), j.GetProperty("unidadeMedida").GetString(),
            j.GetProperty("marcaId").GetGuid(), j.GetProperty("nomeMarca").GetString(), j.GetProperty("fornecedorId").GetGuid(), j.GetProperty("nomeFornecedor").GetString()));
        Assert.Empty(j.GetProperty("variacoes").EnumerateArray());
        Assert.EndsWith($"{Rota}/{j.GetProperty("id").GetGuid()}", r.Local!, StringComparison.OrdinalIgnoreCase);

        var mov = Assert.Single(await api.MovimentosDoProdutoNoBancoAsync(j.GetProperty("id").GetGuid()));
        Assert.Equal(("entrada", 7m, "Estoque inicial"), (mov.Tipo, mov.Quantidade, mov.Observacao));
        Assert.Equal(loja.Id, (await api.ProdutoDaLojaNoBancoAsync(j.GetProperty("id").GetGuid())).LojaId);
    }

    [FatoIntegracao]
    public async Task Criar_sem_estoque_nao_gera_movimento_e_codigo_repetido_na_loja_da_409()
    {
        var loja = await NovaAsync();
        var outra = await NovaAsync();
        var semEstoque = await loja.ProdutoPelaApiAsync("Sem estoque", 0m, "111");

        Assert.Empty(await api.MovimentosDoProdutoNoBancoAsync(semEstoque));
        var repetido = await loja.Painel.Post(Rota, CorpoProduto("Outro", codigo: "111"));
        Assert.Equal((409, "O código de barras '111' já está cadastrado em outro produto da sua loja."), (repetido.Codigo, repetido.Erro));
        Assert.Equal(201, (await outra.Painel.Post(Rota, CorpoProduto("Dela", codigo: "111"))).Codigo);       // outra loja pode repetir
        Assert.Equal(201, (await loja.Painel.Post(Rota, CorpoProduto("Sem código", codigo: null))).Codigo);
    }

    [FatoIntegracao]
    public async Task Buscar_soma_o_estoque_das_variacoes_ativas_e_404_para_outra_loja()
    {
        var loja = await NovaAsync();
        var outra = await NovaAsync();
        var id = await loja.ProdutoPelaApiAsync("Camisa", 9m);
        await loja.VariacaoPelaApiAsync(id, "P", "Azul", 2);
        var grande = await loja.VariacaoPelaApiAsync(id, "G", "Azul", 3);

        var d = (await loja.Painel.Get($"{Rota}/{id}")).Json;
        Assert.Equal(5m, d.GetProperty("estoque").GetDecimal());                              // variações ativas somadas, no lugar do estoque do produto
        Assert.Equal(2, d.GetProperty("variacoes").GetArrayLength());

        (await loja.Painel.Delete($"{Rota}/{id}/variacoes/{grande}")).DeveSerSucesso();       // desativa a variação G
        var depois = (await loja.Painel.Get($"{Rota}/{id}")).Json;
        Assert.Equal(2m, depois.GetProperty("estoque").GetDecimal());
        Assert.Equal(1, depois.GetProperty("variacoes").GetArrayLength());

        Assert.Equal(404, (await outra.Painel.Get($"{Rota}/{id}")).Codigo);
        Assert.Equal(404, (await loja.Painel.Get($"{Rota}/{Guid.NewGuid()}")).Codigo);
    }

    [FatoIntegracao]
    public async Task Buscar_sem_variacoes_ativas_usa_o_estoque_do_produto()
    {
        var loja = await NovaAsync();
        var id = await loja.ProdutoPelaApiAsync("Camisa", 9m);
        var v = await loja.VariacaoPelaApiAsync(id, "P", "Azul", 4);
        (await loja.Painel.Delete($"{Rota}/{id}/variacoes/{v}")).DeveSerSucesso();

        Assert.Equal(9m, (await loja.Painel.Get($"{Rota}/{id}")).Json.GetProperty("estoque").GetDecimal());
    }

    // ── Listar ───────────────────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Listar_filtra_por_busca_categoria_status_e_marca_ordena_por_nome_e_isola_por_loja()
    {
        var loja = await NovaAsync();
        var outra = await NovaAsync();
        var marca = await loja.MarcaIdAsync("Bosch");
        await loja.ProdutoPelaApiAsync("Zebra", 1m, "999", "outro", marca: marca);
        await loja.ProdutoPelaApiAsync("Abacaxi", 1m, null, "frutas");
        await loja.ProdutoPelaApiAsync("Mamão", 1m, "123456", "frutas", ativo: false);
        await outra.ProdutoPelaApiAsync("Da outra");

        string[] Nomes(Resposta r) => r.Json.EnumerateArray().Select(p => p.GetProperty("nome").GetString()!).ToArray();

        Assert.Equal(["Abacaxi", "Mamão", "Zebra"], Nomes(await loja.Painel.Get(Rota)));
        Assert.Equal(["Abacaxi"], Nomes(await loja.Painel.Get($"{Rota}?busca=ABAC")));            // sem diferenciar maiúscula
        Assert.Equal(["Mamão"], Nomes(await loja.Painel.Get($"{Rota}?busca=3456")));              // por código de barras
        Assert.Equal(["Abacaxi", "Mamão"], Nomes(await loja.Painel.Get($"{Rota}?categoria=frutas")));
        Assert.Equal(["Mamão"], Nomes(await loja.Painel.Get($"{Rota}?ativo=false")));
        Assert.Equal(["Zebra"], Nomes(await loja.Painel.Get($"{Rota}?marcaId={marca}")));
        Assert.Equal(["Da outra"], Nomes(await outra.Painel.Get(Rota)));
    }

    // ── Atualizar ────────────────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Atualizar_troca_os_campos_mas_nao_o_estoque()
    {
        var loja = await NovaAsync();
        var outra = await NovaAsync();
        var marca = await loja.MarcaIdAsync("Bosch");
        var id = await loja.ProdutoPelaApiAsync("Velho", 5m, "111");
        var antes = await api.ProdutoDaLojaNoBancoAsync(id);

        var r = await loja.Painel.Put($"{Rota}/{id}", CorpoProduto("Novo", "outra", 11m, 22m, 999m, 4m, "222", false, "fracionado", "kg", marca, null, "desc"));

        Assert.Equal(200, r.Codigo);
        Assert.Equal(("Novo", 11m, 22m, 5m, 4m, "222", false), (r.Json.GetProperty("nome").GetString(), r.Json.GetProperty("precoCusto").GetDecimal(),
            r.Json.GetProperty("precoVenda").GetDecimal(), r.Json.GetProperty("estoque").GetDecimal(), r.Json.GetProperty("estoqueMinimo").GetDecimal(),
            r.Json.GetProperty("codigoBarras").GetString(), r.Json.GetProperty("ativo").GetBoolean()));
        var depois = await api.ProdutoDaLojaNoBancoAsync(id);
        Assert.Equal((5m, "fracionado", "kg", marca, "desc"), (depois.Estoque, depois.TipoVenda, depois.UnidadeMedida, depois.MarcaId, depois.Descricao));
        Assert.True(depois.AtualizadoEm > antes.AtualizadoEm);
        Assert.Equal(404, (await outra.Painel.Put($"{Rota}/{id}", CorpoProduto())).Codigo);
        Assert.Equal(404, (await loja.Painel.Put($"{Rota}/{Guid.NewGuid()}", CorpoProduto())).Codigo);
    }

    [FatoIntegracao]
    public async Task Atualizar_recusa_codigo_de_outro_produto_da_loja_mas_aceita_o_proprio()
    {
        var loja = await NovaAsync();
        var a = await loja.ProdutoPelaApiAsync("A", 0m, "111");
        await loja.ProdutoPelaApiAsync("B", 0m, "222");

        Assert.Equal(200, (await loja.Painel.Put($"{Rota}/{a}", CorpoProduto("A2", codigo: "111"))).Codigo);
        Assert.Equal((409, "O código de barras '222' já está cadastrado em outro produto da sua loja."), await Erro(loja.Painel.Put($"{Rota}/{a}", CorpoProduto("A", codigo: "222"))));
        Assert.Equal(200, (await loja.Painel.Put($"{Rota}/{a}", CorpoProduto("A", codigo: null))).Codigo);     // sem código: apaga
    }

    // ── Lote ─────────────────────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Precos_em_lote_atualiza_so_os_produtos_da_propria_loja()
    {
        var loja = await NovaAsync();
        var outra = await NovaAsync();
        var a = await loja.ProdutoPelaApiAsync("A", custo: 1m, venda: 2m);
        var b = await loja.ProdutoPelaApiAsync("B", custo: 3m, venda: 4m);
        var dela = await outra.ProdutoPelaApiAsync("Dela", custo: 5m, venda: 6m);

        Assert.Equal(0, (await loja.Painel.Put($"{Rota}/precos-em-lote", new { produtos = Array.Empty<object>() })).Json.GetProperty("atualizados").GetInt32());
        var r = await loja.Painel.Put($"{Rota}/precos-em-lote", new { produtos = new[]
        {
            new { id = a, precoCusto = 10m, precoVenda = 20m }, new { id = dela, precoCusto = 99m, precoVenda = 99m }, new { id = Guid.NewGuid(), precoCusto = 1m, precoVenda = 1m },
        } });

        Assert.Equal(1, r.Json.GetProperty("atualizados").GetInt32());
        Assert.Equal((10m, 20m), ((await api.ProdutoDaLojaNoBancoAsync(a)).PrecoCusto, (await api.ProdutoDaLojaNoBancoAsync(a)).PrecoVenda));
        Assert.Equal((3m, 4m), ((await api.ProdutoDaLojaNoBancoAsync(b)).PrecoCusto, (await api.ProdutoDaLojaNoBancoAsync(b)).PrecoVenda));
        Assert.Equal((5m, 6m), ((await api.ProdutoDaLojaNoBancoAsync(dela)).PrecoCusto, (await api.ProdutoDaLojaNoBancoAsync(dela)).PrecoVenda));
    }

    [FatoIntegracao]
    public async Task Status_em_lote_ativa_e_inativa_so_os_produtos_da_propria_loja()
    {
        var loja = await NovaAsync();
        var outra = await NovaAsync();
        var a = await loja.ProdutoPelaApiAsync("A");
        var b = await loja.ProdutoPelaApiAsync("B");
        var dela = await outra.ProdutoPelaApiAsync("Dela");

        Assert.Equal(0, (await loja.Painel.Put($"{Rota}/status-em-lote", new { ids = Array.Empty<Guid>(), ativo = false })).Json.GetProperty("atualizados").GetInt32());
        var r = await loja.Painel.Put($"{Rota}/status-em-lote", new { ids = new[] { a, dela }, ativo = false });

        Assert.Equal(1, r.Json.GetProperty("atualizados").GetInt32());
        Assert.False((await api.ProdutoDaLojaNoBancoAsync(a)).Ativo);
        Assert.True((await api.ProdutoDaLojaNoBancoAsync(b)).Ativo);
        Assert.True((await api.ProdutoDaLojaNoBancoAsync(dela)).Ativo);
        (await loja.Painel.Put($"{Rota}/status-em-lote", new { ids = new[] { a }, ativo = true })).DeveSerSucesso();
        Assert.True((await api.ProdutoDaLojaNoBancoAsync(a)).Ativo);
    }

    // ── Excluir ──────────────────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Excluir_remove_o_produto_as_variacoes_e_os_movimentos()
    {
        var loja = await NovaAsync();
        var outra = await NovaAsync();
        var id = await loja.ProdutoPelaApiAsync("Camisa", 4m);
        var variacao = await loja.VariacaoPelaApiAsync(id, "P", "Azul", 2);

        Assert.Equal(404, (await outra.Painel.Delete($"{Rota}/{id}")).Codigo);
        var r = await loja.Painel.Delete($"{Rota}/{id}");

        Assert.Equal("Produto excluído com sucesso.", r.Json.GetProperty("mensagem").GetString());
        Assert.Equal(404, (await loja.Painel.Get($"{Rota}/{id}")).Codigo);
        Assert.False(await api.NoBancoAsync(db => db.ProdutoVariacoes.AnyAsync(v => v.Id == variacao)));
        Assert.Empty(await api.MovimentosDoProdutoNoBancoAsync(id));
        Assert.Equal(404, (await loja.Painel.Delete($"{Rota}/{Guid.NewGuid()}")).Codigo);
    }

    [FatoIntegracao]
    public async Task Excluir_produto_ja_vendido_e_recusado()
    {
        var loja = await NovaAsync();
        var id = await api.ProdutoAsync(loja.Id, "Vendido", estoque: 5);
        await loja.VenderAsync([CenariosVendas.ItemProduto(id)]);

        var r = await loja.Painel.Delete($"{Rota}/{id}");

        Assert.Equal((400, "Não é possível excluir: este produto já tem vendas registradas. Você pode apenas inativá-lo."), (r.Codigo, r.Erro));
        Assert.Equal(200, (await loja.Painel.Get($"{Rota}/{id}")).Codigo);
    }

    // ── Códigos de barras ────────────────────────────────────────────────────

    private static bool Ean13Valido(string c)
    {
        if (c.Length != 13 || !c.All(char.IsDigit)) return false;
        var soma = 0;
        for (var i = 0; i < 12; i++) soma += (c[i] - '0') * (i % 2 == 0 ? 1 : 3);
        return (10 - soma % 10) % 10 == c[12] - '0';
    }

    [FatoIntegracao]
    public async Task Gerar_codigos_cria_ean13_internos_unicos_e_limita_a_quantidade()
    {
        var loja = await NovaAsync();

        var um = (await loja.Painel.Get($"{Rota}/gerar-codigos")).Json.GetProperty("codigos").EnumerateArray().Select(c => c.GetString()!).ToList();
        var muitos = (await loja.Painel.Get($"{Rota}/gerar-codigos?quantidade=5000")).Json.GetProperty("codigos").EnumerateArray().Select(c => c.GetString()!).ToList();
        var nenhum = (await loja.Painel.Get($"{Rota}/gerar-codigos?quantidade=0")).Json.GetProperty("codigos").GetArrayLength();

        Assert.Single(um);
        Assert.Equal(200, muitos.Count);
        Assert.Equal(1, nenhum);                                       // quantidade mínima é 1
        Assert.Equal(200, muitos.Distinct().Count());
        Assert.All(um.Concat(muitos), c => { Assert.StartsWith("2", c); Assert.True(Ean13Valido(c), c); });
    }

    [FatoIntegracao]
    public async Task Gerar_codigos_nao_repete_os_ja_usados_pela_loja_nem_os_ignorados()
    {
        var loja = await NovaAsync();
        var gerados = (await loja.Painel.Get($"{Rota}/gerar-codigos?quantidade=3")).Json.GetProperty("codigos").EnumerateArray().Select(c => c.GetString()!).ToList();
        await loja.ProdutoPelaApiAsync("Com código", 0m, gerados[0]);
        var id = await loja.ProdutoPelaApiAsync("Com variação");
        await loja.VariacaoPelaApiAsync(id, "P", "Azul", 0, gerados[1]);

        var novos = (await loja.Painel.Get($"{Rota}/gerar-codigos?quantidade=200&ignorar={gerados[2]},")).Json.GetProperty("codigos").EnumerateArray().Select(c => c.GetString()!).ToList();

        Assert.Empty(novos.Intersect(gerados));
    }

    // ── Variações ────────────────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Listar_variacoes_ordena_por_tamanho_e_cor_e_404_para_outra_loja()
    {
        var loja = await NovaAsync();
        var outra = await NovaAsync();
        var id = await loja.ProdutoPelaApiAsync("Camisa");
        await loja.VariacaoPelaApiAsync(id, "P", "Verde", 1);
        await loja.VariacaoPelaApiAsync(id, "G", "Azul", 2);
        await loja.VariacaoPelaApiAsync(id, "G", "Amarelo", 3);

        var lista = (await loja.Painel.Get($"{Rota}/{id}/variacoes")).Json.EnumerateArray().ToList();

        Assert.Equal([("G", "Amarelo"), ("G", "Azul"), ("P", "Verde")], lista.Select(v => (v.GetProperty("tamanho").GetString()!, v.GetProperty("cor").GetString()!)));
        Assert.Equal(404, (await outra.Painel.Get($"{Rota}/{id}/variacoes")).Codigo);
        Assert.Equal(404, (await loja.Painel.Get($"{Rota}/{Guid.NewGuid()}/variacoes")).Codigo);
    }

    [FatoIntegracao]
    public async Task Adicionar_variacao_valida_codigo_na_loja_e_troca_codigo_em_branco_por_nulo()
    {
        var loja = await NovaAsync();
        var outra = await NovaAsync();
        var id = await loja.ProdutoPelaApiAsync("Camisa");
        await loja.VariacaoPelaApiAsync(id, "P", "Azul", 0, "555");

        var repetida = await loja.Painel.Post($"{Rota}/{id}/variacoes", CorpoVariacao("M", "Azul", null, "555"));
        var vazia = await loja.Painel.Post($"{Rota}/{id}/variacoes", CorpoVariacao("M", "Azul", "outro", "   ", 4, 2));

        Assert.Equal((409, "O código de barras '555' já está cadastrado em outra variação da sua loja."), (repetida.Codigo, repetida.Erro));
        Assert.Equal(200, vazia.Codigo);
        Assert.Equal(JsonValueKind.Null, vazia.Json.GetProperty("codigoBarras").ValueKind);
        Assert.Equal(("outro", 4, 2, true), (vazia.Json.GetProperty("outroCampo").GetString(), vazia.Json.GetProperty("estoque").GetInt32(),
            vazia.Json.GetProperty("estoqueMinimo").GetInt32(), vazia.Json.GetProperty("ativo").GetBoolean()));
        Assert.Equal(404, (await outra.Painel.Post($"{Rota}/{id}/variacoes", CorpoVariacao())).Codigo);
    }

    [FatoIntegracao]
    public async Task Atualizar_variacao_troca_os_campos_e_registra_ajuste_quando_o_estoque_muda()
    {
        var loja = await NovaAsync();
        var id = await loja.ProdutoPelaApiAsync("Camisa");
        var v = await loja.VariacaoPelaApiAsync(id, "P", "Azul", 2, "555");

        var mesmoEstoque = await loja.Painel.Put($"{Rota}/{id}/variacoes/{v}", CorpoVariacao("M", "Verde", null, "555", 2, 3));
        Assert.Equal(200, mesmoEstoque.Codigo);
        Assert.Empty(await api.MovimentosDoProdutoNoBancoAsync(id));

        var r = await loja.Painel.Put($"{Rota}/{id}/variacoes/{v}", CorpoVariacao("G", null, null, null, 9, 3));

        Assert.Equal(("G", 9), (r.Json.GetProperty("tamanho").GetString(), r.Json.GetProperty("estoque").GetInt32()));
        var mov = Assert.Single(await api.MovimentosDoProdutoNoBancoAsync(id));
        Assert.Equal(("ajuste", 9m, "Editado via cadastro do produto (G)"), (mov.Tipo, mov.Quantidade, mov.Observacao));
    }

    [FatoIntegracao]
    public async Task Atualizar_variacao_recusa_codigo_de_outra_variacao_e_404_para_variacao_de_outro_produto()
    {
        var loja = await NovaAsync();
        var id = await loja.ProdutoPelaApiAsync("Camisa");
        var outroProduto = await loja.ProdutoPelaApiAsync("Calça");
        var a = await loja.VariacaoPelaApiAsync(id, "P", "Azul", 0, "555");
        var b = await loja.VariacaoPelaApiAsync(id, "M", "Azul", 0, "666");

        Assert.Equal((409, "O código de barras '666' já está cadastrado em outra variação da sua loja."), await Erro(loja.Painel.Put($"{Rota}/{id}/variacoes/{a}", CorpoVariacao("P", "Azul", null, "666"))));
        Assert.Equal(200, (await loja.Painel.Put($"{Rota}/{id}/variacoes/{b}", CorpoVariacao("M", "Azul", null, "666"))).Codigo);    // o próprio código
        Assert.Equal(404, (await loja.Painel.Put($"{Rota}/{outroProduto}/variacoes/{a}", CorpoVariacao())).Codigo);
        Assert.Equal(404, (await loja.Painel.Delete($"{Rota}/{outroProduto}/variacoes/{a}")).Codigo);
    }

    [FatoIntegracao]
    public async Task Remover_variacao_so_desativa_e_devolve_204()
    {
        var loja = await NovaAsync();
        var id = await loja.ProdutoPelaApiAsync("Camisa");
        var v = await loja.VariacaoPelaApiAsync(id, "P", "Azul", 2);

        var r = await loja.Painel.Delete($"{Rota}/{id}/variacoes/{v}");

        Assert.Equal(204, r.Codigo);
        Assert.False((await api.VariacaoNoBancoDoProdutoAsync(v)).Ativo);
        Assert.Equal(404, (await loja.Painel.Delete($"{Rota}/{id}/variacoes/{Guid.NewGuid()}")).Codigo);
    }
}
