using LojaApi.Application.Vendas;
using LojaApi.Models;
using LojaApi.Shared.Application;
using LojaApi.src.Models.Funcionarios;
using LojaApi.Tests.Suporte.Falsos;

namespace LojaApi.Tests.Application.Handlers.VendasTeste;

// Handlers de Vendas (venda, troca, caixa, origens e relatórios), testados com repositório de mentira.
// Registrar/Excluir venda também usam a carteira (banco): o que depende dela é coberto pelos testes de API.
public class VendasHandlersTests
{
    private readonly RepositorioVendasFalso _repo = new();
    private readonly Guid _loja = Guid.NewGuid();

    private Produto NovoProduto(decimal estoque = 10m, bool ativo = true, Guid? loja = null)
    {
        var p = new Produto { LojaId = loja ?? _loja, Nome = "Camiseta", Estoque = estoque, Ativo = ativo, PrecoCusto = 20m };
        _repo.Produtos.Add(p);
        return p;
    }

    private static ItemVendaRequest Item(Guid? produto, decimal qtd = 1m, decimal preco = 50m, Guid? variacao = null, Guid? servico = null) =>
        new(produto, qtd, preco, variacao, servico);

    private static CriarVendaRequest Venda(List<ItemVendaRequest> itens, Guid? cliente = null, DateTime? data = null, Guid? funcionario = null) =>
        new(itens, cliente, 0m, "pix", null, null, null, data, null, funcionario);

    private Task<Resultado<object>> Registrar(CriarVendaRequest req, Guid? loja = null) =>
        new RegistrarVendaCommandHandler(_repo, null!).HandleAsync(new(loja ?? _loja, req));

    // ── Registrar venda: validações (nada é gravado) ─────────────────────────

    [Fact]
    public async Task Venda_recusa_sem_itens_data_futura_e_item_sem_produto_nem_servico()
    {
        Assert.Equal("A venda deve ter ao menos um item.", (await Registrar(Venda([]))).Mensagem);
        Assert.Equal("A data da venda não pode ser no futuro.", (await Registrar(Venda([Item(NovoProduto().Id)], data: DateTime.UtcNow.AddDays(2)))).Mensagem);
        Assert.Equal("Item sem produto nem serviço.", (await Registrar(Venda([Item(null)]))).Mensagem);
        Assert.Equal(0, _repo.Salvamentos);
        Assert.Empty(_repo.Vendas);
    }

    [Fact]
    public async Task Venda_recusa_produto_inexistente_inativo_sem_estoque_ou_de_outra_loja()
    {
        var inativo = NovoProduto(ativo: false);
        var pouco = NovoProduto(estoque: 1m);
        var deOutra = NovoProduto(loja: Guid.NewGuid());
        var inexistente = Guid.NewGuid();

        Assert.Equal($"Produto {inexistente} não encontrado.", (await Registrar(Venda([Item(inexistente)]))).Mensagem);
        Assert.Equal("Produto 'Camiseta' está inativo.", (await Registrar(Venda([Item(inativo.Id)]))).Mensagem);
        Assert.Equal("Estoque insuficiente para 'Camiseta'.", (await Registrar(Venda([Item(pouco.Id, 2m)]))).Mensagem);
        Assert.Equal($"Produto {deOutra.Id} não encontrado.", (await Registrar(Venda([Item(deOutra.Id)]))).Mensagem);
        Assert.Equal(10m, deOutra.Estoque);
        Assert.Equal(0, _repo.Salvamentos);
    }

    [Fact]
    public async Task Venda_valida_variacao_servico_cliente_e_funcionario_da_propria_loja()
    {
        var produto = NovoProduto();
        var variacaoDeOutra = new ProdutoVariacao { ProdutoId = NovoProduto(loja: Guid.NewGuid()).Id, Estoque = 5 };
        var semEstoque = new ProdutoVariacao { ProdutoId = produto.Id, Tamanho = "M", Cor = "Azul", Estoque = 0 };
        _repo.Variacoes.AddRange([variacaoDeOutra, semEstoque]);
        var servicoDeOutra = new Servico { LojaId = Guid.NewGuid(), Nome = "Banho" };
        _repo.Servicos.Add(servicoDeOutra);
        var clienteDeOutra = new Cliente { LojaId = Guid.NewGuid(), Nome = "Dela" };
        _repo.Clientes.Add(clienteDeOutra);

        Assert.Equal("Variação não encontrada.", (await Registrar(Venda([Item(produto.Id, variacao: variacaoDeOutra.Id)]))).Mensagem);
        Assert.Equal("Estoque insuficiente para 'Camiseta' (M/Azul).", (await Registrar(Venda([Item(produto.Id, variacao: semEstoque.Id)]))).Mensagem);
        Assert.Equal("Serviço não encontrado.", (await Registrar(Venda([Item(null, servico: servicoDeOutra.Id)]))).Mensagem);
        Assert.Equal("Cliente não encontrado.", (await Registrar(Venda([Item(produto.Id)], cliente: clienteDeOutra.Id))).Mensagem);
        Assert.Equal("Funcionário não encontrado.", (await Registrar(Venda([Item(produto.Id)], funcionario: Guid.NewGuid()))).Mensagem);
        Assert.Equal(0, _repo.Salvamentos);
    }

    [Fact]
    public async Task Venda_sem_loja_do_usuario_nao_filtra_produto_por_loja()
    {
        var produto = NovoProduto(loja: Guid.NewGuid());

        // sem loja o comportamento antigo vale (cai na carteira só se houver valor nela; aqui é pix): a venda é gravada
        var r = await new RegistrarVendaCommandHandler(_repo, null!).HandleAsync(new(null, Venda([Item(produto.Id, 3m)])));

        Assert.Equal(TipoResultado.Ok, r.Tipo);
        Assert.Equal(7m, produto.Estoque);
        Assert.Equal("saida", Assert.Single(_repo.MovimentosEstoque).Tipo);
    }

    [Fact]
    public async Task Venda_ok_baixa_o_estoque_grava_item_e_movimento_e_gera_comissao_so_de_itens_sem_agendamento()
    {
        var produto = NovoProduto(estoque: 10m);
        var func = new Profissional { LojaId = _loja, Nome = "Joana", ComissaoVendasAtiva = true, ComissaoVendasPercentual = 10m };
        _repo.Funcionarios.Add(func);

        var r = await Registrar(Venda([Item(produto.Id, 2m, 50m)], funcionario: func.Id));

        Assert.Equal(TipoResultado.Ok, r.Tipo);
        Assert.Equal(8m, produto.Estoque);
        Assert.Equal(100m, Assert.Single(_repo.Vendas).Total);
        Assert.Equal("Camiseta", Assert.Single(_repo.ItensVenda).NomeProduto);
        var comissao = Assert.Single(_repo.Comissoes);
        Assert.Equal((100m, 10m, 10m), (comissao.ValorServico, comissao.ComissaoPercentual, comissao.ValorComissao));
        Assert.Equal(1, _repo.Salvamentos);
    }

    // ── Excluir venda ────────────────────────────────────────────────────────

    [Fact]
    public async Task Excluir_venda_recusa_de_outra_loja_de_mes_passado_e_com_nota_autorizada()
    {
        var deOutra = new Venda { LojaId = Guid.NewGuid() };
        var antiga = new Venda { LojaId = _loja, CriadaEm = DateTime.UtcNow.AddMonths(-2) };
        var comNota = new Venda { LojaId = _loja, CriadaEm = DateTime.UtcNow };
        _repo.Vendas.AddRange([deOutra, antiga, comNota]);
        var handler = new ExcluirVendaCommandHandler(_repo, null!);

        Assert.Equal(TipoResultado.NaoEncontrado, (await handler.HandleAsync(new(_loja, deOutra.Id))).Tipo);
        Assert.Equal("Só é possível excluir vendas do mês atual.", (await handler.HandleAsync(new(_loja, antiga.Id))).Mensagem);
        _repo.NotaAutorizada = true;
        Assert.StartsWith("Não é possível excluir: já existe uma nota fiscal autorizada", (await handler.HandleAsync(new(_loja, comNota.Id))).Mensagem);
        Assert.Equal(3, _repo.Vendas.Count);
        Assert.Equal(0, _repo.Salvamentos);
    }

    // ── Trocas ───────────────────────────────────────────────────────────────

    private static ItemTrocaRequest ItemT(Guid produto, int qtd = 1, decimal preco = 50m, bool volta = true) => new(produto, "Camiseta", null, qtd, preco, volta);

    [Fact]
    public async Task Troca_recusa_cliente_de_outra_loja_lista_vazia_e_estoque_insuficiente()
    {
        var produto = NovoProduto(estoque: 1m);
        var clienteDeOutra = new Cliente { LojaId = Guid.NewGuid(), Nome = "Dela" };
        var cliente = new Cliente { LojaId = _loja, Nome = "Minha" };
        _repo.Clientes.AddRange([clienteDeOutra, cliente]);
        var handler = new RegistrarTrocaCommandHandler(_repo);

        Assert.Equal("Cliente não encontrado.", (await handler.HandleAsync(new(_loja, new(clienteDeOutra.Id, [ItemT(produto.Id)], [ItemT(produto.Id)], null)))).Mensagem);
        Assert.Equal("A troca precisa de ao menos um produto devolvido e um novo.", (await handler.HandleAsync(new(_loja, new(cliente.Id, [], [ItemT(produto.Id)], null)))).Mensagem);
        Assert.Equal("Estoque insuficiente para 'Camiseta'.", (await handler.HandleAsync(new(_loja, new(cliente.Id, [ItemT(produto.Id)], [ItemT(produto.Id, 2)], null)))).Mensagem);
        Assert.Equal(0, _repo.Salvamentos);
    }

    [Fact]
    public async Task Troca_com_sobra_gera_credito_volta_estoque_dos_devolvidos_e_baixa_os_novos()
    {
        var velho = NovoProduto(estoque: 0m);
        var novo = NovoProduto(estoque: 3m);
        var cliente = new Cliente { LojaId = _loja, Nome = "Maria" };
        _repo.Clientes.Add(cliente);

        var r = await new RegistrarTrocaCommandHandler(_repo).HandleAsync(new(_loja, new(cliente.Id, [ItemT(velho.Id, 1, 100m)], [ItemT(novo.Id, 1, 30m)], "pix")));

        Assert.Equal(TipoResultado.Ok, r.Tipo);
        Assert.Equal(1m, velho.Estoque);
        Assert.Equal(2m, novo.Estoque);
        Assert.Equal(70m, cliente.CreditoLoja);
        var troca = Assert.Single(_repo.Trocas);
        Assert.Equal((-70m, 70m, null), (troca.Diferenca, troca.CreditoGerado, troca.FormaPagamento));   // forma de pagamento só quando o cliente paga
        Assert.Equal(["entrada", "saida"], _repo.MovimentosEstoque.Select(m => m.Tipo));
        Assert.Equal(1, _repo.Salvamentos);
    }

    [Fact]
    public async Task Troca_devolvido_que_nao_volta_ao_estoque_nao_mexe_no_estoque_nem_gera_movimento()
    {
        var velho = NovoProduto(estoque: 0m);
        var novo = NovoProduto(estoque: 3m);
        var cliente = new Cliente { LojaId = _loja, Nome = "Maria" };
        _repo.Clientes.Add(cliente);

        await new RegistrarTrocaCommandHandler(_repo).HandleAsync(new(_loja, new(cliente.Id, [ItemT(velho.Id, 1, 10m, volta: false)], [ItemT(novo.Id, 1, 30m)], "dinheiro")));

        Assert.Equal(0m, velho.Estoque);
        Assert.Equal(0m, cliente.CreditoLoja);
        Assert.Equal("dinheiro", Assert.Single(_repo.Trocas).FormaPagamento);
        Assert.Equal("saida", Assert.Single(_repo.MovimentosEstoque).Tipo);
    }

    // ── Caixa ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Movimento_de_caixa_valida_loja_tipo_valor_e_data()
    {
        var handler = new CriarMovimentoCaixaCommandHandler(_repo);
        var hoje = DateTime.UtcNow.Date;

        Assert.Equal("Loja não encontrada.", (await handler.HandleAsync(new(null, new("entrada", 10, hoje, null, null, null)))).Mensagem);
        Assert.Equal("Tipo inválido.", (await handler.HandleAsync(new(_loja, new("troca", 10, hoje, null, null, null)))).Mensagem);
        Assert.Equal("Informe um valor maior que zero.", (await handler.HandleAsync(new(_loja, new("entrada", 0, hoje, null, null, null)))).Mensagem);
        Assert.Equal("A data não pode ser no futuro.", (await handler.HandleAsync(new(_loja, new("entrada", 10, hoje.AddDays(1), null, null, null)))).Mensagem);
        Assert.Equal(0, _repo.Salvamentos);
    }

    [Fact]
    public async Task Movimento_com_conta_bancaria_da_loja_espelha_um_ajuste_e_conta_de_outra_loja_e_ignorada()
    {
        var minha = new ContaBancaria { LojaId = _loja };
        var dela = new ContaBancaria { LojaId = Guid.NewGuid() };
        _repo.Contas.AddRange([minha, dela]);
        var handler = new CriarMovimentoCaixaCommandHandler(_repo);
        var hoje = DateTime.UtcNow.Date;

        await handler.HandleAsync(new(_loja, new("saida", 25m, hoje, null, "malote", minha.Id)));
        await handler.HandleAsync(new(_loja, new("entrada", 5m, hoje, null, null, dela.Id)));

        var ajuste = Assert.Single(_repo.Ajustes);
        Assert.Equal(("saida", 25m, "Sangria de caixa (malote)"), (ajuste.Tipo, ajuste.Valor, ajuste.Observacao));
        Assert.Equal(ajuste.Id, _repo.MovimentosCaixa[0].AjusteContaBancariaId);
        Assert.Null(_repo.MovimentosCaixa[1].AjusteContaBancariaId);
        Assert.Equal(new DateTime(hoje.Year, hoje.Month, hoje.Day, 12, 0, 0, DateTimeKind.Utc), _repo.MovimentosCaixa[0].Data);
    }

    [Fact]
    public async Task Editar_movimento_refaz_o_espelho_e_excluir_remove_o_ajuste_junto()
    {
        var conta = new ContaBancaria { LojaId = _loja };
        _repo.Contas.Add(conta);
        var hoje = DateTime.UtcNow.Date;
        await new CriarMovimentoCaixaCommandHandler(_repo).HandleAsync(new(_loja, new("entrada", 10m, hoje, null, null, conta.Id)));
        var movimento = Assert.Single(_repo.MovimentosCaixa);
        var ajusteAntigo = Assert.Single(_repo.Ajustes);

        await new EditarMovimentoCaixaCommandHandler(_repo).HandleAsync(new(_loja, movimento.Id, new("saida", 99m, hoje, null, null, conta.Id)));

        var ajusteNovo = Assert.Single(_repo.Ajustes);
        Assert.NotEqual(ajusteAntigo.Id, ajusteNovo.Id);
        Assert.Equal(("saida", 99m), (ajusteNovo.Tipo, ajusteNovo.Valor));

        // sem conta, o espelho some
        await new EditarMovimentoCaixaCommandHandler(_repo).HandleAsync(new(_loja, movimento.Id, new("saida", 99m, hoje, null, null, null)));
        Assert.Empty(_repo.Ajustes);
        Assert.Null(movimento.ContaBancariaId);

        Assert.Equal(TipoResultado.NaoEncontrado, (await new EditarMovimentoCaixaCommandHandler(_repo).HandleAsync(new(Guid.NewGuid(), movimento.Id, new("saida", 1m, hoje, null, null, null)))).Tipo);
        Assert.Equal(TipoResultado.Ok, (await new ExcluirMovimentoCaixaCommandHandler(_repo).HandleAsync(new(_loja, movimento.Id))).Tipo);
        Assert.Empty(_repo.MovimentosCaixa);
    }

    [Fact]
    public async Task Lista_de_movimentos_sem_loja_vem_vazia_e_filtra_pelo_dia_inteiro()
    {
        var handler = new ListarMovimentosCaixaQueryHandler(_repo);
        _repo.MovimentosCaixa.Add(new MovimentoCaixa { LojaId = _loja, Tipo = "entrada", Valor = 1m, Data = new DateTime(2026, 6, 10, 12, 0, 0, DateTimeKind.Utc) });
        _repo.MovimentosCaixa.Add(new MovimentoCaixa { LojaId = _loja, Tipo = "entrada", Valor = 2m, Data = new DateTime(2026, 6, 20, 12, 0, 0, DateTimeKind.Utc) });

        Assert.Empty((Array)(await handler.HandleAsync(new(null, null, null))).Valor!);
        var lista = (List<MovimentoCaixaDto>)(await handler.HandleAsync(new(_loja, new(2026, 6, 10), new(2026, 6, 10)))).Valor!;
        Assert.Equal(1m, Assert.Single(lista).Valor);
    }

    // ── Origens ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Origem_valida_nome_e_duplicada_e_grava_com_o_nome_aparado()
    {
        var handler = new CriarOrigemVendaCommandHandler(_repo);

        Assert.Equal("Loja não encontrada.", (await handler.HandleAsync(new(null, new("X", 0, true)))).Mensagem);
        Assert.Equal("Informe o nome da origem.", (await handler.HandleAsync(new(_loja, new("  ", 0, true)))).Mensagem);
        var ok = await handler.HandleAsync(new(_loja, new("  Instagram ", 3, true)));
        Assert.Equal("Instagram", ((OrigemVenda)ok.Valor!).Nome);
        Assert.Equal("Já existe uma origem com esse nome.", (await handler.HandleAsync(new(_loja, new("Instagram", 1, true)))).Mensagem);
        Assert.Single(_repo.Origens);
    }

    [Fact]
    public async Task Excluir_origem_em_uso_so_desativa_e_a_livre_e_removida()
    {
        var emUso = new OrigemVenda { LojaId = _loja, Nome = "Site" };
        var livre = new OrigemVenda { LojaId = _loja, Nome = "Feira" };
        var deOutra = new OrigemVenda { LojaId = Guid.NewGuid(), Nome = "Dela" };
        _repo.Origens.AddRange([emUso, livre, deOutra]);
        _repo.Vendas.Add(new Venda { LojaId = _loja, OrigemVendaId = emUso.Id });
        var handler = new ExcluirOrigemVendaCommandHandler(_repo);

        Assert.Equal("Origem em uso — foi desativada em vez de excluída.", ((dynamic)(await handler.HandleAsync(new(_loja, emUso.Id))).Valor!).mensagem);
        Assert.False(emUso.Ativa);
        Assert.Equal("Origem excluída.", ((dynamic)(await handler.HandleAsync(new(_loja, livre.Id))).Valor!).mensagem);
        Assert.DoesNotContain(livre, _repo.Origens);
        Assert.Equal(TipoResultado.NaoEncontrado, (await handler.HandleAsync(new(_loja, deOutra.Id))).Tipo);
        Assert.Equal(TipoResultado.NaoEncontrado, (await handler.HandleAsync(new(null, emUso.Id))).Tipo);
    }

    [Fact]
    public async Task Origens_padrao_so_para_loja_sem_nenhuma_e_na_ordem_certa()
    {
        var handler = new CriarOrigensPadraoCommandHandler(_repo);

        Assert.Equal("Loja não encontrada.", (await handler.HandleAsync(new(null))).Mensagem);
        Assert.Equal(TipoResultado.Ok, (await handler.HandleAsync(new(_loja))).Tipo);
        Assert.Equal(["Loja física", "Site", "WhatsApp"], _repo.Origens.OrderBy(o => o.Ordem).Select(o => o.Nome));
        Assert.Equal("Você já tem origens cadastradas.", (await handler.HandleAsync(new(_loja))).Mensagem);
        Assert.Equal(3, _repo.Origens.Count);
    }

    // ── Relatórios ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Resumo_e_fluxo_somam_so_as_vendas_da_loja()
    {
        _repo.Vendas.Add(new Venda { LojaId = _loja, TotalFinal = 90m, Desconto = 10m, CriadaEm = new DateTime(2026, 3, 10, 0, 0, 0, DateTimeKind.Utc), Itens = [new ItemVenda { Quantidade = 2 }] });
        _repo.Vendas.Add(new Venda { LojaId = _loja, TotalFinal = 300m, CriadaEm = new DateTime(2026, 3, 11, 0, 0, 0, DateTimeKind.Utc), Itens = [new ItemVenda { Quantidade = 3 }] });
        _repo.Vendas.Add(new Venda { LojaId = Guid.NewGuid(), TotalFinal = 999m, CriadaEm = new DateTime(2026, 3, 10, 0, 0, 0, DateTimeKind.Utc) });

        var resumo = (ResumoVendasDto)(await new ResumoVendasQueryHandler(_repo).HandleAsync(new(_loja, null, null))).Valor!;
        var meses = (List<FluxoMesDto>)(await new FluxoMensalQueryHandler(_repo).HandleAsync(new(_loja, 2026))).Valor!;
        var dias = (List<FluxoDiaDto>)(await new FluxoDiarioQueryHandler(_repo).HandleAsync(new(_loja, 3, 2026))).Valor!;

        Assert.Equal((390m, 10m, 195m, 5m, 2), (resumo.TotalVendido, resumo.TotalDescontos, resumo.TicketMedio, resumo.TotalItensVendidos, resumo.TotalVendas));
        Assert.Equal(("Mar", 2, 390m), (meses[2].NomeMes, meses[2].QtdVendas, meses[2].Entradas));
        Assert.Equal(12, meses.Count);
        Assert.Equal(31, dias.Count);
        Assert.Equal(("10/03", 1, 90m), (dias[9].Data, dias[9].QtdVendas, dias[9].Entradas));
    }

    [Fact]
    public async Task Ranking_agrupa_por_produto_ordena_por_quantidade_e_respeita_o_top()
    {
        var a = new Produto { Nome = "A", Categoria = "x", PrecoCusto = 10m };
        var b = new Produto { Nome = "B", Categoria = "y", PrecoCusto = 5m };
        _repo.Vendas.Add(new Venda
        {
            LojaId = _loja,
            Itens =
            [
                new ItemVenda { ProdutoId = a.Id, Produto = a, Quantidade = 1, Subtotal = 30m },
                new ItemVenda { ProdutoId = b.Id, Produto = b, Quantidade = 3, Subtotal = 60m },
                new ItemVenda { ProdutoId = null, Quantidade = 9, Subtotal = 999m },      // serviço: fora do ranking
            ]
        });

        var todos = (List<ProdutoRankingDto>)(await new ProdutosRankingQueryHandler(_repo).HandleAsync(new(_loja, null, null, 10))).Valor!;
        var top1 = (List<ProdutoRankingDto>)(await new ProdutosRankingQueryHandler(_repo).HandleAsync(new(_loja, null, null, 1))).Valor!;

        Assert.Equal(["B", "A"], todos.Select(r => r.Nome));
        Assert.Equal(45m, todos[0].LucroEstimado);      // 60 - 3 × 5
        Assert.Equal(20m, todos[1].LucroEstimado);      // 30 - 1 × 10
        Assert.Equal("B", Assert.Single(top1).Nome);
    }
}
