
namespace LojaApi.Tests.Api.ChacaraTeste;

// Telas de configuração do dono da chácara: dados, preços, faixas, horários, períodos especiais, fotos e pagamento.
[Collection(ColecaoApi.Nome)]
public class ConfiguracaoChacaraApiTests(ApiFixture api)
{
    [TeoriaIntegracao]
    [InlineData("/api/chacara/info")]
    [InlineData("/api/chacara/configuracao-preco")]
    [InlineData("/api/chacara/faixas-preco")]
    [InlineData("/api/chacara/horarios")]
    [InlineData("/api/chacara/periodos-especiais")]
    [InlineData("/api/chacara/fotos")]
    [InlineData("/api/chacara/reservas")]
    [InlineData("/api/chacara/dashboard")]
    public async Task Painel_exige_login(string url)
    {
        var loja = await api.NovaLojaAsync();
        Assert.Equal(401, (await loja.Site.Get(url)).Codigo);
    }

    // ── Dados da chácara ─────────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Info_salva_aparando_espacos_e_devolve_na_proxima_leitura()
    {
        var loja = await api.NovaLojaAsync();

        var salvo = await loja.Painel.Put("/api/chacara/info", new
        {
            descricao = "  Chácara linda  ", endereco = " Rua A, 10 ", comodidades = "piscina,wifi",
            comodidadesExtras = "   ", horaEntrada = "  ", horaSaida = " 21:30 ",
            locadorNome = " Fulano ", locadorCpf = "123.456.789-00",
        });

        Assert.Equal(200, salvo.Codigo);
        var lido = (await loja.Painel.Get("/api/chacara/info")).Json;
        Assert.Equal("Chácara linda", lido.GetProperty("descricao").GetString());
        Assert.Equal("Rua A, 10", lido.GetProperty("endereco").GetString());
        Assert.Equal("21:30", lido.GetProperty("horaSaida").GetString());
        Assert.Equal(JsonValueKind.Null, lido.GetProperty("comodidadesExtras").ValueKind);  // só espaços vira vazio
        Assert.Equal("Fulano", lido.GetProperty("locadorNome").GetString());
    }

    [FatoIntegracao]
    public async Task Info_sem_os_campos_obrigatorios_e_recusada()
    {
        var loja = await api.NovaLojaAsync();
        Assert.Equal(400, (await loja.Painel.Put("/api/chacara/info", new { descricao = (string?)null, endereco = (string?)null, comodidades = (string?)null })).Codigo);
    }

    // ── Preços ───────────────────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Configuracao_de_preco_nasce_com_valores_padrao()
    {
        var loja = await api.NovaLojaAsync();
        var cfg = (await loja.Painel.Get("/api/chacara/configuracao-preco")).Json;

        Assert.Equal(3, cfg.GetProperty("minimoPessoas").GetInt32());
        Assert.Equal(50, cfg.GetProperty("limitePessoasParaTaxaLimpeza").GetInt32());
        Assert.Equal(250m, cfg.GetProperty("valorTaxaLimpeza").GetDecimal());
    }

    [FatoIntegracao]
    public async Task Configuracao_de_preco_salva_e_os_campos_do_cartao_sao_opcionais()
    {
        var loja = await api.NovaLojaAsync();
        var basico = new { minimoPessoas = 4, limitePessoasParaTaxaLimpeza = 60, valorTaxaLimpeza = 300, valorMultaNaoLimpeza = 100, percentualEntradaMinimo = 40 };

        Assert.Equal(200, (await loja.Painel.Put("/api/chacara/configuracao-preco", basico)).Codigo);
        var depoisBasico = (await loja.Painel.Get("/api/chacara/configuracao-preco")).Json;
        Assert.Equal(4, depoisBasico.GetProperty("minimoPessoas").GetInt32());
        Assert.Equal(5m, depoisBasico.GetProperty("percentualTaxaCartao").GetDecimal());   // não veio: manteve o padrão

        var completo = new { minimoPessoas = 5, limitePessoasParaTaxaLimpeza = 70, valorTaxaLimpeza = 350, valorMultaNaoLimpeza = 120, percentualEntradaMinimo = 30,
            percentualTaxaCartao = 7.5, valorLimiteParcelas = 900, parcelasAteLimite = 3, parcelasAcimaLimite = 6 };
        Assert.Equal(200, (await loja.Painel.Put("/api/chacara/configuracao-preco", completo)).Codigo);
        var depois = (await loja.Painel.Get("/api/chacara/configuracao-preco")).Json;
        Assert.Equal(7.5m, depois.GetProperty("percentualTaxaCartao").GetDecimal());
        Assert.Equal(6, depois.GetProperty("parcelasAcimaLimite").GetInt32());
    }

    [FatoIntegracao]
    public async Task Configuracao_de_preco_recusa_taxa_e_parcelas_fora_do_limite()
    {
        var loja = await api.NovaLojaAsync();
        object Com(object extra) => new Dictionary<string, object>
        {
            ["minimoPessoas"] = 4, ["limitePessoasParaTaxaLimpeza"] = 60, ["valorTaxaLimpeza"] = 300,
            ["valorMultaNaoLimpeza"] = 100, ["percentualEntradaMinimo"] = 40,
        }.Concat(extra.GetType().GetProperties().ToDictionary(p => p.Name, p => p.GetValue(extra)!)).ToDictionary(x => x.Key, x => x.Value);

        var taxa = await loja.Painel.Put("/api/chacara/configuracao-preco", Com(new { percentualTaxaCartao = 20 }));
        Assert.Equal(400, taxa.Codigo);
        Assert.Equal("A taxa do cartão deve ficar entre 0% e 15%.", taxa.Erro);

        var parcelas = await loja.Painel.Put("/api/chacara/configuracao-preco", Com(new { parcelasAteLimite = 0 }));
        Assert.Equal(400, parcelas.Codigo);
        Assert.Equal("O número de parcelas deve ficar entre 1 e 12.", parcelas.Erro);
    }

    // ── Faixas de preço ──────────────────────────────────────────────────────

    private static object Faixa(int ate, decimal semana = 500, decimal fim = 800, decimal pacote = 1400) =>
        new { pessoasAte = ate, valorDiariaSemana = semana, valorDiariaFimSemana = fim, valorPacote2DiasFimSemana = pacote };

    [FatoIntegracao]
    public async Task Faixas_criar_listar_editar_e_excluir()
    {
        var loja = await api.NovaLojaAsync();

        var criada = await loja.Painel.Post("/api/chacara/faixas-preco", Faixa(50));
        Assert.Equal(200, criada.Codigo);
        await loja.Painel.Post("/api/chacara/faixas-preco", Faixa(100, 700, 1000, 1800));
        Assert.Equal(2, (await loja.Painel.Get("/api/chacara/faixas-preco")).Json.GetArrayLength());

        var editada = await loja.Painel.Put($"/api/chacara/faixas-preco/{criada.Id}", Faixa(60, 510, 810, 1410));
        Assert.Equal(200, editada.Codigo);
        Assert.Equal(60, editada.Json.GetProperty("pessoasAte").GetInt32());
        Assert.Equal(510m, editada.Json.GetProperty("valorDiariaSemana").GetDecimal());

        Assert.Equal(200, (await loja.Painel.Delete($"/api/chacara/faixas-preco/{criada.Id}")).Codigo);
        Assert.Equal(404, (await loja.Painel.Delete($"/api/chacara/faixas-preco/{criada.Id}")).Codigo);
        Assert.Equal(1, (await loja.Painel.Get("/api/chacara/faixas-preco")).Json.GetArrayLength());
    }

    [FatoIntegracao]
    public async Task Faixas_recusam_limite_invalido_e_duplicado()
    {
        var loja = await api.NovaLojaAsync();
        Assert.Equal("Informe um limite de pessoas válido.", (await loja.Painel.Post("/api/chacara/faixas-preco", Faixa(0))).Erro);

        await loja.Painel.Post("/api/chacara/faixas-preco", Faixa(50));
        var segunda = await loja.Painel.Post("/api/chacara/faixas-preco", Faixa(100));

        var duplicada = await loja.Painel.Post("/api/chacara/faixas-preco", Faixa(50));
        Assert.Equal(409, duplicada.Codigo);
        Assert.Equal(409, (await loja.Painel.Put($"/api/chacara/faixas-preco/{segunda.Id}", Faixa(50))).Codigo);
        Assert.Equal(404, (await loja.Painel.Put("/api/chacara/faixas-preco/999999", Faixa(5))).Codigo);
    }

    // ── Horários de entrada e saída ──────────────────────────────────────────

    private static object Horario(string tipo, string hora, decimal ajuste = 0, int ordem = 0) => new { tipo, hora, ajuste, ordem };

    [FatoIntegracao]
    public async Task Horarios_validam_tipo_e_hora_e_nao_aceitam_repetido()
    {
        var loja = await api.NovaLojaAsync();
        Assert.Equal("Tipo deve ser 'entrada' ou 'saida'.", (await loja.Painel.Post("/api/chacara/horarios", Horario("xxx", "08:00"))).Erro);
        Assert.Equal("Informe um horário válido (HH:mm).", (await loja.Painel.Post("/api/chacara/horarios", Horario("entrada", "25:00"))).Erro);

        Assert.Equal(200, (await loja.Painel.Post("/api/chacara/horarios", Horario("entrada", "08:00"))).Codigo);
        Assert.Equal(409, (await loja.Painel.Post("/api/chacara/horarios", Horario("entrada", "08:00", 5, 1))).Codigo);
        // o mesmo horário como saída é outra coisa e pode
        Assert.Equal(200, (await loja.Painel.Post("/api/chacara/horarios", Horario("saida", "08:00"))).Codigo);
    }

    [FatoIntegracao]
    public async Task Horarios_editar_e_excluir()
    {
        var loja = await api.NovaLojaAsync();
        await loja.Painel.Post("/api/chacara/horarios", Horario("entrada", "08:00"));
        var h2 = await loja.Painel.Post("/api/chacara/horarios", Horario("entrada", "14:30", -50, 1));

        Assert.Equal(409, (await loja.Painel.Put($"/api/chacara/horarios/{h2.Id}", Horario("entrada", "08:00"))).Codigo);
        Assert.Equal(400, (await loja.Painel.Put($"/api/chacara/horarios/{h2.Id}", Horario("entrada", "99:99"))).Codigo);
        var ok = await loja.Painel.Put($"/api/chacara/horarios/{h2.Id}", Horario("entrada", "15:00", -40, 2));
        Assert.Equal(-40m, ok.Json.GetProperty("ajuste").GetDecimal());
        Assert.Equal(404, (await loja.Painel.Put("/api/chacara/horarios/999999", Horario("entrada", "10:00"))).Codigo);

        Assert.Equal(200, (await loja.Painel.Delete($"/api/chacara/horarios/{h2.Id}")).Codigo);
        Assert.Equal(404, (await loja.Painel.Delete($"/api/chacara/horarios/{h2.Id}")).Codigo);
    }

    // ── Períodos especiais ───────────────────────────────────────────────────

    private static object Periodo(string nome, DateTime ini, DateTime fim, decimal valor = 5000) =>
        new { nome, dataInicio = Datas.Texto(ini), dataFim = Datas.Texto(fim), valorTotal = valor };

    [FatoIntegracao]
    public async Task Periodos_especiais_validam_nome_e_datas()
    {
        var loja = await api.NovaLojaAsync();
        var ini = Datas.Sexta(10);

        Assert.Equal("Informe um nome para o período.", (await loja.Painel.Post("/api/chacara/periodos-especiais", Periodo("  ", ini, ini.AddDays(2)))).Erro);
        Assert.Equal("Data final não pode ser antes da data inicial.", (await loja.Painel.Post("/api/chacara/periodos-especiais", Periodo("Natal", ini.AddDays(2), ini))).Erro);
        Assert.Equal(404, (await loja.Painel.Put("/api/chacara/periodos-especiais/999999", Periodo("X", ini, ini.AddDays(1)))).Codigo);
    }

    [FatoIntegracao]
    public async Task Periodos_especiais_criar_editar_e_listar_com_datas_ao_meio_dia_UTC()
    {
        var loja = await api.NovaLojaAsync();
        var ini = Datas.Sexta(10);

        var criado = await loja.Painel.Post("/api/chacara/periodos-especiais", Periodo("  Natal ", ini, ini.AddDays(2)));
        Assert.Equal(200, criado.Codigo);
        Assert.Equal("Natal", criado.Json.GetProperty("nome").GetString());
        Assert.Equal($"{Datas.Texto(ini)}T12:00:00Z", criado.Json.GetProperty("dataInicio").GetString());

        var editado = await loja.Painel.Put($"/api/chacara/periodos-especiais/{criado.Id}", Periodo(" Natal 2030 ", ini.AddDays(-1), ini.AddDays(2), 5500));
        Assert.Equal("Natal 2030", editado.Json.GetProperty("nome").GetString());
        Assert.Equal(5500m, editado.Json.GetProperty("valorTotal").GetDecimal());

        var lista = (await loja.Painel.Get("/api/chacara/periodos-especiais")).Json;
        Assert.Equal(1, lista.GetArrayLength());
        Assert.False(lista[0].GetProperty("confirmado").GetBoolean()); // sem reserva confirmada no período
    }

    // ── Fotos ────────────────────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Fotos_adicionar_reordenar_e_remover()
    {
        var loja = await api.NovaLojaAsync();
        Assert.Equal("URL inválida.", (await loja.Painel.Post("/api/chacara/fotos", new { url = "   " })).Erro);

        var ids = new List<int>();
        for (var i = 1; i <= 3; i++)
        {
            var foto = await loja.Painel.Post("/api/chacara/fotos", new { url = $" https://img.test/foto{i}.jpg " });
            Assert.Equal($"https://img.test/foto{i}.jpg", foto.Json.GetProperty("url").GetString());
            ids.Add(foto.Id);
        }

        // reordena: a última vira a primeira (ids que não são da loja são ignorados)
        Assert.Equal(200, (await loja.Painel.Put("/api/chacara/fotos/ordem", new { ids = new[] { ids[2], ids[0], ids[1], 99999999 } })).Codigo);
        var urls = (await loja.Painel.Get("/api/chacara/fotos")).Json.EnumerateArray().Select(f => f.GetProperty("url").GetString()).ToList();
        Assert.Equal(["https://img.test/foto3.jpg", "https://img.test/foto1.jpg", "https://img.test/foto2.jpg"], urls);

        Assert.Equal(200, (await loja.Painel.Delete($"/api/chacara/fotos/{ids[1]}")).Codigo);
        Assert.Equal(404, (await loja.Painel.Delete($"/api/chacara/fotos/{ids[1]}")).Codigo);
        Assert.Equal(2, (await loja.Painel.Get("/api/chacara/fotos")).Json.GetArrayLength());
    }

    [FatoIntegracao]
    public async Task Fotos_tem_limite_por_loja()
    {
        var loja = await api.NovaLojaAsync();
        for (var i = 0; i < 30; i++)
            Assert.Equal(200, (await loja.Painel.Post("/api/chacara/fotos", new { url = $"https://img.test/{i}.jpg" })).Codigo);

        Assert.Equal(400, (await loja.Painel.Post("/api/chacara/fotos", new { url = "https://img.test/31.jpg" })).Codigo);
    }

    // ── Pagamento (Mercado Pago) ─────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Pagamento_comeca_nao_configurado_e_exige_as_duas_chaves()
    {
        var loja = await api.NovaLojaAsync();
        var estado = (await loja.Painel.Get("/api/chacara/info/pagamento")).Json;
        Assert.False(estado.GetProperty("configurado").GetBoolean());

        var vazio = await loja.Painel.Put("/api/chacara/info/pagamento", new { accessToken = "", publicKey = "" });
        Assert.Equal(400, vazio.Codigo);
        Assert.Equal("Informe o Access Token e a Public Key do Mercado Pago.", vazio.Erro);

        Assert.False((await loja.Painel.Delete("/api/chacara/info/pagamento")).Json.GetProperty("configurado").GetBoolean());
    }

    [FatoIntegracao]
    public async Task Pagamento_configurado_nunca_devolve_o_access_token_inteiro()
    {
        var loja = await api.NovaLojaAsync();
        const string token = "APP_USR-1234567890-segredo-ABCD";

        var salvo = await loja.Painel.Put("/api/chacara/info/pagamento", new { accessToken = token, publicKey = "APP_USR-pub-1" });
        Assert.Equal(200, salvo.Codigo);

        var lido = await loja.Painel.Get("/api/chacara/info/pagamento");
        Assert.True(lido.Json.GetProperty("configurado").GetBoolean());
        Assert.DoesNotContain(token, lido.Corpo);
        Assert.DoesNotContain(token, salvo.Corpo);

        await loja.Painel.Delete("/api/chacara/info/pagamento");
        Assert.False((await loja.Painel.Get("/api/chacara/info/pagamento")).Json.GetProperty("configurado").GetBoolean());
    }
}
