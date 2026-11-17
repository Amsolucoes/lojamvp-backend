using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using static LojaApi.Tests.Suporte.CenariosFinanceiro;

namespace LojaApi.Tests.Api.FinanceiroTeste;

// Financeiro, parte 4a: Planejamento (configuração, tetos por categoria, metas) e troca de categoria de lançamentos e compras de cartão.
[Collection(ColecaoApi.Nome)]
public class FinanceiroPlanejamentoApiTests(ApiFixture api)
{
    private Task<LojaTeste> NovaAsync() => api.NovaLojaAsync();

    private static async Task<(int, string)> Erro(Task<Resposta> r) { var x = await r; return (x.Codigo, x.Erro); }

    private async Task<Guid> CategoriaAsync(LojaTeste loja, string nome, string tipo = "pagar") =>
        (await loja.Painel.Post($"{Rota}/categorias", new { nome, tipo })).Json.GetProperty("id").GetGuid();

    // ── Login ────────────────────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Exige_login_mas_qualquer_papel_da_loja_usa_o_planejamento()
    {
        var loja = await NovaAsync();
        var operador = await api.OperadorAsync(loja);

        Assert.Equal(401, (await loja.Site.Put($"{Rota}/planejamento/config", new { percentualPoupanca = 10m })).Codigo);
        Assert.Equal(401, (await loja.Site.Post($"{Rota}/planejamento/metas", new { })).Codigo);
        Assert.Equal(401, (await loja.Site.Patch($"{Rota}/lancamentos/{Guid.NewGuid()}/categoria", new { })).Codigo);
        Assert.Equal(200, (await operador.Put($"{Rota}/planejamento/config", new { percentualPoupanca = 10m })).Codigo);
    }

    // ── Configuração ─────────────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Salvar_config_cria_o_planejamento_e_depois_atualiza_o_mesmo()
    {
        var loja = await NovaAsync();
        var outra = await NovaAsync();

        var r = await loja.Painel.Put($"{Rota}/planejamento/config", new { receitaPlanejada = 5000m, percentualPoupanca = 15m });

        Assert.Equal(200, r.Codigo);
        Assert.Equal(["receitaPlanejada", "percentualPoupanca"], r.Json.EnumerateObject().Select(x => x.Name));
        Assert.Equal((5000m, 15m), (r.Json.GetProperty("receitaPlanejada").GetDecimal(), r.Json.GetProperty("percentualPoupanca").GetDecimal()));
        var segunda = await loja.Painel.Put($"{Rota}/planejamento/config", new { receitaPlanejada = 0m, percentualPoupanca = 20m });
        Assert.Equal(JsonValueKind.Null, segunda.Json.GetProperty("receitaPlanejada").ValueKind);
        var nulo = await loja.Painel.Put($"{Rota}/planejamento/config", new { percentualPoupanca = 0m });
        Assert.Equal((JsonValueKind.Null, 0m), (nulo.Json.GetProperty("receitaPlanejada").ValueKind, nulo.Json.GetProperty("percentualPoupanca").GetDecimal()));

        var planos = await api.NoBancoAsync(db => db.PlanejamentosFinanceiros.AsNoTracking().Where(p => p.LojaId == loja.Id).ToListAsync());
        Assert.Equal(0m, Assert.Single(planos).PercentualPoupanca);
        Assert.Empty(await api.NoBancoAsync(db => db.PlanejamentosFinanceiros.AsNoTracking().Where(p => p.LojaId == outra.Id).ToListAsync()));
    }

    [FatoIntegracao]
    public async Task Salvar_config_valida_percentual_receita_e_loja()
    {
        var loja = await NovaAsync();

        Assert.Equal((400, "O percentual a guardar deve ficar entre 0 e 100."), await Erro(loja.Painel.Put($"{Rota}/planejamento/config", new { percentualPoupanca = -1m })));
        Assert.Equal((400, "O percentual a guardar deve ficar entre 0 e 100."), await Erro(loja.Painel.Put($"{Rota}/planejamento/config", new { percentualPoupanca = 100.01m })));
        Assert.Equal((400, "A receita planejada não pode ser negativa."), await Erro(loja.Painel.Put($"{Rota}/planejamento/config", new { receitaPlanejada = -1m, percentualPoupanca = 10m })));
        Assert.Equal(200, (await loja.Painel.Put($"{Rota}/planejamento/config", new { percentualPoupanca = 100m })).Codigo);

        await api.SemVinculoAsync(loja);
        Assert.Equal((400, "Loja não encontrada."), await Erro(loja.Painel.Put($"{Rota}/planejamento/config", new { percentualPoupanca = 10m })));
    }

    // ── Tetos por categoria ──────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Salvar_categorias_cria_e_atualiza_por_categoria_e_tipo_ignorando_categoria_de_outra_loja()
    {
        var loja = await NovaAsync();
        var outra = await NovaAsync();
        var aluguel = await CategoriaAsync(loja, "Aluguel");
        var ambos = await CategoriaAsync(loja, "Outros", "ambos");
        var alheia = await CategoriaAsync(outra, "Alheia");

        var r = await loja.Painel.Put($"{Rota}/planejamento/categorias", new
        {
            itens = new object[]
            {
                new { categoriaId = aluguel, ativo = true, valor = 900m },
                new { categoriaId = ambos, ativo = true, valor = 100m, tipo = "pagar" },
                new { categoriaId = ambos, ativo = false, valor = 50m, tipo = "receber" },
                new { categoriaId = alheia, ativo = true, valor = 1m },
                new { categoriaId = Guid.NewGuid(), ativo = true, valor = -5m },
            },
        });

        Assert.Equal(200, r.Codigo);
        Assert.Equal(["ok"], r.Json.EnumerateObject().Select(x => x.Name));
        var tetos = await api.NoBancoAsync(db => db.PlanejamentoCategorias.AsNoTracking().Where(p => p.LojaId == loja.Id).ToListAsync());
        Assert.Equal(3, tetos.Count);
        Assert.Equal((true, 900m, "pagar"), (tetos.Single(t => t.CategoriaId == aluguel).Ativo, tetos.Single(t => t.CategoriaId == aluguel).Valor, tetos.Single(t => t.CategoriaId == aluguel).Tipo));
        Assert.Equal((false, 50m), (tetos.Single(t => t.CategoriaId == ambos && t.Tipo == "receber").Ativo, tetos.Single(t => t.CategoriaId == ambos && t.Tipo == "receber").Valor));
        Assert.Empty(await api.NoBancoAsync(db => db.PlanejamentoCategorias.AsNoTracking().Where(p => p.CategoriaId == alheia).ToListAsync()));

        await loja.Painel.Put($"{Rota}/planejamento/categorias", new { itens = new object[] { new { categoriaId = aluguel, ativo = false, valor = 1200m, tipo = "qualquer" } } });
        var depois = await api.NoBancoAsync(db => db.PlanejamentoCategorias.AsNoTracking().Where(p => p.LojaId == loja.Id && p.CategoriaId == aluguel).ToListAsync());
        Assert.Equal((false, 1200m), (Assert.Single(depois).Ativo, depois[0].Valor));
    }

    [FatoIntegracao]
    public async Task Salvar_categorias_recusa_valor_negativo_sem_gravar_nada_e_sem_loja_devolve_400()
    {
        var loja = await NovaAsync();
        var a = await CategoriaAsync(loja, "A");
        var b = await CategoriaAsync(loja, "B");

        var r = await loja.Painel.Put($"{Rota}/planejamento/categorias", new { itens = new object[] { new { categoriaId = a, ativo = true, valor = 10m }, new { categoriaId = b, ativo = true, valor = -1m } } });

        Assert.Equal((400, "O valor planejado não pode ser negativo."), (r.Codigo, r.Erro));
        Assert.Empty(await api.NoBancoAsync(db => db.PlanejamentoCategorias.AsNoTracking().Where(p => p.LojaId == loja.Id).ToListAsync()));

        await api.SemVinculoAsync(loja);
        Assert.Equal((400, "Loja não encontrada."), await Erro(loja.Painel.Put($"{Rota}/planejamento/categorias", new { itens = Array.Empty<object>() })));
    }

    // ── Metas ────────────────────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Criar_meta_corta_o_nome_usa_icone_padrao_nao_deixa_o_guardado_negativo_e_grava_a_data_em_utc()
    {
        var loja = await NovaAsync();

        var r = await loja.Painel.Post($"{Rota}/planejamento/metas", new { nome = "  Viagem  ", icone = "  ", valorAlvo = 5000m, valorGuardado = -10m, dataAlvo = "2032-06-15T23:00:00" });

        Assert.Equal(200, r.Codigo);
        Assert.Equal(["id"], r.Json.EnumerateObject().Select(x => x.Name));
        var meta = Assert.Single(await api.NoBancoAsync(db => db.MetasPlanejamento.AsNoTracking().Where(m => m.LojaId == loja.Id).ToListAsync()));
        Assert.Equal((r.Json.GetProperty("id").GetGuid(), "Viagem", "🎯", 5000m, 0m, new DateTime(2032, 6, 15, 0, 0, 0, DateTimeKind.Utc)),
            (meta.Id, meta.Nome, meta.Icone, meta.ValorAlvo, meta.ValorGuardado, meta.DataAlvo));

        var semData = await loja.Painel.Post($"{Rota}/planejamento/metas", new { nome = "Reserva", icone = "🏠", valorAlvo = 100m, valorGuardado = 30m });
        var outra = (await api.NoBancoAsync(db => db.MetasPlanejamento.AsNoTracking().Where(m => m.Id == semData.Json.GetProperty("id").GetGuid()).ToListAsync())).Single();
        Assert.Equal(("🏠", 30m, null), (outra.Icone, outra.ValorGuardado, outra.DataAlvo));
    }

    [FatoIntegracao]
    public async Task Criar_meta_valida_nome_valor_e_loja()
    {
        var loja = await NovaAsync();

        Assert.Equal((400, "Informe o nome da meta."), await Erro(loja.Painel.Post($"{Rota}/planejamento/metas", new { nome = "   ", valorAlvo = 10m })));
        Assert.Equal((400, "Informe o valor da meta."), await Erro(loja.Painel.Post($"{Rota}/planejamento/metas", new { nome = "X", valorAlvo = 0m })));
        Assert.Equal((400, "Informe o valor da meta."), await Erro(loja.Painel.Post($"{Rota}/planejamento/metas", new { nome = "X", valorAlvo = -1m })));
        Assert.Empty(await api.NoBancoAsync(db => db.MetasPlanejamento.AsNoTracking().Where(m => m.LojaId == loja.Id).ToListAsync()));

        await api.SemVinculoAsync(loja);
        Assert.Equal((400, "Loja não encontrada."), await Erro(loja.Painel.Post($"{Rota}/planejamento/metas", new { nome = "X", valorAlvo = 10m })));
    }

    [FatoIntegracao]
    public async Task Atualizar_meta_troca_os_dados_valida_e_so_na_propria_loja()
    {
        var loja = await NovaAsync();
        var outra = await NovaAsync();
        var id = (await loja.Painel.Post($"{Rota}/planejamento/metas", new { nome = "Carro", valorAlvo = 1000m, valorGuardado = 100m, dataAlvo = "2032-01-01" })).Json.GetProperty("id").GetGuid();
        object Corpo(string nome = "Moto", decimal alvo = 800m) => new { nome, icone = "🏍️", valorAlvo = alvo, valorGuardado = -5m };

        var r = await loja.Painel.Put($"{Rota}/planejamento/metas/{id}", Corpo(" Moto "));

        Assert.Equal(200, r.Codigo);
        Assert.Equal(id, r.Json.GetProperty("id").GetGuid());
        var meta = Assert.Single(await api.NoBancoAsync(db => db.MetasPlanejamento.AsNoTracking().Where(m => m.Id == id).ToListAsync()));
        Assert.Equal(("Moto", "🏍️", 800m, 0m, null), (meta.Nome, meta.Icone, meta.ValorAlvo, meta.ValorGuardado, meta.DataAlvo));
        Assert.Equal((400, "Informe o nome da meta."), await Erro(loja.Painel.Put($"{Rota}/planejamento/metas/{id}", Corpo(" "))));
        Assert.Equal((400, "Informe o valor da meta."), await Erro(loja.Painel.Put($"{Rota}/planejamento/metas/{id}", Corpo(alvo: 0m))));
        Assert.Equal(404, (await outra.Painel.Put($"{Rota}/planejamento/metas/{id}", Corpo())).Codigo);
        Assert.Equal(404, (await loja.Painel.Put($"{Rota}/planejamento/metas/{Guid.NewGuid()}", Corpo())).Codigo);
    }

    [FatoIntegracao]
    public async Task Aportar_soma_ou_retira_sem_deixar_negativo_e_zero_e_recusado()
    {
        var loja = await NovaAsync();
        var outra = await NovaAsync();
        var id = (await loja.Painel.Post($"{Rota}/planejamento/metas", new { nome = "Reserva", valorAlvo = 1000m, valorGuardado = 100m })).Json.GetProperty("id").GetGuid();
        string Url() => $"{Rota}/planejamento/metas/{id}/aportar";

        var soma = await loja.Painel.Post(Url(), new { valor = 50.5m });
        Assert.Equal(["valorGuardado"], soma.Json.EnumerateObject().Select(x => x.Name));
        Assert.Equal(150.5m, soma.Json.GetProperty("valorGuardado").GetDecimal());
        Assert.Equal(100.5m, (await loja.Painel.Post(Url(), new { valor = -50m })).Json.GetProperty("valorGuardado").GetDecimal());
        Assert.Equal(0m, (await loja.Painel.Post(Url(), new { valor = -999m })).Json.GetProperty("valorGuardado").GetDecimal());

        Assert.Equal((400, "Informe um valor."), await Erro(loja.Painel.Post(Url(), new { valor = 0m })));
        Assert.Equal(404, (await outra.Painel.Post(Url(), new { valor = 1m })).Codigo);
        Assert.Equal(404, (await loja.Painel.Post($"{Rota}/planejamento/metas/{Guid.NewGuid()}/aportar", new { valor = 1m })).Codigo);
    }

    [FatoIntegracao]
    public async Task Excluir_meta_devolve_204_e_so_na_propria_loja()
    {
        var loja = await NovaAsync();
        var outra = await NovaAsync();
        var id = (await loja.Painel.Post($"{Rota}/planejamento/metas", new { nome = "Reserva", valorAlvo = 1000m })).Json.GetProperty("id").GetGuid();

        Assert.Equal(404, (await outra.Painel.Delete($"{Rota}/planejamento/metas/{id}")).Codigo);
        var r = await loja.Painel.Delete($"{Rota}/planejamento/metas/{id}");

        Assert.Equal(204, r.Codigo);
        Assert.Empty(await api.NoBancoAsync(db => db.MetasPlanejamento.AsNoTracking().Where(m => m.Id == id).ToListAsync()));
        Assert.Equal(404, (await loja.Painel.Delete($"{Rota}/planejamento/metas/{id}")).Codigo);
    }

    // ── Trocar categoria de lançamento ───────────────────────────────────────

    [FatoIntegracao]
    public async Task Trocar_categoria_do_lancamento_avulso_ou_parcela_muda_so_ele_e_aceita_limpar()
    {
        var loja = await NovaAsync();
        var outra = await NovaAsync();
        var conta = await loja.ContaPelaApiAsync();
        var antiga = await CategoriaAsync(loja, "Antiga");
        var nova = await CategoriaAsync(loja, "Nova");
        var alheia = await CategoriaAsync(outra, "Alheia");
        var avulso = await loja.AvulsoPelaApiAsync(conta, categoriaId: antiga);
        var grupo = await loja.ParceladoPelaApiAsync(conta, 3);
        var parcelas = await api.GrupoNoBancoAsync(grupo);

        var r = await loja.Painel.Patch($"{Rota}/lancamentos/{avulso}/categoria", new { categoriaId = nova });

        Assert.Equal(200, r.Codigo);
        Assert.Equal(["id", "categoriaId", "alterados"], r.Json.EnumerateObject().Select(x => x.Name));
        Assert.Equal((avulso, nova, 1), (r.Json.GetProperty("id").GetGuid(), r.Json.GetProperty("categoriaId").GetGuid(), r.Json.GetProperty("alterados").GetInt32()));
        Assert.Equal(nova, (await api.LancamentoNoBancoPorIdAsync(avulso)).CategoriaId);

        var uma = await loja.Painel.Patch($"{Rota}/lancamentos/{parcelas[1].Id}/categoria", new { categoriaId = nova });
        Assert.Equal(1, uma.Json.GetProperty("alterados").GetInt32());
        Assert.Equal([null, nova, null], (await api.GrupoNoBancoAsync(grupo)).Select(p => p.CategoriaId));

        var limpa = await loja.Painel.Patch($"{Rota}/lancamentos/{avulso}/categoria", new { categoriaId = (Guid?)null });
        Assert.Equal(JsonValueKind.Null, limpa.Json.GetProperty("categoriaId").ValueKind);
        Assert.Null((await api.LancamentoNoBancoPorIdAsync(avulso)).CategoriaId);

        Assert.Equal((400, "Categoria não encontrada."), await Erro(loja.Painel.Patch($"{Rota}/lancamentos/{avulso}/categoria", new { categoriaId = alheia })));
        Assert.Equal(404, (await outra.Painel.Patch($"{Rota}/lancamentos/{avulso}/categoria", new { categoriaId = alheia })).Codigo);
        Assert.Equal(404, (await loja.Painel.Patch($"{Rota}/lancamentos/{Guid.NewGuid()}/categoria", new { categoriaId = nova })).Codigo);
    }

    [FatoIntegracao]
    public async Task Trocar_categoria_de_parcelamento_com_todas_as_parcelas_e_de_conta_fixa_muda_o_grupo_e_o_modelo()
    {
        var loja = await NovaAsync();
        var conta = await loja.ContaPelaApiAsync();
        var nova = await CategoriaAsync(loja, "Nova");
        var grupo = await loja.ParceladoPelaApiAsync(conta, 3);
        var parcelas = await api.GrupoNoBancoAsync(grupo);
        var fixoId = (await loja.Painel.Post($"{Rota}/fixos", new { contaBancariaId = conta, tipo = "pagar", descricao = "Aluguel", valor = 900m, diaVencimento = 5 })).Json.GetProperty("id").GetGuid();
        var doFixo = (await api.LancamentosDaLojaNoBancoAsync(loja.Id)).First(l => l.LancamentoFixoId == fixoId);

        var todas = await loja.Painel.Patch($"{Rota}/lancamentos/{parcelas[0].Id}/categoria", new { categoriaId = nova, todasParcelas = true });
        Assert.Equal(3, todas.Json.GetProperty("alterados").GetInt32());
        Assert.All(await api.GrupoNoBancoAsync(grupo), p => Assert.Equal(nova, p.CategoriaId));

        var fixa = await loja.Painel.Patch($"{Rota}/lancamentos/{doFixo.Id}/categoria", new { categoriaId = nova });

        Assert.Equal(60, fixa.Json.GetProperty("alterados").GetInt32());
        Assert.All((await api.LancamentosDaLojaNoBancoAsync(loja.Id)).Where(l => l.LancamentoFixoId == fixoId), l => Assert.Equal(nova, l.CategoriaId));
        Assert.Equal(nova, Assert.Single(await api.FixosNoBancoAsync(loja.Id)).CategoriaId);
    }

    // ── Trocar categoria de compra do cartão ─────────────────────────────────

    [FatoIntegracao]
    public async Task Trocar_categoria_da_compra_do_cartao_avulsa_parcelada_e_fixa()
    {
        var loja = await NovaAsync();
        var outra = await NovaAsync();
        var conta = await loja.ContaPelaApiAsync();
        var cartao = await loja.CartaoPelaApiAsync(conta);
        var nova = await CategoriaAsync(loja, "Nova");
        var alheia = await CategoriaAsync(outra, "Alheia");
        await loja.CompraNoCartaoAsync(cartao, 10m, "2032-01-05", "Avulsa");
        await loja.Painel.Post($"{Rota}/cartoes/{cartao}/lancamentos/parcelado", new { descricao = "TV", valorParcela = 50m, totalParcelas = 3, dataCompra = "2032-02-05" });
        var fixoId = (await loja.Painel.Post($"{Rota}/cartoes/{cartao}/fixos", new { descricao = "Netflix", valor = 40m, diaCompra = 5 })).Json.GetProperty("id").GetGuid();
        var compras = await api.ComprasDoCartaoNoBancoAsync(cartao);
        var avulsa = compras.Single(c => c.Descricao == "Avulsa");
        var tv = compras.Where(c => c.Descricao.StartsWith("TV")).ToList();
        var netflix = compras.First(c => c.CartaoFixoId == fixoId);
        string Url(Guid id) => $"{Rota}/cartoes/lancamentos/{id}/categoria";

        var r = await loja.Painel.Patch(Url(avulsa.Id), new { categoriaId = nova });
        Assert.Equal(["alterados"], r.Json.EnumerateObject().Select(x => x.Name));
        Assert.Equal(1, r.Json.GetProperty("alterados").GetInt32());

        Assert.Equal(1, (await loja.Painel.Patch(Url(tv[0].Id), new { categoriaId = nova })).Json.GetProperty("alterados").GetInt32());
        Assert.Equal(3, (await loja.Painel.Patch(Url(tv[1].Id), new { categoriaId = nova, todasParcelas = true })).Json.GetProperty("alterados").GetInt32());
        Assert.Equal(60, (await loja.Painel.Patch(Url(netflix.Id), new { categoriaId = nova })).Json.GetProperty("alterados").GetInt32());

        var depois = await api.ComprasDoCartaoNoBancoAsync(cartao);
        Assert.All(depois, c => Assert.Equal(nova, c.CategoriaId));
        Assert.Equal(nova, Assert.Single(await api.FixosDoCartaoNoBancoAsync(cartao)).CategoriaId);

        await loja.Painel.Patch(Url(avulsa.Id), new { categoriaId = (Guid?)null });
        Assert.Null((await api.ComprasDoCartaoNoBancoAsync(cartao)).Single(c => c.Id == avulsa.Id).CategoriaId);
        Assert.Equal((400, "Categoria não encontrada."), await Erro(loja.Painel.Patch(Url(avulsa.Id), new { categoriaId = alheia })));
        Assert.Equal(404, (await outra.Painel.Patch(Url(avulsa.Id), new { categoriaId = alheia })).Codigo);
        Assert.Equal(404, (await loja.Painel.Patch(Url(Guid.NewGuid()), new { categoriaId = nova })).Codigo);
    }
}
