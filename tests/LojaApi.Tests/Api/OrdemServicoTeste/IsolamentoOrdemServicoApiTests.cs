using Microsoft.EntityFrameworkCore;
using static LojaApi.Tests.Suporte.CenariosOrdemServico;

namespace LojaApi.Tests.Api.OrdemServicoTeste;

// Uma loja nunca mexe nos dados de outra: cliente, produto, serviço e checklist da ordem, estoque na conclusão e na reversão.
// (Antes, a ordem aceitava ids de outra loja e a conclusão podia baixar o estoque de outra loja.)
[Collection(ColecaoApi.Nome)]
public class IsolamentoOrdemServicoApiTests(ApiFixture api)
{
    private const string Orc = Rota + "/orcamentos";

    private Task<LojaTeste> NovaAsync() => api.NovaLojaDeServicosAsync();

    [FatoIntegracao]
    public async Task Criar_recusa_cliente_produto_servico_e_checklist_de_outra_loja()
    {
        var loja = await NovaAsync();
        var outra = await NovaAsync();
        var meuCliente = await loja.ClienteIdAsync("Meu");
        var clienteDela = await outra.ClienteIdAsync("Dela");
        var produtoDela = await api.ProdutoAsync(outra.Id, "Peça dela", estoque: 5);
        var servicoDela = await outra.ServicoIdAsync("Serviço dela");
        var itemDela = await outra.ChecklistItemAsync(await outra.ChecklistCategoriaAsync());

        var r1 = await loja.Painel.Post(Orc, CorpoOrcamento(clienteDela, [MaoDeObra()]));
        var r2 = await loja.Painel.Post(Orc, CorpoOrcamento(meuCliente, [Peca(produtoDela, " Peça dela ")]));
        var r3 = await loja.Painel.Post(Orc, CorpoOrcamento(meuCliente, [MaoDeObra("Banho", 1, 50m, servicoDela)]));
        var r4 = await loja.Painel.Post(Orc, CorpoOrcamento(meuCliente, [MaoDeObra()], checklist: [Resposta(itemDela)]));

        Assert.Equal((400, "Cliente não encontrado."), (r1.Codigo, r1.Erro));
        Assert.Equal((400, "Produto do item 'Peça dela' não encontrado."), (r2.Codigo, r2.Erro));
        Assert.Equal((400, "Serviço do item 'Banho' não encontrado."), (r3.Codigo, r3.Erro));
        Assert.Equal((400, "Item de checklist não encontrado."), (r4.Codigo, r4.Erro));
        Assert.Empty((await loja.Painel.Get(Orc)).Json.EnumerateArray());
    }

    [FatoIntegracao]
    public async Task Editar_recusa_cliente_produto_servico_e_checklist_de_outra_loja_e_nao_mexe_na_ordem()
    {
        var loja = await NovaAsync();
        var outra = await NovaAsync();
        var cliente = await loja.ClienteIdAsync("Meu");
        var clienteDela = await outra.ClienteIdAsync("Dela");
        var produtoDela = await api.ProdutoAsync(outra.Id, "Peça dela", estoque: 5);
        var servicoDela = await outra.ServicoIdAsync("Serviço dela");
        var itemDela = await outra.ChecklistItemAsync(await outra.ChecklistCategoriaAsync());
        var id = await loja.OrcamentoAsync(cliente, [MaoDeObra("Original", 1, 10m)]);

        var r1 = await loja.Painel.Put($"{Orc}/{id}", CorpoOrcamento(clienteDela, [MaoDeObra()]));
        var r2 = await loja.Painel.Put($"{Orc}/{id}", CorpoOrcamento(cliente, [Peca(produtoDela, "Peça dela")]));
        var r3 = await loja.Painel.Put($"{Orc}/{id}", CorpoOrcamento(cliente, [MaoDeObra("Banho", 1, 50m, servicoDela)]));
        var r4 = await loja.Painel.Put($"{Orc}/{id}", CorpoOrcamento(cliente, [MaoDeObra()], checklist: [Resposta(itemDela)]));

        Assert.Equal([(400, "Cliente não encontrado."), (400, "Produto do item 'Peça dela' não encontrado."), (400, "Serviço do item 'Banho' não encontrado."), (400, "Item de checklist não encontrado.")],
            new[] { r1, r2, r3, r4 }.Select(r => (r.Codigo, r.Erro)));
        var d = (await loja.Painel.Get($"{Orc}/{id}")).Json;
        Assert.Equal("Original", Assert.Single(d.GetProperty("itens").EnumerateArray()).GetProperty("descricao").GetString());
    }

    [FatoIntegracao]
    public async Task Concluir_nao_baixa_o_estoque_de_produto_de_outra_loja()
    {
        var loja = await NovaAsync();
        var outra = await NovaAsync();
        var cliente = await loja.ClienteIdAsync();
        var conta = await loja.ContaBancariaIdAsync();
        var meu = await api.ProdutoAsync(loja.Id, "Meu", estoque: 5);
        var dela = await api.ProdutoAsync(outra.Id, "Dela", estoque: 5);
        var id = await loja.OrcamentoEmAndamentoAsync(cliente, [Peca(meu, "Peça", 2, 10m)]);
        await api.NoBancoAsync(db => db.ItensOrcamentoServico.Where(i => i.OrcamentoId == id).ExecuteUpdateAsync(s => s.SetProperty(i => i.ProdutoId, dela)));

        var r = await loja.ConcluirAsync(id, conta);

        Assert.Equal((400, "Produto do item 'Peça' não encontrado."), (r.Codigo, r.Erro));
        Assert.Equal(5m, (await api.ProdutoDaLojaNoBancoAsync(dela)).Estoque);
        Assert.Equal("em_andamento", (await api.OrcamentoNoBancoAsync(id)).Status);
    }

    [FatoIntegracao]
    public async Task Desfazer_nao_devolve_estoque_a_produto_de_outra_loja()
    {
        var loja = await NovaAsync();
        var outra = await NovaAsync();
        var cliente = await loja.ClienteIdAsync();
        var conta = await loja.ContaBancariaIdAsync();
        var meu = await api.ProdutoAsync(loja.Id, "Meu", estoque: 5);
        var dela = await api.ProdutoAsync(outra.Id, "Dela", estoque: 5);
        var id = await loja.OrcamentoEmAndamentoAsync(cliente, [Peca(meu, "Peça", 2, 10m)]);
        (await loja.ConcluirAsync(id, conta)).DeveSerSucesso();
        await api.NoBancoAsync(db => db.ItensOrcamentoServico.Where(i => i.OrcamentoId == id).ExecuteUpdateAsync(s => s.SetProperty(i => i.ProdutoId, dela)));

        (await loja.Painel.Patch($"{Orc}/{id}/desfazer-conclusao")).DeveSerSucesso();

        Assert.Equal(5m, (await api.ProdutoDaLojaNoBancoAsync(dela)).Estoque);
        Assert.Empty(await api.MovimentosDoProdutoAsync(dela));
    }

    [FatoIntegracao]
    public async Task Concluir_com_erro_de_pagamento_nao_deixa_a_baixa_de_estoque_gravada()
    {
        var loja = await NovaAsync();
        var cliente = await loja.ClienteIdAsync();
        var conta = await loja.ContaBancariaIdAsync();       // a categoria "Ordem de Serviço" ainda não existe nesta loja
        var produto = await api.ProdutoAsync(loja.Id, "Pastilha", estoque: 5);
        var id = await loja.OrcamentoEmAndamentoAsync(cliente, [Peca(produto, "Pastilha", 2, 10m)]);

        var r = await loja.ConcluirAsync(id, conta, jaPago: true);       // já pago, mas sem forma de pagamento

        Assert.Equal((400, "Informe a forma de pagamento."), (r.Codigo, r.Erro));
        Assert.Equal(5m, (await api.ProdutoDaLojaNoBancoAsync(produto)).Estoque);
        Assert.Empty(await api.MovimentosDoProdutoAsync(produto));
        Assert.Equal("em_andamento", (await api.OrcamentoNoBancoAsync(id)).Status);
    }
}
