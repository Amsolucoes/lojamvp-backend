using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace LojaApi.Tests.Api.CarteiraTeste;

// Carteira do cliente ("fiado"): compra agora no caixa, paga depois. Usa uma loja de produtos (caixa, clientes, vendas).
[Collection(ColecaoApi.Nome)]
public class CarteiraApiTests(ApiFixture api)
{
    private sealed record Cenario(LojaTeste Loja, Guid ProdutoId, Guid ClienteId);

    private async Task<Cenario> PrepararAsync(decimal estoque = 100, string nomeCliente = "Maria da Carteira")
    {
        var loja = await api.NovaLojaAsync(perfilId: ApiFixture.PerfilLoja);
        var produto = await loja.Painel.Post("/api/produtos", new
        {
            nome = "Produto " + Guid.NewGuid().ToString("N")[..6], categoria = "Outro", precoCusto = 10, precoVenda = 100,
            estoque, estoqueMinimo = 0, ativo = true, tipoVenda = "unidade", unidadeMedida = "un",
        });
        produto.DeveSerSucesso();
        var cliente = await loja.Painel.Post("/api/clientes", new { nome = nomeCliente, telefone = "67999990000" });
        cliente.DeveSerSucesso();
        return new Cenario(loja, produto.Json.GetProperty("id").GetGuid(), cliente.Json.GetProperty("id").GetGuid());
    }

    private static string Formas(params (string forma, decimal valor)[] formas) =>
        JsonSerializer.Serialize(formas.Select(f => new { f.forma, f.valor, parcelas = 1 }));

    // Vende `quantidade` x R$ 100. Sem `formas`, a venda inteira vai para a carteira.
    private static Task<Resposta> Vender(Cenario c, int quantidade = 1, Guid? cliente = null, string? formas = null, string formaPrincipal = "carteira",
        decimal desconto = 0, bool semCliente = false) =>
        c.Loja.Painel.Post("/api/vendas", new
        {
            itens = new[] { new { produtoId = c.ProdutoId, quantidade, precoUnitario = 100m } },
            clienteId = semCliente ? (Guid?)null : cliente ?? c.ClienteId,
            desconto,
            formaPagamento = formaPrincipal,
            formasPagamento = formas ?? Formas(("carteira", quantidade * 100m - desconto)),
            troco = (decimal?)null, creditoUsado = (decimal?)null, dataVenda = (DateTime?)null, origemVendaId = (Guid?)null, funcionarioId = (Guid?)null,
        });

    private static async Task<JsonElement> Carteira(Cenario c, Guid? cliente = null) =>
        (await c.Loja.Painel.Get($"/api/clientes/{cliente ?? c.ClienteId}/carteira")).Json;

    private static Task<Resposta> Receber(Cenario c, decimal valor, string forma = "pix", Guid? cliente = null, string? data = null, string? obs = null) =>
        c.Loja.Painel.Post($"/api/clientes/{cliente ?? c.ClienteId}/carteira/pagamentos", new { valor, formaPagamento = forma, data, observacao = obs, contaBancariaId = (Guid?)null });

    private static async Task<decimal> Estoque(Cenario c) =>
        (await c.Loja.Painel.Get($"/api/produtos/{c.ProdutoId}")).Json.GetProperty("estoque").GetDecimal();

    // ── Vender na carteira ───────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Venda_inteira_na_carteira_vira_divida_do_cliente_e_baixa_o_estoque()
    {
        var c = await PrepararAsync(estoque: 10);
        var venda = await Vender(c, quantidade: 2);

        Assert.Equal(201, venda.Codigo);
        Assert.Equal(200m, venda.Json.GetProperty("valorCarteira").GetDecimal());
        Assert.Equal("carteira", venda.Json.GetProperty("formaPagamento").GetString());
        Assert.Equal(8m, await Estoque(c));   // vendeu de verdade, o estoque sai

        var carteira = await Carteira(c);
        Assert.Equal(200m, carteira.GetProperty("saldo").GetDecimal());
        Assert.Equal(1, carteira.GetProperty("emAberto").GetArrayLength());
        var mov = carteira.GetProperty("movimentos")[0];
        Assert.Equal("compra", mov.GetProperty("tipo").GetString());
        Assert.Equal(venda.Json.GetProperty("id").GetGuid(), mov.GetProperty("vendaId").GetGuid());
    }

    [FatoIntegracao]
    public async Task Venda_dividida_leva_para_a_carteira_so_a_parte_que_sobrou()
    {
        var c = await PrepararAsync();
        var venda = await Vender(c, quantidade: 1, formas: Formas(("pix", 40m), ("carteira", 60m)), formaPrincipal: "pix");

        Assert.Equal(201, venda.Codigo);
        Assert.Equal(60m, venda.Json.GetProperty("valorCarteira").GetDecimal());
        Assert.Equal("pix", venda.Json.GetProperty("formaPagamento").GetString());
        Assert.Equal(60m, (await Carteira(c)).GetProperty("saldo").GetDecimal());
    }

    [FatoIntegracao]
    public async Task Venda_sem_carteira_nao_mexe_na_carteira()
    {
        var c = await PrepararAsync();
        var venda = await Vender(c, formas: Formas(("pix", 100m)), formaPrincipal: "pix");

        Assert.Equal(201, venda.Codigo);
        Assert.Equal(JsonValueKind.Null, venda.Json.GetProperty("valorCarteira").ValueKind);
        Assert.Equal(0m, (await Carteira(c)).GetProperty("saldo").GetDecimal());
    }

    [FatoIntegracao]
    public async Task Venda_na_carteira_com_desconto_usa_o_valor_final()
    {
        var c = await PrepararAsync();
        var venda = await Vender(c, quantidade: 1, desconto: 15);
        Assert.Equal(85m, venda.Json.GetProperty("valorCarteira").GetDecimal());
        Assert.Equal(85m, (await Carteira(c)).GetProperty("saldo").GetDecimal());
    }

    [FatoIntegracao]
    public async Task Venda_na_carteira_exige_cliente_e_nao_deixa_rastro_quando_recusada()
    {
        var c = await PrepararAsync(estoque: 10);
        var venda = await Vender(c, semCliente: true);

        Assert.Equal(400, venda.Codigo);
        Assert.Equal("Selecione um cliente para vender na carteira.", venda.Erro);
        Assert.Equal(10m, await Estoque(c));                                                // estoque intacto
        Assert.Equal(0, (await c.Loja.Painel.Get("/api/vendas")).Json.GetArrayLength());   // nenhuma venda criada
    }

    [FatoIntegracao]
    public async Task Valor_na_carteira_nao_pode_passar_do_total_da_venda()
    {
        var c = await PrepararAsync(estoque: 10);
        var venda = await Vender(c, formas: Formas(("carteira", 150m)));   // venda de 100

        Assert.Equal(400, venda.Codigo);
        Assert.Equal("O valor na carteira não pode ser maior que o total da venda.", venda.Erro);
        Assert.Equal(10m, await Estoque(c));
    }

    // ── Limite ───────────────────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Limite_bloqueia_a_venda_que_estoura_e_libera_o_que_cabe()
    {
        var c = await PrepararAsync(estoque: 10);
        var limite = await c.Loja.Painel.Put($"/api/clientes/{c.ClienteId}/carteira/limite", new { limite = 150 });
        Assert.Equal(200, limite.Codigo);

        Assert.Equal(201, (await Vender(c)).Codigo);        // deve 100 de 150
        var estouro = await Vender(c);                       // +100 = 200 > 150
        Assert.Equal(400, estouro.Codigo);
        Assert.StartsWith("Limite da carteira excedido.", estouro.Erro);
        Assert.Contains("R$ 50,00", estouro.Erro);           // disponível
        Assert.Equal(9m, await Estoque(c));                  // a recusada não baixou estoque

        var carteira = await Carteira(c);
        Assert.Equal(100m, carteira.GetProperty("saldo").GetDecimal());
        Assert.Equal(150m, carteira.GetProperty("limite").GetDecimal());
        Assert.Equal(50m, carteira.GetProperty("disponivel").GetDecimal());
    }

    [FatoIntegracao]
    public async Task Pagar_libera_de_novo_o_limite()
    {
        var c = await PrepararAsync();
        await c.Loja.Painel.Put($"/api/clientes/{c.ClienteId}/carteira/limite", new { limite = 100 });
        await Vender(c);
        Assert.Equal(400, (await Vender(c)).Codigo);

        (await Receber(c, 100m)).DeveSerSucesso();
        Assert.Equal(201, (await Vender(c)).Codigo);
    }

    [FatoIntegracao]
    public async Task Limite_pode_ser_removido_e_nao_aceita_valor_negativo()
    {
        var c = await PrepararAsync();
        await c.Loja.Painel.Put($"/api/clientes/{c.ClienteId}/carteira/limite", new { limite = 100 });

        Assert.Equal("O limite não pode ser negativo.", (await c.Loja.Painel.Put($"/api/clientes/{c.ClienteId}/carteira/limite", new { limite = -1 })).Erro);
        Assert.Equal(200, (await c.Loja.Painel.Put($"/api/clientes/{c.ClienteId}/carteira/limite", new { limite = (decimal?)null })).Codigo);
        Assert.Equal(JsonValueKind.Null, (await Carteira(c)).GetProperty("limite").ValueKind);
        Assert.Equal(201, (await Vender(c, quantidade: 5)).Codigo);   // sem limite, vende
    }

    // ── Receber o pagamento ──────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Recebimento_baixa_a_divida_e_entra_no_caixa_na_data_do_pagamento()
    {
        var c = await PrepararAsync();
        await Vender(c, quantidade: 2);   // deve 200

        var parcial = await Receber(c, 120m, "pix", obs: "fim do mês");
        Assert.Equal(200, parcial.Codigo);
        Assert.Equal(80m, parcial.Json.GetProperty("saldo").GetDecimal());
        Assert.False(parcial.Json.GetProperty("quitado").GetBoolean());

        var carteira = await Carteira(c);
        Assert.Equal(80m, carteira.GetProperty("saldo").GetDecimal());
        Assert.Equal(80m, carteira.GetProperty("emAberto")[0].GetProperty("emAberto").GetDecimal());

        // o dinheiro entrou no caixa como uma entrada
        var entradas = (await c.Loja.Painel.Get("/api/movimentos-caixa")).Json.EnumerateArray().ToList();
        var entrada = Assert.Single(entradas);
        Assert.Equal("entrada", entrada.GetProperty("tipo").GetString());
        Assert.Equal(120m, entrada.GetProperty("valor").GetDecimal());
        Assert.Contains("Maria da Carteira", entrada.GetProperty("observacao").GetString());
        Assert.Contains("Pix", entrada.GetProperty("observacao").GetString());

        var quitar = await Receber(c, 80m, "dinheiro");
        Assert.True(quitar.Json.GetProperty("quitado").GetBoolean());
        Assert.Equal(0m, (await Carteira(c)).GetProperty("saldo").GetDecimal());
        Assert.Equal(0, (await Carteira(c)).GetProperty("emAberto").GetArrayLength());
    }

    [FatoIntegracao]
    public async Task Compra_na_carteira_nao_entra_no_caixa_ate_o_cliente_pagar()
    {
        var c = await PrepararAsync();
        await Vender(c);
        Assert.Equal(0, (await c.Loja.Painel.Get("/api/movimentos-caixa")).Json.GetArrayLength());
    }

    [FatoIntegracao]
    public async Task Recebimento_valida_valor_forma_data_e_saldo()
    {
        var c = await PrepararAsync();
        Assert.Equal("O cliente deve apenas R$ 0,00 na carteira.", (await Receber(c, 10m)).Erro);   // sem dívida

        await Vender(c);   // deve 100
        Assert.Equal("O cliente deve apenas R$ 100,00 na carteira.", (await Receber(c, 100.01m)).Erro);
        Assert.Equal("Informe um valor maior que zero.", (await Receber(c, 0m)).Erro);
        Assert.Equal("Forma de pagamento inválida.", (await Receber(c, 10m, "carteira")).Erro);
        Assert.Equal("A data não pode ser no futuro.", (await Receber(c, 10m, data: DateTime.UtcNow.AddDays(3).ToString("yyyy-MM-dd"))).Erro);

        Assert.Equal(100m, (await Carteira(c)).GetProperty("saldo").GetDecimal());   // nada disso mexeu na dívida
        Assert.Equal(0, (await c.Loja.Painel.Get("/api/movimentos-caixa")).Json.GetArrayLength());
    }

    [FatoIntegracao]
    public async Task Recebimento_pode_ter_data_passada()
    {
        var c = await PrepararAsync();
        await Vender(c);
        var ontem = DateTime.UtcNow.AddDays(-2).ToString("yyyy-MM-dd");

        (await Receber(c, 30m, data: ontem)).DeveSerSucesso();

        var entrada = (await c.Loja.Painel.Get("/api/movimentos-caixa")).Json[0];
        Assert.StartsWith(ontem, entrada.GetProperty("data").GetString());
    }

    [FatoIntegracao]
    public async Task Desfazer_recebimento_devolve_a_divida_e_tira_a_entrada_do_caixa()
    {
        var c = await PrepararAsync();
        await Vender(c);
        var pagamento = await Receber(c, 100m);
        Assert.Equal(0m, (await Carteira(c)).GetProperty("saldo").GetDecimal());

        var desfeito = await c.Loja.Painel.Delete($"/api/clientes/{c.ClienteId}/carteira/pagamentos/{pagamento.Json.GetProperty("id").GetGuid()}");

        Assert.Equal(200, desfeito.Codigo);
        Assert.Equal(100m, desfeito.Json.GetProperty("saldo").GetDecimal());
        Assert.Equal(100m, (await Carteira(c)).GetProperty("saldo").GetDecimal());
        Assert.Equal(0, (await c.Loja.Painel.Get("/api/movimentos-caixa")).Json.GetArrayLength());
        Assert.Equal(404, (await c.Loja.Painel.Delete($"/api/clientes/{c.ClienteId}/carteira/pagamentos/{pagamento.Json.GetProperty("id").GetGuid()}")).Codigo);
    }

    [FatoIntegracao]
    public async Task Extrato_mostra_o_saldo_depois_de_cada_lancamento_do_mais_novo_para_o_mais_antigo()
    {
        var c = await PrepararAsync();
        await Vender(c, quantidade: 1);
        await Vender(c, quantidade: 2);
        await Receber(c, 50m);

        var movs = (await Carteira(c)).GetProperty("movimentos").EnumerateArray().ToList();
        Assert.Equal(3, movs.Count);
        Assert.Equal("pagamento", movs[0].GetProperty("tipo").GetString());
        Assert.Equal(new[] { 250m, 300m, 100m }, movs.Select(m => m.GetProperty("saldoApos").GetDecimal()));
    }

    // ── Excluir venda e excluir cliente ──────────────────────────────────────

    [FatoIntegracao]
    public async Task Excluir_a_venda_tira_a_compra_da_carteira_e_devolve_o_estoque()
    {
        var c = await PrepararAsync(estoque: 10);
        var venda = await Vender(c, quantidade: 2);

        var exclusao = await c.Loja.Painel.Delete($"/api/vendas/{venda.Json.GetProperty("id").GetGuid()}");

        Assert.Equal(200, exclusao.Codigo);
        Assert.Equal(0m, (await Carteira(c)).GetProperty("saldo").GetDecimal());
        Assert.Equal(10m, await Estoque(c));
    }

    [FatoIntegracao]
    public async Task Venda_ja_paga_na_carteira_so_pode_ser_excluida_depois_de_desfazer_o_recebimento()
    {
        var c = await PrepararAsync(estoque: 10);
        var venda = await Vender(c);
        var vendaId = venda.Json.GetProperty("id").GetGuid();
        var pagamento = await Receber(c, 100m);

        var bloqueada = await c.Loja.Painel.Delete($"/api/vendas/{vendaId}");
        Assert.Equal(400, bloqueada.Codigo);
        Assert.Contains("já pagou parte do saldo", bloqueada.Erro);
        Assert.Equal(9m, await Estoque(c));                                                   // a recusa não estornou nada
        Assert.Equal(1, (await c.Loja.Painel.Get("/api/vendas")).Json.GetArrayLength());

        await c.Loja.Painel.Delete($"/api/clientes/{c.ClienteId}/carteira/pagamentos/{pagamento.Json.GetProperty("id").GetGuid()}");
        Assert.Equal(200, (await c.Loja.Painel.Delete($"/api/vendas/{vendaId}")).Codigo);
        Assert.Equal(10m, await Estoque(c));
    }

    [FatoIntegracao]
    public async Task Cliente_com_saldo_na_carteira_nao_pode_ser_excluido()
    {
        var c = await PrepararAsync();
        await Vender(c);

        var r = await c.Loja.Painel.Delete($"/api/clientes/{c.ClienteId}");
        Assert.Equal(400, r.Codigo);
        Assert.Contains("saldo a pagar na carteira", r.Erro);

        await Receber(c, 100m);
        Assert.Equal(204, (await c.Loja.Painel.Delete($"/api/clientes/{c.ClienteId}")).Codigo);
    }

    // ── Cadastro de clientes e lista de devedores ────────────────────────────

    [FatoIntegracao]
    public async Task Lista_de_clientes_mostra_saldo_e_limite_da_carteira()
    {
        var c = await PrepararAsync();
        await c.Loja.Painel.Put($"/api/clientes/{c.ClienteId}/carteira/limite", new { limite = 500 });
        await Vender(c, quantidade: 2);

        var lista = (await c.Loja.Painel.Get("/api/clientes")).Json;
        var cliente = lista.EnumerateArray().Single();
        Assert.Equal(200m, cliente.GetProperty("saldoCarteira").GetDecimal());
        Assert.Equal(500m, cliente.GetProperty("limiteCarteira").GetDecimal());

        var um = (await c.Loja.Painel.Get($"/api/clientes/{c.ClienteId}")).Json;
        Assert.Equal(200m, um.GetProperty("saldoCarteira").GetDecimal());
    }

    [FatoIntegracao]
    public async Task Editar_o_cadastro_do_cliente_nao_apaga_o_limite_da_carteira()
    {
        var c = await PrepararAsync();
        await c.Loja.Painel.Put($"/api/clientes/{c.ClienteId}/carteira/limite", new { limite = 500 });

        var edicao = await c.Loja.Painel.Put($"/api/clientes/{c.ClienteId}", new { nome = "Maria Editada", telefone = "67911112222" });

        Assert.Equal(200, edicao.Codigo);
        Assert.Equal(500m, edicao.Json.GetProperty("limiteCarteira").GetDecimal());
        Assert.Equal(500m, (await Carteira(c)).GetProperty("limite").GetDecimal());
    }

    [FatoIntegracao]
    public async Task Devedores_lista_quem_deve_do_maior_para_o_menor_e_some_quando_quita()
    {
        var c = await PrepararAsync(nomeCliente: "Pequena");
        var grande = (await c.Loja.Painel.Post("/api/clientes", new { nome = "Grande", telefone = "67988880000" })).Json.GetProperty("id").GetGuid();
        var quitada = (await c.Loja.Painel.Post("/api/clientes", new { nome = "Quitada", telefone = "67977770000" })).Json.GetProperty("id").GetGuid();

        await Vender(c, quantidade: 1);                         // Pequena deve 100
        await Vender(c, quantidade: 3, cliente: grande);        // Grande deve 300
        await Vender(c, quantidade: 1, cliente: quitada);
        await Receber(c, 100m, cliente: quitada);

        var r = (await c.Loja.Painel.Get("/api/carteira/devedores")).Json;
        Assert.Equal(400m, r.GetProperty("total").GetDecimal());
        Assert.Equal(["Grande", "Pequena"], r.GetProperty("clientes").EnumerateArray().Select(x => x.GetProperty("nome").GetString()));
        Assert.Equal(300m, r.GetProperty("clientes")[0].GetProperty("saldo").GetDecimal());
    }

    // ── Fluxo de caixa: venda na carteira que vai sendo quitada ──────────────

    [FatoIntegracao]
    public async Task Movimentacao_mostra_o_que_falta_receber_de_cada_venda_e_por_onde_foi_paga()
    {
        var c = await PrepararAsync();
        var venda = await Vender(c, quantidade: 1, formas: Formas(("dinheiro", 30m), ("carteira", 70m)), formaPrincipal: "dinheiro");
        var vendaId = venda.Json.GetProperty("id").GetGuid();

        var antes = (await c.Loja.Painel.Get("/api/carteira/movimentacao")).Json;
        var v0 = antes.GetProperty("vendas").EnumerateArray().Single();
        Assert.Equal(vendaId, v0.GetProperty("vendaId").GetGuid());
        Assert.Equal(70m, v0.GetProperty("emAberto").GetDecimal());
        Assert.Equal(0, v0.GetProperty("pagamentos").GetArrayLength());

        await Receber(c, 70m, "pix");

        var depois = (await c.Loja.Painel.Get("/api/carteira/movimentacao")).Json;
        var v1 = depois.GetProperty("vendas").EnumerateArray().Single();
        Assert.Equal(0m, v1.GetProperty("emAberto").GetDecimal());
        var parte = v1.GetProperty("pagamentos").EnumerateArray().Single();
        Assert.Equal("pix", parte.GetProperty("forma").GetString());
        Assert.Equal(70m, parte.GetProperty("valor").GetDecimal());

        var rec = depois.GetProperty("recebimentos").EnumerateArray().Single();
        Assert.Equal("Maria da Carteira", rec.GetProperty("clienteNome").GetString());
        Assert.Equal(vendaId, rec.GetProperty("abateu")[0].GetProperty("vendaId").GetGuid());
        // o recebimento aponta para a entrada criada no caixa (o fluxo de caixa usa isso para não contá-la como "ajuste")
        var entradaCaixa = (await c.Loja.Painel.Get("/api/movimentos-caixa")).Json.EnumerateArray().Single();
        Assert.Equal(entradaCaixa.GetProperty("id").GetGuid(), rec.GetProperty("movimentoCaixaId").GetGuid());
    }

    [FatoIntegracao]
    public async Task Movimentacao_de_uma_loja_nao_inclui_clientes_de_outra()
    {
        var a = await PrepararAsync();
        var b = await PrepararAsync();
        await Vender(a);
        await Receber(a, 40m);

        var daB = (await b.Loja.Painel.Get("/api/carteira/movimentacao")).Json;
        Assert.Equal(0, daB.GetProperty("vendas").GetArrayLength());
        Assert.Equal(0, daB.GetProperty("recebimentos").GetArrayLength());
        Assert.Equal(401, (await a.Loja.Site.Get("/api/carteira/movimentacao")).Codigo);
    }

    // ── Conta bancária (Financeiro) ──────────────────────────────────────────

    [FatoIntegracao]
    public async Task Recebimento_pode_ser_lancado_numa_conta_bancaria_e_desfazer_tira_de_la()
    {
        var c = await PrepararAsync();
        var conta = await c.Loja.Painel.Post("/api/financeiro/contas", new { nome = "Conta Pix", saldoInicial = 0, banco = "Nubank", limite = 0 });
        conta.DeveSerSucesso();
        var contaId = conta.Json.GetProperty("id").GetGuid();
        await Vender(c);

        var recebimento = await c.Loja.Painel.Post($"/api/clientes/{c.ClienteId}/carteira/pagamentos",
            new { valor = 100m, formaPagamento = "pix", data = (string?)null, observacao = (string?)null, contaBancariaId = contaId });
        recebimento.DeveSerSucesso();

        var ajustes = await api.NoBancoAsync(db => db.AjustesContaBancaria.AsNoTracking().Where(a => a.ContaBancariaId == contaId).ToListAsync());
        var ajuste = Assert.Single(ajustes);
        Assert.Equal(("entrada", 100m), (ajuste.Tipo, ajuste.Valor));
        Assert.Contains("carteira", ajuste.Observacao, StringComparison.OrdinalIgnoreCase);
        var entradaCaixa = (await c.Loja.Painel.Get("/api/movimentos-caixa")).Json.EnumerateArray().Single();
        Assert.Equal(contaId, entradaCaixa.GetProperty("contaBancariaId").GetGuid());

        (await c.Loja.Painel.Delete($"/api/clientes/{c.ClienteId}/carteira/pagamentos/{recebimento.Json.GetProperty("id").GetGuid()}")).DeveSerSucesso();
        Assert.Empty(await api.NoBancoAsync(db => db.AjustesContaBancaria.AsNoTracking().Where(a => a.ContaBancariaId == contaId).ToListAsync()));
    }

    [FatoIntegracao]
    public async Task Conta_bancaria_de_outra_loja_e_ignorada_no_recebimento()
    {
        var a = await PrepararAsync();
        var b = await PrepararAsync();
        var contaDaB = (await b.Loja.Painel.Post("/api/financeiro/contas", new { nome = "Conta da B", saldoInicial = 0, banco = "X", limite = 0 })).Json.GetProperty("id").GetGuid();
        await Vender(a);

        (await a.Loja.Painel.Post($"/api/clientes/{a.ClienteId}/carteira/pagamentos",
            new { valor = 100m, formaPagamento = "pix", data = (string?)null, observacao = (string?)null, contaBancariaId = contaDaB })).DeveSerSucesso();

        Assert.Empty(await api.NoBancoAsync(db => db.AjustesContaBancaria.AsNoTracking().Where(x => x.ContaBancariaId == contaDaB).ToListAsync()));
    }

    // ── Segurança: uma loja não mexe na carteira de outra ────────────────────

    [FatoIntegracao]
    public async Task Loja_nao_ve_nem_altera_a_carteira_de_cliente_de_outra_loja()
    {
        var a = await PrepararAsync();
        var b = await PrepararAsync();
        await Vender(a, quantidade: 2);   // cliente da loja A deve 200

        Assert.Equal(404, (await b.Loja.Painel.Get($"/api/clientes/{a.ClienteId}/carteira")).Codigo);
        Assert.Equal(404, (await b.Loja.Painel.Put($"/api/clientes/{a.ClienteId}/carteira/limite", new { limite = 1 })).Codigo);
        Assert.Equal(404, (await Receber(b, 50m, cliente: a.ClienteId)).Codigo);

        Assert.Equal(0m, (await b.Loja.Painel.Get("/api/carteira/devedores")).Json.GetProperty("total").GetDecimal());
        Assert.Equal(200m, (await Carteira(a)).GetProperty("saldo").GetDecimal());          // intacta
        Assert.Equal(0, (await a.Loja.Painel.Get("/api/movimentos-caixa")).Json.GetArrayLength());
    }

    [FatoIntegracao]
    public async Task Loja_nao_consegue_lancar_venda_na_carteira_de_cliente_de_outra_loja()
    {
        var a = await PrepararAsync();
        var b = await PrepararAsync(estoque: 10);

        var venda = await Vender(b, cliente: a.ClienteId);   // loja B usando o cliente da loja A

        Assert.Equal(400, venda.Codigo);
        Assert.Equal("Cliente não encontrado.", venda.Erro);
        Assert.Equal(10m, await Estoque(b));
        Assert.Equal(0m, (await Carteira(a)).GetProperty("saldo").GetDecimal());
    }

    [FatoIntegracao]
    public async Task Carteira_exige_login()
    {
        var c = await PrepararAsync();
        Assert.Equal(401, (await c.Loja.Site.Get($"/api/clientes/{c.ClienteId}/carteira")).Codigo);
        Assert.Equal(401, (await c.Loja.Site.Get("/api/carteira/devedores")).Codigo);
        Assert.Equal(401, (await c.Loja.Site.Post($"/api/clientes/{c.ClienteId}/carteira/pagamentos", new { valor = 1, formaPagamento = "pix" })).Codigo);
    }
}
