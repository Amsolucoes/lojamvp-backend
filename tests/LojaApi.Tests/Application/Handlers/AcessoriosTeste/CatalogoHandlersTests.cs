using LojaApi.Application.Acessorios;
using LojaApi.Shared.Application;
using LojaApi.src.Models;
using LojaApi.Tests.Suporte.Falsos;

namespace LojaApi.Tests.Application.Handlers.AcessoriosTeste;

// Handlers do catálogo de acessórios (produtos, categorias e avaliações), testados com repositório de mentira.
public class CatalogoHandlersTests
{
    private readonly RepositorioAcessoriosFalso _repo = new();

    private static SalvarProdutoAcessorioRequest Produto(string nome = "Leitor", decimal preco = 100m) =>
        new(nome, "desc", preco, null, 5, "outro", null, null, true, false, 0);

    // ── Produtos ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Criar_produto_exige_nome_e_preco_positivo()
    {
        var handler = new CriarProdutoAcessorioCommandHandler(_repo);

        Assert.Equal((TipoResultado.Invalido, "Preencha nome e preço válido."), ((await handler.HandleAsync(new(Produto(" ")))) is var a ? (a.Tipo, a.Mensagem) : default));
        Assert.Equal("Preencha nome e preço válido.", (await handler.HandleAsync(new(Produto(preco: 0)))).Mensagem);
        Assert.Equal(0, _repo.Salvamentos);

        var ok = await handler.HandleAsync(new(Produto("  Leitor X  ")));
        Assert.Equal(TipoResultado.Ok, ok.Tipo);
        Assert.Equal("Leitor X", Assert.Single(_repo.Produtos).Nome);
    }

    [Fact]
    public async Task Excluir_produto_ja_usado_em_pedido_so_desativa()
    {
        var livre = new ProdutoAcessorio { Nome = "A" };
        var usado = new ProdutoAcessorio { Nome = "B" };
        _repo.Produtos.AddRange([livre, usado]);
        _repo.ProdutosComPedido.Add(usado.Id);
        var handler = new ExcluirProdutoAcessorioCommandHandler(_repo);

        Assert.Equal("Produto excluído.", ((dynamic)(await handler.HandleAsync(new(livre.Id))).Valor!).mensagem);
        Assert.Equal("Produto já usado em pedidos — foi desativado em vez de excluído.", ((dynamic)(await handler.HandleAsync(new(usado.Id))).Valor!).mensagem);
        Assert.DoesNotContain(livre, _repo.Produtos);
        Assert.False(usado.Ativo);
        Assert.Equal(TipoResultado.NaoEncontrado, (await handler.HandleAsync(new(Guid.NewGuid()))).Tipo);
    }

    [Fact]
    public async Task Atualizar_produto_troca_os_campos_e_marca_a_data_de_atualizacao()
    {
        var p = new ProdutoAcessorio { Nome = "Velho", Preco = 10, AtualizadoEm = DateTime.UtcNow.AddDays(-1) };
        _repo.Produtos.Add(p);

        var r = await new AtualizarProdutoAcessorioCommandHandler(_repo).HandleAsync(new(p.Id, Produto("  Novo ", 99m)));

        Assert.Same(p, r.Valor);
        Assert.Equal(("Novo", 99m, 5), (p.Nome, p.Preco, p.Estoque));
        Assert.True(p.AtualizadoEm > DateTime.UtcNow.AddMinutes(-1));
        Assert.Equal(TipoResultado.NaoEncontrado, (await new AtualizarProdutoAcessorioCommandHandler(_repo).HandleAsync(new(Guid.NewGuid(), Produto()))).Tipo);
    }

    [Fact]
    public async Task Detalhe_do_produto_so_para_ativo_e_mostra_novo_e_estoque()
    {
        var ativo = new ProdutoAcessorio { Nome = "A", Estoque = 3, Destaque = true };
        var inativo = new ProdutoAcessorio { Nome = "B", Ativo = false };
        var antigo = new ProdutoAcessorio { Nome = "C", CriadoEm = DateTime.UtcNow.AddDays(-15) };
        _repo.Produtos.AddRange([ativo, inativo, antigo]);
        var handler = new BuscarProdutoAcessorioQueryHandler(_repo);

        var a = (dynamic)(await handler.HandleAsync(new(ativo.Id))).Valor!;
        Assert.Equal((true, 3, true, true), (a.disponivel, a.estoque, a.destaque, a.novo));
        Assert.False(((dynamic)(await handler.HandleAsync(new(antigo.Id))).Valor!).novo);
        Assert.Equal(TipoResultado.NaoEncontrado, (await handler.HandleAsync(new(inativo.Id))).Tipo);
    }

    // ── Categorias ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Criar_categoria_gera_a_chave_recusa_duplicada_e_vai_para_o_fim_da_ordem()
    {
        _repo.Categorias.Add(new CategoriaAcessorio { Nome = "Outro", Chave = "outro", Ordem = 3 });
        var handler = new CriarCategoriaAcessorioCommandHandler(_repo);

        Assert.Equal("Digite o nome da categoria.", (await handler.HandleAsync(new(new(" ")))).Mensagem);
        Assert.Equal("Nome inválido.", (await handler.HandleAsync(new(new("?!")))).Mensagem);

        var r = await handler.HandleAsync(new(new("  Câmeras & Áudio ")));
        var criada = (CategoriaAcessorio)r.Valor!;
        Assert.Equal(("Câmeras & Áudio", "cameras_audio", 4), (criada.Nome, criada.Chave, criada.Ordem));

        var duplicada = await handler.HandleAsync(new(new("cameras AUDIO")));
        Assert.Equal((TipoResultado.Conflito, "Já existe uma categoria com esse nome."), (duplicada.Tipo, duplicada.Mensagem));
    }

    [Fact]
    public async Task Primeira_categoria_tem_ordem_zero()
    {
        var r = await new CriarCategoriaAcessorioCommandHandler(_repo).HandleAsync(new(new("Primeira")));

        Assert.Equal(0, ((CategoriaAcessorio)r.Valor!).Ordem);
    }

    [Fact]
    public async Task Renomear_categoria_nao_troca_a_chave()
    {
        var c = new CategoriaAcessorio { Nome = "Velha", Chave = "velha" };
        _repo.Categorias.Add(c);
        var handler = new AtualizarCategoriaAcessorioCommandHandler(_repo);

        Assert.Equal("Digite o nome da categoria.", (await handler.HandleAsync(new(c.Id, new("")))).Mensagem);
        await handler.HandleAsync(new(c.Id, new("  Nova ")));

        Assert.Equal(("Nova", "velha"), (c.Nome, c.Chave));
        Assert.Equal(TipoResultado.NaoEncontrado, (await handler.HandleAsync(new(Guid.NewGuid(), new("x")))).Tipo);
    }

    [Fact]
    public async Task Excluir_categoria_em_uso_so_desativa()
    {
        var livre = new CategoriaAcessorio { Nome = "Livre", Chave = "livre" };
        var emUso = new CategoriaAcessorio { Nome = "Uso", Chave = "uso" };
        _repo.Categorias.AddRange([livre, emUso]);
        _repo.ChavesEmUso.Add("uso");
        var handler = new ExcluirCategoriaAcessorioCommandHandler(_repo);

        Assert.Equal("Categoria excluída.", ((dynamic)(await handler.HandleAsync(new(livre.Id))).Valor!).mensagem);
        Assert.Equal("Categoria em uso por produto(s) — foi desativada em vez de excluída.", ((dynamic)(await handler.HandleAsync(new(emUso.Id))).Valor!).mensagem);
        Assert.DoesNotContain(livre, _repo.Categorias);
        Assert.False(emUso.Ativa);
    }

    // ── Avaliações ───────────────────────────────────────────────────────────

    private (ProdutoAcessorio Produto, PedidoAcessorio Pedido) ProdutoComCompraPaga(string status = "pago")
    {
        var produto = new ProdutoAcessorio { Nome = "Mouse" };
        var pedido = new PedidoAcessorio { ClienteNome = "Maria", ClienteEmail = "Maria@Teste.com", Status = status, PagoEm = DateTime.UtcNow };
        pedido.Itens.Add(new ItemPedidoAcessorio { ProdutoId = produto.Id });
        _repo.Produtos.Add(produto);
        _repo.Pedidos.Add(pedido);
        return (produto, pedido);
    }

    [Fact]
    public async Task Avaliar_valida_nota_email_e_compra_paga()
    {
        var (produto, _) = ProdutoComCompraPaga();
        var (naoPago, _) = ProdutoComCompraPaga("aguardando_pagamento");
        var handler = new CriarAvaliacaoAcessorioCommandHandler(_repo);

        Assert.Equal("A nota deve ser de 1 a 5.", (await handler.HandleAsync(new(new(produto.Id, "maria@teste.com", 0, null)))).Mensagem);
        Assert.Equal("A nota deve ser de 1 a 5.", (await handler.HandleAsync(new(new(produto.Id, "maria@teste.com", 6, null)))).Mensagem);
        Assert.Equal("Informe o e-mail usado na compra.", (await handler.HandleAsync(new(new(produto.Id, " ", 5, null)))).Mensagem);
        Assert.Equal("Não encontramos uma compra paga desse produto com esse e-mail.", (await handler.HandleAsync(new(new(produto.Id, "outro@teste.com", 5, null)))).Mensagem);
        Assert.Equal("Não encontramos uma compra paga desse produto com esse e-mail.", (await handler.HandleAsync(new(new(naoPago.Id, "maria@teste.com", 5, null)))).Mensagem);
        Assert.Empty(_repo.Avaliacoes);
    }

    [Fact]
    public async Task Avaliar_grava_uma_vez_por_pedido_e_comentario_em_branco_vira_nulo()
    {
        var (produto, pedido) = ProdutoComCompraPaga();
        var handler = new CriarAvaliacaoAcessorioCommandHandler(_repo);

        var ok = await handler.HandleAsync(new(new(produto.Id, "  MARIA@teste.COM ", 4, "   ")));

        Assert.Equal("Avaliação enviada, obrigado!", ((dynamic)ok.Valor!).mensagem);
        var av = Assert.Single(_repo.Avaliacoes);
        Assert.Equal(("Maria", 4, null, pedido.Id), (av.ClienteNome, av.Nota, av.Comentario, av.PedidoId));
        Assert.Equal("Você já avaliou este produto.", (await handler.HandleAsync(new(new(produto.Id, "maria@teste.com", 5, null)))).Mensagem);
    }

    [Fact]
    public async Task Avaliacoes_trazem_media_com_uma_casa_e_total()
    {
        var produto = new ProdutoAcessorio();
        foreach (var nota in new[] { 5, 4, 4 })
            _repo.Avaliacoes.Add(new AvaliacaoAcessorio { ProdutoId = produto.Id, ClienteNome = "c", Nota = nota });
        var handler = new ListarAvaliacoesAcessorioQueryHandler(_repo);

        var r = (dynamic)(await handler.HandleAsync(new(produto.Id))).Valor!;
        var vazio = (dynamic)(await handler.HandleAsync(new(Guid.NewGuid()))).Valor!;

        Assert.Equal((4.3, 3), ((double)r.media, (int)r.total));
        Assert.Equal((0d, 0), ((double)vazio.media, (int)vazio.total));
    }

    // ── Frete ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Frete_das_duas_rotas()
    {
        var simples = (dynamic)(await new CalcularFreteQueryHandler().HandleAsync(new("ms"))).Valor!;
        var completo = (dynamic)(await new ConsultarFreteQueryHandler().HandleAsync(new("am", 500m))).Valor!;

        Assert.Equal(15m, (decimal)simples.valorFrete);
        Assert.Equal((35m, false), ((decimal)completo.valorFrete, (bool)completo.gratis));
    }
}
