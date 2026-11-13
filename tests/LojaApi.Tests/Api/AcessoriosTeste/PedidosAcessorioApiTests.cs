using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace LojaApi.Tests.Api.AcessoriosTeste;

// Pedidos de acessórios. Existem DUAS famílias de rotas com comportamentos parecidos, e as duas são mantidas como estão:
//   A) /api/loja-acessorios/...           (frete, Pix, cartão, status simples, lista resumida, status que devolve estoque)
//   B) /api/loja-acessorios/produtos/...  (frete com subtotal, Pix, status que reativa pedido vencido, lista e status devolvendo o pedido)
[Collection(ColecaoApi.Nome)]
public class PedidosAcessorioApiTests(ApiFixture api)
{
    private const string A = CenariosAcessorios.Base;
    private const string B = CenariosAcessorios.Produtos;

    private static Guid Id(JsonElement e) => e.GetProperty("id").GetGuid();

    // ── Frete ────────────────────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Frete_por_estado_na_rota_A()
    {
        var (_, visitante, _) = await api.AcessoriosAsync();

        async Task<decimal> Frete(string uf) => (await visitante.Get($"{A}/frete?uf={uf}")).Json.GetProperty("valorFrete").GetDecimal();

        Assert.Equal((15m, 25m, 25m, 35m), (await Frete("MS"), await Frete("sp"), await Frete("MT"), await Frete("AM")));
    }

    [FatoIntegracao]
    public async Task Frete_na_rota_B_traz_o_indicador_de_gratis_e_ignora_o_subtotal_enquanto_nao_ha_limite()
    {
        var (_, visitante, _) = await api.AcessoriosAsync();

        var ms = (await visitante.Get($"{B}/frete?uf=ms&subtotal=99999")).Json;
        var am = (await visitante.Get($"{B}/frete?uf=AM")).Json;

        Assert.Equal((15m, false), (ms.GetProperty("valorFrete").GetDecimal(), ms.GetProperty("gratis").GetBoolean()));
        Assert.Equal((35m, false), (am.GetProperty("valorFrete").GetDecimal(), am.GetProperty("gratis").GetBoolean()));
    }

    // ── Criar pedido com Pix (as duas rotas se comportam igual) ──────────────

    public static IEnumerable<object[]> RotasDoPix() => [[A + "/pedidos"], [B + "/pedidos"]];

    [FatoIntegracao]
    public async Task Pix_valida_itens_cpf_produto_e_estoque_sem_gravar_nada()
    {
        var (super, visitante, _) = await api.AcessoriosAsync();
        var produto = await super.ProdutoAcessorioAsync("Mouse", estoque: 2);
        var inativo = await super.ProdutoAcessorioAsync(ativo: false, estoque: 5);

        foreach (var rota in new[] { A + "/pedidos", B + "/pedidos" })
        {
            var semItens = await visitante.Post(rota, CenariosAcessorios.CorpoPedido(Array.Empty<(Guid, int)>()));
            Assert.Equal((400, "O pedido precisa ter ao menos um item."), (semItens.Codigo, semItens.Erro));

            var semCpf = await visitante.Post(rota, CenariosAcessorios.CorpoPedido(produto, cpf: "  "));
            Assert.Equal((400, "CPF é obrigatório para gerar o Pix."), (semCpf.Codigo, semCpf.Erro));

            var semProduto = await visitante.Post(rota, CenariosAcessorios.CorpoPedido(Guid.NewGuid()));
            Assert.Equal((400, "Produto não encontrado."), (semProduto.Codigo, semProduto.Erro));

            var produtoInativo = await visitante.Post(rota, CenariosAcessorios.CorpoPedido(inativo));
            Assert.Equal((400, "Produto não encontrado."), (produtoInativo.Codigo, produtoInativo.Erro));

            var semEstoque = await visitante.Post(rota, CenariosAcessorios.CorpoPedido(produto, 3));
            Assert.Equal((400, "Estoque insuficiente para 'Mouse'. Disponível: 2."), (semEstoque.Codigo, semEstoque.Erro));
        }

        Assert.Equal(2, (await api.ProdutoNoBancoAsync(produto)).Estoque);
        Assert.Equal(0, await api.NoBancoAsync(db => db.PedidosAcessorio.CountAsync(p => p.ClienteEmail == "maria@teste.com" && p.Itens.Any(i => i.ProdutoId == produto))));
    }

    [FatoIntegracao]
    public async Task Pix_cria_o_pedido_reserva_o_estoque_e_devolve_o_qr_code()
    {
        var (super, visitante, _) = await api.AcessoriosAsync();
        var normal = await super.ProdutoAcessorioAsync("Normal", 100m, 10);
        var promocao = await super.ProdutoAcessorioAsync("Promo", 80m, 10, promo: 60m);

        foreach (var (rota, uf, frete) in new[] { (A + "/pedidos", " ms ", 15m), (B + "/pedidos", "sp", 25m) })
        {
            var r = await visitante.Post(rota, CenariosAcessorios.CorpoPedido([(normal, 2), (promocao, 1)], uf));

            Assert.Equal(200, r.Codigo);
            var j = r.Json;
            Assert.Equal((260m + frete, "000201-qr-de-teste", "QkFTRTY0"), (j.GetProperty("total").GetDecimal(), j.GetProperty("qrCode").GetString(), j.GetProperty("qrCodeBase64").GetString()));

            var pedido = await api.PedidoNoBancoAsync(Id(j));
            Assert.Equal(("aguardando_pagamento", 260m, frete, 260m + frete, "pending"), (pedido.Status, pedido.Subtotal, pedido.ValorFrete, pedido.Total, pedido.MpStatus));
            Assert.StartsWith("pix-", pedido.MpPaymentId);
            Assert.Equal(("Maria Souza", "maria@teste.com", "67999990000", "12345678909", "79000-000", "Rua A", "Campo Grande", uf.Trim().ToUpper()),
                (pedido.ClienteNome, pedido.ClienteEmail, pedido.ClienteTelefone, pedido.ClienteCpfCnpj, pedido.Cep, pedido.Endereco, pedido.Cidade, pedido.Uf));
            Assert.InRange(pedido.ExpiraEm!.Value, DateTime.UtcNow.AddMinutes(29), DateTime.UtcNow.AddMinutes(31));
            Assert.Equal([("Normal", 2, 100m, 200m), ("Promo", 1, 60m, 60m)], pedido.Itens.OrderBy(i => i.NomeProduto).Select(i => (i.NomeProduto, i.Quantidade, i.PrecoUnitario, i.Subtotal)).ToList());
        }

        Assert.Equal(6, (await api.ProdutoNoBancoAsync(normal)).Estoque);     // 10 - 2 - 2
        Assert.Equal(8, (await api.ProdutoNoBancoAsync(promocao)).Estoque);   // 10 - 1 - 1
    }

    [FatoIntegracao]
    public async Task Pix_que_falha_no_mercado_pago_devolve_o_estoque_e_apaga_o_pedido()
    {
        var (super, visitante, _) = await api.AcessoriosAsync();
        var produto = await super.ProdutoAcessorioAsync(estoque: 5);
        api.MercadoPago.FalharPix = true;
        try
        {
            foreach (var rota in new[] { A + "/pedidos", B + "/pedidos" })
            {
                var r = await visitante.Post(rota, CenariosAcessorios.CorpoPedido(produto, 2));
                Assert.Equal((400, "Não foi possível gerar o pagamento. Tente novamente."), (r.Codigo, r.Erro));
            }
        }
        finally { api.MercadoPago.FalharPix = false; }

        Assert.Equal(5, (await api.ProdutoNoBancoAsync(produto)).Estoque);
        Assert.Equal(0, await api.NoBancoAsync(db => db.ItensPedidoAcessorio.CountAsync(i => i.ProdutoId == produto)));
    }

    [FatoIntegracao]
    public async Task Criar_pedido_libera_antes_o_estoque_de_pedidos_vencidos()
    {
        var (super, visitante, _) = await api.AcessoriosAsync();
        var produto = await super.ProdutoAcessorioAsync(estoque: 2);
        var antigo = await visitante.PedidoPixAsync(produto, 2);
        await api.VencerPedidoAsync(antigo);

        // sem a liberação, só restariam 0 unidades e este pedido seria recusado
        var r = await visitante.Post(A + "/pedidos", CenariosAcessorios.CorpoPedido(produto, 2));

        Assert.Equal(200, r.Codigo);
        Assert.Equal("cancelado", (await api.PedidoNoBancoAsync(antigo)).Status);
        Assert.Equal(0, (await api.ProdutoNoBancoAsync(produto)).Estoque);
    }

    // ── Criar pedido com cartão (só na rota A) ───────────────────────────────

    [FatoIntegracao]
    public async Task Cartao_valida_itens_e_cpf()
    {
        var (super, visitante, _) = await api.AcessoriosAsync();
        var produto = await super.ProdutoAcessorioAsync("Teclado", estoque: 1);

        var semItens = await visitante.Post(A + "/pedidos/cartao", new { clienteNome = "M", clienteEmail = "m@t.com", clienteTelefone = "1", clienteCpfCnpj = "123", cep = "1", endereco = "R", cidade = "C", uf = "MS", itens = Array.Empty<object>(), token = "t", paymentMethodId = "visa", installments = 1, issuerId = "1" });
        Assert.Equal((400, "O pedido precisa ter ao menos um item."), (semItens.Codigo, semItens.Erro));

        var semCpf = await visitante.Post(A + "/pedidos/cartao", CenariosAcessorios.CorpoPedidoCartao(produto, cpf: ""));
        Assert.Equal((400, "CPF é obrigatório."), (semCpf.Codigo, semCpf.Erro));

        var semProduto = await visitante.Post(A + "/pedidos/cartao", CenariosAcessorios.CorpoPedidoCartao(Guid.NewGuid()));
        Assert.Equal((400, "Produto não encontrado."), (semProduto.Codigo, semProduto.Erro));

        var semEstoque = await visitante.Post(A + "/pedidos/cartao", CenariosAcessorios.CorpoPedidoCartao(produto, 2));
        Assert.Equal((400, "Estoque insuficiente para 'Teclado'. Disponível: 1."), (semEstoque.Codigo, semEstoque.Erro));
        Assert.Equal(1, (await api.ProdutoNoBancoAsync(produto)).Estoque);
    }

    [FatoIntegracao]
    public async Task Cartao_aprovado_marca_o_pedido_como_pago_na_hora()
    {
        var (super, visitante, _) = await api.AcessoriosAsync();
        var produto = await super.ProdutoAcessorioAsync(preco: 100m, estoque: 4);

        var r = await visitante.Post(A + "/pedidos/cartao", CenariosAcessorios.CorpoPedidoCartao(produto, 2, "sp"));

        Assert.Equal(200, r.Codigo);
        Assert.Equal(("pago", 225m), (r.Json.GetProperty("status").GetString(), r.Json.GetProperty("total").GetDecimal()));
        var pedido = await api.PedidoNoBancoAsync(Id(r.Json));
        Assert.Equal(("pago", "approved"), (pedido.Status, pedido.MpStatus));
        Assert.StartsWith("cartao-", pedido.MpPaymentId);
        Assert.NotNull(pedido.PagoEm);
        Assert.Null(pedido.ExpiraEm);
        Assert.Equal(2, (await api.ProdutoNoBancoAsync(produto)).Estoque);
        var cobranca = api.MercadoPago.CobrancasDeCartao.Last();
        Assert.Equal((225m, 3), (cobranca.Valor, cobranca.Parcelas));
    }

    [FatoIntegracao]
    public async Task Cartao_recusado_devolve_o_estoque_e_apaga_o_pedido()
    {
        var (super, visitante, _) = await api.AcessoriosAsync();
        var produto = await super.ProdutoAcessorioAsync(estoque: 4);

        var r = await visitante.Post(A + "/pedidos/cartao", CenariosAcessorios.CorpoPedidoCartao(produto, 3, token: "cartao-recusado"));

        Assert.Equal((400, "Cartão recusado pelo banco emissor."), (r.Codigo, r.Erro));
        Assert.Equal(4, (await api.ProdutoNoBancoAsync(produto)).Estoque);
        Assert.Equal(0, await api.NoBancoAsync(db => db.ItensPedidoAcessorio.CountAsync(i => i.ProdutoId == produto)));
    }

    // ── Consultar o status do pagamento ──────────────────────────────────────

    [FatoIntegracao]
    public async Task Status_na_rota_A_so_aprova_quando_o_mercado_pago_aprova_e_404_para_inexistente()
    {
        var (super, visitante, _) = await api.AcessoriosAsync();
        var produto = await super.ProdutoAcessorioAsync();
        var pedido = await visitante.PedidoPixAsync(produto, rota: A + "/pedidos");

        Assert.Equal("aguardando_pagamento", (await visitante.Get($"{A}/pedidos/{pedido}/status")).Json.GetProperty("status").GetString());

        api.MercadoPago.Aprovar((await api.PedidoNoBancoAsync(pedido)).MpPaymentId!);
        Assert.Equal("pago", (await visitante.Get($"{A}/pedidos/{pedido}/status")).Json.GetProperty("status").GetString());

        var pago = await api.PedidoNoBancoAsync(pedido);
        Assert.Equal(("pago", "approved"), (pago.Status, pago.MpStatus));
        Assert.NotNull(pago.PagoEm);
        Assert.Null(pago.ExpiraEm);
        Assert.Equal(404, (await visitante.Get($"{A}/pedidos/{Guid.NewGuid()}/status")).Codigo);
    }

    [FatoIntegracao]
    public async Task Status_na_rota_A_nao_reativa_pedido_cancelado()
    {
        var (super, visitante, _) = await api.AcessoriosAsync();
        var produto = await super.ProdutoAcessorioAsync(estoque: 3);
        var pedido = await visitante.PedidoPixAsync(produto, 1, A + "/pedidos");
        api.MercadoPago.Aprovar((await api.PedidoNoBancoAsync(pedido)).MpPaymentId!);
        await api.VencerPedidoAsync(pedido);
        await visitante.Get($"{B}?categoria=nenhuma");    // vence e cancela

        var r = await visitante.Get($"{A}/pedidos/{pedido}/status");

        Assert.Equal("cancelado", r.Json.GetProperty("status").GetString());   // a rota A só confere quem está aguardando
        Assert.Equal(3, (await api.ProdutoNoBancoAsync(produto)).Estoque);
    }

    [FatoIntegracao]
    public async Task Status_na_rota_B_reativa_pedido_vencido_se_ainda_houver_estoque()
    {
        var (super, visitante, _) = await api.AcessoriosAsync();
        var produto = await super.ProdutoAcessorioAsync(estoque: 3);
        var pedido = await visitante.PedidoPixAsync(produto, 2);
        await api.VencerPedidoAsync(pedido);
        await visitante.Get($"{B}?categoria=nenhuma");
        Assert.Equal(3, (await api.ProdutoNoBancoAsync(produto)).Estoque);      // o estoque voltou ao vencer
        api.MercadoPago.Aprovar((await api.PedidoNoBancoAsync(pedido)).MpPaymentId!);   // o Pix cai depois do prazo

        var r = await visitante.Get($"{B}/pedidos/{pedido}/status");

        Assert.Equal("pago", r.Json.GetProperty("status").GetString());
        Assert.False(r.Json.TryGetProperty("aviso", out _));
        Assert.Equal(1, (await api.ProdutoNoBancoAsync(produto)).Estoque);      // reservou de novo
        var pago = await api.PedidoNoBancoAsync(pedido);
        Assert.Equal(("pago", "approved"), (pago.Status, pago.MpStatus));
        Assert.NotNull(pago.PagoEm);
    }

    [FatoIntegracao]
    public async Task Status_na_rota_B_nao_reativa_pedido_vencido_sem_estoque_e_avisa()
    {
        var (super, visitante, _) = await api.AcessoriosAsync();
        var produto = await super.ProdutoAcessorioAsync("Cabo", estoque: 2);
        var pedido = await visitante.PedidoPixAsync(produto, 2);
        await api.VencerPedidoAsync(pedido);
        await visitante.Get($"{B}?categoria=nenhuma");
        await visitante.PedidoPixAsync(produto, 2);                              // outro cliente leva o estoque
        api.MercadoPago.Aprovar((await api.PedidoNoBancoAsync(pedido)).MpPaymentId!);

        var r = await visitante.Get($"{B}/pedidos/{pedido}/status");

        Assert.Equal("cancelado", r.Json.GetProperty("status").GetString());
        Assert.Equal("Pagamento identificado, mas sem estoque suficiente para: Cabo. Fale com o suporte.", r.Json.GetProperty("aviso").GetString());
        Assert.Equal("cancelado", (await api.PedidoNoBancoAsync(pedido)).Status);
        Assert.Equal(404, (await visitante.Get($"{B}/pedidos/{Guid.NewGuid()}/status")).Codigo);
    }

    // ── Gestão de pedidos (superadmin) ───────────────────────────────────────

    [FatoIntegracao]
    public async Task Lista_da_rota_A_traz_o_resumo_do_pedido_do_mais_novo_para_o_mais_antigo_e_filtra_por_status()
    {
        var (super, visitante, _) = await api.AcessoriosAsync();
        var produto = await super.ProdutoAcessorioAsync("Item lista", 100m, 10);
        var primeiro = await visitante.PedidoPixAsync(produto, 1, A + "/pedidos");
        await Task.Delay(20);
        var segundo = await visitante.PedidoPixAsync(produto, 2, A + "/pedidos");
        await api.NoBancoAsync(db => db.PedidosAcessorio.Where(p => p.Id == primeiro).ExecuteUpdateAsync(s => s.SetProperty(p => p.Status, "enviado")));

        var todos = (await super.Get($"{A}/pedidos")).Json.EnumerateArray().Where(p => new[] { primeiro, segundo }.Contains(Id(p))).ToList();
        var enviados = (await super.Get($"{A}/pedidos?status=enviado")).Json.EnumerateArray().Select(Id).ToList();

        Assert.Equal([segundo, primeiro], todos.Select(Id).ToList());
        var p2 = todos[0];
        Assert.Equal(("Maria Souza", "maria@teste.com", "MS", 200m, 15m, 215m, "aguardando_pagamento"),
            (p2.GetProperty("clienteNome").GetString(), p2.GetProperty("clienteEmail").GetString(), p2.GetProperty("uf").GetString(), p2.GetProperty("subtotal").GetDecimal(),
             p2.GetProperty("valorFrete").GetDecimal(), p2.GetProperty("total").GetDecimal(), p2.GetProperty("status").GetString()));
        var item = p2.GetProperty("itens").EnumerateArray().Single();
        Assert.Equal(("Item lista", 2, 100m, 200m), (item.GetProperty("nomeProduto").GetString(), item.GetProperty("quantidade").GetInt32(), item.GetProperty("precoUnitario").GetDecimal(), item.GetProperty("subtotal").GetDecimal()));
        Assert.False(p2.TryGetProperty("mpPaymentId", out _));    // o resumo não expõe os dados do Mercado Pago
        Assert.Contains(primeiro, enviados);
        Assert.DoesNotContain(segundo, enviados);
    }

    // A lista da rota B devolve a entidade do banco direto, e com pedidos que têm itens o JSON entra em ciclo (item -> pedido -> itens)
    // e a API responde 500. É um defeito antigo, mantido como está e de propósito NÃO travado aqui; só o que funciona é conferido.
    [FatoIntegracao]
    public async Task Lista_da_rota_B_filtra_por_status_e_vem_vazia_quando_nao_ha_pedido_naquele_status()
    {
        var (super, _, _) = await api.AcessoriosAsync();

        var r = await super.Get($"{B}/pedidos?status=status-que-nao-existe");

        Assert.Equal(200, r.Codigo);
        Assert.Equal(0, r.Json.GetArrayLength());
    }

    [FatoIntegracao]
    public async Task Mudar_status_na_rota_A_valida_grava_rastreio_e_devolve_estoque_ao_cancelar()
    {
        var (super, visitante, _) = await api.AcessoriosAsync();
        var produto = await super.ProdutoAcessorioAsync(estoque: 5);
        var pedido = await visitante.PedidoPixAsync(produto, 3, A + "/pedidos");
        Assert.Equal(2, (await api.ProdutoNoBancoAsync(produto)).Estoque);

        Assert.Equal((400, "Status inválido."), ((await super.Patch($"{A}/pedidos/{pedido}/status", new { status = "sumiu" })) is var a ? (a.Codigo, a.Erro) : default));
        Assert.Equal(404, (await super.Patch($"{A}/pedidos/{Guid.NewGuid()}/status", new { status = "pago" })).Codigo);

        var enviado = await super.Patch($"{A}/pedidos/{pedido}/status", new { status = "enviado", codigoRastreio = "BR123" });
        Assert.Equal(("enviado", "BR123", "Maria Souza"), (enviado.Json.GetProperty("status").GetString(), enviado.Json.GetProperty("codigoRastreio").GetString(), enviado.Json.GetProperty("clienteNome").GetString()));
        var noBanco = await api.PedidoNoBancoAsync(pedido);
        Assert.NotNull(noBanco.EnviadoEm);
        Assert.Null(noBanco.ExpiraEm);
        Assert.Equal(2, (await api.ProdutoNoBancoAsync(produto)).Estoque);       // enviar não mexe no estoque

        await super.Patch($"{A}/pedidos/{pedido}/status", new { status = "cancelado" });
        Assert.Equal(5, (await api.ProdutoNoBancoAsync(produto)).Estoque);       // cancelar devolve na hora
        Assert.Equal("BR123", (await api.PedidoNoBancoAsync(pedido)).CodigoRastreio);   // sem novo rastreio, mantém o anterior

        await super.Patch($"{A}/pedidos/{pedido}/status", new { status = "cancelado" });
        Assert.Equal(5, (await api.ProdutoNoBancoAsync(produto)).Estoque);       // cancelar de novo não devolve outra vez
    }

    [FatoIntegracao]
    public async Task Mudar_status_na_rota_B_valida_e_nao_mexe_no_estoque()
    {
        var (super, visitante, _) = await api.AcessoriosAsync();
        var produto = await super.ProdutoAcessorioAsync(estoque: 5);
        var pedido = await visitante.PedidoPixAsync(produto, 3);

        Assert.Equal((400, "Status inválido."), ((await super.Patch($"{B}/pedidos/{pedido}/status", new { status = "sumiu" })) is var a ? (a.Codigo, a.Erro) : default));
        Assert.Equal(404, (await super.Patch($"{B}/pedidos/{Guid.NewGuid()}/status", new { status = "pago" })).Codigo);

        var enviado = await super.Patch($"{B}/pedidos/{pedido}/status", new { status = "enviado", codigoRastreio = "BR9" });
        Assert.Equal(("enviado", "BR9"), (enviado.Json.GetProperty("status").GetString(), enviado.Json.GetProperty("codigoRastreio").GetString()));
        Assert.NotNull((await api.PedidoNoBancoAsync(pedido)).EnviadoEm);

        await super.Patch($"{B}/pedidos/{pedido}/status", new { status = "cancelado" });
        Assert.Equal(2, (await api.ProdutoNoBancoAsync(produto)).Estoque);       // diferente da rota A: aqui o estoque NÃO volta
        Assert.NotNull((await api.PedidoNoBancoAsync(pedido)).ExpiraEm);          // e o prazo não é limpo
    }
}
