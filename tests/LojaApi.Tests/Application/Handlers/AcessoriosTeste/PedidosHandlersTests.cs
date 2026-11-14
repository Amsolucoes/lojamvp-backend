using LojaApi.Application.Acessorios;
using LojaApi.Shared.Application;
using LojaApi.src.Models;
using LojaApi.Tests.Suporte.Falsos;

namespace LojaApi.Tests.Application.Handlers.AcessoriosTeste;

// Handlers dos pedidos de acessórios, testados com repositório e Mercado Pago de mentira (sem banco, sem HTTP).
public class PedidosHandlersTests
{
    private readonly RepositorioAcessoriosFalso _repo = new();
    private readonly PagamentoOnlineAcessorioFalso _pagamento = new();

    private ProdutoAcessorio Produto(string nome = "Mouse", decimal preco = 100m, int estoque = 10, decimal? promo = null, bool ativo = true)
    {
        var p = new ProdutoAcessorio { Nome = nome, Preco = preco, Estoque = estoque, PrecoPromocional = promo, Ativo = ativo };
        _repo.Produtos.Add(p);
        return p;
    }

    private static CriarPedidoRequest Pedido(string? cpf = "12345678909", string uf = "ms", params (Guid Produto, int Quantidade)[] itens) =>
        new(" Maria ", " m@t.com ", " 6799 ", cpf, " 79000 ", " Rua A ", "10", null, "Centro", " Campo Grande ", uf,
            itens.Select(i => new ItemPedidoRequest(i.Produto, i.Quantidade)).ToList());

    private Task<Resultado<object>> Pix(CriarPedidoRequest req) => new CriarPedidoPixCommandHandler(_repo, _pagamento).HandleAsync(new(req));

    private PedidoAcessorio PedidoAguardando(ProdutoAcessorio produto, int quantidade = 2, string status = "aguardando_pagamento", string? mpId = "mp-1")
    {
        var pedido = new PedidoAcessorio { ClienteNome = "Maria", Status = status, MpPaymentId = mpId, ExpiraEm = DateTime.UtcNow.AddMinutes(10) };
        pedido.Itens.Add(new ItemPedidoAcessorio { ProdutoId = produto.Id, NomeProduto = produto.Nome, Quantidade = quantidade });
        _repo.Pedidos.Add(pedido);
        return pedido;
    }

    // ── Pix ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Pix_recusa_pedido_sem_itens_sem_cpf_e_produto_inativo_sem_gravar()
    {
        var inativo = Produto(ativo: false);
        var pouco = Produto("Teclado", estoque: 1);

        var semItens = await Pix(Pedido());
        Assert.Equal((TipoResultado.Invalido, "O pedido precisa ter ao menos um item."), (semItens.Tipo, semItens.Mensagem));

        var semCpf = await Pix(Pedido("  ", "MS", (pouco.Id, 1)));
        Assert.Equal("CPF é obrigatório para gerar o Pix.", semCpf.Mensagem);

        var produtoInativo = await Pix(Pedido("123", "MS", (inativo.Id, 1)));
        Assert.Equal("Produto não encontrado.", produtoInativo.Mensagem);

        var semEstoque = await Pix(Pedido("123", "MS", (pouco.Id, 2)));
        Assert.Equal("Estoque insuficiente para 'Teclado'. Disponível: 1.", semEstoque.Mensagem);

        Assert.Equal(0, _repo.Salvamentos);
        Assert.Empty(_repo.Pedidos);
        Assert.Empty(_pagamento.PixCobrados);
    }

    [Fact]
    public async Task Pix_calcula_o_total_com_preco_promocional_e_frete_reserva_o_estoque_e_grava_o_pagamento()
    {
        var normal = Produto("Normal", 100m, 10);
        var promo = Produto("Promo", 80m, 10, promo: 60m);

        var r = await Pix(Pedido("123", " sp ", (normal.Id, 2), (promo.Id, 1)));

        Assert.Equal(TipoResultado.Ok, r.Tipo);
        var pedido = Assert.Single(_repo.Pedidos);
        Assert.Equal((260m, 25m, 285m, "SP", "Maria", "m@t.com"), (pedido.Subtotal, pedido.ValorFrete, pedido.Total, pedido.Uf, pedido.ClienteNome, pedido.ClienteEmail));
        Assert.Equal([285m], _pagamento.PixCobrados);
        Assert.Equal((8, 9), (normal.Estoque, promo.Estoque));
        Assert.Equal(("pix-1", "pending", "aguardando_pagamento"), (pedido.MpPaymentId, pedido.MpStatus, pedido.Status));
        Assert.InRange(pedido.ExpiraEm!.Value, DateTime.UtcNow.AddMinutes(29), DateTime.UtcNow.AddMinutes(31));
        Assert.Equal(2, _repo.Salvamentos);     // o pedido e depois o id do Mercado Pago
    }

    [Fact]
    public async Task Pix_que_falha_devolve_o_estoque_e_apaga_o_pedido()
    {
        var produto = Produto(estoque: 5);
        _pagamento.RespostaPix = new(null, null, null, null, "fora do ar");

        var r = await Pix(Pedido("123", "MS", (produto.Id, 2)));

        Assert.Equal((TipoResultado.Invalido, "Não foi possível gerar o pagamento. Tente novamente."), (r.Tipo, r.Mensagem));
        Assert.Equal(5, produto.Estoque);
        Assert.Empty(_repo.Pedidos);
    }

    [Fact]
    public async Task Pix_libera_antes_o_estoque_de_pedidos_vencidos()
    {
        var produto = Produto(estoque: 2);
        var vencido = PedidoAguardando(produto, 2);
        vencido.ExpiraEm = DateTime.UtcNow.AddMinutes(-1);
        produto.Estoque = 0;     // o vencido tinha reservado tudo

        var r = await Pix(Pedido("123", "MS", (produto.Id, 2)));

        Assert.Equal(TipoResultado.Ok, r.Tipo);
        Assert.Equal("cancelado", vencido.Status);
        Assert.Equal(0, produto.Estoque);       // voltou 2 e o novo pedido reservou 2
    }

    // ── Cartão ───────────────────────────────────────────────────────────────

    private Task<Resultado<object>> Cartao(string? cpf, Guid produto, int quantidade = 1) => new CriarPedidoCartaoCommandHandler(_repo, _pagamento).HandleAsync(new(
        new CriarPedidoCartaoRequest("Maria", "m@t.com", "6799", cpf, "79000", "Rua A", "10", null, "Centro", "Campo Grande", "SP",
            [new ItemPedidoRequest(produto, quantidade)], "tok", "visa", 3, "24")));

    [Fact]
    public async Task Cartao_exige_cpf_com_a_mensagem_propria_e_aprovado_marca_como_pago()
    {
        var produto = Produto(estoque: 4);

        var semCpf = await Cartao("", produto.Id);
        Assert.Equal((TipoResultado.Invalido, "CPF é obrigatório."), (semCpf.Tipo, semCpf.Mensagem));

        var r = await Cartao("123", produto.Id, 2);

        Assert.Equal(TipoResultado.Ok, r.Tipo);
        var pedido = Assert.Single(_repo.Pedidos);
        Assert.Equal(("pago", "approved", 2), (pedido.Status, pedido.MpStatus, produto.Estoque));
        Assert.NotNull(pedido.PagoEm);
        Assert.Null(pedido.ExpiraEm);
        Assert.Equal([(225m, 3)], _pagamento.CartoesCobrados);
    }

    [Fact]
    public async Task Cartao_recusado_ou_com_erro_devolve_o_estoque_e_usa_a_mensagem_do_banco_ou_a_padrao()
    {
        var produto = Produto(estoque: 4);

        _pagamento.RespostaCartao = new(null, null, null, null, "Cartão recusado pelo banco emissor.");
        var comErro = await Cartao("123", produto.Id, 3);
        Assert.Equal("Cartão recusado pelo banco emissor.", comErro.Mensagem);

        _pagamento.RespostaCartao = new("c-2", "rejected", null, null, null);
        var rejeitado = await Cartao("123", produto.Id, 3);
        Assert.Equal("Pagamento recusado pela operadora do cartão. Confira os dados e tente novamente.", rejeitado.Mensagem);

        Assert.Equal(4, produto.Estoque);
        Assert.Empty(_repo.Pedidos);
    }

    // ── Status do pagamento ──────────────────────────────────────────────────

    [Fact]
    public async Task Status_simples_so_aprova_quem_esta_aguardando_e_nao_reativa_cancelado()
    {
        var produto = Produto(estoque: 5);
        var aguardando = PedidoAguardando(produto);
        var cancelado = PedidoAguardando(produto, status: "cancelado", mpId: "mp-2");
        _pagamento.StatusConsultado = "approved";
        var handler = new ConsultarStatusPedidoCommandHandler(_repo, _pagamento);

        var pago = await handler.HandleAsync(new(aguardando.Id));
        var continua = await handler.HandleAsync(new(cancelado.Id));

        Assert.Equal("pago", ((dynamic)pago.Valor!).Status);
        Assert.Equal("cancelado", ((dynamic)continua.Valor!).Status);
        Assert.Equal(("pago", "approved"), (aguardando.Status, aguardando.MpStatus));
        Assert.Null(aguardando.ExpiraEm);
        Assert.Equal(TipoResultado.NaoEncontrado, (await handler.HandleAsync(new(Guid.NewGuid()))).Tipo);
    }

    [Fact]
    public async Task Status_com_reativacao_reserva_de_novo_o_estoque_quando_ainda_ha()
    {
        var produto = Produto(estoque: 5);
        var cancelado = PedidoAguardando(produto, 2, "cancelado");
        _pagamento.StatusConsultado = "approved";

        var r = await new ConsultarStatusPedidoComReativacaoCommandHandler(_repo, _pagamento).HandleAsync(new(cancelado.Id));

        Assert.Equal("pago", ((dynamic)r.Valor!).Status);
        Assert.Equal(3, produto.Estoque);
        Assert.Equal(("pago", "approved"), (cancelado.Status, cancelado.MpStatus));
    }

    [Fact]
    public async Task Status_com_reativacao_nao_reativa_sem_estoque_e_avisa_quais_produtos_faltam()
    {
        var produto = Produto("Cabo", estoque: 1);
        var cancelado = PedidoAguardando(produto, 2, "cancelado");
        _pagamento.StatusConsultado = "approved";

        var r = await new ConsultarStatusPedidoComReativacaoCommandHandler(_repo, _pagamento).HandleAsync(new(cancelado.Id));

        var valor = (dynamic)r.Valor!;
        Assert.Equal("cancelado", valor.Status);
        Assert.Equal("Pagamento identificado, mas sem estoque suficiente para: Cabo. Fale com o suporte.", valor.aviso);
        Assert.Equal(1, produto.Estoque);
        Assert.Equal(0, _repo.Salvamentos);
    }

    [Fact]
    public async Task Status_nao_consulta_o_mercado_pago_quando_nao_ha_pagamento_ou_nao_foi_aprovado()
    {
        var produto = Produto();
        var semPagamento = PedidoAguardando(produto, mpId: null);
        var pendente = PedidoAguardando(produto);
        _pagamento.StatusConsultado = "pending";
        var handler = new ConsultarStatusPedidoCommandHandler(_repo, _pagamento);

        Assert.Equal("aguardando_pagamento", ((dynamic)(await handler.HandleAsync(new(semPagamento.Id))).Valor!).Status);
        Assert.Equal("aguardando_pagamento", ((dynamic)(await handler.HandleAsync(new(pendente.Id))).Valor!).Status);
        Assert.Equal(0, _repo.Salvamentos);
    }

    // ── Mudar o status (superadmin) ──────────────────────────────────────────

    [Fact]
    public async Task Mudar_status_valida_o_status_e_o_pedido()
    {
        var produto = Produto();
        var pedido = PedidoAguardando(produto);

        foreach (var (nome, resultado) in new[]
        {
            ("com estoque", await new AtualizarStatusPedidoCommandHandler(_repo).HandleAsync(new(pedido.Id, new("sumiu", null)))),
            ("simples", await new AtualizarStatusPedidoSimplesCommandHandler(_repo).HandleAsync(new(pedido.Id, new("sumiu", null)))),
        })
            Assert.True(resultado is { Tipo: TipoResultado.Invalido, Mensagem: "Status inválido." }, nome);

        Assert.Equal(TipoResultado.NaoEncontrado, (await new AtualizarStatusPedidoCommandHandler(_repo).HandleAsync(new(Guid.NewGuid(), new("pago", null)))).Tipo);
        Assert.Equal(TipoResultado.NaoEncontrado, (await new AtualizarStatusPedidoSimplesCommandHandler(_repo).HandleAsync(new(Guid.NewGuid(), new("pago", null)))).Tipo);
        Assert.Equal(0, _repo.Salvamentos);
    }

    [Fact]
    public async Task Mudar_status_na_versao_com_estoque_devolve_o_estoque_uma_so_vez_ao_cancelar_e_limpa_o_prazo()
    {
        var produto = Produto(estoque: 3);
        var pedido = PedidoAguardando(produto, 2);
        var handler = new AtualizarStatusPedidoCommandHandler(_repo);

        await handler.HandleAsync(new(pedido.Id, new("enviado", "BR1")));
        Assert.Equal((3, "BR1", null), (produto.Estoque, pedido.CodigoRastreio, pedido.ExpiraEm));
        Assert.NotNull(pedido.EnviadoEm);

        await handler.HandleAsync(new(pedido.Id, new("cancelado", null)));
        await handler.HandleAsync(new(pedido.Id, new("cancelado", null)));

        Assert.Equal((5, "BR1"), (produto.Estoque, pedido.CodigoRastreio));     // devolveu 2 uma única vez; sem novo rastreio, mantém o antigo
    }

    [Fact]
    public async Task Mudar_status_na_versao_simples_nao_mexe_no_estoque_nem_no_prazo()
    {
        var produto = Produto(estoque: 3);
        var pedido = PedidoAguardando(produto, 2);

        var r = await new AtualizarStatusPedidoSimplesCommandHandler(_repo).HandleAsync(new(pedido.Id, new("cancelado", "BR9")));

        Assert.Same(pedido, r.Valor);
        Assert.Equal((3, "cancelado", "BR9"), (produto.Estoque, pedido.Status, pedido.CodigoRastreio));
        Assert.NotNull(pedido.ExpiraEm);
    }
}
