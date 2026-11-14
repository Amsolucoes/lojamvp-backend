using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace LojaApi.Tests.Api.VendasTeste;

// Origens de venda (de onde veio o cliente: loja física, site, WhatsApp...).
[Collection(ColecaoApi.Nome)]
public class OrigensVendaApiTests(ApiFixture api)
{
    private const string Rota = "/api/origens-venda";

    private Task<LojaTeste> NovaAsync() => api.NovaLojaDeServicosAsync();

    private static Guid Id(JsonElement e) => e.GetProperty("id").GetGuid();

    private static object Corpo(string nome, int ordem = 0, bool ativa = true) => new { nome, ordem, ativa };

    [FatoIntegracao]
    public async Task Exige_login()
    {
        var loja = await NovaAsync();
        Assert.Equal(401, (await loja.Site.Get(Rota)).Codigo);
        Assert.Equal(401, (await loja.Site.Get($"{Rota}/todas")).Codigo);
        Assert.Equal(401, (await loja.Site.Post(Rota, Corpo("x"))).Codigo);
        Assert.Equal(401, (await loja.Site.Post($"{Rota}/seed-padrao")).Codigo);
    }

    [FatoIntegracao]
    public async Task Criar_valida_nome_e_duplicado_e_grava_sem_espacos()
    {
        var loja = await NovaAsync();
        Assert.Equal((400, "Informe o nome da origem."), ((await loja.Painel.Post(Rota, Corpo("  "))) is var a ? (a.Codigo, a.Erro) : default));

        var r = await loja.Painel.Post(Rota, Corpo("  Instagram ", 3, true));
        Assert.Equal(200, r.Codigo);
        Assert.Equal(("Instagram", 3, true), (r.Json.GetProperty("nome").GetString(), r.Json.GetProperty("ordem").GetInt32(), r.Json.GetProperty("ativa").GetBoolean()));

        var duplicada = await loja.Painel.Post(Rota, Corpo("Instagram"));
        Assert.Equal((400, "Já existe uma origem com esse nome."), (duplicada.Codigo, duplicada.Erro));

        // o mesmo nome em outra loja é permitido
        var outra = await NovaAsync();
        Assert.Equal(200, (await outra.Painel.Post(Rota, Corpo("Instagram"))).Codigo);
    }

    [FatoIntegracao]
    public async Task Listar_traz_so_as_ativas_por_ordem_e_nome_e_todas_traz_tambem_as_inativas()
    {
        var loja = await NovaAsync();
        var outra = await NovaAsync();
        await loja.Painel.Post(Rota, Corpo("Zeta", 1));
        await loja.Painel.Post(Rota, Corpo("Alfa", 1));
        await loja.Painel.Post(Rota, Corpo("Primeira", 0));
        await loja.Painel.Post(Rota, Corpo("Inativa", 2, false));
        await outra.Painel.Post(Rota, Corpo("Da outra"));

        var ativas = (await loja.Painel.Get(Rota)).Json.EnumerateArray().ToList();
        var todas = (await loja.Painel.Get($"{Rota}/todas")).Json.EnumerateArray().Select(o => o.GetProperty("nome").GetString()).ToList();

        Assert.Equal(["Primeira", "Alfa", "Zeta"], ativas.Select(o => o.GetProperty("nome").GetString()).ToList());
        Assert.Equal(["id", "nome", "ordem"], ativas[0].EnumerateObject().Select(p => p.Name).ToList());     // a lista simples só traz esses campos
        Assert.Equal(["Primeira", "Alfa", "Zeta", "Inativa"], todas);
    }

    [FatoIntegracao]
    public async Task Atualizar_troca_os_campos_e_so_da_propria_loja()
    {
        var loja = await NovaAsync();
        var outra = await NovaAsync();
        var id = Id((await loja.Painel.Post(Rota, Corpo("Antiga", 1))).Json);

        var r = await loja.Painel.Put($"{Rota}/{id}", Corpo("  Nova ", 5, false));

        Assert.Equal(("Nova", 5, false), (r.Json.GetProperty("nome").GetString(), r.Json.GetProperty("ordem").GetInt32(), r.Json.GetProperty("ativa").GetBoolean()));
        Assert.Equal(404, (await outra.Painel.Put($"{Rota}/{id}", Corpo("Invasor"))).Codigo);
        Assert.Equal(404, (await loja.Painel.Put($"{Rota}/{Guid.NewGuid()}", Corpo("x"))).Codigo);
    }

    [FatoIntegracao]
    public async Task Excluir_remove_a_nunca_usada_e_so_desativa_a_que_ja_tem_venda()
    {
        var loja = await NovaAsync();
        var outra = await NovaAsync();
        var livre = Id((await loja.Painel.Post(Rota, Corpo("Livre"))).Json);
        var usada = Id((await loja.Painel.Post(Rota, Corpo("Usada"))).Json);
        var produto = await api.ProdutoAsync(loja.Id, estoque: 5);
        await loja.VenderAsync([CenariosVendas.ItemProduto(produto)], origem: usada);

        Assert.Equal(404, (await outra.Painel.Delete($"{Rota}/{livre}")).Codigo);
        Assert.Equal("Origem excluída.", (await loja.Painel.Delete($"{Rota}/{livre}")).Json.GetProperty("mensagem").GetString());
        Assert.Equal(0, await api.NoBancoAsync(db => db.OrigensVenda.CountAsync(o => o.Id == livre)));

        Assert.Equal("Origem em uso — foi desativada em vez de excluída.", (await loja.Painel.Delete($"{Rota}/{usada}")).Json.GetProperty("mensagem").GetString());
        Assert.False((await api.NoBancoAsync(db => db.OrigensVenda.AsNoTracking().FirstAsync(o => o.Id == usada))).Ativa);
        Assert.Equal(404, (await loja.Painel.Delete($"{Rota}/{Guid.NewGuid()}")).Codigo);
    }

    [FatoIntegracao]
    public async Task Seed_padrao_cria_tres_origens_uma_unica_vez()
    {
        var loja = await NovaAsync();

        var r = await loja.Painel.Post($"{Rota}/seed-padrao");

        Assert.Equal((200, "Origens padrão criadas."), (r.Codigo, r.Json.GetProperty("mensagem").GetString()));
        var origens = (await loja.Painel.Get($"{Rota}/todas")).Json.EnumerateArray().Select(o => (o.GetProperty("nome").GetString()!, o.GetProperty("ordem").GetInt32())).ToList();
        Assert.Equal([("Loja física", 0), ("Site", 1), ("WhatsApp", 2)], origens);

        var denovo = await loja.Painel.Post($"{Rota}/seed-padrao");
        Assert.Equal((400, "Você já tem origens cadastradas."), (denovo.Codigo, denovo.Erro));
    }
}
