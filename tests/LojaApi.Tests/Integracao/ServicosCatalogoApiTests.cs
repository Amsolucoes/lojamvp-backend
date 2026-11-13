using LojaApi.Tests.Chacara.Dominio;

namespace LojaApi.Tests.Integracao;

// Catálogo de serviços da loja (o que ela oferece na agenda).
[Collection(ColecaoApi.Nome)]
public class ServicosCatalogoApiTests(ApiFixture api)
{
    [FatoIntegracao]
    public async Task Exige_login()
    {
        var loja = await api.NovaLojaDeServicosAsync();
        Assert.Equal(401, (await loja.Site.Get("/api/servicos")).Codigo);
        Assert.Equal(401, (await loja.Site.Post("/api/servicos", new { nome = "x", categoria = "y", preco = 1, duracaoMin = 30, ativo = true })).Codigo);
    }

    [FatoIntegracao]
    public async Task Criar_aplica_valores_padrao_e_apara_espacos()
    {
        var loja = await api.NovaLojaDeServicosAsync();

        var r = await loja.CriarServicoAsync("  Banho completo  ", 80, 45, categoria: "   ");

        Assert.Equal(200, r.Codigo);
        Assert.Equal("Banho completo", r.Json.GetProperty("nome").GetString());
        Assert.Equal("Geral", r.Json.GetProperty("categoria").GetString());      // categoria em branco vira "Geral"
        Assert.Equal(80m, r.Json.GetProperty("preco").GetDecimal());
        Assert.Equal(45, r.Json.GetProperty("duracaoMin").GetInt32());
        Assert.True(r.Json.GetProperty("ativo").GetBoolean());
    }

    [FatoIntegracao]
    public async Task Criar_exige_nome()
    {
        var loja = await api.NovaLojaDeServicosAsync();
        var r = await loja.CriarServicoAsync("   ");
        Assert.Equal(400, r.Codigo);
        Assert.Equal("Nome do serviço é obrigatório.", r.Erro);
    }

    [FatoIntegracao]
    public async Task Listar_ordena_por_categoria_e_depois_por_nome()
    {
        var loja = await api.NovaLojaDeServicosAsync();
        await loja.CriarServicoAsync("Tosa", categoria: "Tosa");
        await loja.CriarServicoAsync("Hidratação", categoria: "Banho");
        await loja.CriarServicoAsync("Banho simples", categoria: "Banho");

        var nomes = (await loja.Painel.Get("/api/servicos")).Json.EnumerateArray().Select(s => s.GetProperty("nome").GetString()).ToList();

        Assert.Equal(["Banho simples", "Hidratação", "Tosa"], nomes);
    }

    [FatoIntegracao]
    public async Task Atualizar_troca_os_campos_e_devolve_404_para_id_desconhecido()
    {
        var loja = await api.NovaLojaDeServicosAsync();
        var id = await loja.ServicoIdAsync("Banho", 80, 60);

        var r = await loja.Painel.Put($"/api/servicos/{id}", new { nome = " Banho premium ", categoria = "Premium", preco = 120, duracaoMin = 90, ativo = false });

        Assert.Equal(200, r.Codigo);
        Assert.Equal("Banho premium", r.Json.GetProperty("nome").GetString());
        Assert.Equal(120m, r.Json.GetProperty("preco").GetDecimal());
        Assert.False(r.Json.GetProperty("ativo").GetBoolean());
        Assert.Equal(404, (await loja.Painel.Put($"/api/servicos/{Guid.NewGuid()}", new { nome = "x", categoria = "y", preco = 1, duracaoMin = 30, ativo = true })).Codigo);
    }

    [FatoIntegracao]
    public async Task Excluir_remove_o_servico_sem_agendamentos()
    {
        var loja = await api.NovaLojaDeServicosAsync();
        var id = await loja.ServicoIdAsync();

        Assert.Equal("Serviço excluído.", (await loja.Painel.Delete($"/api/servicos/{id}")).Json.GetProperty("mensagem").GetString());
        Assert.Equal(404, (await loja.Painel.Delete($"/api/servicos/{id}")).Codigo);
        Assert.Equal(0, (await loja.Painel.Get("/api/servicos")).Json.GetArrayLength());
    }

    [FatoIntegracao]
    public async Task Nao_exclui_servico_que_ja_tem_agendamento_e_diz_quantos()
    {
        var loja = await api.NovaLojaDeServicosAsync();
        var id = await loja.ServicoIdAsync();
        await loja.AgendarAsync(id, Datas.Segunda().Hora(9));
        await loja.AgendarAsync(id, Datas.Segunda().Hora(11));

        var r = await loja.Painel.Delete($"/api/servicos/{id}");

        Assert.Equal(400, r.Codigo);
        Assert.Equal("Não é possível excluir: este serviço tem 2 agendamento(s) vinculado(s). Você pode desativá-lo em vez de excluir.", r.Erro);
        Assert.Equal(1, (await loja.Painel.Get("/api/servicos")).Json.GetArrayLength());
    }

    [FatoIntegracao]
    public async Task Uma_loja_nao_ve_nem_altera_servicos_de_outra()
    {
        var a = await api.NovaLojaDeServicosAsync();
        var b = await api.NovaLojaDeServicosAsync();
        var idA = await a.ServicoIdAsync("Só da A");

        Assert.Equal(0, (await b.Painel.Get("/api/servicos")).Json.GetArrayLength());
        Assert.Equal(404, (await b.Painel.Put($"/api/servicos/{idA}", new { nome = "invadido", categoria = "x", preco = 1, duracaoMin = 30, ativo = true })).Codigo);
        Assert.Equal(404, (await b.Painel.Delete($"/api/servicos/{idA}")).Codigo);
        Assert.Equal("Só da A", (await a.Painel.Get("/api/servicos")).Json[0].GetProperty("nome").GetString());
    }

    // Pendência do PR anterior: criar, editar e excluir serviço exige admin; o operador só consulta.
    [FatoIntegracao]
    public async Task Operador_so_consulta_o_catalogo_e_nao_cria_edita_nem_exclui()
    {
        var loja = await api.NovaLojaDeServicosAsync();
        var id = await loja.ServicoIdAsync();
        var operador = await api.OperadorAsync(loja);

        Assert.Equal(200, (await operador.Get("/api/servicos")).Codigo);
        Assert.Equal(403, (await operador.Post("/api/servicos", new { nome = "Novo", categoria = "X", preco = 10, duracaoMin = 30, ativo = true })).Codigo);
        Assert.Equal(403, (await operador.Put($"/api/servicos/{id}", new { nome = "Mudou", categoria = "X", preco = 10, duracaoMin = 30, ativo = true })).Codigo);
        Assert.Equal(403, (await operador.Delete($"/api/servicos/{id}")).Codigo);
        Assert.Equal(1, (await loja.Painel.Get("/api/servicos")).Json.GetArrayLength());
        Assert.Equal("Banho", (await loja.Painel.Get("/api/servicos")).Json[0].GetProperty("nome").GetString());
    }
}
