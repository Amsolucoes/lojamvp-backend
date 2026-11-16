using Microsoft.EntityFrameworkCore;
using static LojaApi.Tests.Suporte.CenariosFinanceiro;

namespace LojaApi.Tests.Api.FinanceiroTeste;

// Financeiro, parte 3a: cartões de crédito, compras (avulsa e parcelada), fixos do cartão, edição e exclusão de compras.
[Collection(ColecaoApi.Nome)]
public class FinanceiroCartoesApiTests(ApiFixture api)
{
    private Task<LojaTeste> NovaAsync() => api.NovaLojaAsync();

    private static async Task<(int, string)> Erro(Task<Resposta> r) { var x = await r; return (x.Codigo, x.Erro); }

    private static DateTime Utc(int ano, int mes, int dia, int hora = 12) => new(ano, mes, dia, hora, 0, 0, DateTimeKind.Utc);

    private static DateTime MesAtual() { var h = DateTime.UtcNow; return new DateTime(h.Year, h.Month, 1, 0, 0, 0, DateTimeKind.Utc); }

    private async Task<(LojaTeste Loja, Guid Conta, Guid Cartao)> CenarioAsync()
    {
        var loja = await NovaAsync();
        var conta = await loja.ContaPelaApiAsync();
        return (loja, conta, await loja.CartaoPelaApiAsync(conta));
    }

    [FatoIntegracao]
    public async Task Exige_login_mas_qualquer_papel_da_loja_usa_os_cartoes()
    {
        var (loja, conta, cartao) = await CenarioAsync();
        var operador = await api.OperadorAsync(loja);

        Assert.Equal(401, (await loja.Site.Get($"{Rota}/cartoes")).Codigo);
        Assert.Equal(401, (await loja.Site.Post($"{Rota}/cartoes/{cartao}/lancamentos", new { })).Codigo);
        Assert.Equal(401, (await loja.Site.Get($"{Rota}/cartoes/lancamentos/descricoes")).Codigo);
        Assert.Equal(401, (await loja.Site.Delete($"{Rota}/cartoes/lancamentos/{Guid.NewGuid()}")).Codigo);
        Assert.Equal(200, (await operador.Get($"{Rota}/cartoes")).Codigo);
        Assert.Equal(200, (await operador.Post($"{Rota}/cartoes", new { nome = "Do operador", limite = 1m, diaFechamento = 1, diaVencimento = 5, contaBancariaId = conta })).Codigo);
    }

    // ── Cartão ───────────────────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Criar_cartao_corta_o_nome_normaliza_a_bandeira_e_sem_loja_devolve_400()
    {
        var loja = await NovaAsync();
        var conta = await loja.ContaPelaApiAsync();

        var r = await loja.Painel.Post($"{Rota}/cartoes", new { nome = "  Nubank  ", limite = 5000m, diaFechamento = 3, diaVencimento = 10, contaBancariaId = conta, taxaJurosMensal = 12.5m, bandeira = " MasterCard " });

        Assert.Equal(200, r.Codigo);
        Assert.Equal(["id"], r.Json.EnumerateObject().Select(x => x.Name));
        var c = await api.CartaoNoBancoPorIdAsync(r.Json.GetProperty("id").GetGuid());
        Assert.Equal((loja.Id, "Nubank", 5000m, 3, 10, conta, 12.5m, "mastercard", true), (c.LojaId, c.Nome, c.Limite, c.DiaFechamento, c.DiaVencimento, c.ContaBancariaId, c.TaxaJurosMensal, c.Bandeira, c.Ativo));

        var invalida = await loja.Painel.Post($"{Rota}/cartoes", new { nome = "Outro", limite = 1m, diaFechamento = 1, diaVencimento = 5, contaBancariaId = conta, bandeira = "paypal" });
        Assert.Null((await api.CartaoNoBancoPorIdAsync(invalida.Json.GetProperty("id").GetGuid())).Bandeira);

        await api.SemVinculoAsync(loja);
        Assert.Equal((400, "Loja não encontrada."), await Erro(loja.Painel.Post($"{Rota}/cartoes", new { nome = "x", limite = 1m, diaFechamento = 1, diaVencimento = 5, contaBancariaId = conta })));
    }

    [FatoIntegracao]
    public async Task Listar_cartoes_so_da_loja_e_sem_loja_devolve_lista_vazia()
    {
        var (loja, conta, cartao) = await CenarioAsync();
        var outra = await NovaAsync();
        await outra.CartaoPelaApiAsync(await outra.ContaPelaApiAsync(), "Alheio");

        var lista = (await loja.Painel.Get($"{Rota}/cartoes")).Json.EnumerateArray().ToList();

        Assert.Equal([cartao], lista.Select(x => x.GetProperty("id").GetGuid()));
        Assert.Equal(["id", "nome", "limite", "diaFechamento", "diaVencimento", "contaBancariaId", "ativo", "taxaJurosMensal", "bandeira"], lista[0].EnumerateObject().Select(x => x.Name));
        Assert.Equal(("Cartão Teste", 1000m, 10, 15, conta, true), (lista[0].GetProperty("nome").GetString()!, lista[0].GetProperty("limite").GetDecimal(),
            lista[0].GetProperty("diaFechamento").GetInt32(), lista[0].GetProperty("diaVencimento").GetInt32(), lista[0].GetProperty("contaBancariaId").GetGuid(), lista[0].GetProperty("ativo").GetBoolean()));

        await api.SemVinculoAsync(loja);
        Assert.Empty((await loja.Painel.Get($"{Rota}/cartoes")).Json.EnumerateArray());
    }

    [FatoIntegracao]
    public async Task Atualizar_cartao_troca_os_dados_e_so_na_propria_loja()
    {
        var (loja, conta, cartao) = await CenarioAsync();
        var outra = await NovaAsync();
        var nova = await loja.ContaPelaApiAsync("Nova");

        var r = await loja.Painel.Put($"{Rota}/cartoes/{cartao}", new { nome = " Itaú ", limite = 2000m, diaFechamento = 5, diaVencimento = 12, contaBancariaId = nova, taxaJurosMensal = 8m, bandeira = "VISA" });

        Assert.Equal(200, r.Codigo);
        Assert.Equal(cartao, r.Json.GetProperty("id").GetGuid());
        var c = await api.CartaoNoBancoPorIdAsync(cartao);
        Assert.Equal(("Itaú", 2000m, 5, 12, nova, 8m, "visa"), (c.Nome, c.Limite, c.DiaFechamento, c.DiaVencimento, c.ContaBancariaId, c.TaxaJurosMensal, c.Bandeira));
        Assert.Equal(404, (await outra.Painel.Put($"{Rota}/cartoes/{cartao}", new { nome = "x", limite = 1m, diaFechamento = 1, diaVencimento = 5, contaBancariaId = conta })).Codigo);
        Assert.Equal("Itaú", (await api.CartaoNoBancoPorIdAsync(cartao)).Nome);
        Assert.Equal(404, (await loja.Painel.Put($"{Rota}/cartoes/{Guid.NewGuid()}", new { nome = "x", limite = 1m, diaFechamento = 1, diaVencimento = 5, contaBancariaId = conta })).Codigo);
    }

    // ── Compras ──────────────────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Lancar_compra_grava_ao_meio_dia_com_descricao_cortada_e_so_no_proprio_cartao()
    {
        var (loja, _, cartao) = await CenarioAsync();
        var outra = await NovaAsync();
        var categoria = (await loja.Painel.Post($"{Rota}/categorias", new { nome = "Mercado", tipo = "pagar" })).Json.GetProperty("id").GetGuid();

        var r = await loja.Painel.Post($"{Rota}/cartoes/{cartao}/lancamentos", new { descricao = "  Supermercado  ", valor = 123.45m, dataCompra = "2032-03-10T23:30:00", categoriaId = categoria, observacao = "obs" });

        Assert.Equal(200, r.Codigo);
        Assert.Equal("Lançamento adicionado.", r.Json.GetProperty("mensagem").GetString());
        var compra = Assert.Single(await api.ComprasDoCartaoNoBancoAsync(cartao));
        Assert.Equal((loja.Id, "Supermercado", 123.45m, Utc(2032, 3, 10), "avulsa", categoria, "obs"), (compra.LojaId, compra.Descricao, compra.Valor, compra.DataCompra, compra.Modo, compra.CategoriaId, compra.Observacao));
        Assert.Equal(404, (await outra.Painel.Post($"{Rota}/cartoes/{cartao}/lancamentos", new { descricao = "x", valor = 1m, dataCompra = "2032-03-10" })).Codigo);
        Assert.Equal(404, (await loja.Painel.Post($"{Rota}/cartoes/{Guid.NewGuid()}/lancamentos", new { descricao = "x", valor = 1m, dataCompra = "2032-03-10" })).Codigo);
        Assert.Single(await api.ComprasDoCartaoNoBancoAsync(cartao));
    }

    [FatoIntegracao]
    public async Task Lancar_compra_parcelada_gera_uma_parcela_por_mes_numerada_no_mesmo_grupo_e_valida_de_2_a_24()
    {
        var (loja, _, cartao) = await CenarioAsync();
        var outra = await NovaAsync();
        object Corpo(int total) => new { descricao = " TV ", valorParcela = 100m, totalParcelas = total, dataCompra = "2032-01-31", observacao = "o" };

        var r = await loja.Painel.Post($"{Rota}/cartoes/{cartao}/lancamentos/parcelado", Corpo(3));

        Assert.Equal(200, r.Codigo);
        Assert.Equal(["grupoParcelamentoId", "totalGerado"], r.Json.EnumerateObject().Select(x => x.Name));
        Assert.Equal(3, r.Json.GetProperty("totalGerado").GetInt32());
        var compras = await api.ComprasDoCartaoNoBancoAsync(cartao);
        Assert.Equal(["TV (1/3)", "TV (2/3)", "TV (3/3)"], compras.Select(c => c.Descricao));
        Assert.Equal([Utc(2032, 1, 31), Utc(2032, 2, 29), Utc(2032, 3, 31)], compras.Select(c => c.DataCompra));
        Assert.All(compras, c => Assert.Equal(("parcelada", 100m, 3, "o"), (c.Modo, c.Valor, c.TotalParcelas!.Value, c.Observacao)));
        Assert.Equal([1, 2, 3], compras.Select(c => c.NumeroParcela!.Value));
        Assert.Equal([r.Json.GetProperty("grupoParcelamentoId").GetGuid()], compras.Select(c => c.GrupoParcelamentoId!.Value).Distinct());

        Assert.Equal((400, "Escolha entre 2 e 24 parcelas."), await Erro(loja.Painel.Post($"{Rota}/cartoes/{cartao}/lancamentos/parcelado", Corpo(1))));
        Assert.Equal((400, "Escolha entre 2 e 24 parcelas."), await Erro(loja.Painel.Post($"{Rota}/cartoes/{cartao}/lancamentos/parcelado", Corpo(25))));
        Assert.Equal(200, (await loja.Painel.Post($"{Rota}/cartoes/{cartao}/lancamentos/parcelado", Corpo(24))).Codigo);
        Assert.Equal(404, (await outra.Painel.Post($"{Rota}/cartoes/{cartao}/lancamentos/parcelado", Corpo(3))).Codigo);
        Assert.Equal(27, (await api.ComprasDoCartaoNoBancoAsync(cartao)).Count);
    }

    [FatoIntegracao]
    public async Task Descricoes_recentes_do_cartao_sao_distintas_e_so_da_loja()
    {
        var (loja, _, cartao) = await CenarioAsync();
        var (outra, _, cartaoAlheio) = await CenarioAsync();
        await loja.Painel.Post($"{Rota}/cartoes/{cartao}/lancamentos", new { descricao = "Padaria", valor = 1m, dataCompra = "2032-03-10" });
        await loja.Painel.Post($"{Rota}/cartoes/{cartao}/lancamentos", new { descricao = "Farmácia", valor = 1m, dataCompra = "2032-03-10" });
        await loja.Painel.Post($"{Rota}/cartoes/{cartao}/lancamentos", new { descricao = "Padaria", valor = 1m, dataCompra = "2032-03-11" });
        await outra.Painel.Post($"{Rota}/cartoes/{cartaoAlheio}/lancamentos", new { descricao = "Alheia", valor = 1m, dataCompra = "2032-03-10" });

        var lista = (await loja.Painel.Get($"{Rota}/cartoes/lancamentos/descricoes")).Json.EnumerateArray().Select(x => x.GetString()).ToList();

        Assert.Equal(["Farmácia", "Padaria"], lista.Order().ToList());
        Assert.Equal(2, lista.Count);

        await api.SemVinculoAsync(loja);
        Assert.Empty((await loja.Painel.Get($"{Rota}/cartoes/lancamentos/descricoes")).Json.EnumerateArray());
    }

    // ── Fixos do cartão ──────────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Criar_fixo_do_cartao_gera_60_compras_a_partir_do_mes_atual_e_corrige_o_dia_fora_de_1_a_28()
    {
        var (loja, _, cartao) = await CenarioAsync();
        var inicio = MesAtual();

        var r = await loja.Painel.Post($"{Rota}/cartoes/{cartao}/fixos", new { descricao = " Netflix ", valor = 39.9m, observacao = "o", diaCompra = 20 });

        Assert.Equal(200, r.Codigo);
        Assert.Equal(["id", "geradoAte"], r.Json.EnumerateObject().Select(x => x.Name));
        Assert.Equal(inicio.AddMonths(59), r.Json.GetProperty("geradoAte").GetDateTime().ToUniversalTime());
        var fixo = Assert.Single(await api.FixosDoCartaoNoBancoAsync(cartao));
        Assert.Equal((r.Json.GetProperty("id").GetGuid(), loja.Id, "Netflix", 39.9m, 20, true), (fixo.Id, fixo.LojaId, fixo.Descricao, fixo.Valor, fixo.DiaCompra, fixo.Ativo));
        var compras = await api.ComprasDoCartaoNoBancoAsync(cartao);
        Assert.Equal(60, compras.Count);
        Assert.All(compras, c => Assert.Equal(("fixa", "Netflix", 39.9m, fixo.Id, 20, 12), (c.Modo, c.Descricao, c.Valor, c.CartaoFixoId!.Value, c.DataCompra.Day, c.DataCompra.Hour)));
        Assert.Equal((inicio.Year, inicio.Month), (compras[0].DataCompra.Year, compras[0].DataCompra.Month));

        await loja.Painel.Post($"{Rota}/cartoes/{cartao}/fixos", new { descricao = "Zero", valor = 1m, diaCompra = 0 });
        await loja.Painel.Post($"{Rota}/cartoes/{cartao}/fixos", new { descricao = "Trinta", valor = 1m, diaCompra = 30 });
        var fixos = await api.FixosDoCartaoNoBancoAsync(cartao);
        Assert.Equal([1, 1], fixos.Where(f => f.Descricao is "Zero" or "Trinta").Select(f => f.DiaCompra));
        Assert.Equal(404, (await loja.Painel.Post($"{Rota}/cartoes/{Guid.NewGuid()}/fixos", new { descricao = "x", valor = 1m })).Codigo);
    }

    [FatoIntegracao]
    public async Task Listar_fixos_do_cartao_e_alternar_desativa_e_apaga_as_compras_do_mes_atual_em_diante()
    {
        var (loja, _, cartao) = await CenarioAsync();
        var outra = await NovaAsync();
        var id = (await loja.Painel.Post($"{Rota}/cartoes/{cartao}/fixos", new { descricao = "Spotify", valor = 20m, diaCompra = 5 })).Json.GetProperty("id").GetGuid();

        var lista = (await loja.Painel.Get($"{Rota}/cartoes/{cartao}/fixos")).Json.EnumerateArray().ToList();
        Assert.Equal(["id", "descricao", "valor", "ativo"], lista.Single().EnumerateObject().Select(x => x.Name));
        Assert.Equal(("Spotify", 20m, true), (lista[0].GetProperty("descricao").GetString()!, lista[0].GetProperty("valor").GetDecimal(), lista[0].GetProperty("ativo").GetBoolean()));
        Assert.Empty((await outra.Painel.Get($"{Rota}/cartoes/{cartao}/fixos")).Json.EnumerateArray());

        Assert.Equal(404, (await outra.Painel.Patch($"{Rota}/cartoes/fixos/{id}/ativo")).Codigo);
        var off = await loja.Painel.Patch($"{Rota}/cartoes/fixos/{id}/ativo");
        Assert.Equal(["id", "ativo"], off.Json.EnumerateObject().Select(x => x.Name));
        Assert.False(off.Json.GetProperty("ativo").GetBoolean());
        Assert.Empty(await api.ComprasDoCartaoNoBancoAsync(cartao));

        var on = await loja.Painel.Patch($"{Rota}/cartoes/fixos/{id}/ativo");
        Assert.True(on.Json.GetProperty("ativo").GetBoolean());
        Assert.Empty(await api.ComprasDoCartaoNoBancoAsync(cartao));
        Assert.Equal(404, (await loja.Painel.Patch($"{Rota}/cartoes/fixos/{Guid.NewGuid()}/ativo")).Codigo);
    }

    // ── Editar compra ────────────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Editar_compra_unica_troca_os_dados_e_a_data_so_desta_compra()
    {
        var (loja, _, cartao) = await CenarioAsync();
        var outra = await NovaAsync();
        var categoria = (await loja.Painel.Post($"{Rota}/categorias", new { nome = "Casa", tipo = "pagar" })).Json.GetProperty("id").GetGuid();
        await loja.Painel.Post($"{Rota}/cartoes/{cartao}/lancamentos/parcelado", new { descricao = "Sofá", valorParcela = 50m, totalParcelas = 3, dataCompra = "2032-01-05" });
        var compras = await api.ComprasDoCartaoNoBancoAsync(cartao);

        var r = await loja.Painel.Put($"{Rota}/cartoes/lancamentos/{compras[1].Id}?modo=unica", new { descricao = " Sofá novo ", valor = 70m, dataCompra = "2032-02-20T23:00:00", categoriaId = categoria, observacao = "ignorada" });

        Assert.Equal(200, r.Codigo);
        Assert.Equal("Lançamento atualizado.", r.Json.GetProperty("mensagem").GetString());
        var depois = (await api.ComprasDoCartaoNoBancoAsync(cartao)).ToDictionary(c => c.Id);
        var editada = depois[compras[1].Id];
        Assert.Equal(("Sofá novo", 70m, Utc(2032, 2, 20), categoria, null), (editada.Descricao, editada.Valor, editada.DataCompra, editada.CategoriaId, editada.Observacao));
        Assert.Equal([50m, 50m], new[] { depois[compras[0].Id].Valor, depois[compras[2].Id].Valor });
        Assert.Equal(404, (await outra.Painel.Put($"{Rota}/cartoes/lancamentos/{compras[1].Id}?modo=unica", new { descricao = "x", valor = 1m, dataCompra = "2032-02-20" })).Codigo);
        Assert.Equal(404, (await loja.Painel.Put($"{Rota}/cartoes/lancamentos/{Guid.NewGuid()}?modo=unica", new { descricao = "x", valor = 1m, dataCompra = "2032-02-20" })).Codigo);
    }

    [FatoIntegracao]
    public async Task Editar_parcelada_em_todas_muda_so_as_parcelas_de_faturas_ainda_nao_pagas_mantendo_o_numero()
    {
        var (loja, _, cartao) = await CenarioAsync();
        await loja.Painel.Post($"{Rota}/cartoes/{cartao}/lancamentos/parcelado", new { descricao = "Sofá", valorParcela = 50m, totalParcelas = 3, dataCompra = "2032-01-05" });
        var compras = await api.ComprasDoCartaoNoBancoAsync(cartao);
        await api.FaturaPagaDoMesNoBancoAsync(loja.Id, cartao, 2032, 2);
        await api.FaturaPagaDoMesNoBancoAsync(loja.Id, cartao, 2032, 3, "parcial"); // só fatura paga trava as parcelas

        await loja.Painel.Put($"{Rota}/cartoes/lancamentos/{compras[2].Id}?modo=todas", new { descricao = " Sofá L ", valor = 90m, dataCompra = "2035-05-05" });

        var depois = await api.ComprasDoCartaoNoBancoAsync(cartao);
        Assert.Equal(["Sofá L (1/3)", "Sofá (2/3)", "Sofá L (3/3)"], depois.Select(c => c.Descricao));
        Assert.Equal([90m, 50m, 90m], depois.Select(c => c.Valor));
        Assert.Equal([Utc(2032, 1, 5), Utc(2032, 2, 5), Utc(2032, 3, 5)], depois.Select(c => c.DataCompra));
    }

    [FatoIntegracao]
    public async Task Editar_fixo_em_todas_muda_o_modelo_e_regera_as_compras_futuras()
    {
        var (loja, _, cartao) = await CenarioAsync();
        var id = (await loja.Painel.Post($"{Rota}/cartoes/{cartao}/fixos", new { descricao = "Netflix", valor = 40m, observacao = "o", diaCompra = 5 })).Json.GetProperty("id").GetGuid();
        var proximo = MesAtual().AddMonths(1);
        var item = (await api.ComprasDoCartaoNoBancoAsync(cartao)).Single(c => c.DataCompra.Year == proximo.Year && c.DataCompra.Month == proximo.Month);

        await loja.Painel.Put($"{Rota}/cartoes/lancamentos/{item.Id}?modo=todas", new { descricao = " Netflix 4K ", valor = 60m, dataCompra = "2032-01-18", observacao = "novo" });

        var fixo = Assert.Single(await api.FixosDoCartaoNoBancoAsync(cartao));
        Assert.Equal(("Netflix 4K", 60m, 18, "novo"), (fixo.Descricao, fixo.Valor, fixo.DiaCompra, fixo.Observacao));
        var compras = await api.ComprasDoCartaoNoBancoAsync(cartao);
        Assert.Equal(60, compras.Count);
        var doProximo = compras.Single(c => c.DataCompra.Year == proximo.Year && c.DataCompra.Month == proximo.Month);
        Assert.Equal(("Netflix 4K", 60m, 18), (doProximo.Descricao, doProximo.Valor, doProximo.DataCompra.Day));
    }

    // ── Excluir compra ───────────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Excluir_compra_unica_remove_so_ela_e_so_da_propria_loja()
    {
        var (loja, _, cartao) = await CenarioAsync();
        var outra = await NovaAsync();
        await loja.Painel.Post($"{Rota}/cartoes/{cartao}/lancamentos/parcelado", new { descricao = "Sofá", valorParcela = 50m, totalParcelas = 3, dataCompra = "2032-01-05" });
        var compras = await api.ComprasDoCartaoNoBancoAsync(cartao);

        Assert.Equal(404, (await outra.Painel.Delete($"{Rota}/cartoes/lancamentos/{compras[1].Id}")).Codigo);
        var r = await loja.Painel.Delete($"{Rota}/cartoes/lancamentos/{compras[1].Id}");

        Assert.Equal(200, r.Codigo);
        Assert.Equal("Lançamento excluído.", r.Json.GetProperty("mensagem").GetString());
        Assert.Equal([compras[0].Id, compras[2].Id], (await api.ComprasDoCartaoNoBancoAsync(cartao)).Select(c => c.Id));
        Assert.Equal(404, (await loja.Painel.Delete($"{Rota}/cartoes/lancamentos/{compras[1].Id}")).Codigo);
    }

    [FatoIntegracao]
    public async Task Excluir_parcelada_em_todas_preserva_as_parcelas_de_faturas_ja_pagas()
    {
        var (loja, _, cartao) = await CenarioAsync();
        await loja.Painel.Post($"{Rota}/cartoes/{cartao}/lancamentos/parcelado", new { descricao = "Sofá", valorParcela = 50m, totalParcelas = 4, dataCompra = "2032-01-05" });
        var compras = await api.ComprasDoCartaoNoBancoAsync(cartao);
        await api.FaturaPagaDoMesNoBancoAsync(loja.Id, cartao, 2032, 2);

        var r = await loja.Painel.Delete($"{Rota}/cartoes/lancamentos/{compras[3].Id}?modo=todas");

        Assert.Equal(200, r.Codigo);
        Assert.Equal([compras[1].Id], (await api.ComprasDoCartaoNoBancoAsync(cartao)).Select(c => c.Id));
    }

    [FatoIntegracao]
    public async Task Excluir_fixo_em_todas_desativa_o_modelo_e_apaga_as_compras_do_mes_atual_em_diante()
    {
        var (loja, _, cartao) = await CenarioAsync();
        await loja.Painel.Post($"{Rota}/cartoes/{cartao}/fixos", new { descricao = "Netflix", valor = 40m, diaCompra = 5 });
        var passada = new DateTime(2020, 1, 5, 12, 0, 0, DateTimeKind.Utc);
        var fixoId = (await api.FixosDoCartaoNoBancoAsync(cartao)).Single().Id;
        await api.NoBancoAsync(async db =>
        {
            db.LancamentosCartao.Add(new LojaApi.Models.LancamentoCartao { LojaId = loja.Id, CartaoCreditoId = cartao, Descricao = "Netflix", Valor = 40m, DataCompra = passada, Modo = "fixa", CartaoFixoId = fixoId });
            await db.SaveChangesAsync();
        });
        var alvo = (await api.ComprasDoCartaoNoBancoAsync(cartao))[5];

        var r = await loja.Painel.Delete($"{Rota}/cartoes/lancamentos/{alvo.Id}?modo=todas");

        Assert.Equal(200, r.Codigo);
        Assert.False(Assert.Single(await api.FixosDoCartaoNoBancoAsync(cartao)).Ativo);
        var restantes = await api.ComprasDoCartaoNoBancoAsync(cartao);
        Assert.Equal([passada], restantes.Select(c => c.DataCompra));
    }
}
