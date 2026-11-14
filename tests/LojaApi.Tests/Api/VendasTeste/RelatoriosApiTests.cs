using System.Text.Json;

namespace LojaApi.Tests.Api.VendasTeste;

// Relatórios de vendas. Cada teste usa um ano antigo só dele para os números não dependerem de outros testes.
[Collection(ColecaoApi.Nome)]
public class RelatoriosApiTests(ApiFixture api)
{
    private Task<LojaTeste> NovaAsync() => api.NovaLojaDeServicosAsync();

    private static DateTime Dia(int ano, int mes, int dia) => new(ano, mes, dia, 0, 0, 0, DateTimeKind.Utc);

    private static string Q(DateTime d) => Uri.EscapeDataString(d.ToString("yyyy-MM-dd") + "T00:00:00Z");

    [FatoIntegracao]
    public async Task Exige_login()
    {
        var loja = await NovaAsync();
        foreach (var url in new[] { "/api/relatorios/resumo", "/api/relatorios/produtos-ranking", "/api/relatorios/fluxo-diario?mes=1&ano=2001", "/api/relatorios/fluxo-mensal?ano=2001" })
            Assert.Equal(401, (await loja.Site.Get(url)).Codigo);
    }

    [FatoIntegracao]
    public async Task Resumo_soma_o_periodo_e_calcula_o_ticket_medio()
    {
        var loja = await NovaAsync();
        var produto = await api.ProdutoAsync(loja.Id, estoque: 100);
        await loja.VenderAsync([CenariosVendas.ItemProduto(produto, 2, 50m)], desconto: 10m, data: Dia(2002, 3, 10));      // 90, 2 itens
        await loja.VenderAsync([CenariosVendas.ItemProduto(produto, 3, 100m)], data: Dia(2002, 3, 31));                    // 300, 3 itens (o "ate" vale o dia inteiro)
        await loja.VenderAsync([CenariosVendas.ItemProduto(produto, 1, 70m)], data: Dia(2002, 4, 5));                      // fora do período

        var j = (await loja.Painel.Get($"/api/relatorios/resumo?de={Q(Dia(2002, 3, 1))}&ate={Q(Dia(2002, 3, 31))}")).Json;

        Assert.Equal((390m, 10m, 195m, 5m, 2), (j.GetProperty("totalVendido").GetDecimal(), j.GetProperty("totalDescontos").GetDecimal(), j.GetProperty("ticketMedio").GetDecimal(),
            j.GetProperty("totalItensVendidos").GetDecimal(), j.GetProperty("totalVendas").GetInt32()));
    }

    [FatoIntegracao]
    public async Task Resumo_de_periodo_sem_vendas_vem_zerado()
    {
        var loja = await NovaAsync();

        var j = (await loja.Painel.Get($"/api/relatorios/resumo?de={Q(Dia(2001, 1, 1))}&ate={Q(Dia(2001, 1, 31))}")).Json;

        Assert.Equal((0m, 0m, 0m, 0m, 0), (j.GetProperty("totalVendido").GetDecimal(), j.GetProperty("totalDescontos").GetDecimal(), j.GetProperty("ticketMedio").GetDecimal(),
            j.GetProperty("totalItensVendidos").GetDecimal(), j.GetProperty("totalVendas").GetInt32()));
    }

    [FatoIntegracao]
    public async Task Ranking_ordena_por_quantidade_calcula_o_lucro_limita_no_top_e_ignora_servicos()
    {
        var loja = await NovaAsync();
        var camiseta = await api.ProdutoAsync(loja.Id, "Camiseta", estoque: 100, preco: 50m, custo: 20m);
        var meia = await api.ProdutoAsync(loja.Id, "Meia", estoque: 100, preco: 10m, custo: 4m);
        var bone = await api.ProdutoAsync(loja.Id, "Boné", estoque: 100, preco: 30m, custo: 10m);
        var servico = await loja.ServicoIdAsync("Ajuste", 25);
        var data = Dia(2003, 6, 15);
        await loja.VenderAsync([CenariosVendas.ItemProduto(camiseta, 2, 50m), CenariosVendas.ItemProduto(meia, 5, 10m), CenariosVendas.ItemServico(servico, 1, 25m)], data: data);
        await loja.VenderAsync([CenariosVendas.ItemProduto(camiseta, 1, 50m), CenariosVendas.ItemProduto(bone, 1, 30m)], data: data);

        var de = Q(Dia(2003, 1, 1));
        var ate = Q(Dia(2003, 12, 31));
        var todos = (await loja.Painel.Get($"/api/relatorios/produtos-ranking?de={de}&ate={ate}")).Json.EnumerateArray().ToList();
        var top1 = (await loja.Painel.Get($"/api/relatorios/produtos-ranking?de={de}&ate={ate}&top=1")).Json.EnumerateArray().ToList();

        Assert.Equal(["Meia", "Camiseta", "Boné"], todos.Select(r => r.GetProperty("nome").GetString()).ToList());       // 5, 3 e 1 unidades; sem o serviço
        var camisetaRanking = todos[1];
        Assert.Equal((camiseta, "outro", 3m, 150m, 90m), (camisetaRanking.GetProperty("id").GetGuid(), camisetaRanking.GetProperty("categoria").GetString(), camisetaRanking.GetProperty("qtdVendida").GetDecimal(),
            camisetaRanking.GetProperty("receita").GetDecimal(), camisetaRanking.GetProperty("lucroEstimado").GetDecimal()));     // receita 150 - custo 3 x 20
        Assert.Equal(30m, todos[0].GetProperty("lucroEstimado").GetDecimal());                                                    // 50 - 5 x 4
        Assert.Equal(["Meia"], top1.Select(r => r.GetProperty("nome").GetString()).ToList());
    }

    [FatoIntegracao]
    public async Task Fluxo_diario_lista_todos_os_dias_do_mes_com_data_curta()
    {
        var loja = await NovaAsync();
        var produto = await api.ProdutoAsync(loja.Id, estoque: 100);
        await loja.VenderAsync([CenariosVendas.ItemProduto(produto, 1, 100m)], desconto: 5m, data: Dia(2004, 2, 3));
        await loja.VenderAsync([CenariosVendas.ItemProduto(produto, 1, 40m)], data: Dia(2004, 2, 3));
        await loja.VenderAsync([CenariosVendas.ItemProduto(produto, 1, 60m)], data: Dia(2004, 2, 29));
        await loja.VenderAsync([CenariosVendas.ItemProduto(produto, 1, 60m)], data: Dia(2004, 3, 1));        // outro mês

        var dias = (await loja.Painel.Get("/api/relatorios/fluxo-diario?mes=2&ano=2004")).Json.EnumerateArray().ToList();

        Assert.Equal(29, dias.Count);       // 2004 foi bissexto
        Assert.Equal(("01/02", "29/02"), (dias[0].GetProperty("data").GetString(), dias[28].GetProperty("data").GetString()));
        Assert.Equal(("03/02", 2, 135m, 5m), (dias[2].GetProperty("data").GetString(), dias[2].GetProperty("qtdVendas").GetInt32(), dias[2].GetProperty("entradas").GetDecimal(), dias[2].GetProperty("descontos").GetDecimal()));
        Assert.Equal((1, 60m), (dias[28].GetProperty("qtdVendas").GetInt32(), dias[28].GetProperty("entradas").GetDecimal()));
        Assert.Equal(0, dias[10].GetProperty("qtdVendas").GetInt32());
    }

    [FatoIntegracao]
    public async Task Fluxo_mensal_traz_os_12_meses_do_ano()
    {
        var loja = await NovaAsync();
        var produto = await api.ProdutoAsync(loja.Id, estoque: 100);
        await loja.VenderAsync([CenariosVendas.ItemProduto(produto, 1, 100m)], desconto: 10m, data: Dia(2005, 1, 10));
        await loja.VenderAsync([CenariosVendas.ItemProduto(produto, 1, 50m)], data: Dia(2005, 1, 20));
        await loja.VenderAsync([CenariosVendas.ItemProduto(produto, 1, 80m)], data: Dia(2005, 12, 31));
        await loja.VenderAsync([CenariosVendas.ItemProduto(produto, 1, 80m)], data: Dia(2006, 1, 1));         // outro ano

        var meses = (await loja.Painel.Get("/api/relatorios/fluxo-mensal?ano=2005")).Json.EnumerateArray().ToList();

        Assert.Equal(12, meses.Count);
        Assert.Equal(["Jan", "Fev", "Mar", "Abr", "Mai", "Jun", "Jul", "Ago", "Set", "Out", "Nov", "Dez"], meses.Select(m => m.GetProperty("nomeMes").GetString()).ToList());
        Assert.Equal((1, 2, 140m, 10m), (meses[0].GetProperty("mes").GetInt32(), meses[0].GetProperty("qtdVendas").GetInt32(), meses[0].GetProperty("entradas").GetDecimal(), meses[0].GetProperty("descontos").GetDecimal()));
        Assert.Equal((1, 80m), (meses[11].GetProperty("qtdVendas").GetInt32(), meses[11].GetProperty("entradas").GetDecimal()));
        Assert.Equal(0, meses[5].GetProperty("qtdVendas").GetInt32());
    }
}
