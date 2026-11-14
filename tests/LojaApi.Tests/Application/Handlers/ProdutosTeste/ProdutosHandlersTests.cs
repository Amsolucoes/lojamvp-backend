using LojaApi.Application.Produtos;
using LojaApi.Models;
using LojaApi.Shared.Application;
using LojaApi.src.Models;
using LojaApi.src.Models.Etiquetas;
using LojaApi.Tests.Suporte.Falsos;

namespace LojaApi.Tests.Application.Handlers.ProdutosTeste;

// Handlers de Produtos (cadastro, estoque, categorias, marcas, fornecedores e etiquetas), testados com repositório de mentira.
public class ProdutosHandlersTests
{
    private readonly RepositorioProdutosFalso _repo = new();
    private readonly Guid _loja = Guid.NewGuid();
    private readonly Guid _outraLoja = Guid.NewGuid();

    private Produto NovoProduto(string nome = "Camisa", decimal estoque = 5m, Guid? loja = null, string? codigo = null, bool ativo = true, Guid? marca = null, Guid? fornecedor = null, string categoria = "outro")
    {
        var p = new Produto { LojaId = loja ?? _loja, Nome = nome, Estoque = estoque, CodigoBarras = codigo, Ativo = ativo, MarcaId = marca, FornecedorId = fornecedor, Categoria = categoria };
        _repo.Produtos.Add(p);
        return p;
    }

    private static SalvarProdutoRequest Req(string nome = "Camisa", decimal estoque = 0m, string? codigo = null, Guid? marca = null, Guid? fornecedor = null) =>
        new(nome, null, "outro", 20m, 50m, estoque, 3m, codigo, true, "unidade", "un", marca, fornecedor);

    private static SalvarVariacaoRequest ReqVar(string? tamanho = "M", string? cor = "Azul", string? codigo = null, int estoque = 0) => new(tamanho, cor, null, codigo, estoque, 1);

    // ── Criar / atualizar produto ────────────────────────────────────────────

    [Fact]
    public async Task Criar_recusa_codigo_repetido_na_loja_e_marca_ou_fornecedor_de_outra_loja()
    {
        NovoProduto(codigo: "111");
        var marcaDeOutra = new Marca { LojaId = _outraLoja }; _repo.Marcas.Add(marcaDeOutra);
        var fornecedorDeOutra = new Fornecedor { LojaId = _outraLoja }; _repo.Fornecedores.Add(fornecedorDeOutra);
        var handler = new CriarProdutoCommandHandler(_repo);

        Assert.Equal(TipoResultado.Conflito, (await handler.HandleAsync(new(_loja, Req(codigo: "111")))).Tipo);
        Assert.Equal("Marca não encontrada.", (await handler.HandleAsync(new(_loja, Req(marca: marcaDeOutra.Id)))).Mensagem);
        Assert.Equal("Fornecedor não encontrado.", (await handler.HandleAsync(new(_loja, Req(fornecedor: fornecedorDeOutra.Id)))).Mensagem);
        Assert.Single(_repo.Produtos);
        Assert.Equal(0, _repo.Salvamentos);
    }

    [Fact]
    public async Task Criar_ok_grava_o_produto_na_loja_e_o_estoque_inicial_como_movimento()
    {
        var marca = new Marca { LojaId = _loja, Nome = "Bosch" }; _repo.Marcas.Add(marca);

        var r = await new CriarProdutoCommandHandler(_repo).HandleAsync(new(_loja, Req("Pastilha", 7m, "999", marca.Id)));

        Assert.Equal(TipoResultado.Ok, r.Tipo);
        var produto = Assert.Single(_repo.Produtos);
        Assert.Equal((_loja, 7m, marca.Id), (produto.LojaId, produto.Estoque, produto.MarcaId));
        var mov = Assert.Single(_repo.Movimentos);
        Assert.Equal(("entrada", 7m, "Estoque inicial", _loja), (mov.Tipo, mov.Quantidade, mov.Observacao, mov.LojaId));
        Assert.Equal(1, _repo.Salvamentos);
        Assert.Equal(produto.Id, ((ProdutoDto)r.Valor!).Id);

        await new CriarProdutoCommandHandler(_repo).HandleAsync(new(_loja, Req("Sem estoque", 0m)));
        Assert.Single(_repo.Movimentos);                                              // estoque zero não gera movimento
    }

    [Fact]
    public async Task Criar_sem_loja_nao_valida_marca_e_cria_o_produto_sem_loja()
    {
        var marcaQualquer = new Marca { LojaId = _outraLoja }; _repo.Marcas.Add(marcaQualquer);

        var r = await new CriarProdutoCommandHandler(_repo).HandleAsync(new(null, Req(marca: marcaQualquer.Id)));

        Assert.Equal(TipoResultado.Ok, r.Tipo);
        Assert.Null(Assert.Single(_repo.Produtos).LojaId);
    }

    [Fact]
    public async Task Atualizar_nao_mexe_no_estoque_aceita_o_proprio_codigo_e_recusa_o_de_outro_produto()
    {
        var a = NovoProduto("A", 5m, codigo: "111");
        NovoProduto("B", 1m, codigo: "222");
        var deOutra = NovoProduto("Dela", loja: _outraLoja);
        var handler = new AtualizarProdutoCommandHandler(_repo);

        Assert.Equal(TipoResultado.NaoEncontrado, (await handler.HandleAsync(new(_loja, deOutra.Id, Req()))).Tipo);
        Assert.Equal(TipoResultado.Conflito, (await handler.HandleAsync(new(_loja, a.Id, Req("A", codigo: "222")))).Tipo);
        var ok = await handler.HandleAsync(new(_loja, a.Id, Req("A novo", 999m, "111")));

        Assert.Equal(TipoResultado.Ok, ok.Tipo);
        Assert.Equal(("A novo", 5m), (a.Nome, a.Estoque));
        Assert.Equal(1, _repo.Salvamentos);
    }

    [Fact]
    public async Task Buscar_e_listar_mostram_a_soma_das_variacoes_ativas_e_isolam_por_loja()
    {
        var p = NovoProduto("Camisa", 9m);
        _repo.Variacoes.Add(new ProdutoVariacao { ProdutoId = p.Id, Estoque = 2 });
        _repo.Variacoes.Add(new ProdutoVariacao { ProdutoId = p.Id, Estoque = 3 });
        _repo.Variacoes.Add(new ProdutoVariacao { ProdutoId = p.Id, Estoque = 10, Ativo = false });
        var deOutra = NovoProduto("Dela", loja: _outraLoja);

        var dto = (ProdutoDto)(await new BuscarProdutoQueryHandler(_repo).HandleAsync(new(_loja, p.Id))).Valor!;
        var lista = (List<ProdutoDto>)(await new ListarProdutosQueryHandler(_repo).HandleAsync(new(_loja, null, null, null, null))).Valor!;

        Assert.Equal((5m, 2), (dto.Estoque, dto.Variacoes!.Count));
        Assert.Equal("Camisa", Assert.Single(lista).Nome);
        Assert.Equal(TipoResultado.NaoEncontrado, (await new BuscarProdutoQueryHandler(_repo).HandleAsync(new(_loja, deOutra.Id))).Tipo);
    }

    // ── Lote e exclusão ──────────────────────────────────────────────────────

    [Fact]
    public async Task Lote_so_atualiza_produtos_da_loja_e_lista_vazia_devolve_zero_sem_gravar()
    {
        var meu = NovoProduto("Meu"); meu.PrecoCusto = 1m; meu.PrecoVenda = 2m;
        var alheio = NovoProduto("Dele", loja: _outraLoja); alheio.PrecoVenda = 9m;

        var vazio = await new AtualizarPrecosEmLoteCommandHandler(_repo).HandleAsync(new(_loja, new([])));
        Assert.Equal(0, ((dynamic)vazio.Valor!).atualizados);
        Assert.Equal(0, _repo.Salvamentos);

        var r = await new AtualizarPrecosEmLoteCommandHandler(_repo).HandleAsync(new(_loja, new([new(meu.Id, 10m, 20m), new(alheio.Id, 99m, 99m)])));
        Assert.Equal(1, ((dynamic)r.Valor!).atualizados);
        Assert.Equal((10m, 20m, 9m), (meu.PrecoCusto, meu.PrecoVenda, alheio.PrecoVenda));

        var status = await new AtualizarStatusEmLoteCommandHandler(_repo).HandleAsync(new(_loja, new([meu.Id, alheio.Id], false)));
        Assert.Equal(1, ((dynamic)status.Valor!).atualizados);
        Assert.False(meu.Ativo);
        Assert.True(alheio.Ativo);
    }

    [Fact]
    public async Task Excluir_recusa_produto_ja_vendido_e_remove_o_resto_com_as_dependencias()
    {
        var vendido = NovoProduto("Vendido");
        _repo.ProdutosVendidos.Add(vendido.Id);
        var livre = NovoProduto("Livre");
        _repo.Variacoes.Add(new ProdutoVariacao { ProdutoId = livre.Id });
        _repo.Movimentos.Add(new MovimentoEstoque { ProdutoId = livre.Id });
        var handler = new ExcluirProdutoCommandHandler(_repo);

        Assert.StartsWith("Não é possível excluir: este produto já tem vendas registradas.", (await handler.HandleAsync(new(_loja, vendido.Id))).Mensagem);
        Assert.Equal(TipoResultado.NaoEncontrado, (await handler.HandleAsync(new(_loja, Guid.NewGuid()))).Tipo);
        Assert.Equal(TipoResultado.Ok, (await handler.HandleAsync(new(_loja, livre.Id))).Tipo);

        Assert.DoesNotContain(livre, _repo.Produtos);
        Assert.Empty(_repo.Variacoes);
        Assert.Empty(_repo.Movimentos);
        Assert.Contains(vendido, _repo.Produtos);
    }

    // ── Variações ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Variacao_valida_produto_da_loja_e_codigo_repetido_e_grava_codigo_em_branco_como_nulo()
    {
        var p = NovoProduto();
        var deOutra = NovoProduto("Dela", loja: _outraLoja);
        _repo.Variacoes.Add(new ProdutoVariacao { ProdutoId = p.Id, CodigoBarras = "555" });
        var handler = new AdicionarVariacaoCommandHandler(_repo);

        Assert.Equal(TipoResultado.NaoEncontrado, (await handler.HandleAsync(new(_loja, deOutra.Id, ReqVar()))).Tipo);
        Assert.Equal(TipoResultado.Conflito, (await handler.HandleAsync(new(_loja, p.Id, ReqVar(codigo: "555")))).Tipo);
        var ok = await handler.HandleAsync(new(_loja, p.Id, ReqVar("G", "Preto", "   ", 4)));

        Assert.Equal(TipoResultado.Ok, ok.Tipo);
        var nova = _repo.Variacoes.Last();
        Assert.Equal((null, 4), (nova.CodigoBarras, nova.Estoque));
    }

    [Fact]
    public async Task Atualizar_variacao_registra_ajuste_so_quando_o_estoque_muda_e_ignora_variacao_de_outra_loja()
    {
        var p = NovoProduto();
        var deOutra = NovoProduto("Dela", loja: _outraLoja);
        var v = new ProdutoVariacao { ProdutoId = p.Id, Tamanho = "P", Cor = "Azul", Estoque = 2 }; _repo.Variacoes.Add(v);
        var alheia = new ProdutoVariacao { ProdutoId = deOutra.Id, Estoque = 5 }; _repo.Variacoes.Add(alheia);
        var handler = new AtualizarVariacaoCommandHandler(_repo);

        Assert.Equal(TipoResultado.NaoEncontrado, (await handler.HandleAsync(new(_loja, deOutra.Id, alheia.Id, ReqVar(estoque: 99)))).Tipo);
        Assert.Equal(5, alheia.Estoque);

        await handler.HandleAsync(new(_loja, p.Id, v.Id, ReqVar("P", "Azul", null, 2)));
        Assert.Empty(_repo.Movimentos);

        await handler.HandleAsync(new(_loja, p.Id, v.Id, ReqVar("G", "Preto", null, 9)));
        var mov = Assert.Single(_repo.Movimentos);
        Assert.Equal(("ajuste", 9m, "Editado via cadastro do produto (G / Preto)"), (mov.Tipo, mov.Quantidade, mov.Observacao));
    }

    [Fact]
    public async Task Remover_variacao_so_desativa_e_nao_toca_na_de_outra_loja()
    {
        var p = NovoProduto();
        var deOutra = NovoProduto("Dela", loja: _outraLoja);
        var v = new ProdutoVariacao { ProdutoId = p.Id }; _repo.Variacoes.Add(v);
        var alheia = new ProdutoVariacao { ProdutoId = deOutra.Id }; _repo.Variacoes.Add(alheia);
        var handler = new RemoverVariacaoCommandHandler(_repo);

        Assert.Equal(TipoResultado.NaoEncontrado, (await handler.HandleAsync(new(_loja, deOutra.Id, alheia.Id))).Tipo);
        Assert.True(alheia.Ativo);
        Assert.Equal(TipoResultado.Ok, (await handler.HandleAsync(new(_loja, p.Id, v.Id))).Tipo);
        Assert.False(v.Ativo);
    }

    // ── Estoque ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Ajuste_de_estoque_valida_na_ordem_certa_e_so_grava_quando_aceita()
    {
        var p = NovoProduto(estoque: 2m);
        var deOutra = NovoProduto("Dela", loja: _outraLoja);
        var handler = new AjusteEstoqueCommandHandler(_repo);

        Assert.Equal("Produto não encontrado.", (await handler.HandleAsync(new(_loja, new(deOutra.Id, 1m, "entrada", null)))).Mensagem);
        Assert.Equal("Tipo de movimento inválido.", (await handler.HandleAsync(new(_loja, new(p.Id, 1m, "x", null)))).Mensagem);
        Assert.StartsWith("A quantidade da saída (3) é maior que o estoque atual (2", (await handler.HandleAsync(new(_loja, new(p.Id, 3m, "saida", null)))).Mensagem);
        Assert.Equal(0, _repo.Salvamentos);

        Assert.Equal(7m, ((dynamic)(await handler.HandleAsync(new(_loja, new(p.Id, 5m, "entrada", "compra")))).Valor!).estoqueAgora);
        Assert.Equal(1m, ((dynamic)(await handler.HandleAsync(new(_loja, new(p.Id, 1m, "ajuste", null)))).Valor!).estoqueAgora);
        Assert.Equal(["entrada", "ajuste"], _repo.Movimentos.Select(m => m.Tipo));
        Assert.Equal(2, _repo.Salvamentos);
    }

    [Fact]
    public async Task Ajuste_de_variacao_so_vale_para_variacao_da_loja_e_monta_a_observacao_com_a_grade()
    {
        var p = NovoProduto();
        var deOutra = NovoProduto("Dela", loja: _outraLoja);
        var v = new ProdutoVariacao { ProdutoId = p.Id, Tamanho = "P", Cor = "Azul", Estoque = 10 }; _repo.Variacoes.Add(v);
        var alheia = new ProdutoVariacao { ProdutoId = deOutra.Id, Estoque = 5 }; _repo.Variacoes.Add(alheia);
        var handler = new AjusteVariacaoCommandHandler(_repo);

        Assert.Equal("Variação não encontrada.", (await handler.HandleAsync(new(_loja, new(deOutra.Id, alheia.Id, 3, "saida", null)))).Mensagem);
        Assert.Equal(5, alheia.Estoque);
        Assert.Equal("A quantidade da saída (50) é maior que o estoque atual desta variação (10).", (await handler.HandleAsync(new(_loja, new(p.Id, v.Id, 50, "saida", null)))).Mensagem);

        var r = await handler.HandleAsync(new(_loja, new(p.Id, v.Id, 3, "saida", "  quebra ")));
        Assert.Equal(7, ((dynamic)r.Valor!).estoqueAgora);
        Assert.Equal("quebra - P / Azul", Assert.Single(_repo.Movimentos).Observacao);
    }

    // ── Categorias e marcas ──────────────────────────────────────────────────

    [Fact]
    public async Task Categoria_criar_atualizar_e_excluir_seguem_as_regras_de_nome_e_uso()
    {
        var criar = new CriarCategoriaCommandHandler(_repo);
        var a = (CategoriaProdutoDto)(await criar.HandleAsync(new(_loja, new("  Roupas ", "letra", true, true, null)))).Valor!;
        await criar.HandleAsync(new(_loja, new("Calçados", "numero", true, false, null)));
        Assert.Equal("Loja não encontrada.", (await criar.HandleAsync(new(null, new("x", "letra", true, true, null)))).Mensagem);
        Assert.Equal("Nome da categoria é obrigatório.", (await criar.HandleAsync(new(_loja, new(" ", "letra", true, true, null)))).Mensagem);
        Assert.Equal(TipoResultado.Conflito, (await criar.HandleAsync(new(_loja, new("ROUPAS", "letra", true, true, null)))).Tipo);
        Assert.Equal([0, 1], _repo.Categorias.Select(c => c.Ordem));

        var camisa = NovoProduto("Camisa", categoria: "Roupas");
        var deOutra = NovoProduto("Dela", loja: _outraLoja, categoria: "Roupas");
        var atualizar = new AtualizarCategoriaCommandHandler(_repo);
        Assert.Equal(TipoResultado.Conflito, (await atualizar.HandleAsync(new(_loja, a.Id, new("calçados", "letra", true, true, null)))).Tipo);
        var calcados = _repo.Categorias[1];
        await atualizar.HandleAsync(new(_loja, calcados.Id, new("CALÇADOS", "numero", true, false, null)));   // só a caixa: não renomeia os produtos
        Assert.Equal((0, "CALÇADOS"), (_repo.CategoriasRenomeadas, calcados.Nome));
        await atualizar.HandleAsync(new(_loja, a.Id, new("Vestuário", "letra", true, true, null)));
        Assert.Equal(("Vestuário", "Roupas"), (camisa.Categoria, deOutra.Categoria));

        var excluir = new ExcluirCategoriaCommandHandler(_repo);
        Assert.Equal("Não é possível excluir: 1 produto(s) usam esta categoria.", (await excluir.HandleAsync(new(_loja, a.Id))).Mensagem);
        camisa.Ativo = false;
        Assert.Equal(TipoResultado.Ok, (await excluir.HandleAsync(new(_loja, a.Id))).Tipo);
        Assert.False(_repo.Categorias[0].Ativo);
    }

    [Fact]
    public async Task Marca_criar_atualizar_e_excluir_seguem_as_regras_de_nome_e_uso()
    {
        var criar = new CriarMarcaCommandHandler(_repo);
        var bosch = (MarcaDto)(await criar.HandleAsync(new(_loja, new("  Bosch ")))).Valor!;
        Assert.Equal("Bosch", bosch.Nome);
        Assert.Equal("Nome da marca é obrigatório.", (await criar.HandleAsync(new(_loja, new(" ")))).Mensagem);
        Assert.Equal(TipoResultado.Conflito, (await criar.HandleAsync(new(_loja, new("BOSCH")))).Tipo);
        await criar.HandleAsync(new(_loja, new("Ipiranga")));

        var atualizar = new AtualizarMarcaCommandHandler(_repo);
        Assert.Equal(TipoResultado.Conflito, (await atualizar.HandleAsync(new(_loja, bosch.Id, new("ipiranga")))).Tipo);
        Assert.Equal("BOSCH", ((MarcaDto)(await atualizar.HandleAsync(new(_loja, bosch.Id, new("BOSCH")))).Valor!).Nome);
        Assert.Equal(TipoResultado.NaoEncontrado, (await atualizar.HandleAsync(new(_outraLoja, bosch.Id, new("x")))).Tipo);

        NovoProduto("Pneu", marca: bosch.Id);
        var excluir = new ExcluirMarcaCommandHandler(_repo);
        Assert.Equal("Não é possível excluir: 1 produto(s) usam esta marca.", (await excluir.HandleAsync(new(_loja, bosch.Id))).Mensagem);
    }

    // ── Fornecedores ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Fornecedor_criar_monta_o_endereco_e_excluir_em_uso_so_desativa()
    {
        var criar = new CriarFornecedorCommandHandler(_repo);
        Assert.Equal("Loja não encontrada.", (await criar.HandleAsync(new(null, new("x", null, null, null, null, null)))).Mensagem);
        Assert.Equal("Nome é obrigatório.", (await criar.HandleAsync(new(_loja, new(" ", null, null, null, null, null)))).Mensagem);

        var dto = (FornecedorDto)(await criar.HandleAsync(new(_loja, new(" Alfa ", null, null, null, null, null, true, "79000-000", "Rua A", "10", "Centro", "Campo Grande", "ms")))).Valor!;
        Assert.Equal(("Alfa", "Rua A, 10 - Centro - Campo Grande/MS - 79000-000", 0), (dto.Nome, dto.Endereco, dto.QtdProdutos));

        NovoProduto("P1", fornecedor: dto.Id);
        var excluir = new ExcluirFornecedorCommandHandler(_repo);
        Assert.Equal(TipoResultado.NaoEncontrado, (await excluir.HandleAsync(new(_outraLoja, dto.Id))).Tipo);
        Assert.Equal("Fornecedor em uso — foi desativado em vez de excluído.", ((dynamic)(await excluir.HandleAsync(new(_loja, dto.Id))).Valor!).mensagem);
        Assert.False(_repo.Fornecedores[0].Ativo);

        _repo.Produtos.Clear();
        Assert.Equal("Fornecedor excluído.", ((dynamic)(await excluir.HandleAsync(new(_loja, dto.Id))).Valor!).mensagem);
        Assert.Empty(_repo.Fornecedores);
    }

    // ── Etiquetas ────────────────────────────────────────────────────────────

    private static SalvarModeloRequest Modelo(string nome = "Gôndola", decimal largura = 50m, decimal altura = 25m, int? escala = null) =>
        new(nome, true, false, null, true, true, false, true, largura, altura, null, null, null, escala);

    [Fact]
    public async Task Modelos_de_etiqueta_listar_cria_o_padrao_e_criar_valida_nome_e_tamanho()
    {
        var listar = new ListarModelosEtiquetaCommandHandler(_repo);
        Assert.Equal("Loja não encontrada.", (await listar.HandleAsync(new(null))).Mensagem);
        var primeira = (List<ConfiguracaoEtiqueta>)(await listar.HandleAsync(new(_loja))).Valor!;
        await listar.HandleAsync(new(_loja));
        Assert.Equal(("Padrão", true), (Assert.Single(primeira).Nome, primeira[0].Padrao));
        Assert.Single(_repo.Modelos);                                                                    // a segunda leitura não cria outro

        var criar = new CriarModeloEtiquetaCommandHandler(_repo);
        Assert.Equal("Digite um nome para o modelo.", (await criar.HandleAsync(new(_loja, Modelo(" ")))).Mensagem);
        Assert.Equal("Largura e altura devem ser maiores que zero.", (await criar.HandleAsync(new(_loja, Modelo(largura: 0m)))).Mensagem);
        var ok = (ConfiguracaoEtiqueta)(await criar.HandleAsync(new(_loja, Modelo(" Gôndola ", escala: 400)))).Valor!;
        Assert.Equal(("Gôndola", 100, "#000000", "#FFFFFF"), (ok.Nome, ok.EscalaFonte, ok.CorTexto, ok.CorFundo));
    }

    [Fact]
    public async Task Modelos_de_etiqueta_marcar_padrao_e_excluir_nunca_deixam_a_loja_sem_modelo_ou_sem_padrao()
    {
        var padrao = new ConfiguracaoEtiqueta { LojaId = _loja, Nome = "Padrão", Padrao = true };
        var segundo = new ConfiguracaoEtiqueta { LojaId = _loja, Nome = "Segundo" };
        var deOutra = new ConfiguracaoEtiqueta { LojaId = _outraLoja, Nome = "Dela", Padrao = true };
        _repo.Modelos.AddRange([padrao, segundo, deOutra]);

        Assert.Equal(TipoResultado.NaoEncontrado, (await new MarcarModeloPadraoCommandHandler(_repo).HandleAsync(new(_loja, deOutra.Id))).Tipo);
        await new MarcarModeloPadraoCommandHandler(_repo).HandleAsync(new(_loja, segundo.Id));
        Assert.Equal((false, true, true), (padrao.Padrao, segundo.Padrao, deOutra.Padrao));

        var excluir = new ExcluirModeloEtiquetaCommandHandler(_repo);
        Assert.Equal(TipoResultado.Ok, (await excluir.HandleAsync(new(_loja, segundo.Id))).Tipo);          // excluiu o padrão: o outro assume
        Assert.True(padrao.Padrao);
        Assert.Equal("Você precisa ter ao menos um modelo de etiqueta.", (await excluir.HandleAsync(new(_loja, padrao.Id))).Mensagem);
        Assert.Contains(padrao, _repo.Modelos);
    }
}
