using System.Text.Json;
using LojaApi.src.Models;
using Microsoft.EntityFrameworkCore;

namespace LojaApi.Tests.Api.AcessoriosTeste;

// Loja de acessórios da plataforma: catálogo público, gestão de produtos (superadmin), categorias e avaliações.
[Collection(ColecaoApi.Nome)]
public class ProdutosAcessorioApiTests(ApiFixture api)
{
    private const string P = CenariosAcessorios.Produtos;

    private static List<JsonElement> Lista(Resposta r) => r.Json.EnumerateArray().ToList();
    private static Guid Id(JsonElement e) => e.GetProperty("id").GetGuid();

    // ── Permissões ───────────────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Gestao_exige_superadmin_e_o_catalogo_e_publico()
    {
        var (super, visitante, loja) = await api.AcessoriosAsync();
        var produto = await super.ProdutoAcessorioAsync();

        Assert.Equal(200, (await visitante.Get(P)).Codigo);
        Assert.Equal(200, (await visitante.Get($"{P}/{produto}")).Codigo);
        Assert.Equal(200, (await visitante.Get($"{P}/categorias")).Codigo);

        foreach (var (metodo, url) in new[] { ("GET", $"{P}/todos"), ("POST", P), ("PUT", $"{P}/{produto}"), ("DELETE", $"{P}/{produto}"),
                                              ("POST", $"{P}/categorias"), ("PUT", $"{P}/categorias/{Guid.NewGuid()}"), ("DELETE", $"{P}/categorias/{Guid.NewGuid()}"),
                                              ("GET", $"{P}/pedidos"), ("PATCH", $"{P}/pedidos/{Guid.NewGuid()}/status") })
        {
            Task<Resposta> Chamar(ClienteApi c) => metodo switch
            {
                "GET" => c.Get(url), "POST" => c.Post(url, new { }), "PUT" => c.Put(url, new { }), "PATCH" => c.Patch(url, new { }), _ => c.Delete(url),
            };
            Assert.Equal(401, (await Chamar(visitante)).Codigo);            // sem login
            Assert.Equal(403, (await Chamar(loja.Painel)).Codigo);          // dono de loja comum (admin) não gerencia o catálogo da plataforma
        }
    }

    // ── Gestão de produtos ───────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Criar_valida_nome_e_preco_e_devolve_o_produto()
    {
        var (super, _, _) = await api.AcessoriosAsync();

        Assert.Equal((400, "Preencha nome e preço válido."), ((await super.Post(P, CenariosAcessorios.CorpoProduto(nome: "  "))) is var a ? (a.Codigo, a.Erro) : default));
        Assert.Equal((400, "Preencha nome e preço válido."), ((await super.Post(P, CenariosAcessorios.CorpoProduto(preco: 0))) is var b ? (b.Codigo, b.Erro) : default));

        var r = await super.Post(P, CenariosAcessorios.CorpoProduto("  Impressora X  ", 250.5m, 7, 199.9m, "impressora_fiscal", "a.jpg,b.jpg", 1.25m, true, true, 3, "Boa"));

        Assert.Equal(200, r.Codigo);
        var j = r.Json;
        Assert.Equal(("Impressora X", "Boa", 250.5m, 199.9m, 7, "impressora_fiscal", "a.jpg,b.jpg", 1.25m, true, true, 3),
            (j.GetProperty("nome").GetString(), j.GetProperty("descricao").GetString(), j.GetProperty("preco").GetDecimal(), j.GetProperty("precoPromocional").GetDecimal(),
             j.GetProperty("estoque").GetInt32(), j.GetProperty("categoria").GetString(), j.GetProperty("imagensUrls").GetString(), j.GetProperty("pesoKg").GetDecimal(),
             j.GetProperty("ativo").GetBoolean(), j.GetProperty("destaque").GetBoolean(), j.GetProperty("ordem").GetInt32()));
        var noBanco = await api.ProdutoNoBancoAsync(Id(j));
        Assert.Equal("Impressora X", noBanco.Nome);
    }

    [FatoIntegracao]
    public async Task Atualizar_troca_todos_os_campos_e_o_404_para_inexistente()
    {
        var (super, _, _) = await api.AcessoriosAsync();
        var id = await super.ProdutoAcessorioAsync(preco: 50, estoque: 2);
        var antes = await api.ProdutoNoBancoAsync(id);
        await Task.Delay(20);

        var r = await super.Put($"{P}/{id}", CenariosAcessorios.CorpoProduto("  Novo nome ", 80m, 9, 70m, "outro", "x.png", 2m, false, true, 5, "Desc"));

        Assert.Equal(200, r.Codigo);
        var depois = await api.ProdutoNoBancoAsync(id);
        Assert.Equal(("Novo nome", "Desc", 80m, 70m, 9, "x.png", 2m, false, true, 5), (depois.Nome, depois.Descricao, depois.Preco, depois.PrecoPromocional, depois.Estoque, depois.ImagensUrls, depois.PesoKg, depois.Ativo, depois.Destaque, depois.Ordem));
        Assert.True(depois.AtualizadoEm > antes.AtualizadoEm);
        Assert.Equal(depois.CriadoEm, antes.CriadoEm);
        Assert.Equal("Novo nome", r.Json.GetProperty("nome").GetString());
        Assert.Equal(404, (await super.Put($"{P}/{Guid.NewGuid()}", CenariosAcessorios.CorpoProduto())).Codigo);
    }

    [FatoIntegracao]
    public async Task Excluir_remove_quem_nunca_foi_pedido_e_so_desativa_quem_ja_foi()
    {
        var (super, visitante, _) = await api.AcessoriosAsync();
        var livre = await super.ProdutoAcessorioAsync();
        var usado = await super.ProdutoAcessorioAsync();
        await visitante.PedidoPixAsync(usado);

        var excluiu = await super.Delete($"{P}/{livre}");
        Assert.Equal("Produto excluído.", excluiu.Json.GetProperty("mensagem").GetString());
        Assert.Equal(0, await api.NoBancoAsync(db => db.ProdutosAcessorio.CountAsync(p => p.Id == livre)));

        var desativou = await super.Delete($"{P}/{usado}");
        Assert.Equal("Produto já usado em pedidos — foi desativado em vez de excluído.", desativou.Json.GetProperty("mensagem").GetString());
        Assert.False((await api.ProdutoNoBancoAsync(usado)).Ativo);

        Assert.Equal(404, (await super.Delete($"{P}/{Guid.NewGuid()}")).Codigo);
    }

    [FatoIntegracao]
    public async Task Todos_lista_inclusive_inativos_por_ordem_e_nome()
    {
        var (super, _, _) = await api.AcessoriosAsync();
        var sufixo = Guid.NewGuid().ToString("N")[..6];
        var b = await super.ProdutoAcessorioAsync($"B-{sufixo}", ordem: -900);
        var a = await super.ProdutoAcessorioAsync($"A-{sufixo}", ordem: -900);
        var inativo = await super.ProdutoAcessorioAsync($"C-{sufixo}", ativo: false, ordem: -901);

        var ids = Lista(await super.Get($"{P}/todos")).Select(Id).Where(i => new[] { a, b, inativo }.Contains(i)).ToList();

        Assert.Equal([inativo, a, b], ids);   // ordem -901 primeiro; depois -900 por nome
    }

    // ── Catálogo público ─────────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Catalogo_so_mostra_ativos_filtra_por_categoria_e_calcula_os_campos_da_vitrine()
    {
        var (super, visitante, _) = await api.AcessoriosAsync();
        var categoria = $"cat_{Guid.NewGuid():N}"[..14];
        var normal = await super.ProdutoAcessorioAsync(preco: 100, estoque: 3, categoria: categoria, ordem: -800);
        var semEstoque = await super.ProdutoAcessorioAsync(preco: 50, estoque: 0, promo: 40, categoria: categoria, destaque: true, ordem: -799);
        var inativo = await super.ProdutoAcessorioAsync(categoria: categoria, ativo: false);
        var velho = await super.ProdutoAcessorioAsync(categoria: categoria, ordem: -798);
        await api.NoBancoAsync(db => db.ProdutosAcessorio.Where(p => p.Id == velho).ExecuteUpdateAsync(s => s.SetProperty(p => p.CriadoEm, DateTime.UtcNow.AddDays(-15))));

        var lista = Lista(await visitante.Get($"{P}?categoria={categoria}"));

        Assert.Equal([normal, semEstoque, velho], lista.Select(Id).ToList());   // sem o inativo, por ordem
        Assert.Equal((true, false, true, 0d, 0), (lista[0].GetProperty("disponivel").GetBoolean(), lista[0].GetProperty("destaque").GetBoolean(), lista[0].GetProperty("novo").GetBoolean(),
            lista[0].GetProperty("mediaAvaliacoes").GetDouble(), lista[0].GetProperty("totalAvaliacoes").GetInt32()));
        Assert.Equal((false, true, 40m), (lista[1].GetProperty("disponivel").GetBoolean(), lista[1].GetProperty("destaque").GetBoolean(), lista[1].GetProperty("precoPromocional").GetDecimal()));
        Assert.False(lista[2].GetProperty("novo").GetBoolean());       // criado há 15 dias: não é mais "novo" (limite 14)
        Assert.False(lista[0].TryGetProperty("estoque", out _));       // a lista não expõe o estoque exato
        Assert.DoesNotContain(inativo, lista.Select(Id));

        // sem filtro de categoria, o produto aparece no catálogo geral
        Assert.Contains(normal, Lista(await visitante.Get(P)).Select(Id));
    }

    [FatoIntegracao]
    public async Task Buscar_devolve_o_detalhe_com_estoque_e_404_para_inativo_ou_inexistente()
    {
        var (super, visitante, _) = await api.AcessoriosAsync();
        var r = await super.Post(P, CenariosAcessorios.CorpoProduto("Detalhe", 90m, 4, 80m, "outro", "i.jpg", 0.5m, true, true, 0, "D"));
        var id = Id(r.Json);

        var j = (await visitante.Get($"{P}/{id}")).Json;

        Assert.Equal(("Detalhe", 90m, 80m, 4, true, true, true, 0.5m), (j.GetProperty("nome").GetString(), j.GetProperty("preco").GetDecimal(), j.GetProperty("precoPromocional").GetDecimal(),
            j.GetProperty("estoque").GetInt32(), j.GetProperty("disponivel").GetBoolean(), j.GetProperty("destaque").GetBoolean(), j.GetProperty("novo").GetBoolean(), j.GetProperty("pesoKg").GetDecimal()));

        await super.Put($"{P}/{id}", CenariosAcessorios.CorpoProduto(ativo: false));
        Assert.Equal(404, (await visitante.Get($"{P}/{id}")).Codigo);
        Assert.Equal(404, (await visitante.Get($"{P}/{Guid.NewGuid()}")).Codigo);
    }

    [FatoIntegracao]
    public async Task Catalogo_libera_o_estoque_de_pedidos_vencidos()
    {
        var (super, visitante, _) = await api.AcessoriosAsync();
        var produto = await super.ProdutoAcessorioAsync(estoque: 5);
        var pedido = await visitante.PedidoPixAsync(produto, 2);
        Assert.Equal(3, (await api.ProdutoNoBancoAsync(produto)).Estoque);
        await api.VencerPedidoAsync(pedido);

        await visitante.Get($"{P}?categoria=nenhuma");   // qualquer visita ao catálogo libera os vencidos

        Assert.Equal(5, (await api.ProdutoNoBancoAsync(produto)).Estoque);
        Assert.Equal("cancelado", (await api.PedidoNoBancoAsync(pedido)).Status);
    }

    // ── Categorias ───────────────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Listar_categorias_traz_so_as_ativas_por_ordem_e_nome()
    {
        var (super, visitante, _) = await api.AcessoriosAsync();
        var nome = $"Zeta {Guid.NewGuid():N}"[..14];
        var criada = (await super.Post($"{P}/categorias", new { nome })).Json;

        var lista = Lista(await visitante.Get($"{P}/categorias"));

        Assert.Contains(lista, c => c.GetProperty("chave").GetString() == "outro");     // categorias de fábrica
        Assert.Contains(Id(criada), lista.Select(Id));
        var ordens = lista.Select(c => c.GetProperty("ordem").GetInt32()).ToList();
        Assert.Equal(ordens.OrderBy(x => x).ToList(), ordens);

        await super.Delete($"{P}/categorias/{Id(criada)}");   // sem produtos: some de vez
        Assert.DoesNotContain(Id(criada), Lista(await visitante.Get($"{P}/categorias")).Select(Id));
    }

    [FatoIntegracao]
    public async Task Criar_categoria_gera_a_chave_sem_acento_recusa_duplicada_e_ordena_no_fim()
    {
        var (super, _, _) = await api.AcessoriosAsync();
        var sufixo = Guid.NewGuid().ToString("N")[..6];

        Assert.Equal((400, "Digite o nome da categoria."), ((await super.Post($"{P}/categorias", new { nome = "  " })) is var a ? (a.Codigo, a.Erro) : default));
        Assert.Equal((400, "Nome inválido."), ((await super.Post($"{P}/categorias", new { nome = "!!! ???" })) is var b ? (b.Codigo, b.Erro) : default));

        var r = await super.Post($"{P}/categorias", new { nome = $"  Câmeras & Áudio {sufixo}  " });

        Assert.Equal(200, r.Codigo);
        Assert.Equal(($"Câmeras & Áudio {sufixo}", $"cameras_audio_{sufixo}", true), (r.Json.GetProperty("nome").GetString(), r.Json.GetProperty("chave").GetString(), r.Json.GetProperty("ativa").GetBoolean()));
        var maior = await api.NoBancoAsync(db => db.CategoriasAcessorio.MaxAsync(c => c.Ordem));
        Assert.Equal(maior, r.Json.GetProperty("ordem").GetInt32());     // a nova ficou com a maior ordem

        var duplicada = await super.Post($"{P}/categorias", new { nome = $"CAMERAS audio {sufixo}" });
        Assert.Equal((409, "Já existe uma categoria com esse nome."), (duplicada.Codigo, duplicada.Erro));
    }

    [FatoIntegracao]
    public async Task Atualizar_categoria_muda_so_o_nome_e_mantem_a_chave()
    {
        var (super, _, _) = await api.AcessoriosAsync();
        var sufixo = Guid.NewGuid().ToString("N")[..6];
        var criada = (await super.Post($"{P}/categorias", new { nome = $"Antiga {sufixo}" })).Json;
        var id = Id(criada);

        Assert.Equal((400, "Digite o nome da categoria."), ((await super.Put($"{P}/categorias/{id}", new { nome = "" })) is var a ? (a.Codigo, a.Erro) : default));
        var r = await super.Put($"{P}/categorias/{id}", new { nome = $"  Nova {sufixo} " });

        Assert.Equal(($"Nova {sufixo}", criada.GetProperty("chave").GetString()), (r.Json.GetProperty("nome").GetString(), r.Json.GetProperty("chave").GetString()));
        Assert.Equal(404, (await super.Put($"{P}/categorias/{Guid.NewGuid()}", new { nome = "x" })).Codigo);
    }

    [FatoIntegracao]
    public async Task Excluir_categoria_em_uso_so_desativa()
    {
        var (super, visitante, _) = await api.AcessoriosAsync();
        var criada = (await super.Post($"{P}/categorias", new { nome = $"Em uso {Guid.NewGuid():N}"[..14] })).Json;
        var chave = criada.GetProperty("chave").GetString()!;
        await super.ProdutoAcessorioAsync(categoria: chave);

        var r = await super.Delete($"{P}/categorias/{Id(criada)}");

        Assert.Equal("Categoria em uso por produto(s) — foi desativada em vez de excluída.", r.Json.GetProperty("mensagem").GetString());
        Assert.False((await api.NoBancoAsync(db => db.CategoriasAcessorio.AsNoTracking().FirstAsync(c => c.Id == Id(criada)))).Ativa);
        Assert.DoesNotContain(Id(criada), Lista(await visitante.Get($"{P}/categorias")).Select(Id));
        Assert.Equal(404, (await super.Delete($"{P}/categorias/{Guid.NewGuid()}")).Codigo);
    }

    // ── Avaliações ───────────────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Avaliar_exige_nota_valida_email_e_compra_paga()
    {
        var (super, visitante, _) = await api.AcessoriosAsync();
        var produto = await super.ProdutoAcessorioAsync();
        object Pedido(int nota, string email = "maria@teste.com") => new { produtoId = produto, email, nota, comentario = "  ótimo  " };

        Assert.Equal((400, "A nota deve ser de 1 a 5."), ((await visitante.Post($"{P}/avaliacoes", Pedido(0))) is var a ? (a.Codigo, a.Erro) : default));
        Assert.Equal((400, "A nota deve ser de 1 a 5."), ((await visitante.Post($"{P}/avaliacoes", Pedido(6))) is var b ? (b.Codigo, b.Erro) : default));
        Assert.Equal((400, "Informe o e-mail usado na compra."), ((await visitante.Post($"{P}/avaliacoes", Pedido(5, "  "))) is var c ? (c.Codigo, c.Erro) : default));
        Assert.Equal((400, "Não encontramos uma compra paga desse produto com esse e-mail."), ((await visitante.Post($"{P}/avaliacoes", Pedido(5))) is var d ? (d.Codigo, d.Erro) : default));

        // pedido ainda não pago também não vale
        await visitante.PedidoPixAsync(produto);
        Assert.Equal(400, (await visitante.Post($"{P}/avaliacoes", Pedido(5))).Codigo);
    }

    [FatoIntegracao]
    public async Task Avaliar_depois_de_pagar_grava_uma_vez_e_a_media_aparece_no_catalogo()
    {
        var (super, visitante, _) = await api.AcessoriosAsync();
        var produto = await super.ProdutoAcessorioAsync();
        var pedido = await visitante.PedidoPixAsync(produto);
        await api.NoBancoAsync(db => db.PedidosAcessorio.Where(p => p.Id == pedido).ExecuteUpdateAsync(s => s.SetProperty(p => p.Status, "pago").SetProperty(p => p.PagoEm, DateTime.UtcNow)));

        // o e-mail é comparado sem diferença de maiúsculas/minúsculas e sem espaços nas pontas
        var r = await visitante.Post($"{P}/avaliacoes", new { produtoId = produto, email = "  MARIA@teste.COM ", nota = 4, comentario = "  bom  " });
        Assert.Equal((200, "Avaliação enviada, obrigado!"), (r.Codigo, r.Json.GetProperty("mensagem").GetString()));

        var gravada = await api.NoBancoAsync(db => db.AvaliacoesAcessorio.AsNoTracking().SingleAsync(a => a.ProdutoId == produto));
        Assert.Equal(("Maria Souza", 4, "bom", pedido), (gravada.ClienteNome, gravada.Nota, gravada.Comentario, gravada.PedidoId));

        var repetida = await visitante.Post($"{P}/avaliacoes", new { produtoId = produto, email = "maria@teste.com", nota = 5, comentario = (string?)null });
        Assert.Equal((400, "Você já avaliou este produto."), (repetida.Codigo, repetida.Erro));

        var lista = (await visitante.Get($"{P}/{produto}/avaliacoes")).Json;
        Assert.Equal((4.0, 1), (lista.GetProperty("media").GetDouble(), lista.GetProperty("total").GetInt32()));
        var av = lista.GetProperty("avaliacoes").EnumerateArray().Single();
        Assert.Equal(("Maria Souza", 4, "bom"), (av.GetProperty("clienteNome").GetString(), av.GetProperty("nota").GetInt32(), av.GetProperty("comentario").GetString()));

        var vitrine = Lista(await visitante.Get(P)).Single(x => Id(x) == produto);
        Assert.Equal((4d, 1), (vitrine.GetProperty("mediaAvaliacoes").GetDouble(), vitrine.GetProperty("totalAvaliacoes").GetInt32()));
    }

    [FatoIntegracao]
    public async Task Avaliacoes_de_produto_sem_nenhuma_vem_com_media_zero_e_comentario_em_branco_vira_nulo()
    {
        var (super, visitante, _) = await api.AcessoriosAsync();
        var produto = await super.ProdutoAcessorioAsync();

        var vazio = (await visitante.Get($"{P}/{produto}/avaliacoes")).Json;
        Assert.Equal((0d, 0, 0), (vazio.GetProperty("media").GetDouble(), vazio.GetProperty("total").GetInt32(), vazio.GetProperty("avaliacoes").GetArrayLength()));

        var pedido = await visitante.PedidoPixAsync(produto);
        await api.NoBancoAsync(db => db.PedidosAcessorio.Where(p => p.Id == pedido).ExecuteUpdateAsync(s => s.SetProperty(p => p.Status, "pago").SetProperty(p => p.PagoEm, DateTime.UtcNow)));
        await visitante.Post($"{P}/avaliacoes", new { produtoId = produto, email = "maria@teste.com", nota = 3, comentario = "   " });
        Assert.Null((await api.NoBancoAsync(db => db.AvaliacoesAcessorio.AsNoTracking().SingleAsync(a => a.ProdutoId == produto))).Comentario);
    }
}
