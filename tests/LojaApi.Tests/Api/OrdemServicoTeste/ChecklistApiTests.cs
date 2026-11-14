using System.Text.Json;

namespace LojaApi.Tests.Api.OrdemServicoTeste;

// Checklist de inspeção da oficina: categorias e itens configuráveis.
[Collection(ColecaoApi.Nome)]
public class ChecklistApiTests(ApiFixture api)
{
    private const string Cat = CenariosOrdemServico.Rota + "/checklist-categorias";
    private const string Itens = CenariosOrdemServico.Rota + "/checklist-itens";

    private Task<LojaTeste> NovaAsync() => api.NovaLojaDeServicosAsync();

    [FatoIntegracao]
    public async Task Exige_login_e_so_admin_altera()
    {
        var loja = await NovaAsync();
        var operador = await api.OperadorAsync(loja);
        var cat = await loja.ChecklistCategoriaAsync();
        var item = await loja.ChecklistItemAsync(cat);

        Assert.Equal(401, (await loja.Site.Get(Cat)).Codigo);
        Assert.Equal(401, (await loja.Site.Post(Cat, new { nome = "x", ordem = 0, ativa = true })).Codigo);
        Assert.Equal(200, (await operador.Get(Cat)).Codigo);           // ler qualquer logado pode
        Assert.Equal(403, (await operador.Post(Cat, new { nome = "x", ordem = 0, ativa = true })).Codigo);
        Assert.Equal(403, (await operador.Put($"{Cat}/{cat}", new { nome = "x", ordem = 0, ativa = true })).Codigo);
        Assert.Equal(403, (await operador.Delete($"{Cat}/{cat}")).Codigo);
        Assert.Equal(403, (await operador.Post(Itens, new { categoriaId = cat, nome = "x", ordem = 0, ativo = true })).Codigo);
        Assert.Equal(403, (await operador.Put($"{Itens}/{item}", new { categoriaId = cat, nome = "x", ordem = 0, ativo = true })).Codigo);
        Assert.Equal(403, (await operador.Delete($"{Itens}/{item}")).Codigo);
    }

    [FatoIntegracao]
    public async Task Categoria_e_item_sao_criados_aparados_e_listados_por_ordem()
    {
        var loja = await NovaAsync();
        var segunda = await loja.Painel.Post(Cat, new { nome = "  Suspensão ", ordem = 2, ativa = true });
        Assert.Equal(("Suspensão", 2, true), (segunda.Json.GetProperty("nome").GetString(), segunda.Json.GetProperty("ordem").GetInt32(), segunda.Json.GetProperty("ativa").GetBoolean()));
        var primeira = await loja.ChecklistCategoriaAsync("Freios", 1);
        await loja.ChecklistItemAsync(primeira, "Disco", 2);
        var criado = await loja.Painel.Post(Itens, new { categoriaId = primeira, nome = "  Pastilha ", ordem = 1, ativo = true });
        Assert.Equal(("Pastilha", 1, true), (criado.Json.GetProperty("nome").GetString(), criado.Json.GetProperty("ordem").GetInt32(), criado.Json.GetProperty("ativo").GetBoolean()));
        Assert.Equal(primeira, criado.Json.GetProperty("categoriaId").GetGuid());

        var lista = (await loja.Painel.Get(Cat)).Json.EnumerateArray().ToList();

        Assert.Equal(["Freios", "Suspensão"], lista.Select(c => c.GetProperty("nome").GetString()));
        Assert.Equal(["Pastilha", "Disco"], lista[0].GetProperty("itens").EnumerateArray().Select(i => i.GetProperty("nome").GetString()));
        Assert.Empty(lista[1].GetProperty("itens").EnumerateArray());
    }

    [FatoIntegracao]
    public async Task Atualizar_troca_os_campos_e_404_para_outra_loja()
    {
        var loja = await NovaAsync();
        var outra = await NovaAsync();
        var cat = await loja.ChecklistCategoriaAsync("Freios");
        var item = await loja.ChecklistItemAsync(cat, "Disco");

        var c = await loja.Painel.Put($"{Cat}/{cat}", new { nome = " Motor ", ordem = 5, ativa = false });
        var i = await loja.Painel.Put($"{Itens}/{item}", new { categoriaId = cat, nome = " Correia ", ordem = 3, ativo = false });

        Assert.Equal(("Motor", 5, false), (c.Json.GetProperty("nome").GetString(), c.Json.GetProperty("ordem").GetInt32(), c.Json.GetProperty("ativa").GetBoolean()));
        Assert.Equal(("Correia", 3, false), (i.Json.GetProperty("nome").GetString(), i.Json.GetProperty("ordem").GetInt32(), i.Json.GetProperty("ativo").GetBoolean()));
        Assert.Equal(404, (await outra.Painel.Put($"{Cat}/{cat}", new { nome = "x", ordem = 0, ativa = true })).Codigo);
        Assert.Equal(404, (await outra.Painel.Put($"{Itens}/{item}", new { categoriaId = cat, nome = "x", ordem = 0, ativo = true })).Codigo);
        Assert.Equal(404, (await loja.Painel.Put($"{Cat}/{Guid.NewGuid()}", new { nome = "x", ordem = 0, ativa = true })).Codigo);
    }

    [FatoIntegracao]
    public async Task Item_so_pode_ser_criado_em_categoria_da_propria_loja()
    {
        var loja = await NovaAsync();
        var outra = await NovaAsync();
        var catDela = await outra.ChecklistCategoriaAsync();

        var r = await loja.Painel.Post(Itens, new { categoriaId = catDela, nome = "x", ordem = 0, ativo = true });

        Assert.Equal((400, "Categoria de checklist não encontrada."), (r.Codigo, r.Erro));
        Assert.Empty((await outra.Painel.Get(Cat)).Json[0].GetProperty("itens").EnumerateArray());
    }

    [FatoIntegracao]
    public async Task Excluir_categoria_e_item_em_uso_em_orcamento_protege_e_o_livre_e_removido()
    {
        var loja = await NovaAsync();
        var cliente = await loja.ClienteIdAsync();
        var catUsada = await loja.ChecklistCategoriaAsync("Usada");
        var itemUsado = await loja.ChecklistItemAsync(catUsada, "Usado");
        var catLivre = await loja.ChecklistCategoriaAsync("Livre");
        var itemLivre = await loja.ChecklistItemAsync(catLivre, "Livre");
        await loja.OrcamentoAsync(cliente, [CenariosOrdemServico.MaoDeObra()], checklist: [CenariosOrdemServico.Resposta(itemUsado, "ruim")]);

        var cat = await loja.Painel.Delete($"{Cat}/{catUsada}");
        var item = await loja.Painel.Delete($"{Itens}/{itemUsado}");

        Assert.Equal((400, "Não é possível excluir: esta categoria tem itens já usados em orçamentos. Desative os itens em vez de excluir."), (cat.Codigo, cat.Erro));
        Assert.Equal("Item em uso em orçamentos — foi desativado em vez de excluído.", item.Json.GetProperty("mensagem").GetString());
        Assert.Equal("Item excluído.", (await loja.Painel.Delete($"{Itens}/{itemLivre}")).Json.GetProperty("mensagem").GetString());
        Assert.Equal("Categoria excluída.", (await loja.Painel.Delete($"{Cat}/{catLivre}")).Json.GetProperty("mensagem").GetString());
        var restantes = (await loja.Painel.Get(Cat)).Json.EnumerateArray().ToList();
        Assert.Equal("Usada", Assert.Single(restantes).GetProperty("nome").GetString());
        Assert.False(restantes[0].GetProperty("itens")[0].GetProperty("ativo").GetBoolean());
        Assert.Equal(404, (await loja.Painel.Delete($"{Cat}/{Guid.NewGuid()}")).Codigo);
        Assert.Equal(404, (await loja.Painel.Delete($"{Itens}/{Guid.NewGuid()}")).Codigo);
    }

    [FatoIntegracao]
    public async Task Cada_loja_ve_so_o_seu_checklist()
    {
        var loja = await NovaAsync();
        var outra = await NovaAsync();
        await loja.ChecklistCategoriaAsync("Minha");

        Assert.Empty((await outra.Painel.Get(Cat)).Json.EnumerateArray());
        Assert.Equal(404, (await outra.Painel.Delete($"{Cat}/{(await loja.Painel.Get(Cat)).Json[0].GetProperty("id").GetGuid()}")).Codigo);
    }
}
