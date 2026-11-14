using Microsoft.EntityFrameworkCore;
using static LojaApi.Tests.Suporte.CenariosProdutos;

namespace LojaApi.Tests.Api.ProdutosTeste;

// Categorias da loja (com a configuração da grade) e marcas dos produtos.
[Collection(ColecaoApi.Nome)]
public class CategoriasMarcasApiTests(ApiFixture api)
{
    private const string C = "/api/categorias";
    private const string M = "/api/marcas";

    private Task<LojaTeste> NovaAsync() => api.NovaLojaDeServicosAsync();

    private static object Cat(string nome, string tipo = "letra", bool tamanho = true, bool cor = true, string? personalizados = null) =>
        new { nome, tipoTamanho = tipo, usaTamanho = tamanho, usaCor = cor, tamanhosPersonalizados = personalizados };

    private static async Task<(int, string)> Erro(Task<Resposta> r) { var x = await r; return (x.Codigo, x.Erro); }

    [FatoIntegracao]
    public async Task Exige_login_e_so_admin_altera()
    {
        var loja = await NovaAsync();
        var operador = await api.OperadorAsync(loja);
        var cat = (await loja.Painel.Post(C, Cat("Roupas"))).Json.GetProperty("id").GetGuid();
        var marca = await loja.MarcaIdAsync();

        Assert.Equal(401, (await loja.Site.Get(C)).Codigo);
        Assert.Equal(401, (await loja.Site.Get(M)).Codigo);
        Assert.Equal(200, (await operador.Get(C)).Codigo);
        Assert.Equal(200, (await operador.Get(M)).Codigo);
        Assert.Equal(403, (await operador.Post(C, Cat("x"))).Codigo);
        Assert.Equal(403, (await operador.Put($"{C}/{cat}", Cat("x"))).Codigo);
        Assert.Equal(403, (await operador.Delete($"{C}/{cat}")).Codigo);
        Assert.Equal(403, (await operador.Post(M, new { nome = "x" })).Codigo);
        Assert.Equal(403, (await operador.Put($"{M}/{marca}", new { nome = "x" })).Codigo);
        Assert.Equal(403, (await operador.Delete($"{M}/{marca}")).Codigo);
    }

    // ── Categorias ───────────────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Criar_categoria_valida_nome_e_duplicada_e_numera_a_ordem_no_fim()
    {
        var loja = await NovaAsync();

        Assert.Equal((400, "Nome da categoria é obrigatório."), await Erro(loja.Painel.Post(C, Cat("  "))));
        var a = await loja.Painel.Post(C, Cat("  Roupas ", "personalizado", true, false, "Único,Bebê"));
        Assert.Equal(("Roupas", "personalizado", true, false, "Único,Bebê"), (a.Json.GetProperty("nome").GetString(), a.Json.GetProperty("tipoTamanho").GetString(),
            a.Json.GetProperty("usaTamanho").GetBoolean(), a.Json.GetProperty("usaCor").GetBoolean(), a.Json.GetProperty("tamanhosPersonalizados").GetString()));
        await loja.Painel.Post(C, Cat("Calçados"));
        Assert.Equal((409, "Já existe uma categoria com esse nome."), await Erro(loja.Painel.Post(C, Cat("ROUPAS"))));

        var lista = (await loja.Painel.Get(C)).Json.EnumerateArray().ToList();
        Assert.Equal(["Roupas", "Calçados"], lista.Select(c => c.GetProperty("nome").GetString()));        // por ordem de criação
        Assert.Equal([0, 1], await api.NoBancoAsync(db => db.CategoriasLoja.Where(c => c.LojaId == loja.Id).OrderBy(c => c.Ordem).Select(c => c.Ordem).ToListAsync()));
    }

    [FatoIntegracao]
    public async Task Atualizar_categoria_renomeia_tambem_os_produtos_e_valida_duplicada()
    {
        var loja = await NovaAsync();
        var outra = await NovaAsync();
        var id = (await loja.Painel.Post(C, Cat("Roupas"))).Json.GetProperty("id").GetGuid();
        await loja.Painel.Post(C, Cat("Calçados"));
        var produto = await loja.ProdutoPelaApiAsync("Camisa", categoria: "Roupas");
        var deOutra = await outra.ProdutoPelaApiAsync("Dela", categoria: "Roupas");

        Assert.Equal((409, "Já existe uma categoria com esse nome."), await Erro(loja.Painel.Put($"{C}/{id}", Cat("calçados"))));
        Assert.Equal((400, "Nome da categoria é obrigatório."), await Erro(loja.Painel.Put($"{C}/{id}", Cat(" "))));
        var r = await loja.Painel.Put($"{C}/{id}", Cat("Vestuário", "numero", false, true));

        Assert.Equal(("Vestuário", "numero", false, true), (r.Json.GetProperty("nome").GetString(), r.Json.GetProperty("tipoTamanho").GetString(),
            r.Json.GetProperty("usaTamanho").GetBoolean(), r.Json.GetProperty("usaCor").GetBoolean()));
        Assert.Equal("Vestuário", (await api.ProdutoDaLojaNoBancoAsync(produto)).Categoria);
        Assert.Equal("Roupas", (await api.ProdutoDaLojaNoBancoAsync(deOutra)).Categoria);       // produto de outra loja não muda
        Assert.Equal(404, (await outra.Painel.Put($"{C}/{id}", Cat("x"))).Codigo);
        Assert.Equal(404, (await loja.Painel.Put($"{C}/{Guid.NewGuid()}", Cat("x"))).Codigo);
    }

    [FatoIntegracao]
    public async Task Atualizar_categoria_so_com_outra_caixa_nao_mexe_nos_produtos()
    {
        var loja = await NovaAsync();
        var id = (await loja.Painel.Post(C, Cat("Roupas"))).Json.GetProperty("id").GetGuid();
        var produto = await loja.ProdutoPelaApiAsync("Camisa", categoria: "Roupas");

        var r = await loja.Painel.Put($"{C}/{id}", Cat("ROUPAS"));

        Assert.Equal("ROUPAS", r.Json.GetProperty("nome").GetString());
        Assert.Equal("Roupas", (await api.ProdutoDaLojaNoBancoAsync(produto)).Categoria);
    }

    [FatoIntegracao]
    public async Task Excluir_categoria_com_produto_ativo_e_recusado_e_sem_produto_so_desativa()
    {
        var loja = await NovaAsync();
        var emUso = (await loja.Painel.Post(C, Cat("Roupas"))).Json.GetProperty("id").GetGuid();
        var livre = (await loja.Painel.Post(C, Cat("Livre"))).Json.GetProperty("id").GetGuid();
        await loja.ProdutoPelaApiAsync("Camisa", categoria: "Roupas");
        await loja.ProdutoPelaApiAsync("Inativo", categoria: "Livre", ativo: false);       // produto inativo não conta

        Assert.Equal((400, "Não é possível excluir: 1 produto(s) usam esta categoria."), await Erro(loja.Painel.Delete($"{C}/{emUso}")));
        Assert.Equal("Categoria excluída.", (await loja.Painel.Delete($"{C}/{livre}")).Json.GetProperty("mensagem").GetString());
        Assert.Equal(["Roupas"], (await loja.Painel.Get(C)).Json.EnumerateArray().Select(c => c.GetProperty("nome").GetString()));
        Assert.Equal(200, (await loja.Painel.Delete($"{C}/{livre}")).Codigo);       // a categoria desativada ainda é encontrada: excluir de novo repete a resposta
        Assert.False((await api.NoBancoAsync(db => db.CategoriasLoja.AsNoTracking().FirstAsync(c => c.Id == livre))).Ativo);
    }

    [FatoIntegracao]
    public async Task Categorias_sao_por_loja_e_a_listagem_so_traz_as_ativas()
    {
        var loja = await NovaAsync();
        var outra = await NovaAsync();
        await loja.Painel.Post(C, Cat("Minha"));

        Assert.Empty((await outra.Painel.Get(C)).Json.EnumerateArray());
        var id = (await loja.Painel.Get(C)).Json[0].GetProperty("id").GetGuid();
        Assert.Equal(404, (await outra.Painel.Delete($"{C}/{id}")).Codigo);
    }

    // ── Marcas ───────────────────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Criar_marca_valida_nome_e_duplicada_e_lista_por_nome()
    {
        var loja = await NovaAsync();

        Assert.Equal((400, "Nome da marca é obrigatório."), await Erro(loja.Painel.Post(M, new { nome = " " })));
        var r = await loja.Painel.Post(M, new { nome = "  Bosch " });
        Assert.Equal("Bosch", r.Json.GetProperty("nome").GetString());
        await loja.Painel.Post(M, new { nome = "Ipiranga" });
        await loja.Painel.Post(M, new { nome = "Amarok" });
        Assert.Equal((409, "Já existe uma marca com esse nome."), await Erro(loja.Painel.Post(M, new { nome = "BOSCH" })));

        Assert.Equal(["Amarok", "Bosch", "Ipiranga"], (await loja.Painel.Get(M)).Json.EnumerateArray().Select(m => m.GetProperty("nome").GetString()));
    }

    [FatoIntegracao]
    public async Task Atualizar_marca_valida_duplicada_e_404_para_outra_loja()
    {
        var loja = await NovaAsync();
        var outra = await NovaAsync();
        var bosch = await loja.MarcaIdAsync("Bosch");
        await loja.MarcaIdAsync("Ipiranga");

        Assert.Equal((409, "Já existe uma marca com esse nome."), await Erro(loja.Painel.Put($"{M}/{bosch}", new { nome = "ipiranga" })));
        Assert.Equal((400, "Nome da marca é obrigatório."), await Erro(loja.Painel.Put($"{M}/{bosch}", new { nome = "" })));
        Assert.Equal("BOSCH", (await loja.Painel.Put($"{M}/{bosch}", new { nome = " BOSCH " })).Json.GetProperty("nome").GetString());   // só mudar a caixa é permitido
        Assert.Equal(404, (await outra.Painel.Put($"{M}/{bosch}", new { nome = "x" })).Codigo);
        Assert.Equal(404, (await loja.Painel.Put($"{M}/{Guid.NewGuid()}", new { nome = "x" })).Codigo);
    }

    [FatoIntegracao]
    public async Task Excluir_marca_com_produto_ativo_e_recusado_e_sem_produto_so_desativa()
    {
        var loja = await NovaAsync();
        var emUso = await loja.MarcaIdAsync("Em uso");
        var livre = await loja.MarcaIdAsync("Livre");
        await loja.ProdutoPelaApiAsync("Pneu", marca: emUso);

        Assert.Equal((400, "Não é possível excluir: 1 produto(s) usam esta marca."), await Erro(loja.Painel.Delete($"{M}/{emUso}")));
        Assert.Equal("Marca excluída.", (await loja.Painel.Delete($"{M}/{livre}")).Json.GetProperty("mensagem").GetString());
        Assert.Equal(["Em uso"], (await loja.Painel.Get(M)).Json.EnumerateArray().Select(m => m.GetProperty("nome").GetString()));
        Assert.Equal(404, (await (await NovaAsync()).Painel.Delete($"{M}/{emUso}")).Codigo);
    }
}
