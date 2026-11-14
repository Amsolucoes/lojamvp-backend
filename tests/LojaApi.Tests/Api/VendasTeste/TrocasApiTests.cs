using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace LojaApi.Tests.Api.VendasTeste;

// Trocas de produtos: o cliente devolve itens e leva outros; sobra crédito ou fica uma diferença a pagar.
[Collection(ColecaoApi.Nome)]
public class TrocasApiTests(ApiFixture api)
{
    private Task<LojaTeste> NovaAsync() => api.NovaLojaDeServicosAsync();

    private static object Item(Guid produto, string nome, int quantidade = 1, decimal preco = 50m, bool voltaEstoque = true, Guid? variacao = null) =>
        new { produtoId = produto, nomeProduto = nome, variacaoId = variacao, quantidade, precoUnitario = preco, voltaEstoque };

    private static object Troca(Guid cliente, object[] devolvidos, object[] novos, string? forma = null) => new { clienteId = cliente, devolvidos, novos, formaPagamento = forma };

    [FatoIntegracao]
    public async Task Exige_login()
    {
        var loja = await NovaAsync();
        Assert.Equal(401, (await loja.Site.Get("/api/trocas")).Codigo);
        Assert.Equal(401, (await loja.Site.Post("/api/trocas", new { })).Codigo);
    }

    [FatoIntegracao]
    public async Task Valida_cliente_itens_e_estoque_dos_novos_sem_gravar_nada()
    {
        var loja = await NovaAsync();
        var cliente = await loja.ClienteIdAsync("Trocadora");
        var velho = await api.ProdutoAsync(loja.Id, "Velho", estoque: 1);
        var novo = await api.ProdutoAsync(loja.Id, "Novo", estoque: 1);
        var grade = await api.ProdutoAsync(loja.Id, "Grade", estoque: 0);
        var variacao = await api.VariacaoAsync(grade, "P", "Rosa", estoque: 1);

        async Task<(int, string)> Tenta(object corpo) { var r = await loja.Painel.Post("/api/trocas", corpo); return (r.Codigo, r.Erro); }

        Assert.Equal((400, "Cliente não encontrado."), await Tenta(Troca(Guid.NewGuid(), [Item(velho, "Velho")], [Item(novo, "Novo")])));
        Assert.Equal((400, "A troca precisa de ao menos um produto devolvido e um novo."), await Tenta(Troca(cliente, [], [Item(novo, "Novo")])));
        Assert.Equal((400, "A troca precisa de ao menos um produto devolvido e um novo."), await Tenta(Troca(cliente, [Item(velho, "Velho")], [])));
        Assert.Equal((400, "Estoque insuficiente para 'Novo'."), await Tenta(Troca(cliente, [Item(velho, "Velho")], [Item(novo, "Novo", 2)])));
        Assert.Equal((400, "Estoque insuficiente para 'Grade'."), await Tenta(Troca(cliente, [Item(velho, "Velho")], [Item(grade, "Grade", 2, variacao: variacao)])));
        Assert.Equal((400, "Estoque insuficiente para 'Grade'."), await Tenta(Troca(cliente, [Item(velho, "Velho")], [Item(grade, "Grade", 1, variacao: Guid.NewGuid())])));

        Assert.Equal(0, (await loja.Painel.Get("/api/trocas")).Json.GetArrayLength());
        Assert.Equal((1m, 1m), ((await api.ProdutoDaLojaNoBancoAsync(velho)).Estoque, (await api.ProdutoDaLojaNoBancoAsync(novo)).Estoque));
    }

    [FatoIntegracao]
    public async Task Troca_sem_diferenca_move_o_estoque_e_nao_gera_credito()
    {
        var loja = await NovaAsync();
        var cliente = await loja.ClienteIdAsync("Igual");
        var velho = await api.ProdutoAsync(loja.Id, "Velho", estoque: 5);
        var novo = await api.ProdutoAsync(loja.Id, "Novo", estoque: 5);

        var r = await loja.Painel.Post("/api/trocas", Troca(cliente, [Item(velho, "Velho", 2, 40m)], [Item(novo, "Novo", 1, 80m)], "pix"));

        Assert.Equal(200, r.Codigo);
        var j = r.Json;
        Assert.Equal((80m, 80m, 0m, 0m, 0m, "Troca realizada sem diferença"),
            (j.GetProperty("totalDevolvido").GetDecimal(), j.GetProperty("totalNovo").GetDecimal(), j.GetProperty("diferenca").GetDecimal(), j.GetProperty("creditoGerado").GetDecimal(),
             j.GetProperty("creditoTotalCliente").GetDecimal(), j.GetProperty("mensagem").GetString()));
        Assert.Equal((7m, 4m), ((await api.ProdutoDaLojaNoBancoAsync(velho)).Estoque, (await api.ProdutoDaLojaNoBancoAsync(novo)).Estoque));
        var troca = await api.NoBancoAsync(db => db.Trocas.AsNoTracking().SingleAsync(t => t.Id == j.GetProperty("id").GetGuid()));
        Assert.Equal((loja.Id, cliente, (string?)null), (troca.LojaId, troca.ClienteId, troca.FormaPagamento));      // sem diferença a pagar, a forma não é gravada
        var entrada = Assert.Single(await api.MovimentosDoProdutoAsync(velho));
        var saida = Assert.Single(await api.MovimentosDoProdutoAsync(novo));
        Assert.Equal(("entrada", 2m, "Troca - devolução Velho", "saida", 1m, "Troca - saída Novo"), (entrada.Tipo, entrada.Quantidade, entrada.Observacao, saida.Tipo, saida.Quantidade, saida.Observacao));
    }

    [FatoIntegracao]
    public async Task Troca_que_sobra_valor_vira_credito_para_o_cliente()
    {
        var loja = await NovaAsync();
        var cliente = await loja.ClienteIdAsync("Credito");
        var caro = await api.ProdutoAsync(loja.Id, "Caro", estoque: 5);
        var barato = await api.ProdutoAsync(loja.Id, "Barato", estoque: 5);
        await api.DarCreditoAsync(cliente, 10m);

        var r = await loja.Painel.Post("/api/trocas", Troca(cliente, [Item(caro, "Caro", 1, 100m)], [Item(barato, "Barato", 1, 30m)], "pix"));

        var j = r.Json;
        Assert.Equal((-70m, 70m, 80m), (j.GetProperty("diferenca").GetDecimal(), j.GetProperty("creditoGerado").GetDecimal(), j.GetProperty("creditoTotalCliente").GetDecimal()));
        Assert.StartsWith("Crédito de ", j.GetProperty("mensagem").GetString());
        Assert.EndsWith(" gerado para o cliente", j.GetProperty("mensagem").GetString());
        Assert.Equal(80m, await api.CreditoDoClienteAsync(cliente));
        var troca = await api.NoBancoAsync(db => db.Trocas.AsNoTracking().SingleAsync(t => t.Id == j.GetProperty("id").GetGuid()));
        Assert.Null(troca.FormaPagamento);
    }

    [FatoIntegracao]
    public async Task Troca_com_diferenca_a_pagar_grava_a_forma_de_pagamento_e_nao_mexe_no_credito()
    {
        var loja = await NovaAsync();
        var cliente = await loja.ClienteIdAsync("Paga");
        var barato = await api.ProdutoAsync(loja.Id, "Barato", estoque: 5);
        var caro = await api.ProdutoAsync(loja.Id, "Caro", estoque: 5);

        var r = await loja.Painel.Post("/api/trocas", Troca(cliente, [Item(barato, "Barato", 1, 30m)], [Item(caro, "Caro", 1, 100m)], "dinheiro"));

        var j = r.Json;
        Assert.Equal((70m, 0m, 0m), (j.GetProperty("diferenca").GetDecimal(), j.GetProperty("creditoGerado").GetDecimal(), j.GetProperty("creditoTotalCliente").GetDecimal()));
        Assert.StartsWith("Cliente deve pagar a diferença de ", j.GetProperty("mensagem").GetString());
        var troca = await api.NoBancoAsync(db => db.Trocas.AsNoTracking().SingleAsync(t => t.Id == j.GetProperty("id").GetGuid()));
        Assert.Equal("dinheiro", troca.FormaPagamento);
    }

    [FatoIntegracao]
    public async Task Devolvido_que_nao_volta_ao_estoque_e_troca_com_variacao()
    {
        var loja = await NovaAsync();
        var cliente = await loja.ClienteIdAsync("Variacao");
        var defeito = await api.ProdutoAsync(loja.Id, "Com defeito", estoque: 5);
        var grade = await api.ProdutoAsync(loja.Id, "Tênis", estoque: 0);
        var devolvida = await api.VariacaoAsync(grade, "40", "Preto", estoque: 2);
        var levada = await api.VariacaoAsync(grade, "42", "Preto", estoque: 3);

        var r = await loja.Painel.Post("/api/trocas", Troca(cliente,
            [Item(defeito, "Com defeito", 1, 50m, voltaEstoque: false), Item(grade, "Tênis 40", 1, 50m, voltaEstoque: true, variacao: devolvida)],
            [Item(grade, "Tênis 42", 2, 50m, variacao: levada)]));

        Assert.Equal(200, r.Codigo);
        Assert.Equal(5m, (await api.ProdutoDaLojaNoBancoAsync(defeito)).Estoque);          // não voltou
        Assert.Empty(await api.MovimentosDoProdutoAsync(defeito));
        Assert.Equal((3, 1), ((await api.VariacaoNoBancoAsync(devolvida)).Estoque, (await api.VariacaoNoBancoAsync(levada)).Estoque));   // +1 e -2
        Assert.Equal(0m, (await api.ProdutoDaLojaNoBancoAsync(grade)).Estoque);            // o estoque do produto pai não muda
    }

    [FatoIntegracao]
    public async Task Listar_traz_as_trocas_da_loja_com_itens_da_mais_nova_para_a_mais_antiga()
    {
        var loja = await NovaAsync();
        var outra = await NovaAsync();
        var cliente = await loja.ClienteIdAsync("Listada");
        var a = await api.ProdutoAsync(loja.Id, "A", estoque: 9);
        var b = await api.ProdutoAsync(loja.Id, "B", estoque: 9);
        var primeira = (await loja.Painel.Post("/api/trocas", Troca(cliente, [Item(a, "A", 1, 10m)], [Item(b, "B", 1, 10m)]))).Json.GetProperty("id").GetGuid();
        await Task.Delay(20);
        var segunda = (await loja.Painel.Post("/api/trocas", Troca(cliente, [Item(b, "B", 1, 20m)], [Item(a, "A", 1, 5m)]))).Json.GetProperty("id").GetGuid();

        var lista = (await loja.Painel.Get("/api/trocas")).Json.EnumerateArray().ToList();

        Assert.Equal([segunda, primeira], lista.Select(t => t.GetProperty("id").GetGuid()).ToList());
        Assert.Equal(("Listada", 20m, 5m, -15m, 15m), (lista[0].GetProperty("nomeCliente").GetString(), lista[0].GetProperty("totalDevolvido").GetDecimal(), lista[0].GetProperty("totalNovo").GetDecimal(),
            lista[0].GetProperty("diferenca").GetDecimal(), lista[0].GetProperty("creditoGerado").GetDecimal()));
        var itens = lista[0].GetProperty("itens").EnumerateArray().ToList();
        Assert.Equal([("B", 1, 20m, "devolvido", true), ("A", 1, 5m, "novo", true)],
            itens.Select(i => (i.GetProperty("nomeProduto").GetString()!, i.GetProperty("quantidade").GetInt32(), i.GetProperty("precoUnitario").GetDecimal(), i.GetProperty("tipo").GetString()!, i.GetProperty("voltaEstoque").GetBoolean())).OrderBy(i => i.Item4).ToList());
        Assert.Equal(0, (await outra.Painel.Get("/api/trocas")).Json.GetArrayLength());
    }
}
