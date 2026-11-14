using Microsoft.EntityFrameworkCore;

namespace LojaApi.Tests.Api.VendasTeste;

// Uma loja nunca enxerga nem mexe nos dados de outra: produto, variação, serviço, cliente e relatórios.
// (Antes, a venda e a troca aceitavam ids de outra loja e os relatórios somavam todas as lojas.)
[Collection(ColecaoApi.Nome)]
public class IsolamentoVendasApiTests(ApiFixture api)
{
    private static object ItemTroca(Guid produto, string nome, int quantidade = 1, decimal preco = 50m, bool volta = true, Guid? variacao = null) =>
        new { produtoId = produto, nomeProduto = nome, variacaoId = variacao, quantidade, precoUnitario = preco, voltaEstoque = volta };

    [FatoIntegracao]
    public async Task Venda_nao_aceita_produto_nem_variacao_de_outra_loja_e_nao_mexe_no_estoque_dela()
    {
        var minha = await api.NovaLojaDeServicosAsync();
        var outra = await api.NovaLojaDeServicosAsync();
        var produtoDela = await api.ProdutoAsync(outra.Id, "Dela", estoque: 10);
        var grade = await api.ProdutoAsync(outra.Id, "Grade", estoque: 0);
        var variacaoDela = await api.VariacaoAsync(grade, estoque: 5);
        var meu = await api.ProdutoAsync(minha.Id, "Meu", estoque: 10);

        var r1 = await minha.Painel.Post("/api/vendas", CenariosVendas.CorpoVenda([CenariosVendas.ItemProduto(produtoDela)]));
        var r2 = await minha.Painel.Post("/api/vendas", CenariosVendas.CorpoVenda([CenariosVendas.ItemProduto(meu, variacao: variacaoDela)]));

        Assert.Equal((400, $"Produto {produtoDela} não encontrado."), (r1.Codigo, r1.Erro));
        Assert.Equal((400, "Variação não encontrada."), (r2.Codigo, r2.Erro));
        Assert.Equal(10m, (await api.ProdutoDaLojaNoBancoAsync(produtoDela)).Estoque);
        Assert.Equal(5, (await api.VariacaoNoBancoAsync(variacaoDela)).Estoque);
        Assert.Equal(10m, (await api.ProdutoDaLojaNoBancoAsync(meu)).Estoque);
    }

    [FatoIntegracao]
    public async Task Venda_nao_aceita_servico_nem_cliente_de_outra_loja()
    {
        var minha = await api.NovaLojaDeServicosAsync();
        var outra = await api.NovaLojaDeServicosAsync();
        var servicoDela = await outra.ServicoIdAsync("Banho dela");
        var clienteDela = await outra.ClienteIdAsync("Cliente dela");
        var meu = await api.ProdutoAsync(minha.Id, "Meu", estoque: 10);
        await api.DarCreditoAsync(clienteDela, 100m);

        var r1 = await minha.Painel.Post("/api/vendas", CenariosVendas.CorpoVenda([CenariosVendas.ItemServico(servicoDela)]));
        var r2 = await minha.Painel.Post("/api/vendas", CenariosVendas.CorpoVenda([CenariosVendas.ItemProduto(meu)], cliente: clienteDela, credito: 40m));

        Assert.Equal((400, "Serviço não encontrado."), (r1.Codigo, r1.Erro));
        Assert.Equal((400, "Cliente não encontrado."), (r2.Codigo, r2.Erro));
        Assert.Equal(100m, await api.CreditoDoClienteAsync(clienteDela));
        Assert.Equal(10m, (await api.ProdutoDaLojaNoBancoAsync(meu)).Estoque);
    }

    [FatoIntegracao]
    public async Task Troca_nao_aceita_cliente_nem_produto_de_outra_loja_e_nao_gera_credito_la()
    {
        var minha = await api.NovaLojaDeServicosAsync();
        var outra = await api.NovaLojaDeServicosAsync();
        var clienteDela = await outra.ClienteIdAsync("Cliente dela");
        var clienteMeu = await minha.ClienteIdAsync("Cliente meu");
        var produtoDela = await api.ProdutoAsync(outra.Id, "Dela", estoque: 10);
        var meu = await api.ProdutoAsync(minha.Id, "Meu", estoque: 10);

        var r1 = await minha.Painel.Post("/api/trocas", new { clienteId = clienteDela, devolvidos = new[] { ItemTroca(meu, "Meu") }, novos = new[] { ItemTroca(meu, "Meu") } });
        var r2 = await minha.Painel.Post("/api/trocas", new { clienteId = clienteMeu, devolvidos = new[] { ItemTroca(meu, "Meu") }, novos = new[] { ItemTroca(produtoDela, "Dela") } });

        Assert.Equal((400, "Cliente não encontrado."), (r1.Codigo, r1.Erro));
        Assert.Equal((400, "Estoque insuficiente para 'Dela'."), (r2.Codigo, r2.Erro));
        var r3 = await minha.Painel.Post("/api/trocas", new { clienteId = clienteMeu, devolvidos = new[] { ItemTroca(produtoDela, "Dela") }, novos = new[] { ItemTroca(meu, "Meu") } });
        Assert.Equal((400, "Produto 'Dela' não encontrado."), (r3.Codigo, r3.Erro));
        Assert.Empty(await api.MovimentosDoProdutoAsync(produtoDela));
        Assert.Equal(10m, (await api.ProdutoDaLojaNoBancoAsync(produtoDela)).Estoque);
        Assert.Equal(10m, (await api.ProdutoDaLojaNoBancoAsync(meu)).Estoque);
        Assert.Equal(0m, await api.CreditoDoClienteAsync(clienteDela));
    }

    [FatoIntegracao]
    public async Task Relatorios_somam_so_as_vendas_da_propria_loja()
    {
        var minha = await api.NovaLojaDeServicosAsync();
        var outra = await api.NovaLojaDeServicosAsync();
        var meu = await api.ProdutoAsync(minha.Id, "Meu", estoque: 100);
        var dela = await api.ProdutoAsync(outra.Id, "Dela", estoque: 100);
        var dia = new DateTime(2003, 5, 10, 0, 0, 0, DateTimeKind.Utc);
        await minha.VenderAsync([CenariosVendas.ItemProduto(meu, 1, 50m)], data: dia);
        await outra.VenderAsync([CenariosVendas.ItemProduto(dela, 4, 100m)], data: dia);

        var resumo = (await minha.Painel.Get("/api/relatorios/resumo?de=2003-05-01T00:00:00Z&ate=2003-05-31T00:00:00Z")).Json;
        var ranking = (await minha.Painel.Get("/api/relatorios/produtos-ranking?de=2003-05-01T00:00:00Z&ate=2003-05-31T00:00:00Z")).Json;
        var diario = (await minha.Painel.Get("/api/relatorios/fluxo-diario?mes=5&ano=2003")).Json;
        var mensal = (await minha.Painel.Get("/api/relatorios/fluxo-mensal?ano=2003")).Json;

        Assert.Equal((50m, 1), (resumo.GetProperty("totalVendido").GetDecimal(), resumo.GetProperty("totalVendas").GetInt32()));
        Assert.Equal("Meu", Assert.Single(ranking.EnumerateArray()).GetProperty("nome").GetString());
        Assert.Equal(50m, diario.EnumerateArray().Sum(d => d.GetProperty("entradas").GetDecimal()));
        Assert.Equal(50m, mensal.EnumerateArray().Sum(m => m.GetProperty("entradas").GetDecimal()));
    }

    [FatoIntegracao]
    public async Task Venda_e_troca_da_propria_loja_continuam_funcionando()
    {
        var loja = await api.NovaLojaDeServicosAsync();
        var produto = await api.ProdutoAsync(loja.Id, "Meu", estoque: 10);
        var variacao = await api.VariacaoAsync(produto, estoque: 5);
        var servico = await loja.ServicoIdAsync("Banho");
        var cliente = await loja.ClienteIdAsync("Maria");

        await loja.VenderAsync([CenariosVendas.ItemProduto(produto, 2), CenariosVendas.ItemServico(servico)], cliente: cliente);
        var troca = await loja.Painel.Post("/api/trocas", new { clienteId = cliente, devolvidos = new[] { ItemTroca(produto, "Meu", 1, 50m, variacao: variacao) }, novos = new[] { ItemTroca(produto, "Meu", 1, 20m) } });

        Assert.Equal(200, troca.Codigo);
        Assert.Equal(8m - 1m, (await api.ProdutoDaLojaNoBancoAsync(produto)).Estoque);
        Assert.Equal(6, (await api.VariacaoNoBancoAsync(variacao)).Estoque);
        Assert.Equal(30m, await api.CreditoDoClienteAsync(cliente));
    }
}
