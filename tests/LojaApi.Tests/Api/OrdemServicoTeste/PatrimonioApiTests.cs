using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using static LojaApi.Tests.Suporte.CenariosOrdemServico;

namespace LojaApi.Tests.Api.OrdemServicoTeste;

// Patrimônio da oficina (ferramentas e equipamentos): cadastro, contagens com comparativo e PDFs.
[Collection(ColecaoApi.Nome)]
public class PatrimonioApiTests(ApiFixture api)
{
    private const string P = Rota + "/patrimonio";

    private Task<LojaTeste> NovaAsync() => api.NovaLojaDeServicosAsync();

    private static object Item(string nome = "Chave de fenda", string? categoria = "Manuais", int qtd = 4, decimal valor = 25m, string? obs = null, bool ativo = true) =>
        new { nome, categoria, quantidadeEsperada = qtd, valorUnitario = valor, observacao = obs, ativo };

    private static async Task<Guid> ItemAsync(LojaTeste loja, string nome = "Chave de fenda", string? categoria = "Manuais", int qtd = 4, decimal valor = 25m, bool ativo = true)
    {
        var r = await loja.Painel.Post($"{P}/itens", Item(nome, categoria, qtd, valor, null, ativo));
        r.DeveSerSucesso();
        return r.Json.GetProperty("id").GetGuid();
    }

    private static object Contagem(object[] itens, DateTime? data = null, string? responsavel = "Carlos", string? obs = null) =>
        new { dataContagem = data, responsavel, observacao = obs, itens };

    private static object ItemContado(Guid item, int contada, string? obs = null) => new { itemPatrimonioId = item, quantidadeContada = contada, observacao = obs };

    [FatoIntegracao]
    public async Task Exige_login_e_so_admin_altera()
    {
        var loja = await NovaAsync();
        var operador = await api.OperadorAsync(loja);
        var item = await ItemAsync(loja);
        var contagem = (await loja.Painel.Post($"{P}/contagens", Contagem([ItemContado(item, 4)]))).Json.GetProperty("id").GetGuid();

        foreach (var u in new[] { $"{P}/itens", $"{P}/contagens", $"{P}/contagens/{contagem}", $"{P}/relatorio-pdf", $"{P}/contagens/{contagem}/pdf" })
            Assert.Equal(401, (await loja.Site.Get(u)).Codigo);
        Assert.Equal(200, (await operador.Get($"{P}/itens")).Codigo);
        Assert.Equal(200, (await operador.Get($"{P}/contagens")).Codigo);
        Assert.Equal(403, (await operador.Post($"{P}/itens", Item())).Codigo);
        Assert.Equal(403, (await operador.Put($"{P}/itens/{item}", Item())).Codigo);
        Assert.Equal(403, (await operador.Delete($"{P}/itens/{item}")).Codigo);
        Assert.Equal(403, (await operador.Post($"{P}/contagens", Contagem([ItemContado(item, 1)]))).Codigo);
        Assert.Equal(403, (await operador.Delete($"{P}/contagens/{contagem}")).Codigo);
    }

    [FatoIntegracao]
    public async Task Criar_item_valida_o_nome_apara_os_campos_e_listar_calcula_o_valor_total()
    {
        var loja = await NovaAsync();
        var vazio = await loja.Painel.Post($"{P}/itens", Item(" "));
        Assert.Equal((400, "Nome é obrigatório."), (vazio.Codigo, vazio.Erro));

        var r = await loja.Painel.Post($"{P}/itens", Item("  Macaco hidráulico ", "  Elevação ", 2, 300m, "novo"));
        Assert.Equal(("Macaco hidráulico", "Elevação", 2, 300m, true), (r.Json.GetProperty("nome").GetString(), r.Json.GetProperty("categoria").GetString(),
            r.Json.GetProperty("quantidadeEsperada").GetInt32(), r.Json.GetProperty("valorUnitario").GetDecimal(), r.Json.GetProperty("ativo").GetBoolean()));
        await ItemAsync(loja, "Alicate", "Manuais", 3, 40m);
        await ItemAsync(loja, "Sem categoria", null, 1, 10m);
        await loja.Painel.Post($"{P}/itens", Item("Inativo", "Manuais", 1, 5m, null, false));

        var ativos = (await loja.Painel.Get($"{P}/itens")).Json.EnumerateArray().ToList();
        var todos = (await loja.Painel.Get($"{P}/itens?incluirInativos=true")).Json.EnumerateArray().ToList();

        Assert.Equal(["Macaco hidráulico", "Alicate", "Sem categoria"], ativos.Select(i => i.GetProperty("nome").GetString()));   // por categoria (sem categoria por último) e nome
        Assert.Equal(600m, ativos[0].GetProperty("valorTotal").GetDecimal());
        Assert.Equal(120m, ativos[1].GetProperty("valorTotal").GetDecimal());
        Assert.Contains("Inativo", todos.Select(i => i.GetProperty("nome").GetString()));
        Assert.Equal(4, todos.Count);
    }

    [FatoIntegracao]
    public async Task Atualizar_item_troca_os_campos_e_so_a_propria_loja_enxerga()
    {
        var loja = await NovaAsync();
        var outra = await NovaAsync();
        var id = await ItemAsync(loja);

        var r = await loja.Painel.Put($"{P}/itens/{id}", Item(" Novo nome ", "  ", 9, 12.5m, "obs", false));

        Assert.Equal(("Novo nome", 9, 12.5m, false), (r.Json.GetProperty("nome").GetString(), r.Json.GetProperty("quantidadeEsperada").GetInt32(),
            r.Json.GetProperty("valorUnitario").GetDecimal(), r.Json.GetProperty("ativo").GetBoolean()));
        Assert.Equal(JsonValueKind.Null, r.Json.GetProperty("categoria").ValueKind);   // categoria em branco vira nula
        Assert.Equal(404, (await outra.Painel.Put($"{P}/itens/{id}", Item())).Codigo);
        Assert.Equal(404, (await loja.Painel.Put($"{P}/itens/{Guid.NewGuid()}", Item())).Codigo);
        Assert.Empty((await outra.Painel.Get($"{P}/itens?incluirInativos=true")).Json.EnumerateArray());
    }

    [FatoIntegracao]
    public async Task Excluir_item_ja_contado_so_desativa_e_o_livre_e_removido()
    {
        var loja = await NovaAsync();
        var outra = await NovaAsync();
        var contado = await ItemAsync(loja, "Contado");
        var livre = await ItemAsync(loja, "Livre");
        (await loja.Painel.Post($"{P}/contagens", Contagem([ItemContado(contado, 1)]))).DeveSerSucesso();

        Assert.Equal(404, (await outra.Painel.Delete($"{P}/itens/{livre}")).Codigo);
        Assert.Equal("Item já usado em alguma contagem — foi desativado em vez de excluído.", (await loja.Painel.Delete($"{P}/itens/{contado}")).Json.GetProperty("mensagem").GetString());
        Assert.Equal("Item excluído.", (await loja.Painel.Delete($"{P}/itens/{livre}")).Json.GetProperty("mensagem").GetString());

        var todos = (await loja.Painel.Get($"{P}/itens?incluirInativos=true")).Json.EnumerateArray().ToList();
        Assert.Equal("Contado", Assert.Single(todos).GetProperty("nome").GetString());
        Assert.False(todos[0].GetProperty("ativo").GetBoolean());
        Assert.Equal(404, (await loja.Painel.Delete($"{P}/itens/{Guid.NewGuid()}")).Codigo);
    }

    [FatoIntegracao]
    public async Task Criar_contagem_valida_os_itens_e_guarda_o_esperado_da_epoca()
    {
        var loja = await NovaAsync();
        var outra = await NovaAsync();
        var item = await ItemAsync(loja, "Chave", qtd: 4);
        var dela = await ItemAsync(outra, "Dela", qtd: 1);

        var semItens = await loja.Painel.Post($"{P}/contagens", Contagem([]));
        var alheio = await loja.Painel.Post($"{P}/contagens", Contagem([ItemContado(dela, 1)]));
        var inexistente = await loja.Painel.Post($"{P}/contagens", Contagem([ItemContado(Guid.NewGuid(), 1)]));
        Assert.Equal((400, "A contagem precisa ter ao menos um item."), (semItens.Codigo, semItens.Erro));
        Assert.Equal((400, "Algum item de patrimônio informado não foi encontrado."), (alheio.Codigo, alheio.Erro));
        Assert.Equal((400, "Algum item de patrimônio informado não foi encontrado."), (inexistente.Codigo, inexistente.Erro));
        Assert.Empty((await loja.Painel.Get($"{P}/contagens")).Json.EnumerateArray());

        var criada = await loja.Painel.Post($"{P}/contagens", Contagem([ItemContado(item, 3, "emprestada")], new DateTime(2031, 4, 2, 10, 0, 0), "Carlos", "mensal"));
        Assert.Equal(200, criada.Codigo);
        Assert.Equal(new DateTime(2031, 4, 2, 10, 0, 0, DateTimeKind.Utc), criada.Json.GetProperty("dataContagem").GetDateTime().ToUniversalTime());

        // mudar o cadastro depois não altera o esperado daquela contagem
        (await loja.Painel.Put($"{P}/itens/{item}", Item("Chave", "Manuais", 10, 25m))).DeveSerSucesso();
        var d = (await loja.Painel.Get($"{P}/contagens/{criada.Json.GetProperty("id").GetGuid()}")).Json;
        var linha = Assert.Single(d.GetProperty("itens").EnumerateArray());
        Assert.Equal(("Carlos", "mensal"), (d.GetProperty("responsavel").GetString(), d.GetProperty("observacao").GetString()));
        Assert.Equal(("Chave", "Manuais", 4, 3, -1, 25m, "emprestada"), (linha.GetProperty("nomeItem").GetString(), linha.GetProperty("categoria").GetString(),
            linha.GetProperty("quantidadeEsperadaNoMomento").GetInt32(), linha.GetProperty("quantidadeContada").GetInt32(), linha.GetProperty("diferenca").GetInt32(),
            linha.GetProperty("valorUnitario").GetDecimal(), linha.GetProperty("observacao").GetString()));
    }

    [FatoIntegracao]
    public async Task Listar_contagens_conta_itens_e_divergencias_e_isola_por_loja()
    {
        var loja = await NovaAsync();
        var outra = await NovaAsync();
        var a = await ItemAsync(loja, "A", qtd: 2);
        var b = await ItemAsync(loja, "B", qtd: 5);
        var antiga = (await loja.Painel.Post($"{P}/contagens", Contagem([ItemContado(a, 2)], new DateTime(2031, 1, 1)))).Json.GetProperty("id").GetGuid();
        var nova = (await loja.Painel.Post($"{P}/contagens", Contagem([ItemContado(a, 1), ItemContado(b, 5)], new DateTime(2031, 2, 1)))).Json.GetProperty("id").GetGuid();

        var lista = (await loja.Painel.Get($"{P}/contagens")).Json.EnumerateArray().ToList();

        Assert.Equal([nova, antiga], lista.Select(c => c.GetProperty("id").GetGuid()));
        Assert.Equal((2, 1), (lista[0].GetProperty("qtdItens").GetInt32(), lista[0].GetProperty("qtdDivergentes").GetInt32()));
        Assert.Equal((1, 0), (lista[1].GetProperty("qtdItens").GetInt32(), lista[1].GetProperty("qtdDivergentes").GetInt32()));
        Assert.Empty((await outra.Painel.Get($"{P}/contagens")).Json.EnumerateArray());
        Assert.Equal(404, (await outra.Painel.Get($"{P}/contagens/{nova}")).Codigo);
        Assert.Equal(404, (await loja.Painel.Get($"{P}/contagens/{Guid.NewGuid()}")).Codigo);
    }

    [FatoIntegracao]
    public async Task Excluir_contagem_remove_os_itens_dela_e_so_da_propria_loja()
    {
        var loja = await NovaAsync();
        var outra = await NovaAsync();
        var item = await ItemAsync(loja);
        var id = (await loja.Painel.Post($"{P}/contagens", Contagem([ItemContado(item, 1)]))).Json.GetProperty("id").GetGuid();

        Assert.Equal(404, (await outra.Painel.Delete($"{P}/contagens/{id}")).Codigo);
        Assert.Equal("Contagem excluída.", (await loja.Painel.Delete($"{P}/contagens/{id}")).Json.GetProperty("mensagem").GetString());

        Assert.Equal(404, (await loja.Painel.Get($"{P}/contagens/{id}")).Codigo);
        Assert.False(await api.NoBancoAsync(db => db.ItensContagemPatrimonio.AnyAsync(i => i.ContagemId == id)));
        Assert.Equal(404, (await loja.Painel.Delete($"{P}/contagens/{id}")).Codigo);
    }

    [FatoIntegracao]
    public async Task Pdfs_do_cadastro_e_da_contagem_sao_gerados_e_404_para_outra_loja()
    {
        var loja = await NovaAsync();
        var outra = await NovaAsync();
        var item = await ItemAsync(loja);
        var id = (await loja.Painel.Post($"{P}/contagens", Contagem([ItemContado(item, 3)]))).Json.GetProperty("id").GetGuid();

        var cadastro = await loja.Painel.Get($"{P}/relatorio-pdf");
        var contagem = await loja.Painel.Get($"{P}/contagens/{id}/pdf");

        foreach (var pdf in new[] { cadastro, contagem })
        {
            Assert.Equal(200, pdf.Codigo);
            Assert.Equal("application/pdf", pdf.TipoConteudo);
            Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(pdf.Bytes, 0, 4));
        }
        Assert.Equal(404, (await outra.Painel.Get($"{P}/contagens/{id}/pdf")).Codigo);
        Assert.Equal(200, (await outra.Painel.Get($"{P}/relatorio-pdf")).Codigo);     // loja sem itens: PDF vazio, ainda 200
    }
}
