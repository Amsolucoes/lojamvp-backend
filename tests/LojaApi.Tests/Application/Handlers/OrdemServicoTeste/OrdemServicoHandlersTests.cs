using LojaApi.Application.OrdemServico;
using LojaApi.Models;
using LojaApi.Shared.Application;
using LojaApi.src.Models.Funcionarios;
using LojaApi.src.Models.OrdemServico;
using LojaApi.Tests.Suporte.Falsos;
using NSubstitute;

namespace LojaApi.Tests.Application.Handlers.OrdemServicoTeste;

// Handlers de OrdemServico (checklist, orçamentos e ordens, patrimônio), testados com repositório de mentira.
public class OrdemServicoHandlersTests
{
    private readonly RepositorioOrdemServicoFalso _repo = new();
    private readonly Guid _loja = Guid.NewGuid();
    private readonly Guid _outraLoja = Guid.NewGuid();

    private Cliente NovoCliente(Guid? loja = null) { var c = new Cliente { LojaId = loja ?? _loja, Nome = "Maria" }; _repo.Clientes.Add(c); return c; }
    private Produto NovoProduto(decimal estoque = 10m, Guid? loja = null) { var p = new Produto { LojaId = loja ?? _loja, Nome = "Pastilha", Estoque = estoque }; _repo.Produtos.Add(p); return p; }
    private ContaBancaria NovaConta(Guid? loja = null) { var c = new ContaBancaria { LojaId = loja ?? _loja }; _repo.Contas.Add(c); return c; }
    private Profissional NovoMecanico(decimal? comissao = 10m, string baseCalculo = "total") { var p = new Profissional { LojaId = _loja, Nome = "Zé", ComissaoPadraoPercentual = comissao, ComissaoBaseCalculo = baseCalculo }; _repo.Mecanicos.Add(p); return p; }

    private OrcamentoServico NovaOrdem(string status = "em_andamento", params ItemOrcamentoServico[] itens)
    {
        var o = new OrcamentoServico { LojaId = _loja, ClienteId = Guid.NewGuid(), Status = status, VeiculoDescricao = "Gol", ValorTotal = itens.Sum(i => i.ValorTotal) };
        foreach (var i in itens) o.Itens.Add(i);
        _repo.Orcamentos.Add(o);
        return o;
    }

    private static ItemOrcamentoServico ItemPeca(Guid? produto, int qtd = 2, decimal valor = 100m) =>
        new() { Tipo = "peca", ProdutoId = produto, Descricao = "Pastilha", Quantidade = qtd, ValorUnitario = valor, ValorTotal = qtd * valor };

    private static ItemOrcamentoServico ItemServico(decimal valor = 80m, Guid? servico = null) =>
        new() { Tipo = "servico", ServicoId = servico, Descricao = "Alinhamento", Quantidade = 1, ValorUnitario = valor, ValorTotal = valor };

    private static ItemOrcamentoRequest ItemReq(string tipo = "servico", Guid? produto = null, Guid? servico = null, int qtd = 1, decimal valor = 50m) => new(tipo, produto, servico, " Item ", qtd, valor);

    private static CriarOrcamentoRequest Req(Guid cliente, List<ItemOrcamentoRequest>? itens = null, List<MecanicoOrcamentoRequest>? mecanicos = null, List<ChecklistRespostaRequest>? checklist = null) =>
        new(cliente, "Gol", " abc1d23 ", null, itens ?? [ItemReq()], mecanicos ?? [], checklist);

    // ── Criar / editar ───────────────────────────────────────────────────────

    [Fact]
    public async Task Criar_valida_loja_itens_cliente_tipo_mecanico_e_referencias_de_outra_loja_sem_gravar()
    {
        var cliente = NovoCliente();
        var clienteDeOutra = NovoCliente(_outraLoja);
        var produtoDeOutra = NovoProduto(loja: _outraLoja);
        var servicoDeOutra = new Servico { LojaId = _outraLoja }; _repo.Servicos.Add(servicoDeOutra);
        var checklistDeOutra = new ChecklistItem { LojaId = _outraLoja }; _repo.ItensChecklist.Add(checklistDeOutra);
        var handler = new CriarOrcamentoCommandHandler(_repo);

        Assert.Equal("Loja não encontrada.", (await handler.HandleAsync(new(null, Req(cliente.Id)))).Mensagem);
        Assert.Equal("O orçamento precisa ter ao menos um item.", (await handler.HandleAsync(new(_loja, Req(cliente.Id, [])))).Mensagem);
        Assert.Equal("Cliente não encontrado.", (await handler.HandleAsync(new(_loja, Req(clienteDeOutra.Id)))).Mensagem);
        Assert.Equal("Tipo de item inválido — use 'peca' ou 'servico'.", (await handler.HandleAsync(new(_loja, Req(cliente.Id, [ItemReq("x")])))).Mensagem);
        Assert.Equal("Mecânico/profissional não encontrado.", (await handler.HandleAsync(new(_loja, Req(cliente.Id, mecanicos: [new(Guid.NewGuid(), 5m)])))).Mensagem);
        Assert.Equal("Produto do item 'Item' não encontrado.", (await handler.HandleAsync(new(_loja, Req(cliente.Id, [ItemReq("peca", produto: produtoDeOutra.Id)])))).Mensagem);
        Assert.Equal("Serviço do item 'Item' não encontrado.", (await handler.HandleAsync(new(_loja, Req(cliente.Id, [ItemReq(servico: servicoDeOutra.Id)])))).Mensagem);
        Assert.Equal("Item de checklist não encontrado.", (await handler.HandleAsync(new(_loja, Req(cliente.Id, checklist: [new(checklistDeOutra.Id, "bom", null)])))).Mensagem);
        Assert.Empty(_repo.Orcamentos);
        Assert.Equal(0, _repo.Salvamentos);
    }

    [Fact]
    public async Task Criar_ok_nasce_pendente_soma_os_itens_e_usa_o_percentual_padrao_do_mecanico()
    {
        var cliente = NovoCliente();
        var mecanico = NovoMecanico(comissao: 12m);

        var r = await new CriarOrcamentoCommandHandler(_repo).HandleAsync(new(_loja, Req(cliente.Id,
            [ItemReq("peca", qtd: 2, valor: 100m), ItemReq("servico", valor: 80m)], [new(mecanico.Id, 0m)])));

        Assert.Equal(TipoResultado.Ok, r.Tipo);
        var o = Assert.Single(_repo.Orcamentos);
        Assert.Equal(("pendente", 280m, "ABC1D23"), (o.Status, o.ValorTotal, o.Placa));
        Assert.Equal(2, o.Itens.Count);
        Assert.Equal(12m, Assert.Single(o.Mecanicos).ComissaoPercentual);
        Assert.Equal("Item", o.Itens.First().Descricao);
        Assert.Equal(1, _repo.Salvamentos);
    }

    [Fact]
    public async Task Editar_so_pendente_ou_em_andamento_e_respeita_a_loja()
    {
        var cliente = NovoCliente();
        var concluida = NovaOrdem("concluido", ItemServico());
        var deOutra = NovaOrdem("pendente"); deOutra.LojaId = _outraLoja;
        var handler = new AtualizarOrcamentoCommandHandler(_repo);

        Assert.Equal(TipoResultado.NaoEncontrado, (await handler.HandleAsync(new(_loja, deOutra.Id, Req(cliente.Id)))).Tipo);
        Assert.StartsWith("Só é possível editar uma ordem pendente ou em andamento.", (await handler.HandleAsync(new(_loja, concluida.Id, Req(cliente.Id)))).Mensagem);
        Assert.Equal(0, _repo.Salvamentos);

        var andamento = NovaOrdem("em_andamento", ItemServico(10m));
        var ok = await handler.HandleAsync(new(_loja, andamento.Id, Req(cliente.Id, [ItemReq(qtd: 3, valor: 20m)])));
        Assert.Equal(TipoResultado.Ok, ok.Tipo);
        Assert.Equal(60m, andamento.ValorTotal);
        Assert.Equal("ABC1D23", andamento.Placa);
    }

    // ── Transições ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Aprovar_cancelar_e_reabrir_seguem_o_estado_da_ordem()
    {
        var pendente = NovaOrdem("pendente");
        var concluida = NovaOrdem("concluido");

        Assert.Equal("Só é possível reabrir uma ordem cancelada.", (await new ReabrirOrcamentoCommandHandler(_repo).HandleAsync(new(_loja, pendente.Id))).Mensagem);
        Assert.Equal(TipoResultado.Ok, (await new AprovarOrcamentoCommandHandler(_repo).HandleAsync(new(_loja, pendente.Id))).Tipo);
        Assert.Equal(("em_andamento", true), (pendente.Status, pendente.AprovadoEm.HasValue));
        Assert.Equal("Só é possível aprovar um orçamento pendente.", (await new AprovarOrcamentoCommandHandler(_repo).HandleAsync(new(_loja, pendente.Id))).Mensagem);

        Assert.Equal(TipoResultado.Ok, (await new CancelarOrcamentoCommandHandler(_repo).HandleAsync(new(_loja, pendente.Id))).Tipo);
        Assert.Equal("cancelado", pendente.Status);
        Assert.Equal(TipoResultado.Ok, (await new ReabrirOrcamentoCommandHandler(_repo).HandleAsync(new(_loja, pendente.Id))).Tipo);
        Assert.Equal("em_andamento", pendente.Status);
        Assert.Equal("Não é possível cancelar uma ordem já concluída.", (await new CancelarOrcamentoCommandHandler(_repo).HandleAsync(new(_loja, concluida.Id))).Mensagem);
        Assert.Equal(TipoResultado.NaoEncontrado, (await new AprovarOrcamentoCommandHandler(_repo).HandleAsync(new(null, pendente.Id))).Tipo);
    }

    [Fact]
    public async Task Excluir_so_pendente_e_ajustar_datas_so_aprovada_ou_concluida()
    {
        var pendente = NovaOrdem("pendente");
        var andamento = NovaOrdem("em_andamento");

        Assert.Equal("Só é possível excluir um orçamento ainda pendente. Cancele em vez de excluir.", (await new ExcluirOrcamentoCommandHandler(_repo).HandleAsync(new(_loja, andamento.Id))).Mensagem);
        Assert.Equal("Só é possível ajustar entrada/saída de uma ordem aprovada ou concluída.",
            (await new AjustarDatasOrcamentoCommandHandler(_repo).HandleAsync(new(_loja, pendente.Id, new(DateTime.UtcNow, null)))).Mensagem);
        Assert.Equal(TipoResultado.Ok, (await new ExcluirOrcamentoCommandHandler(_repo).HandleAsync(new(_loja, pendente.Id))).Tipo);
        Assert.DoesNotContain(pendente, _repo.Orcamentos);

        var entrada = new DateTime(2031, 5, 10, 8, 0, 0);
        Assert.Equal("A data/hora de saída não pode ser antes da entrada.",
            (await new AjustarDatasOrcamentoCommandHandler(_repo).HandleAsync(new(_loja, andamento.Id, new(entrada, entrada.AddHours(-1))))).Mensagem);
        Assert.Equal(TipoResultado.Ok, (await new AjustarDatasOrcamentoCommandHandler(_repo).HandleAsync(new(_loja, andamento.Id, new(entrada, entrada.AddHours(3))))).Tipo);
        Assert.Equal(DateTimeKind.Utc, andamento.AprovadoEm!.Value.Kind);
    }

    // ── Concluir ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Concluir_valida_na_ordem_certa_e_nao_grava_nada_quando_recusa()
    {
        var conta = NovaConta();
        var contaDeOutra = NovaConta(_outraLoja);
        var pouco = NovoProduto(estoque: 1m);
        var deOutra = NovoProduto(loja: _outraLoja);
        var pendente = NovaOrdem("pendente");
        var semEstoque = NovaOrdem("em_andamento", ItemPeca(pouco.Id, 3));
        var produtoAlheio = NovaOrdem("em_andamento", ItemPeca(deOutra.Id, 1));
        var simples = NovaOrdem("em_andamento", ItemServico());
        var handler = new ConcluirOrcamentoCommandHandler(_repo);
        ConcluirOrcamentoRequest R(Guid c, bool jaPago = false, string? forma = null) => new(c, null, jaPago, forma);

        Assert.Equal("Só é possível concluir uma ordem em andamento (aprovada).", (await handler.HandleAsync(new(_loja, pendente.Id, R(conta.Id)))).Mensagem);
        Assert.Equal("Conta bancária não encontrada.", (await handler.HandleAsync(new(_loja, simples.Id, R(contaDeOutra.Id)))).Mensagem);
        Assert.StartsWith("Estoque insuficiente para 'Pastilha' — disponível: 1", (await handler.HandleAsync(new(_loja, semEstoque.Id, R(conta.Id)))).Mensagem);
        Assert.Equal("Produto do item 'Pastilha' não encontrado.", (await handler.HandleAsync(new(_loja, produtoAlheio.Id, R(conta.Id)))).Mensagem);
        Assert.Equal("Informe a forma de pagamento.", (await handler.HandleAsync(new(_loja, simples.Id, R(conta.Id, jaPago: true)))).Mensagem);
        Assert.Equal(0, _repo.Salvamentos);
        Assert.Empty(_repo.Lancamentos);
        Assert.Empty(_repo.CategoriasFinanceiras);       // nem a categoria é criada quando a ordem é recusada
        Assert.Equal(10m, deOutra.Estoque);
    }

    [Fact]
    public async Task Concluir_baixa_estoque_gera_lancamento_categoria_e_comissao_numa_unica_gravacao()
    {
        var conta = NovaConta();
        var produto = NovoProduto(estoque: 5m);
        var mecanico = NovoMecanico();
        var servico = Guid.NewGuid();
        var ordem = NovaOrdem("em_andamento", ItemPeca(produto.Id, 2, 100m), ItemServico(80m, servico));
        ordem.Mecanicos.Add(new MecanicoOrcamento { ProfissionalId = mecanico.Id, Profissional = mecanico, ComissaoPercentual = 10m });

        var r = await new ConcluirOrcamentoCommandHandler(_repo).HandleAsync(new(_loja, ordem.Id, new(conta.Id, null, true, "pix", null)));

        Assert.Equal(TipoResultado.Ok, r.Tipo);
        Assert.Equal(3m, produto.Estoque);
        Assert.Equal("saida", Assert.Single(_repo.MovimentosEstoque).Tipo);
        var categoria = Assert.Single(_repo.CategoriasFinanceiras);
        var lancamento = Assert.Single(_repo.Lancamentos);
        Assert.Equal((categoria.Id, 280m, "pago", "Pago via pix", conta.Id), (lancamento.CategoriaId, lancamento.Valor, lancamento.Status, lancamento.Observacao, lancamento.ContaBancariaId));
        Assert.Equal(lancamento.Id, ordem.LancamentoFinanceiroId);
        var comissao = Assert.Single(_repo.Comissoes);
        Assert.Equal((280m, 10m, 28m, "ordem_servico"), (comissao.ValorServico, comissao.ComissaoPercentual, comissao.ValorComissao, comissao.OrigemTipo));
        Assert.Equal(("concluido", true), (ordem.Status, ordem.ConcluidoEm.HasValue));
        Assert.Equal(1, _repo.Salvamentos);                                // tudo junto, numa gravação só
    }

    [Fact]
    public async Task Concluir_reaproveita_a_categoria_e_aplica_excecao_de_comissao_do_servico()
    {
        var conta = NovaConta();
        var mecanico = NovoMecanico();
        var servico = Guid.NewGuid();
        _repo.CategoriasFinanceiras.Add(new CategoriaFinanceira { LojaId = _loja, Nome = "Ordem de Serviço" });
        _repo.Excecoes.Add(new ComissaoServicoProfissional { ProfissionalId = mecanico.Id, ServicoId = servico, ComissaoPercentual = 0m });
        var ordem = NovaOrdem("em_andamento", ItemServico(80m, servico), ItemServico(20m));
        ordem.Mecanicos.Add(new MecanicoOrcamento { ProfissionalId = mecanico.Id, Profissional = mecanico, ComissaoPercentual = 10m });

        await new ConcluirOrcamentoCommandHandler(_repo).HandleAsync(new(_loja, ordem.Id, new(conta.Id, null)));

        Assert.Single(_repo.CategoriasFinanceiras);
        var comissao = Assert.Single(_repo.Comissoes);
        Assert.Equal((100m, 2m, 2m), (comissao.ValorServico, comissao.ComissaoPercentual, comissao.ValorComissao));      // só os 20 do serviço avulso comissionam
    }

    // ── Desfazer conclusão ───────────────────────────────────────────────────

    [Fact]
    public async Task Desfazer_recusa_nao_concluida_lancamento_pago_e_comissao_paga_sem_mexer_em_nada()
    {
        var produto = NovoProduto(estoque: 3m);
        var andamento = NovaOrdem("em_andamento");
        var comLancamentoPago = NovaOrdem("concluido", ItemPeca(produto.Id, 1));
        var lancamento = new LancamentoFinanceiro { Status = "pago" }; _repo.Lancamentos.Add(lancamento);
        comLancamentoPago.LancamentoFinanceiroId = lancamento.Id;
        var comComissaoPaga = NovaOrdem("concluido", ItemPeca(produto.Id, 1));
        _repo.Comissoes.Add(new ComissaoFuncionario { OrigemTipo = "ordem_servico", OrigemId = comComissaoPaga.Id, Status = "pago" });
        var handler = new DesfazerConclusaoOrcamentoCommandHandler(_repo);

        Assert.Equal("Só é possível desfazer uma ordem concluída.", (await handler.HandleAsync(new(_loja, andamento.Id))).Mensagem);
        Assert.StartsWith("O lançamento financeiro desta ordem já foi marcado como pago.", (await handler.HandleAsync(new(_loja, comLancamentoPago.Id))).Mensagem);
        Assert.StartsWith("Uma ou mais comissões geradas por esta ordem já foram pagas.", (await handler.HandleAsync(new(_loja, comComissaoPaga.Id))).Mensagem);
        Assert.Equal(3m, produto.Estoque);
        Assert.Equal(0, _repo.Salvamentos);
    }

    [Fact]
    public async Task Desfazer_repoe_o_estoque_remove_lancamento_e_comissao_e_ignora_produto_de_outra_loja()
    {
        var meu = NovoProduto(estoque: 3m);
        var alheio = NovoProduto(estoque: 3m, loja: _outraLoja);
        var ordem = NovaOrdem("concluido", ItemPeca(meu.Id, 2), ItemPeca(alheio.Id, 1));
        var lancamento = new LancamentoFinanceiro { Status = "pendente" }; _repo.Lancamentos.Add(lancamento);
        ordem.LancamentoFinanceiroId = lancamento.Id;
        ordem.ConcluidoEm = DateTime.UtcNow;
        _repo.Comissoes.Add(new ComissaoFuncionario { OrigemTipo = "ordem_servico", OrigemId = ordem.Id, Status = "pendente" });

        var r = await new DesfazerConclusaoOrcamentoCommandHandler(_repo).HandleAsync(new(_loja, ordem.Id));

        Assert.Equal(TipoResultado.Ok, r.Tipo);
        Assert.Equal((5m, 3m), (meu.Estoque, alheio.Estoque));
        Assert.Equal("entrada", Assert.Single(_repo.MovimentosEstoque).Tipo);
        Assert.Empty(_repo.Lancamentos);
        Assert.Empty(_repo.Comissoes);
        Assert.Equal(("em_andamento", null, null), (ordem.Status, ordem.ConcluidoEm, ordem.LancamentoFinanceiroId));
        Assert.Equal(1, _repo.Salvamentos);
    }

    // ── E-mail e PDF ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Email_e_pdf_do_orcamento_passam_pela_porta_e_traduzem_o_resultado()
    {
        var notificacao = Substitute.For<IOrcamentoNotificacao>();
        var id = Guid.NewGuid();
        notificacao.EnviarPorEmailAsync(id, _loja).Returns(new ResultadoEnvioOrcamentoDto(false, "Cliente não tem e-mail cadastrado."));
        notificacao.GerarPdfAsync(id, _loja).Returns(new byte[] { 1, 2, 3 });
        notificacao.GerarPdfAsync(Arg.Is<Guid>(g => g != id), _loja).Returns((byte[]?)null);

        Assert.Equal("Loja não encontrada.", (await new EnviarOrcamentoPorEmailCommandHandler(notificacao).HandleAsync(new(null, id))).Mensagem);
        Assert.Equal("Cliente não tem e-mail cadastrado.", (await new EnviarOrcamentoPorEmailCommandHandler(notificacao).HandleAsync(new(_loja, id))).Mensagem);

        var pdf = (ArquivoPdfDto)(await new BaixarOrcamentoPdfQueryHandler(notificacao).HandleAsync(new(_loja, id))).Valor!;
        Assert.Equal(($"orcamento-{id.ToString()[..8]}.pdf", 3), (pdf.NomeArquivo, pdf.Pdf.Length));
        Assert.Equal(TipoResultado.NaoEncontrado, (await new BaixarOrcamentoPdfQueryHandler(notificacao).HandleAsync(new(_loja, Guid.NewGuid()))).Tipo);
        Assert.Equal("Loja não encontrada.", (await new BaixarOrcamentoPdfQueryHandler(notificacao).HandleAsync(new(null, id))).Mensagem);
    }

    // ── Checklist ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Checklist_item_so_em_categoria_da_loja_e_exclusao_protege_o_que_esta_em_uso()
    {
        var categoria = new ChecklistCategoria { LojaId = _loja, Nome = "Freios" }; _repo.Categorias.Add(categoria);
        var deOutra = new ChecklistCategoria { LojaId = _outraLoja, Nome = "Dela" }; _repo.Categorias.Add(deOutra);

        Assert.Equal("Categoria de checklist não encontrada.", (await new CriarChecklistItemCommandHandler(_repo).HandleAsync(new(_loja, new(deOutra.Id, "x", 0, true)))).Mensagem);
        var criado = await new CriarChecklistItemCommandHandler(_repo).HandleAsync(new(_loja, new(categoria.Id, " Disco ", 1, true)));
        var item = Assert.Single(_repo.ItensChecklist);
        Assert.Equal(("Disco", categoria.Id), (item.Nome, item.CategoriaId));
        Assert.Equal(TipoResultado.Ok, criado.Tipo);

        _repo.ItensChecklistEmUso.Add(item.Id);
        Assert.StartsWith("Não é possível excluir: esta categoria tem itens já usados", (await new ExcluirChecklistCategoriaCommandHandler(_repo).HandleAsync(new(_loja, categoria.Id))).Mensagem);
        Assert.Equal("Item em uso em orçamentos — foi desativado em vez de excluído.", ((dynamic)(await new ExcluirChecklistItemCommandHandler(_repo).HandleAsync(new(_loja, item.Id))).Valor!).mensagem);
        Assert.False(item.Ativo);

        _repo.ItensChecklistEmUso.Clear();
        Assert.Equal("Item excluído.", ((dynamic)(await new ExcluirChecklistItemCommandHandler(_repo).HandleAsync(new(_loja, item.Id))).Valor!).mensagem);
        Assert.Equal("Categoria excluída.", ((dynamic)(await new ExcluirChecklistCategoriaCommandHandler(_repo).HandleAsync(new(_loja, categoria.Id))).Valor!).mensagem);
        Assert.Equal(TipoResultado.NaoEncontrado, (await new ExcluirChecklistCategoriaCommandHandler(_repo).HandleAsync(new(null, deOutra.Id))).Tipo);
    }

    // ── Patrimônio ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Contagem_guarda_o_esperado_da_epoca_e_recusa_item_de_outra_loja()
    {
        var item = new ItemPatrimonio { LojaId = _loja, Nome = "Chave", QuantidadeEsperada = 4 }; _repo.ItensPatrimonio.Add(item);
        var alheio = new ItemPatrimonio { LojaId = _outraLoja, Nome = "Dela", QuantidadeEsperada = 1 }; _repo.ItensPatrimonio.Add(alheio);
        var handler = new CriarContagemPatrimonioCommandHandler(_repo);

        Assert.Equal("Loja não encontrada.", (await handler.HandleAsync(new(null, new(null, null, null, [new(item.Id, 1, null)])))).Mensagem);
        Assert.Equal("A contagem precisa ter ao menos um item.", (await handler.HandleAsync(new(_loja, new(null, null, null, [])))).Mensagem);
        Assert.Equal("Algum item de patrimônio informado não foi encontrado.", (await handler.HandleAsync(new(_loja, new(null, null, null, [new(alheio.Id, 1, null)])))).Mensagem);
        Assert.Empty(_repo.Contagens);

        var ok = await handler.HandleAsync(new(_loja, new(new DateTime(2031, 2, 1, 10, 0, 0), "Carlos", null, [new(item.Id, 3, "emprestada")])));
        Assert.Equal(TipoResultado.Ok, ok.Tipo);
        var contagem = Assert.Single(_repo.Contagens);
        var linha = Assert.Single(contagem.Itens);
        Assert.Equal((4, 3, DateTimeKind.Utc), (linha.QuantidadeEsperadaNoMomento, linha.QuantidadeContada, contagem.DataContagem.Kind));
    }

    [Fact]
    public async Task Item_de_patrimonio_ja_contado_so_desativa_e_nome_e_obrigatorio()
    {
        var contado = new ItemPatrimonio { LojaId = _loja, Nome = "A" };
        var livre = new ItemPatrimonio { LojaId = _loja, Nome = "B" };
        _repo.ItensPatrimonio.AddRange([contado, livre]);
        _repo.ItensPatrimonioContados.Add(contado.Id);
        var excluir = new ExcluirItemPatrimonioCommandHandler(_repo);

        Assert.Equal("Nome é obrigatório.", (await new CriarItemPatrimonioCommandHandler(_repo).HandleAsync(new(_loja, new(" ", null, 1, 1m, null, true)))).Mensagem);
        Assert.Equal("Item já usado em alguma contagem — foi desativado em vez de excluído.", ((dynamic)(await excluir.HandleAsync(new(_loja, contado.Id))).Valor!).mensagem);
        Assert.False(contado.Ativo);
        Assert.Equal("Item excluído.", ((dynamic)(await excluir.HandleAsync(new(_loja, livre.Id))).Valor!).mensagem);
        Assert.DoesNotContain(livre, _repo.ItensPatrimonio);
    }

    [Fact]
    public async Task Pdfs_do_patrimonio_passam_pela_porta_e_dependem_da_loja()
    {
        var pdf = Substitute.For<IRelatorioPatrimonioPdf>();
        pdf.GerarCadastro(Arg.Any<string?>(), Arg.Any<IReadOnlyList<ItemPatrimonio>>()).Returns(new byte[] { 1 });
        pdf.GerarContagem(Arg.Any<string?>(), Arg.Any<ContagemPatrimonio>()).Returns(new byte[] { 2, 2 });
        var contagem = new ContagemPatrimonio { LojaId = _loja, DataContagem = new DateTime(2031, 2, 1, 0, 0, 0, DateTimeKind.Utc) }; _repo.Contagens.Add(contagem);

        Assert.Equal(TipoResultado.NaoEncontrado, (await new RelatorioPatrimonioPdfQueryHandler(_repo, pdf).HandleAsync(new(null))).Tipo);
        var cadastro = (ArquivoPdfDto)(await new RelatorioPatrimonioPdfQueryHandler(_repo, pdf).HandleAsync(new(_loja))).Valor!;
        var daContagem = (ArquivoPdfDto)(await new ContagemPatrimonioPdfQueryHandler(_repo, pdf).HandleAsync(new(_loja, contagem.Id))).Valor!;

        Assert.Single(cadastro.Pdf);
        Assert.StartsWith("patrimonio-", cadastro.NomeArquivo);
        Assert.Equal(("contagem-patrimonio-2031-02-01.pdf", 2), (daContagem.NomeArquivo, daContagem.Pdf.Length));
        Assert.Equal(TipoResultado.NaoEncontrado, (await new ContagemPatrimonioPdfQueryHandler(_repo, pdf).HandleAsync(new(_outraLoja, contagem.Id))).Tipo);
        pdf.Received(1).GerarContagem("Oficina Teste", contagem);
    }
}
