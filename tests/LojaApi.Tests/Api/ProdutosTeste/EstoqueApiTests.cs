using static LojaApi.Tests.Suporte.CenariosProdutos;

namespace LojaApi.Tests.Api.ProdutosTeste;

// Estoque: histórico de movimentos, ajustes (entrada, saída e ajuste total), alertas de estoque baixo e ajuste por variação.
[Collection(ColecaoApi.Nome)]
public class EstoqueApiTests(ApiFixture api)
{
    private const string E = "/api/estoque";

    private Task<LojaTeste> NovaAsync() => api.NovaLojaDeServicosAsync();

    private static object Ajuste(Guid produto, string tipo, decimal quantidade, string? obs = null) => new { produtoId = produto, tipo, quantidade, observacao = obs };

    private static object AjusteVar(Guid produto, Guid variacao, string tipo, int quantidade, string? obs = null) => new { produtoId = produto, variacaoId = variacao, tipo, quantidade, observacao = obs };

    private static async Task<(int, string)> Erro(Task<Resposta> r) { var x = await r; return (x.Codigo, x.Erro); }

    [FatoIntegracao]
    public async Task Exige_login_e_so_admin_ajusta()
    {
        var loja = await NovaAsync();
        var operador = await api.OperadorAsync(loja);
        var id = await loja.ProdutoPelaApiAsync("Camisa", 5m);

        foreach (var u in new[] { $"{E}/movimentos", $"{E}/alertas" }) Assert.Equal(401, (await loja.Site.Get(u)).Codigo);
        Assert.Equal(401, (await loja.Site.Post($"{E}/ajuste", Ajuste(id, "entrada", 1))).Codigo);
        Assert.Equal(200, (await operador.Get($"{E}/movimentos")).Codigo);
        Assert.Equal(200, (await operador.Get($"{E}/alertas")).Codigo);
        Assert.Equal(403, (await operador.Post($"{E}/ajuste", Ajuste(id, "entrada", 1))).Codigo);
        Assert.Equal(403, (await operador.Post($"{E}/ajuste-variacao", AjusteVar(id, Guid.NewGuid(), "entrada", 1))).Codigo);
    }

    [FatoIntegracao]
    public async Task Ajuste_entrada_soma_saida_subtrai_e_ajuste_define_o_total()
    {
        var loja = await NovaAsync();
        var id = await loja.ProdutoPelaApiAsync("Camisa", 10m);

        var entrada = await loja.Painel.Post($"{E}/ajuste", Ajuste(id, "entrada", 5m, "compra"));
        Assert.Equal((id, "Camisa", 15m), (entrada.Json.GetProperty("produtoId").GetGuid(), entrada.Json.GetProperty("nome").GetString(), entrada.Json.GetProperty("estoqueAgora").GetDecimal()));
        Assert.Equal(11m, (await loja.Painel.Post($"{E}/ajuste", Ajuste(id, "saida", 4m))).Json.GetProperty("estoqueAgora").GetDecimal());
        Assert.Equal(3m, (await loja.Painel.Post($"{E}/ajuste", Ajuste(id, "ajuste", 3m, "contagem"))).Json.GetProperty("estoqueAgora").GetDecimal());
        Assert.Equal(0m, (await loja.Painel.Post($"{E}/ajuste", Ajuste(id, "ajuste", 0m))).Json.GetProperty("estoqueAgora").GetDecimal());   // ajuste aceita zero

        var movs = await api.MovimentosDoProdutoNoBancoAsync(id);
        Assert.Equal(["entrada", "entrada", "saida", "ajuste", "ajuste"], movs.Select(m => m.Tipo));      // o primeiro é o estoque inicial
        Assert.Equal("compra", movs[1].Observacao);
        Assert.All(movs, m => Assert.Equal(loja.Id, m.LojaId));
    }

    [FatoIntegracao]
    public async Task Ajuste_valida_produto_tipo_quantidade_e_saida_maior_que_o_estoque_sem_gravar()
    {
        var loja = await NovaAsync();
        var outra = await NovaAsync();
        var id = await loja.ProdutoPelaApiAsync("Camisa", 2m);

        Assert.Equal((404, "Produto não encontrado."), await Erro(loja.Painel.Post($"{E}/ajuste", Ajuste(Guid.NewGuid(), "entrada", 1))));
        Assert.Equal((404, "Produto não encontrado."), await Erro(outra.Painel.Post($"{E}/ajuste", Ajuste(id, "entrada", 1))));
        Assert.Equal((400, "Tipo de movimento inválido."), await Erro(loja.Painel.Post($"{E}/ajuste", Ajuste(id, "troca", 1))));
        Assert.Equal((400, "A quantidade não pode ser negativa."), await Erro(loja.Painel.Post($"{E}/ajuste", Ajuste(id, "ajuste", -1))));
        Assert.Equal((400, "Informe uma quantidade maior que zero."), await Erro(loja.Painel.Post($"{E}/ajuste", Ajuste(id, "entrada", 0))));
        Assert.Equal((400, "Informe uma quantidade maior que zero."), await Erro(loja.Painel.Post($"{E}/ajuste", Ajuste(id, "saida", 0))));
        Assert.Equal((400, "A quantidade da saída (3) é maior que o estoque atual (2.000)."), await Erro(loja.Painel.Post($"{E}/ajuste", Ajuste(id, "saida", 3m))));

        Assert.Equal(2m, (await api.ProdutoDaLojaNoBancoAsync(id)).Estoque);
        Assert.Single(await api.MovimentosDoProdutoNoBancoAsync(id));       // só o estoque inicial
    }

    [FatoIntegracao]
    public async Task Movimentos_filtra_por_produto_tipo_e_periodo_do_mais_novo_ao_mais_antigo_e_isola_por_loja()
    {
        var loja = await NovaAsync();
        var outra = await NovaAsync();
        var a = await loja.ProdutoPelaApiAsync("A", 5m);
        var b = await loja.ProdutoPelaApiAsync("B", 0m);
        (await loja.Painel.Post($"{E}/ajuste", Ajuste(a, "saida", 1m))).DeveSerSucesso();
        (await loja.Painel.Post($"{E}/ajuste", Ajuste(b, "entrada", 2m))).DeveSerSucesso();
        await outra.ProdutoPelaApiAsync("Dela", 9m);

        var todos = (await loja.Painel.Get($"{E}/movimentos")).Json.EnumerateArray().ToList();
        var doA = (await loja.Painel.Get($"{E}/movimentos?produtoId={a}")).Json.EnumerateArray().ToList();
        var saidas = (await loja.Painel.Get($"{E}/movimentos?tipo=saida")).Json.EnumerateArray().ToList();
        var futuro = (await loja.Painel.Get($"{E}/movimentos?de=2099-01-01T00:00:00Z")).Json.EnumerateArray().ToList();
        var hoje = DateTime.UtcNow.ToString("yyyy-MM-dd") + "T00:00:00Z";
        var dia = (await loja.Painel.Get($"{E}/movimentos?de={hoje}&ate={hoje}")).Json.EnumerateArray().ToList();

        Assert.Equal(3, todos.Count);                                                        // estoque inicial do A + 2 ajustes
        Assert.Equal(["entrada", "saida", "entrada"], todos.Select(m => m.GetProperty("tipo").GetString()));
        Assert.Equal(2, doA.Count);
        Assert.Equal("A", doA[0].GetProperty("nomeProduto").GetString());
        Assert.Equal(1m, Assert.Single(saidas).GetProperty("quantidade").GetDecimal());
        Assert.Empty(futuro);
        Assert.Equal(3, dia.Count);
        Assert.Single((await outra.Painel.Get($"{E}/movimentos")).Json.EnumerateArray());
    }

    [FatoIntegracao]
    public async Task Alertas_traz_so_produtos_ativos_da_loja_com_estoque_no_minimo_ou_abaixo()
    {
        var loja = await NovaAsync();
        var outra = await NovaAsync();
        await loja.Painel.Post(Rota, CorpoProduto("Zerado", estoque: 0m, minimo: 3m));
        await loja.Painel.Post(Rota, CorpoProduto("No mínimo", estoque: 3m, minimo: 3m));
        await loja.Painel.Post(Rota, CorpoProduto("Folgado", estoque: 10m, minimo: 3m));
        await loja.Painel.Post(Rota, CorpoProduto("Inativo", estoque: 0m, minimo: 3m, ativo: false));
        await outra.Painel.Post(Rota, CorpoProduto("Dela", estoque: 0m, minimo: 3m));

        var alertas = (await loja.Painel.Get($"{E}/alertas")).Json.EnumerateArray().OrderBy(a => a.GetProperty("nome").GetString()).ToList();

        Assert.Equal(["No mínimo", "Zerado"], alertas.Select(a => a.GetProperty("nome").GetString()));
        Assert.Equal((false, true), (alertas[0].GetProperty("zerado").GetBoolean(), alertas[1].GetProperty("zerado").GetBoolean()));
        Assert.Equal(3m, alertas[0].GetProperty("estoqueMinimo").GetDecimal());
    }

    // ── Ajuste por variação ──────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Ajuste_de_variacao_soma_subtrai_ou_define_e_registra_o_movimento_com_a_grade()
    {
        var loja = await NovaAsync();
        var id = await loja.ProdutoPelaApiAsync("Camisa");
        var v = await loja.VariacaoPelaApiAsync(id, "P", "Azul", 10);

        var entrada = await loja.Painel.Post($"{E}/ajuste-variacao", AjusteVar(id, v, "entrada", 5));
        Assert.Equal(("Estoque atualizado.", 15), (entrada.Json.GetProperty("mensagem").GetString(), entrada.Json.GetProperty("estoqueAgora").GetInt32()));
        Assert.Equal(12, (await loja.Painel.Post($"{E}/ajuste-variacao", AjusteVar(id, v, "saida", 3, "  quebra "))).Json.GetProperty("estoqueAgora").GetInt32());
        Assert.Equal(7, (await loja.Painel.Post($"{E}/ajuste-variacao", AjusteVar(id, v, "ajuste", 7))).Json.GetProperty("estoqueAgora").GetInt32());

        Assert.Equal(7, (await api.VariacaoNoBancoDoProdutoAsync(v)).Estoque);
        var movs = await api.MovimentosDoProdutoNoBancoAsync(id);
        Assert.Equal(["Entrada - P / Azul", "quebra - P / Azul", "Ajuste - P / Azul"], movs.Select(m => m.Observacao));
        Assert.All(movs, m => Assert.Equal(loja.Id, m.LojaId));
    }

    [FatoIntegracao]
    public async Task Ajuste_de_variacao_valida_variacao_tipo_quantidade_e_saida_maior_que_o_estoque()
    {
        var loja = await NovaAsync();
        var id = await loja.ProdutoPelaApiAsync("Camisa");
        var outro = await loja.ProdutoPelaApiAsync("Calça");
        var v = await loja.VariacaoPelaApiAsync(id, "P", "Azul", 2);

        Assert.Equal((404, "Variação não encontrada."), await Erro(loja.Painel.Post($"{E}/ajuste-variacao", AjusteVar(id, Guid.NewGuid(), "entrada", 1))));
        Assert.Equal((404, "Variação não encontrada."), await Erro(loja.Painel.Post($"{E}/ajuste-variacao", AjusteVar(outro, v, "entrada", 1))));
        Assert.Equal((400, "Tipo de movimento inválido."), await Erro(loja.Painel.Post($"{E}/ajuste-variacao", AjusteVar(id, v, "x", 1))));
        Assert.Equal((400, "A quantidade não pode ser negativa."), await Erro(loja.Painel.Post($"{E}/ajuste-variacao", AjusteVar(id, v, "ajuste", -1))));
        Assert.Equal((400, "Informe uma quantidade maior que zero."), await Erro(loja.Painel.Post($"{E}/ajuste-variacao", AjusteVar(id, v, "saida", 0))));
        Assert.Equal((400, "A quantidade da saída (5) é maior que o estoque atual desta variação (2)."), await Erro(loja.Painel.Post($"{E}/ajuste-variacao", AjusteVar(id, v, "saida", 5))));
        Assert.Equal(2, (await api.VariacaoNoBancoDoProdutoAsync(v)).Estoque);
    }
}
