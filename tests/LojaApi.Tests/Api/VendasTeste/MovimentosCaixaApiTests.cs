using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace LojaApi.Tests.Api.VendasTeste;

// Movimentos de caixa (reforço e sangria), com espelho opcional como ajuste na conta bancária do Financeiro.
[Collection(ColecaoApi.Nome)]
public class MovimentosCaixaApiTests(ApiFixture api)
{
    private const string Rota = "/api/movimentos-caixa";

    private Task<LojaTeste> NovaAsync() => api.NovaLojaDeServicosAsync();

    private static object Corpo(string tipo = "entrada", decimal valor = 100m, DateTime? data = null, Guid? origem = null, string? obs = null, Guid? conta = null) =>
        new { tipo, valor, data = data ?? DateTime.UtcNow.Date, origemVendaId = origem, observacao = obs, contaBancariaId = conta };

    private static Guid Id(JsonElement e) => e.GetProperty("id").GetGuid();

    [FatoIntegracao]
    public async Task Exige_login()
    {
        var loja = await NovaAsync();
        Assert.Equal(401, (await loja.Site.Get(Rota)).Codigo);
        Assert.Equal(401, (await loja.Site.Post(Rota, Corpo())).Codigo);
        Assert.Equal(401, (await loja.Site.Put($"{Rota}/{Guid.NewGuid()}", Corpo())).Codigo);
        Assert.Equal(401, (await loja.Site.Delete($"{Rota}/{Guid.NewGuid()}")).Codigo);
    }

    [FatoIntegracao]
    public async Task Criar_valida_tipo_valor_e_data()
    {
        var loja = await NovaAsync();

        async Task<(int, string)> Tenta(object corpo) { var r = await loja.Painel.Post(Rota, corpo); return (r.Codigo, r.Erro); }

        Assert.Equal((400, "Tipo inválido."), await Tenta(Corpo("transferencia")));
        Assert.Equal((400, "Informe um valor maior que zero."), await Tenta(Corpo(valor: 0)));
        Assert.Equal((400, "Informe um valor maior que zero."), await Tenta(Corpo(valor: -5)));
        Assert.Equal((400, "A data não pode ser no futuro."), await Tenta(Corpo(data: DateTime.UtcNow.Date.AddDays(1))));
        Assert.Equal(0, (await loja.Painel.Get(Rota)).Json.GetArrayLength());
    }

    [FatoIntegracao]
    public async Task Criar_grava_a_data_ao_meio_dia_utc_e_o_nome_da_origem()
    {
        var loja = await NovaAsync();
        var origem = (await loja.Painel.Post("/api/origens-venda", new { nome = "Feira", ordem = 1, ativa = true })).Json.GetProperty("id").GetGuid();
        var dia = DateTime.UtcNow.Date.AddDays(-4);

        var r = await loja.Painel.Post(Rota, Corpo("saida", 35.5m, dia, origem, "Sangria da tarde"));

        Assert.Equal(200, r.Codigo);
        var j = r.Json;
        Assert.Equal(("saida", 35.5m, "Feira", "Sangria da tarde", origem), (j.GetProperty("tipo").GetString(), j.GetProperty("valor").GetDecimal(), j.GetProperty("origemNome").GetString(),
            j.GetProperty("observacao").GetString(), j.GetProperty("origemVendaId").GetGuid()));
        Assert.Equal(dia.AddHours(12), j.GetProperty("data").GetDateTime().ToUniversalTime());
        Assert.Equal(JsonValueKind.Null, j.GetProperty("ajusteContaBancariaId").ValueKind);
        var gravado = await api.NoBancoAsync(db => db.MovimentosCaixa.AsNoTracking().SingleAsync(m => m.Id == Id(j)));
        Assert.Equal(loja.Id, gravado.LojaId);
    }

    [FatoIntegracao]
    public async Task Criar_com_conta_bancaria_espelha_um_ajuste_com_a_descricao_certa()
    {
        var loja = await NovaAsync();
        var outra = await NovaAsync();
        var conta = await loja.ContaBancariaIdAsync("Caixa");
        var contaAlheia = await outra.ContaBancariaIdAsync("Alheia");
        var origem = (await loja.Painel.Post("/api/origens-venda", new { nome = "Balcão", ordem = 1, ativa = true })).Json.GetProperty("id").GetGuid();

        var reforco = await loja.Painel.Post(Rota, Corpo("entrada", 200m, null, origem, "troco inicial", conta));
        var sangria = await loja.Painel.Post(Rota, Corpo("saida", 50m, null, null, null, conta));
        var semEspelho = await loja.Painel.Post(Rota, Corpo("entrada", 10m, null, null, null, contaAlheia));     // conta de outra loja: não espelha

        var ajusteReforco = await api.NoBancoAsync(db => db.AjustesContaBancaria.AsNoTracking().SingleAsync(a => a.Id == reforco.Json.GetProperty("ajusteContaBancariaId").GetGuid()));
        Assert.Equal(("entrada", 200m, conta, loja.Id, "Reforço de caixa — Balcão (troco inicial)"), (ajusteReforco.Tipo, ajusteReforco.Valor, ajusteReforco.ContaBancariaId, ajusteReforco.LojaId, ajusteReforco.Observacao));
        var ajusteSangria = await api.NoBancoAsync(db => db.AjustesContaBancaria.AsNoTracking().SingleAsync(a => a.Id == sangria.Json.GetProperty("ajusteContaBancariaId").GetGuid()));
        Assert.Equal(("saida", "Sangria de caixa"), (ajusteSangria.Tipo, ajusteSangria.Observacao));
        Assert.Equal(JsonValueKind.Null, semEspelho.Json.GetProperty("ajusteContaBancariaId").ValueKind);
        Assert.Equal(0, await api.NoBancoAsync(db => db.AjustesContaBancaria.CountAsync(a => a.ContaBancariaId == contaAlheia)));
    }

    [FatoIntegracao]
    public async Task Listar_filtra_por_periodo_da_loja_do_mais_novo_para_o_mais_antigo()
    {
        var loja = await NovaAsync();
        var outra = await NovaAsync();
        var hoje = DateTime.UtcNow.Date;
        var velho = Id((await loja.Painel.Post(Rota, Corpo(data: hoje.AddDays(-10)))).Json);
        var meio = Id((await loja.Painel.Post(Rota, Corpo("saida", 20m, hoje.AddDays(-3)))).Json);
        var novo = Id((await loja.Painel.Post(Rota, Corpo(data: hoje))).Json);
        await outra.Painel.Post(Rota, Corpo());

        async Task<List<Guid>> Ids(string q) => (await loja.Painel.Get(Rota + q)).Json.EnumerateArray().Select(Id).ToList();
        string D(int dias) => Uri.EscapeDataString(hoje.AddDays(dias).ToString("yyyy-MM-dd"));

        Assert.Equal([novo, meio, velho], await Ids(""));
        Assert.Equal([novo, meio], await Ids($"?de={D(-5)}"));
        Assert.Equal([meio, velho], await Ids($"?ate={D(-3)}"));
        Assert.Equal([meio], await Ids($"?de={D(-5)}&ate={D(-3)}"));
        var item = (await loja.Painel.Get(Rota)).Json[1];
        Assert.Equal(("saida", 20m), (item.GetProperty("tipo").GetString(), item.GetProperty("valor").GetDecimal()));
    }

    [FatoIntegracao]
    public async Task Editar_valida_recria_o_espelho_e_so_edita_da_propria_loja()
    {
        var loja = await NovaAsync();
        var outra = await NovaAsync();
        var conta = await loja.ContaBancariaIdAsync("Caixa");
        var criado = (await loja.Painel.Post(Rota, Corpo("entrada", 100m, null, null, "inicial", conta))).Json;
        var id = Id(criado);
        var ajusteAntigo = criado.GetProperty("ajusteContaBancariaId").GetGuid();

        Assert.Equal(404, (await loja.Painel.Put($"{Rota}/{Guid.NewGuid()}", Corpo())).Codigo);
        Assert.Equal(404, (await outra.Painel.Put($"{Rota}/{id}", Corpo())).Codigo);
        Assert.Equal("Tipo inválido.", (await loja.Painel.Put($"{Rota}/{id}", Corpo("x"))).Erro);
        Assert.Equal("Informe um valor maior que zero.", (await loja.Painel.Put($"{Rota}/{id}", Corpo(valor: 0))).Erro);
        Assert.Equal("A data não pode ser no futuro.", (await loja.Painel.Put($"{Rota}/{id}", Corpo(data: DateTime.UtcNow.Date.AddDays(1)))).Erro);

        var editado = await loja.Painel.Put($"{Rota}/{id}", Corpo("saida", 60m, DateTime.UtcNow.Date.AddDays(-1), null, "depois", conta));

        Assert.Equal(200, editado.Codigo);
        Assert.Equal(("saida", 60m), (editado.Json.GetProperty("tipo").GetString(), editado.Json.GetProperty("valor").GetDecimal()));
        var novoAjuste = editado.Json.GetProperty("ajusteContaBancariaId").GetGuid();
        Assert.NotEqual(ajusteAntigo, novoAjuste);
        Assert.Equal(0, await api.NoBancoAsync(db => db.AjustesContaBancaria.CountAsync(a => a.Id == ajusteAntigo)));       // o espelho antigo some
        var ajuste = await api.NoBancoAsync(db => db.AjustesContaBancaria.AsNoTracking().SingleAsync(a => a.Id == novoAjuste));
        Assert.Equal(("saida", 60m, "Sangria de caixa (depois)"), (ajuste.Tipo, ajuste.Valor, ajuste.Observacao));

        var semConta = await loja.Painel.Put($"{Rota}/{id}", Corpo("entrada", 10m));
        Assert.Equal(JsonValueKind.Null, semConta.Json.GetProperty("ajusteContaBancariaId").ValueKind);
        Assert.Equal(JsonValueKind.Null, semConta.Json.GetProperty("contaBancariaId").ValueKind);
        Assert.Equal(0, await api.NoBancoAsync(db => db.AjustesContaBancaria.CountAsync(a => a.Id == novoAjuste)));
    }

    [FatoIntegracao]
    public async Task Excluir_remove_o_movimento_e_o_espelho_e_so_da_propria_loja()
    {
        var loja = await NovaAsync();
        var outra = await NovaAsync();
        var conta = await loja.ContaBancariaIdAsync("Caixa");
        var criado = (await loja.Painel.Post(Rota, Corpo("entrada", 100m, null, null, null, conta))).Json;
        var id = Id(criado);
        var ajuste = criado.GetProperty("ajusteContaBancariaId").GetGuid();

        Assert.Equal(404, (await outra.Painel.Delete($"{Rota}/{id}")).Codigo);
        var r = await loja.Painel.Delete($"{Rota}/{id}");

        Assert.Equal((200, "Movimento excluído."), (r.Codigo, r.Json.GetProperty("mensagem").GetString()));
        Assert.Equal(0, await api.NoBancoAsync(db => db.MovimentosCaixa.CountAsync(m => m.Id == id)));
        Assert.Equal(0, await api.NoBancoAsync(db => db.AjustesContaBancaria.CountAsync(a => a.Id == ajuste)));
        Assert.Equal(404, (await loja.Painel.Delete($"{Rota}/{id}")).Codigo);
    }
}
