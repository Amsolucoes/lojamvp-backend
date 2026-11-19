using System.Text.Json;
using LojaApi.Domain.Bonificacoes;
using Microsoft.EntityFrameworkCore;

namespace LojaApi.Tests.Api.VendasTeste;

// Bonificação dentro da venda: item marcado como brinde (só admin), com preço zero, estoque baixado, registro no cliente e fora dos relatórios de vendas.
[Collection(ColecaoApi.Nome)]
public class VendasBonificacaoApiTests(ApiFixture api)
{
    private Task<LojaTeste> NovaAsync() => api.NovaLojaDeServicosAsync();

    private static decimal Num(JsonElement e, string campo) => e.GetProperty(campo).GetDecimal();

    private Task<List<Bonificacao>> BonificacoesAsync(Guid lojaId) => api.NoBancoAsync(db => db.Bonificacoes.AsNoTracking().Where(b => b.LojaId == lojaId).ToListAsync());

    [FatoIntegracao]
    public async Task Brinde_na_venda_zera_o_preco_baixa_o_estoque_registra_no_cliente_e_nao_entra_no_total()
    {
        var loja = await NovaAsync();
        var cliente = await loja.ClienteIdAsync("Maria Souza");
        var camiseta = await api.ProdutoAsync(loja.Id, "Camiseta", estoque: 10, preco: 50m, custo: 20m);
        var pulseira = await api.ProdutoAsync(loja.Id, "Pulseira", estoque: 10, preco: 30m, custo: 8m);

        var r = await loja.Painel.Post("/api/vendas", CenariosVendas.CorpoVenda([CenariosVendas.ItemProduto(camiseta, 1, 50m), CenariosVendas.ItemBrinde(pulseira, 2, preco: 99m)], cliente));

        Assert.Equal(201, r.Codigo);
        var venda = r.Json.GetProperty("id").GetGuid();
        Assert.Equal((50m, 50m), (Num(r.Json, "total"), Num(r.Json, "totalFinal")));          // o preço 99 enviado no brinde é ignorado
        var itens = r.Json.GetProperty("itens").EnumerateArray().ToList();
        Assert.Equal(["id", "produtoId", "nomeProduto", "quantidade", "precoUnitario", "subtotal", "servicoId", "bonificacao"], itens[0].EnumerateObject().Select(x => x.Name));
        var brinde = itens.Single(i => i.GetProperty("bonificacao").GetBoolean());
        Assert.Equal(("Pulseira", 2m, 0m, 0m), (brinde.GetProperty("nomeProduto").GetString(), Num(brinde, "quantidade"), Num(brinde, "precoUnitario"), Num(brinde, "subtotal")));
        Assert.False(itens.Single(i => i.GetProperty("nomeProduto").GetString() == "Camiseta").GetProperty("bonificacao").GetBoolean());

        Assert.Equal((9m, 8m), ((await api.ProdutoDaLojaNoBancoAsync(camiseta)).Estoque, (await api.ProdutoDaLojaNoBancoAsync(pulseira)).Estoque));
        var movimentoBrinde = Assert.Single(await api.MovimentosDoProdutoAsync(pulseira));
        Assert.Equal(("saida", 2m, $"Bonificação na venda #{venda.ToString()[..8]} - Pulseira"), (movimentoBrinde.Tipo, movimentoBrinde.Quantidade, movimentoBrinde.Observacao));
        Assert.Equal($"Venda #{venda.ToString()[..8]} - Camiseta", Assert.Single(await api.MovimentosDoProdutoAsync(camiseta)).Observacao);

        var registro = Assert.Single(await BonificacoesAsync(loja.Id));
        Assert.Equal((cliente, "Maria Souza", pulseira, "Pulseira", 2m, 8m, venda), (registro.ClienteId, registro.NomeCliente, registro.ProdutoId, registro.NomeProduto, registro.Quantidade, registro.CustoUnitario, registro.VendaId));
        Assert.NotNull(registro.UsuarioId);
        var noCliente = Assert.Single((await loja.Painel.Get($"/api/bonificacoes?clienteId={cliente}")).Json.EnumerateArray());
        Assert.Equal((venda, 16m), (noCliente.GetProperty("vendaId").GetGuid(), Num(noCliente, "custoTotal")));
        var buscada = (await loja.Painel.Get($"/api/vendas/{venda}")).Json.GetProperty("itens").EnumerateArray();
        Assert.Single(buscada, i => i.GetProperty("bonificacao").GetBoolean());
    }

    [FatoIntegracao]
    public async Task Venda_so_de_brinde_fecha_com_total_zero()
    {
        var loja = await NovaAsync();
        var cliente = await loja.ClienteIdAsync();
        var pulseira = await api.ProdutoAsync(loja.Id, "Pulseira", estoque: 3);

        var r = await loja.Painel.Post("/api/vendas", CenariosVendas.CorpoVenda([CenariosVendas.ItemBrinde(pulseira)], cliente));

        Assert.Equal(201, r.Codigo);
        Assert.Equal((0m, 0m), (Num(r.Json, "total"), Num(r.Json, "totalFinal")));
        Assert.Equal(2m, (await api.ProdutoDaLojaNoBancoAsync(pulseira)).Estoque);
    }

    [FatoIntegracao]
    public async Task So_o_admin_da_brinde_e_o_operador_e_recusado_sem_gravar_nada()
    {
        var loja = await NovaAsync();
        var cliente = await loja.ClienteIdAsync();
        var camiseta = await api.ProdutoAsync(loja.Id, "Camiseta", estoque: 5);
        var pulseira = await api.ProdutoAsync(loja.Id, "Pulseira", estoque: 5);
        var operador = await api.OperadorAsync(loja);

        var brinde = await operador.Post("/api/vendas", CenariosVendas.CorpoVenda([CenariosVendas.ItemProduto(camiseta), CenariosVendas.ItemBrinde(pulseira)], cliente));
        var comum = await operador.Post("/api/vendas", CenariosVendas.CorpoVenda([CenariosVendas.ItemProduto(camiseta)], cliente));

        Assert.Equal((403, "Somente o administrador pode dar bonificação."), (brinde.Codigo, brinde.Erro));
        Assert.Equal(201, comum.Codigo);           // o operador continua vendendo normalmente
        Assert.Equal((4m, 5m), ((await api.ProdutoDaLojaNoBancoAsync(camiseta)).Estoque, (await api.ProdutoDaLojaNoBancoAsync(pulseira)).Estoque));
        Assert.Empty(await BonificacoesAsync(loja.Id));
        Assert.Equal(1, await api.NoBancoAsync(db => db.Vendas.CountAsync(v => v.LojaId == loja.Id)));
    }

    [FatoIntegracao]
    public async Task Brinde_exige_cliente_na_venda_e_so_vale_para_produto_com_estoque()
    {
        var loja = await NovaAsync();
        var cliente = await loja.ClienteIdAsync();
        var pulseira = await api.ProdutoAsync(loja.Id, "Pulseira", estoque: 1);
        var servico = await loja.ServicoIdAsync("Banho", 80);

        var semCliente = await loja.Painel.Post("/api/vendas", CenariosVendas.CorpoVenda([CenariosVendas.ItemBrinde(pulseira)]));
        var brindeDeServico = await loja.Painel.Post("/api/vendas", CenariosVendas.CorpoVenda([new { produtoId = (Guid?)null, quantidade = 1m, precoUnitario = 0m, variacaoId = (Guid?)null, servicoId = (Guid?)servico, agendamentoId = (Guid?)null, assinaturaId = (Guid?)null, bonificacao = true }], cliente));
        var semProduto = await loja.Painel.Post("/api/vendas", CenariosVendas.CorpoVenda([new { produtoId = (Guid?)null, quantidade = 1m, precoUnitario = 0m, variacaoId = (Guid?)null, servicoId = (Guid?)null, agendamentoId = (Guid?)null, assinaturaId = (Guid?)null, bonificacao = true }], cliente));
        var semEstoque = await loja.Painel.Post("/api/vendas", CenariosVendas.CorpoVenda([CenariosVendas.ItemBrinde(pulseira, 2)], cliente));

        Assert.Equal((400, "Informe o cliente para registrar a bonificação."), (semCliente.Codigo, semCliente.Erro));
        Assert.Equal((400, "Bonificação só vale para produtos."), (brindeDeServico.Codigo, brindeDeServico.Erro));
        Assert.Equal((400, "Bonificação só vale para produtos."), (semProduto.Codigo, semProduto.Erro));
        Assert.Equal((400, "Estoque insuficiente para 'Pulseira'."), (semEstoque.Codigo, semEstoque.Erro));
        Assert.Equal(1m, (await api.ProdutoDaLojaNoBancoAsync(pulseira)).Estoque);
        Assert.Empty(await BonificacoesAsync(loja.Id));
    }

    [FatoIntegracao]
    public async Task Brinde_de_variacao_baixa_so_a_variacao_e_leva_o_tamanho_e_a_cor()
    {
        var loja = await NovaAsync();
        var cliente = await loja.ClienteIdAsync();
        var anel = await api.ProdutoAsync(loja.Id, "Anel", estoque: 0, custo: 12m);
        var variacao = await api.VariacaoAsync(anel, "M", "Azul", estoque: 4);

        var r = await loja.Painel.Post("/api/vendas", CenariosVendas.CorpoVenda([CenariosVendas.ItemBrinde(anel, 1, variacao: variacao)], cliente));

        Assert.Equal(201, r.Codigo);
        Assert.Equal(3, (await api.VariacaoNoBancoAsync(variacao)).Estoque);
        var registro = Assert.Single(await BonificacoesAsync(loja.Id));
        Assert.Equal(("Anel (M / Azul)", variacao, 12m), (registro.NomeProduto, registro.VariacaoId, registro.CustoUnitario));
    }

    [FatoIntegracao]
    public async Task Brinde_nao_entra_na_comissao_nem_nos_relatorios_de_vendas()
    {
        var loja = await NovaAsync();
        var cliente = await loja.ClienteIdAsync();
        var camiseta = await api.ProdutoAsync(loja.Id, "Camiseta", estoque: 10, preco: 100m, custo: 40m);
        var pulseira = await api.ProdutoAsync(loja.Id, "Pulseira", estoque: 10, preco: 80m, custo: 8m);
        var vendedor = (await loja.NovoFuncionarioAsync("Vendedor", 10m, comissaoVendas: true, percentualVendas: 10m)).Json.GetProperty("id").GetGuid();

        var r = await loja.Painel.Post("/api/vendas", CenariosVendas.CorpoVenda([CenariosVendas.ItemProduto(camiseta, 1, 100m), CenariosVendas.ItemBrinde(pulseira, 3, preco: 80m)], cliente, funcionario: vendedor));
        var venda = r.Json.GetProperty("id").GetGuid();

        var comissao = await api.NoBancoAsync(db => db.ComissoesFuncionario.AsNoTracking().SingleAsync(c => c.OrigemId == venda));
        Assert.Equal((100m, 10m), (comissao.ValorServico, comissao.ValorComissao));    // só o que foi vendido (o brinde não comissiona)

        var hoje = DateTime.UtcNow;
        var de = Uri.EscapeDataString(hoje.AddDays(-1).ToString("o"));
        var ate = Uri.EscapeDataString(hoje.AddDays(1).ToString("o"));
        var resumo = (await loja.Painel.Get($"/api/relatorios/resumo?de={de}&ate={ate}")).Json;
        var ranking = (await loja.Painel.Get($"/api/relatorios/produtos-ranking?de={de}&ate={ate}")).Json.EnumerateArray().ToList();

        Assert.Equal((100m, 1m, 1), (Num(resumo, "totalVendido"), Num(resumo, "totalItensVendidos"), resumo.GetProperty("totalVendas").GetInt32()));
        Assert.Equal(["Camiseta"], ranking.Select(p => p.GetProperty("nome").GetString()));
    }

    [FatoIntegracao]
    public async Task Excluir_a_venda_devolve_o_estoque_do_brinde_e_apaga_o_registro_no_cliente()
    {
        var loja = await NovaAsync();
        var cliente = await loja.ClienteIdAsync();
        var camiseta = await api.ProdutoAsync(loja.Id, "Camiseta", estoque: 10);
        var pulseira = await api.ProdutoAsync(loja.Id, "Pulseira", estoque: 10);
        var venda = (await loja.Painel.Post("/api/vendas", CenariosVendas.CorpoVenda([CenariosVendas.ItemProduto(camiseta, 2), CenariosVendas.ItemBrinde(pulseira, 3)], cliente))).Json.GetProperty("id").GetGuid();
        Assert.Equal((8m, 7m), ((await api.ProdutoDaLojaNoBancoAsync(camiseta)).Estoque, (await api.ProdutoDaLojaNoBancoAsync(pulseira)).Estoque));
        var bonificacao = (await BonificacoesAsync(loja.Id)).Single().Id;

        var estornoDireto = await loja.Painel.Post($"/api/bonificacoes/{bonificacao}/estornar");
        var excluir = await loja.Painel.Delete($"/api/vendas/{venda}");

        Assert.Equal((400, "Esta bonificação foi dada dentro de uma venda. Para desfazer, exclua a venda."), (estornoDireto.Codigo, estornoDireto.Erro));
        Assert.Equal(200, excluir.Codigo);
        Assert.Equal((10m, 10m), ((await api.ProdutoDaLojaNoBancoAsync(camiseta)).Estoque, (await api.ProdutoDaLojaNoBancoAsync(pulseira)).Estoque));
        Assert.Equal("entrada", (await api.MovimentosDoProdutoAsync(pulseira)).Last().Tipo);
        Assert.Empty(await BonificacoesAsync(loja.Id));
        Assert.Empty((await loja.Painel.Get($"/api/bonificacoes?clienteId={cliente}&incluirEstornadas=true")).Json.EnumerateArray());
    }
}
