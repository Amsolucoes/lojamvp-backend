using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace LojaApi.Tests.Integracao;

// Central de Ajuda: categorias e vídeos. A leitura é pública; a gestão é só do superadmin.
// Os dados são da plataforma (não de uma loja), então cada teste usa um prefixo próprio e só olha o que criou.
[Collection(ColecaoApi.Nome)]
public class AjudaApiTests(ApiFixture api)
{
    private static string Prefixo() => "T" + Guid.NewGuid().ToString("N")[..8];

    private async Task<(LojaTeste Loja, ClienteApi Super)> SuperAsync()
    {
        var loja = await api.NovaLojaAsync(moduloReservasAtivo: false);
        return (loja, await api.SuperadminAsync(loja));
    }

    private static object Categoria(string nome, int ordem = 0, bool ativa = true, string? modulos = null) => new { nome, ordem, ativa, modulosRelacionados = modulos };
    private static object Video(string titulo, string categoria, string youtubeId = "abc123", int ordem = 0, bool ativo = true) => new { titulo, categoria, youtubeId, ordem, ativo };

    private static IEnumerable<JsonElement> Meus(Resposta r, string prefixo, string campo) =>
        r.Json.EnumerateArray().Where(x => x.GetProperty(campo).GetString()!.StartsWith(prefixo));

    // ── Permissões ───────────────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Gestao_so_para_superadmin_e_leitura_publica_para_todos()
    {
        var (loja, super) = await SuperAsync();
        var admin = await api.ComPapelAsync(loja, "admin");
        var p = Prefixo();
        var cat = (await super.Post("/api/categorias-video-ajuda", Categoria(p + "Cat"))).Json.GetProperty("id").GetGuid();
        var vid = (await super.Post("/api/videos-ajuda", Video(p + "V", p + "Cat"))).Json.GetProperty("id").GetGuid();

        Assert.Equal(200, (await loja.Site.Get("/api/categorias-video-ajuda")).Codigo);   // sem login
        Assert.Equal(200, (await loja.Site.Get("/api/videos-ajuda")).Codigo);

        foreach (var cliente in new[] { loja.Site, admin })
        {
            var esperado = ReferenceEquals(cliente, loja.Site) ? 401 : 403;
            Assert.Equal(esperado, (await cliente.Get("/api/categorias-video-ajuda/todas")).Codigo);
            Assert.Equal(esperado, (await cliente.Post("/api/categorias-video-ajuda", Categoria("x"))).Codigo);
            Assert.Equal(esperado, (await cliente.Put($"/api/categorias-video-ajuda/{cat}", Categoria("x"))).Codigo);
            Assert.Equal(esperado, (await cliente.Delete($"/api/categorias-video-ajuda/{cat}")).Codigo);
            Assert.Equal(esperado, (await cliente.Get("/api/videos-ajuda/todos")).Codigo);
            Assert.Equal(esperado, (await cliente.Post("/api/videos-ajuda", Video("x", "x"))).Codigo);
            Assert.Equal(esperado, (await cliente.Put($"/api/videos-ajuda/{vid}", Video("x", "x"))).Codigo);
            Assert.Equal(esperado, (await cliente.Patch($"/api/videos-ajuda/{vid}/ativo")).Codigo);
            Assert.Equal(esperado, (await cliente.Delete($"/api/videos-ajuda/{vid}")).Codigo);
        }
    }

    // ── Categorias ───────────────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Categoria_valida_nome_e_nao_repete()
    {
        var (_, super) = await SuperAsync();
        var p = Prefixo();

        var vazio = await super.Post("/api/categorias-video-ajuda", Categoria("  "));
        Assert.Equal((400, "Informe o nome da categoria."), (vazio.Codigo, vazio.Erro));

        var criada = await super.Post("/api/categorias-video-ajuda", Categoria("  " + p + "Vendas  ", 3, true, "vendas,caixa"));
        Assert.Equal(200, criada.Codigo);
        Assert.Equal((p + "Vendas", 3, true, "vendas,caixa"), (criada.Json.GetProperty("nome").GetString(), criada.Json.GetProperty("ordem").GetInt32(),
            criada.Json.GetProperty("ativa").GetBoolean(), criada.Json.GetProperty("modulosRelacionados").GetString()));

        var repetida = await super.Post("/api/categorias-video-ajuda", Categoria(p + "Vendas"));
        Assert.Equal((400, "Já existe uma categoria com esse nome."), (repetida.Codigo, repetida.Erro));
    }

    [FatoIntegracao]
    public async Task Nome_de_categoria_repetido_e_recusado_mesmo_com_espacos_nas_pontas()
    {
        var (_, super) = await SuperAsync();
        var p = Prefixo();
        (await super.Post("/api/categorias-video-ajuda", Categoria(p + "Vendas"))).DeveSerSucesso();

        var r = await super.Post("/api/categorias-video-ajuda", Categoria("   " + p + "Vendas  "));

        Assert.Equal((400, "Já existe uma categoria com esse nome."), (r.Codigo, r.Erro));
    }

    [FatoIntegracao]
    public async Task Listagem_publica_so_tem_as_ativas_em_ordem_e_a_de_gestao_tem_todas()
    {
        var (loja, super) = await SuperAsync();
        var p = Prefixo();
        await super.Post("/api/categorias-video-ajuda", Categoria(p + "B", 2));
        await super.Post("/api/categorias-video-ajuda", Categoria(p + "A", 2));
        await super.Post("/api/categorias-video-ajuda", Categoria(p + "Primeira", 1));
        await super.Post("/api/categorias-video-ajuda", Categoria(p + "Inativa", 0, false));

        var publica = Meus(await loja.Site.Get("/api/categorias-video-ajuda"), p, "nome").Select(x => x.GetProperty("nome").GetString()).ToList();
        var todas = Meus(await super.Get("/api/categorias-video-ajuda/todas"), p, "nome").Select(x => x.GetProperty("nome").GetString()).ToList();

        Assert.Equal([p + "Primeira", p + "A", p + "B"], publica);                    // por ordem e depois por nome; sem a inativa
        Assert.Equal([p + "Inativa", p + "Primeira", p + "A", p + "B"], todas);
    }

    [FatoIntegracao]
    public async Task Renomear_categoria_leva_os_videos_junto_e_404_para_inexistente()
    {
        var (_, super) = await SuperAsync();
        var p = Prefixo();
        var id = (await super.Post("/api/categorias-video-ajuda", Categoria(p + "Antigo"))).Json.GetProperty("id").GetGuid();
        var video = (await super.Post("/api/videos-ajuda", Video(p + "V", p + "Antigo"))).Json.GetProperty("id").GetGuid();

        var r = await super.Put($"/api/categorias-video-ajuda/{id}", Categoria("  " + p + "Novo ", 5, true, "x"));

        Assert.Equal((p + "Novo", 5), (r.Json.GetProperty("nome").GetString(), r.Json.GetProperty("ordem").GetInt32()));
        Assert.Equal(p + "Novo", (await api.NoBancoAsync(db => db.VideosAjuda.AsNoTracking().FirstAsync(v => v.Id == video))).Categoria);
        Assert.Equal(404, (await super.Put($"/api/categorias-video-ajuda/{Guid.NewGuid()}", Categoria("x"))).Codigo);
    }

    [FatoIntegracao]
    public async Task Excluir_categoria_em_uso_so_desativa_e_sem_uso_remove()
    {
        var (_, super) = await SuperAsync();
        var p = Prefixo();
        var emUso = (await super.Post("/api/categorias-video-ajuda", Categoria(p + "EmUso"))).Json.GetProperty("id").GetGuid();
        var livre = (await super.Post("/api/categorias-video-ajuda", Categoria(p + "Livre"))).Json.GetProperty("id").GetGuid();
        await super.Post("/api/videos-ajuda", Video(p + "V", p + "EmUso"));

        var r1 = await super.Delete($"/api/categorias-video-ajuda/{emUso}");
        Assert.Equal("Categoria em uso por vídeo(s) — foi desativada em vez de excluída.", r1.Json.GetProperty("mensagem").GetString());
        Assert.False((await api.NoBancoAsync(db => db.CategoriasVideoAjuda.AsNoTracking().FirstAsync(c => c.Id == emUso))).Ativa);

        var r2 = await super.Delete($"/api/categorias-video-ajuda/{livre}");
        Assert.Equal("Categoria excluída.", r2.Json.GetProperty("mensagem").GetString());
        Assert.Equal(0, await api.NoBancoAsync(db => db.CategoriasVideoAjuda.CountAsync(c => c.Id == livre)));
        Assert.Equal(404, (await super.Delete($"/api/categorias-video-ajuda/{livre}")).Codigo);
    }

    // ── Vídeos ───────────────────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Video_exige_titulo_categoria_e_id_do_youtube()
    {
        var (_, super) = await SuperAsync();
        foreach (var invalido in new[] { Video(" ", "c"), Video("t", " "), Video("t", "c", " ") })
        {
            var r = await super.Post("/api/videos-ajuda", invalido);
            Assert.Equal((400, "Preencha título, categoria e ID do YouTube."), (r.Codigo, r.Erro));
        }
    }

    [FatoIntegracao]
    public async Task Link_do_youtube_vira_o_id_do_video()
    {
        var (_, super) = await SuperAsync();
        var p = Prefixo();
        var casos = new (string Entrada, string Id)[]
        {
            ("dQw4w9WgXcQ", "dQw4w9WgXcQ"),                                              // já é o ID
            ("https://www.youtube.com/watch?v=dQw4w9WgXcQ", "dQw4w9WgXcQ"),
            ("https://www.youtube.com/watch?feature=share&v=ABC123xyz", "ABC123xyz"),
            ("https://youtu.be/xyz789", "xyz789"),
            ("https://youtu.be/xyz789?t=10", "xyz789"),
            ("https://www.youtube.com/shorts/SHORT1", "SHORT1"),
            ("https://youtube.com/x?a=1", "https://youtube.com/x?a=1"),                                     // sem "v=": devolve como veio
            ("youtube.com/watch?v=semhttp", "youtube.com/watch?v=semhttp"),                              // URL inválida: devolve como veio
        };
        foreach (var (entrada, id) in casos)
        {
            var r = await super.Post("/api/videos-ajuda", Video(p + "V", p + "Cat", "  " + entrada + " "));
            Assert.Equal(id, r.Json.GetProperty("youtubeId").GetString());
        }
    }

    [FatoIntegracao]
    public async Task Listagem_publica_de_videos_so_ativos_por_categoria_e_ordem()
    {
        var (loja, super) = await SuperAsync();
        var p = Prefixo();
        await super.Post("/api/videos-ajuda", Video(p + "B2", p + "B", "id", 2));
        await super.Post("/api/videos-ajuda", Video(p + "B1", p + "B", "id", 1));
        await super.Post("/api/videos-ajuda", Video(p + "A1", p + "A", "id", 9));
        await super.Post("/api/videos-ajuda", Video(p + "Off", p + "A", "id", 0, false));

        var publica = Meus(await loja.Site.Get("/api/videos-ajuda"), p, "titulo").Select(x => x.GetProperty("titulo").GetString()).ToList();
        var todos = Meus(await super.Get("/api/videos-ajuda/todos"), p, "titulo").Select(x => x.GetProperty("titulo").GetString()).ToList();

        Assert.Equal([p + "A1", p + "B1", p + "B2"], publica);
        Assert.Equal([p + "Off", p + "A1", p + "B1", p + "B2"], todos);
    }

    [FatoIntegracao]
    public async Task Atualizar_alternar_e_excluir_video()
    {
        var (_, super) = await SuperAsync();
        var p = Prefixo();
        var id = (await super.Post("/api/videos-ajuda", Video(p + "V", p + "C"))).Json.GetProperty("id").GetGuid();

        var r = await super.Put($"/api/videos-ajuda/{id}", Video("  " + p + "Novo ", " " + p + "Outra ", "https://youtu.be/zzz", 4, false));
        Assert.Equal((p + "Novo", p + "Outra", "zzz", 4, false), (r.Json.GetProperty("titulo").GetString(), r.Json.GetProperty("categoria").GetString(),
            r.Json.GetProperty("youtubeId").GetString(), r.Json.GetProperty("ordem").GetInt32(), r.Json.GetProperty("ativo").GetBoolean()));

        Assert.True((await super.Patch($"/api/videos-ajuda/{id}/ativo")).Json.GetProperty("ativo").GetBoolean());
        Assert.False((await super.Patch($"/api/videos-ajuda/{id}/ativo")).Json.GetProperty("ativo").GetBoolean());

        Assert.Equal("Vídeo excluído.", (await super.Delete($"/api/videos-ajuda/{id}")).Json.GetProperty("mensagem").GetString());
        foreach (var chamada in new[] { await super.Put($"/api/videos-ajuda/{id}", Video("t", "c")), await super.Patch($"/api/videos-ajuda/{id}/ativo"), await super.Delete($"/api/videos-ajuda/{id}") })
            Assert.Equal(404, chamada.Codigo);
    }
}
