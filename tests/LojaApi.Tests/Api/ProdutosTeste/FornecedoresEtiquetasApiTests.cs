using System.Text.Json;
using static LojaApi.Tests.Suporte.CenariosProdutos;

namespace LojaApi.Tests.Api.ProdutosTeste;

// Fornecedores (com endereço detalhado) e modelos de etiqueta.
[Collection(ColecaoApi.Nome)]
public class FornecedoresEtiquetasApiTests(ApiFixture api)
{
    private const string F = "/api/fornecedores";
    private const string E = "/api/etiquetas/modelos";

    private Task<LojaTeste> NovaAsync() => api.NovaLojaDeServicosAsync();

    private static async Task<(int, string)> Erro(Task<Resposta> r) { var x = await r; return (x.Codigo, x.Erro); }

    // ── Fornecedores ─────────────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Fornecedores_exigem_login_e_so_admin_altera()
    {
        var loja = await NovaAsync();
        var operador = await api.OperadorAsync(loja);
        var id = await loja.FornecedorIdAsync();

        foreach (var u in new[] { F, $"{F}/ativos", $"{F}/{id}" }) Assert.Equal(401, (await loja.Site.Get(u)).Codigo);
        Assert.Equal(200, (await operador.Get(F)).Codigo);
        Assert.Equal(403, (await operador.Post(F, CorpoFornecedor())).Codigo);
        Assert.Equal(403, (await operador.Put($"{F}/{id}", CorpoFornecedor())).Codigo);
        Assert.Equal(403, (await operador.Delete($"{F}/{id}")).Codigo);
    }

    [FatoIntegracao]
    public async Task Criar_fornecedor_valida_loja_e_nome_e_devolve_201()
    {
        var loja = await NovaAsync();

        Assert.Equal((400, "Nome é obrigatório."), await Erro(loja.Painel.Post(F, CorpoFornecedor(" "))));
        var r = await loja.Painel.Post(F, CorpoFornecedor("  Alfa ", "12.345.678/0001-90", "6733334444", "alfa@teste.com", null, "obs"));

        Assert.Equal(201, r.Codigo);
        Assert.Equal(("Alfa", "12.345.678/0001-90", "6733334444", "alfa@teste.com", "obs", true, 0), (r.Json.GetProperty("nome").GetString(), r.Json.GetProperty("cnpjCpf").GetString(),
            r.Json.GetProperty("telefone").GetString(), r.Json.GetProperty("email").GetString(), r.Json.GetProperty("observacoes").GetString(), r.Json.GetProperty("ativo").GetBoolean(),
            r.Json.GetProperty("qtdProdutos").GetInt32()));
        Assert.EndsWith($"{F}/{r.Json.GetProperty("id").GetGuid()}", r.Local!, StringComparison.OrdinalIgnoreCase);
    }

    [FatoIntegracao]
    public async Task Endereco_detalhado_monta_o_texto_completo_e_sem_detalhe_vale_o_endereco_informado()
    {
        var loja = await NovaAsync();

        var detalhado = await loja.Painel.Post(F, CorpoFornecedor("Com detalhe", cep: "79000-000", rua: " Rua A ", numero: "10", bairro: "Centro", cidade: "Campo Grande", uf: "ms"));
        var texto = await loja.Painel.Post(F, CorpoFornecedor("Só texto", endereco: "  Av. Brasil, 100 "));
        var parcial = await loja.Painel.Post(F, CorpoFornecedor("Parcial", cidade: "Dourados", uf: "ms"));

        Assert.Equal(("Rua A, 10 - Centro - Campo Grande/MS - 79000-000", "MS", "Rua A"), (detalhado.Json.GetProperty("endereco").GetString(), detalhado.Json.GetProperty("uf").GetString(), detalhado.Json.GetProperty("rua").GetString()));
        Assert.Equal("Av. Brasil, 100", texto.Json.GetProperty("endereco").GetString());
        Assert.Equal("Dourados/MS", parcial.Json.GetProperty("endereco").GetString());
    }

    [FatoIntegracao]
    public async Task Listar_busca_por_nome_cnpj_ou_telefone_conta_produtos_ativos_e_isola_por_loja()
    {
        var loja = await NovaAsync();
        var outra = await NovaAsync();
        var alfa = await loja.Painel.Post(F, CorpoFornecedor("Alfa", "111", "6711112222"));
        var beta = await loja.Painel.Post(F, CorpoFornecedor("Beta", "222", "6733334444"));
        var alfaId = alfa.Json.GetProperty("id").GetGuid();
        await loja.ProdutoPelaApiAsync("P1", fornecedor: alfaId);
        await loja.ProdutoPelaApiAsync("P2", fornecedor: alfaId);
        await loja.ProdutoPelaApiAsync("P3", fornecedor: alfaId, ativo: false);
        await outra.FornecedorIdAsync("Dela");

        string[] Nomes(Resposta r) => r.Json.EnumerateArray().Select(f => f.GetProperty("nome").GetString()!).ToArray();

        var todos = await loja.Painel.Get(F);
        Assert.Equal(["Alfa", "Beta"], Nomes(todos));
        Assert.Equal(2, todos.Json[0].GetProperty("qtdProdutos").GetInt32());
        Assert.Equal(["Beta"], Nomes(await loja.Painel.Get($"{F}?busca=BET")));
        Assert.Equal(["Alfa"], Nomes(await loja.Painel.Get($"{F}?busca=111")));
        Assert.Equal(["Beta"], Nomes(await loja.Painel.Get($"{F}?busca=3333")));
        Assert.Equal(["Dela"], Nomes(await outra.Painel.Get(F)));
    }

    [FatoIntegracao]
    public async Task Ativos_traz_so_id_e_nome_dos_ativos_da_loja_e_buscar_da_404_para_outra_loja()
    {
        var loja = await NovaAsync();
        var outra = await NovaAsync();
        await loja.FornecedorIdAsync("Ativo");
        var inativo = await loja.FornecedorIdAsync("Inativo", ativo: false);

        var ativos = (await loja.Painel.Get($"{F}/ativos")).Json.EnumerateArray().ToList();

        var unico = Assert.Single(ativos);
        Assert.Equal(["id", "nome"], unico.EnumerateObject().Select(p => p.Name).OrderBy(x => x));
        Assert.Equal("Ativo", unico.GetProperty("nome").GetString());
        Assert.Equal(200, (await loja.Painel.Get($"{F}/{inativo}")).Codigo);
        Assert.Equal(404, (await outra.Painel.Get($"{F}/{inativo}")).Codigo);
        Assert.Empty((await outra.Painel.Get($"{F}/ativos")).Json.EnumerateArray());
    }

    [FatoIntegracao]
    public async Task Atualizar_fornecedor_troca_os_campos_e_valida()
    {
        var loja = await NovaAsync();
        var outra = await NovaAsync();
        var id = await loja.FornecedorIdAsync("Alfa");
        await loja.ProdutoPelaApiAsync("P1", fornecedor: id);

        Assert.Equal((400, "Nome é obrigatório."), await Erro(loja.Painel.Put($"{F}/{id}", CorpoFornecedor(""))));
        var r = await loja.Painel.Put($"{F}/{id}", CorpoFornecedor(" Beta ", "999", null, null, null, "novo", false, rua: "Rua B", cidade: "Dourados", uf: "ms"));

        Assert.Equal(200, r.Codigo);
        Assert.Equal(("Beta", "999", false, 1, "Rua B - Dourados/MS"), (r.Json.GetProperty("nome").GetString(), r.Json.GetProperty("cnpjCpf").GetString(), r.Json.GetProperty("ativo").GetBoolean(),
            r.Json.GetProperty("qtdProdutos").GetInt32(), r.Json.GetProperty("endereco").GetString()));
        Assert.Equal(404, (await outra.Painel.Put($"{F}/{id}", CorpoFornecedor())).Codigo);
        Assert.Equal(404, (await loja.Painel.Put($"{F}/{Guid.NewGuid()}", CorpoFornecedor())).Codigo);
    }

    [FatoIntegracao]
    public async Task Excluir_fornecedor_em_uso_so_desativa_e_o_livre_e_removido()
    {
        var loja = await NovaAsync();
        var outra = await NovaAsync();
        var emUso = await loja.FornecedorIdAsync("Em uso");
        var livre = await loja.FornecedorIdAsync("Livre");
        await loja.ProdutoPelaApiAsync("P1", fornecedor: emUso);

        Assert.Equal(404, (await outra.Painel.Delete($"{F}/{livre}")).Codigo);
        Assert.Equal("Fornecedor em uso — foi desativado em vez de excluído.", (await loja.Painel.Delete($"{F}/{emUso}")).Json.GetProperty("mensagem").GetString());
        Assert.Equal("Fornecedor excluído.", (await loja.Painel.Delete($"{F}/{livre}")).Json.GetProperty("mensagem").GetString());

        var restantes = (await loja.Painel.Get(F)).Json.EnumerateArray().ToList();
        Assert.Equal("Em uso", Assert.Single(restantes).GetProperty("nome").GetString());
        Assert.False(restantes[0].GetProperty("ativo").GetBoolean());
    }

    // ── Etiquetas ────────────────────────────────────────────────────────────

    private static object Modelo(string nome = "Gôndola", decimal largura = 50m, decimal altura = 25m, string? corTexto = null, string? corFundo = null, string? fonte = null,
        int? escala = null, string? logo = null) => new
    {
        nome, incluirLogo = true, usarLogoPropria = false, logoEtiquetaUrl = logo, incluirNomeMarca = true, incluirNomeProduto = true, incluirPreco = false, incluirCodigoBarras = true,
        larguraMm = largura, alturaMm = altura, corTexto, corFundo, fonteFamilia = fonte, escalaFonte = escala,
    };

    [FatoIntegracao]
    public async Task Etiquetas_exigem_login_e_qualquer_usuario_logado_gerencia()
    {
        var loja = await NovaAsync();
        var operador = await api.OperadorAsync(loja);

        Assert.Equal(401, (await loja.Site.Get(E)).Codigo);
        Assert.Equal(200, (await operador.Get(E)).Codigo);
        Assert.Equal(200, (await operador.Post(E, Modelo("Do operador"))).Codigo);
    }

    [FatoIntegracao]
    public async Task Listar_cria_o_modelo_padrao_na_primeira_vez_e_ordena_o_padrao_primeiro()
    {
        var loja = await NovaAsync();

        var primeira = (await loja.Painel.Get(E)).Json.EnumerateArray().ToList();

        var padrao = Assert.Single(primeira);
        Assert.Equal(("Padrão", true, 40m, 30m, "#000000", "#FFFFFF", 100), (padrao.GetProperty("nome").GetString(), padrao.GetProperty("padrao").GetBoolean(), padrao.GetProperty("larguraMm").GetDecimal(),
            padrao.GetProperty("alturaMm").GetDecimal(), padrao.GetProperty("corTexto").GetString(), padrao.GetProperty("corFundo").GetString(), padrao.GetProperty("escalaFonte").GetInt32()));
        await loja.Painel.Post(E, Modelo("Bravo"));
        await loja.Painel.Post(E, Modelo("Alfa"));
        Assert.Equal(["Padrão", "Alfa", "Bravo"], (await loja.Painel.Get(E)).Json.EnumerateArray().Select(m => m.GetProperty("nome").GetString()));
        Assert.Equal(3, (await loja.Painel.Get(E)).Json.GetArrayLength());      // a segunda leitura não cria outro padrão
    }

    [FatoIntegracao]
    public async Task Criar_modelo_valida_nome_e_tamanho_e_aplica_os_padroes_de_estilo()
    {
        var loja = await NovaAsync();

        Assert.Equal((400, "Digite um nome para o modelo."), await Erro(loja.Painel.Post(E, Modelo(" "))));
        Assert.Equal((400, "Largura e altura devem ser maiores que zero."), await Erro(loja.Painel.Post(E, Modelo("x", 0m))));
        Assert.Equal((400, "Largura e altura devem ser maiores que zero."), await Erro(loja.Painel.Post(E, Modelo("x", 10m, -1m))));

        var padroes = await loja.Painel.Post(E, Modelo(" Gôndola ", 50m, 25m, "  ", null, "", 10, "  "));
        Assert.Equal(("Gôndola", "#000000", "#FFFFFF", "Arial, sans-serif", 100, false), (padroes.Json.GetProperty("nome").GetString(), padroes.Json.GetProperty("corTexto").GetString(),
            padroes.Json.GetProperty("corFundo").GetString(), padroes.Json.GetProperty("fonteFamilia").GetString(), padroes.Json.GetProperty("escalaFonte").GetInt32(), padroes.Json.GetProperty("padrao").GetBoolean()));
        Assert.Equal(JsonValueKind.Null, padroes.Json.GetProperty("logoEtiquetaUrl").ValueKind);

        var custom = await loja.Painel.Post(E, Modelo("Custom", 50m, 25m, " #112233 ", "#FFEEDD", " Roboto ", 150, " http://x/logo.png "));
        Assert.Equal(("#112233", "#FFEEDD", "Roboto", 150, "http://x/logo.png"), (custom.Json.GetProperty("corTexto").GetString(), custom.Json.GetProperty("corFundo").GetString(),
            custom.Json.GetProperty("fonteFamilia").GetString(), custom.Json.GetProperty("escalaFonte").GetInt32(), custom.Json.GetProperty("logoEtiquetaUrl").GetString()));
        Assert.Equal(100, (await loja.Painel.Post(E, Modelo("Grande", 50m, 25m, null, null, null, 301))).Json.GetProperty("escalaFonte").GetInt32());   // fora de 50-300 volta a 100
    }

    [FatoIntegracao]
    public async Task Atualizar_modelo_troca_os_campos_e_404_para_outra_loja()
    {
        var loja = await NovaAsync();
        var outra = await NovaAsync();
        var id = (await loja.Painel.Post(E, Modelo("Velho"))).Json.GetProperty("id").GetGuid();

        Assert.Equal((400, "Digite um nome para o modelo."), await Erro(loja.Painel.Put($"{E}/{id}", Modelo(""))));
        Assert.Equal((400, "Largura e altura devem ser maiores que zero."), await Erro(loja.Painel.Put($"{E}/{id}", Modelo("x", 0m))));
        var r = await loja.Painel.Put($"{E}/{id}", Modelo("Novo", 60m, 30m, "#111111"));

        Assert.Equal(("Novo", 60m, "#111111"), (r.Json.GetProperty("nome").GetString(), r.Json.GetProperty("larguraMm").GetDecimal(), r.Json.GetProperty("corTexto").GetString()));
        Assert.Equal(404, (await outra.Painel.Put($"{E}/{id}", Modelo())).Codigo);
        Assert.Equal(404, (await loja.Painel.Put($"{E}/{Guid.NewGuid()}", Modelo())).Codigo);
    }

    [FatoIntegracao]
    public async Task Marcar_padrao_deixa_so_um_e_excluir_o_padrao_promove_outro_mas_nunca_o_ultimo_modelo()
    {
        var loja = await NovaAsync();
        var outra = await NovaAsync();
        var padrao = (await loja.Painel.Get(E)).Json[0].GetProperty("id").GetGuid();
        var segundo = (await loja.Painel.Post(E, Modelo("Segundo"))).Json.GetProperty("id").GetGuid();

        Assert.Equal(404, (await outra.Painel.Patch($"{E}/{segundo}/padrao")).Codigo);
        Assert.Equal("Modelo padrão atualizado.", (await loja.Painel.Patch($"{E}/{segundo}/padrao")).Json.GetProperty("mensagem").GetString());
        var modelos = (await loja.Painel.Get(E)).Json.EnumerateArray().ToList();
        Assert.Equal(segundo, Assert.Single(modelos, m => m.GetProperty("padrao").GetBoolean()).GetProperty("id").GetGuid());

        Assert.Equal("Modelo excluído.", (await loja.Painel.Delete($"{E}/{segundo}")).Json.GetProperty("mensagem").GetString());        // excluiu o padrão: o outro vira padrão
        var depois = (await loja.Painel.Get(E)).Json.EnumerateArray().ToList();
        var unico = Assert.Single(depois);
        Assert.Equal((padrao, true), (unico.GetProperty("id").GetGuid(), unico.GetProperty("padrao").GetBoolean()));
        Assert.Equal((400, "Você precisa ter ao menos um modelo de etiqueta."), await Erro(loja.Painel.Delete($"{E}/{padrao}")));
        Assert.Equal(404, (await loja.Painel.Delete($"{E}/{Guid.NewGuid()}")).Codigo);
    }
}
