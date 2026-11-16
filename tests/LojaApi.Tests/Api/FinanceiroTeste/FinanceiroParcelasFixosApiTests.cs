using Microsoft.EntityFrameworkCore;
using static LojaApi.Tests.Suporte.CenariosFinanceiro;

namespace LojaApi.Tests.Api.FinanceiroTeste;

// Financeiro, parte 2: parcelas de um parcelamento, lançamentos fixos, edição e exclusão de lançamentos (única, futuras, todas).
[Collection(ColecaoApi.Nome)]
public class FinanceiroParcelasFixosApiTests(ApiFixture api)
{
    private Task<LojaTeste> NovaAsync() => api.NovaLojaAsync();

    private static async Task<(int, string)> Erro(Task<Resposta> r) { var x = await r; return (x.Codigo, x.Erro); }

    private static DateTime Utc(int ano, int mes, int dia, int hora = 12) => new(ano, mes, dia, hora, 0, 0, DateTimeKind.Utc);

    private static DateTime MesAtual() { var h = DateTime.UtcNow; return new DateTime(h.Year, h.Month, 1, 0, 0, 0, DateTimeKind.Utc); }

    private async Task<Guid> PrimeiraParcelaAsync(Guid grupo) => (await api.GrupoNoBancoAsync(grupo))[0].Id;

    // ── Parcelas ─────────────────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Listar_parcelas_mostra_o_grupo_inteiro_e_recusa_o_que_nao_e_parcelamento()
    {
        var loja = await NovaAsync();
        var outra = await NovaAsync();
        var conta = await loja.ContaPelaApiAsync();
        var grupo = await loja.ParceladoPelaApiAsync(conta, 3, 50m);
        var id = await PrimeiraParcelaAsync(grupo);
        var avulso = await loja.AvulsoPelaApiAsync(conta);
        var financiada = await api.NoBancoAsync(async db =>
        {
            var l = new LojaApi.Models.LancamentoFinanceiro
            {
                LojaId = loja.Id, ContaBancariaId = conta, Descricao = "Fatura financiada", Valor = 1m, Vencimento = Utc(2032, 1, 1), Modo = "parcelada",
                GrupoParcelamentoId = Guid.NewGuid(), NumeroParcela = 1, TotalParcelas = 2, CartaoOrigemId = Guid.NewGuid(),
            };
            db.LancamentosFinanceiros.Add(l);
            await db.SaveChangesAsync();
            return l.Id;
        });

        var r = await loja.Painel.Get($"{Rota}/lancamentos/{id}/parcelas");

        Assert.Equal(200, r.Codigo);
        Assert.Equal(["total", "itens"], r.Json.EnumerateObject().Select(x => x.Name));
        Assert.Equal(3, r.Json.GetProperty("total").GetInt32());
        var itens = r.Json.GetProperty("itens").EnumerateArray().ToList();
        Assert.Equal(["id", "numero", "vencimento", "valor", "status", "pagoEm"], itens[0].EnumerateObject().Select(x => x.Name));
        Assert.Equal([1, 2, 3], itens.Select(x => x.GetProperty("numero").GetInt32()));
        Assert.Equal((400, "Lançamento não encontrado."), await Erro(outra.Painel.Get($"{Rota}/lancamentos/{id}/parcelas")));
        Assert.Equal((400, "Lançamento não encontrado."), await Erro(loja.Painel.Get($"{Rota}/lancamentos/{Guid.NewGuid()}/parcelas")));
        Assert.Equal((400, "Este lançamento não é parcelado."), await Erro(loja.Painel.Get($"{Rota}/lancamentos/{avulso}/parcelas")));
        Assert.Equal((400, "Parcelas de fatura de cartão financiada não podem ser alteradas por aqui."), await Erro(loja.Painel.Get($"{Rota}/lancamentos/{financiada}/parcelas")));
    }

    [FatoIntegracao]
    public async Task Excluir_parcelas_remove_as_escolhidas_e_renumera_as_que_ficam()
    {
        var loja = await NovaAsync();
        var conta = await loja.ContaPelaApiAsync();
        var grupo = await loja.ParceladoPelaApiAsync(conta, 4, 50m);
        var parcelas = await api.GrupoNoBancoAsync(grupo);

        var r = await loja.Painel.Post($"{Rota}/lancamentos/{parcelas[0].Id}/parcelas/excluir", new { ids = new[] { parcelas[1].Id, parcelas[2].Id, parcelas[1].Id } });

        Assert.Equal(200, r.Codigo);
        Assert.Equal((2, 2), (r.Json.GetProperty("excluidas").GetInt32(), r.Json.GetProperty("restantes").GetInt32()));
        var restantes = await api.GrupoNoBancoAsync(grupo);
        Assert.Equal([parcelas[0].Id, parcelas[3].Id], restantes.Select(p => p.Id));
        Assert.Equal([1, 2], restantes.Select(p => p.NumeroParcela!.Value));
        Assert.All(restantes, p => Assert.Equal(2, p.TotalParcelas));
    }

    [FatoIntegracao]
    public async Task Excluir_parcelas_valida_selecao_pagas_e_o_grupo_todo()
    {
        var loja = await NovaAsync();
        var conta = await loja.ContaPelaApiAsync();
        var grupo = await loja.ParceladoPelaApiAsync(conta, 3, 50m);
        var parcelas = await api.GrupoNoBancoAsync(grupo);
        await loja.Painel.Post($"{Rota}/lancamentos/{parcelas[0].Id}/pagamento", new { pago = true });
        string Url(Guid id) => $"{Rota}/lancamentos/{id}/parcelas/excluir";

        Assert.Equal((400, "Selecione as parcelas que quer excluir."), await Erro(loja.Painel.Post(Url(parcelas[1].Id), new { ids = Array.Empty<Guid>() })));
        Assert.Equal((400, "Alguma parcela selecionada não foi encontrada."), await Erro(loja.Painel.Post(Url(parcelas[1].Id), new { ids = new[] { Guid.NewGuid() } })));
        Assert.Equal((400, "Parcelas já pagas não podem ser excluídas. Desmarque o pagamento antes, se for o caso."), await Erro(loja.Painel.Post(Url(parcelas[1].Id), new { ids = new[] { parcelas[0].Id } })));
        Assert.Equal((400, "Parcelas já pagas não podem ser excluídas. Desmarque o pagamento antes, se for o caso."), await Erro(loja.Painel.Post(Url(parcelas[1].Id), new { ids = parcelas.Select(p => p.Id) })));
        var intacto = await api.GrupoNoBancoAsync(await loja.ParceladoPelaApiAsync(conta, 2, 10m));
        Assert.Equal((400, "Para excluir todas as parcelas, exclua o lançamento."), await Erro(loja.Painel.Post(Url(intacto[0].Id), new { ids = intacto.Select(p => p.Id) })));
        Assert.Equal((400, "Este lançamento não é parcelado."), await Erro(loja.Painel.Post(Url(await loja.AvulsoPelaApiAsync(conta)), new { ids = new[] { parcelas[1].Id } })));
        Assert.Equal(3, (await api.GrupoNoBancoAsync(grupo)).Count);
    }

    [FatoIntegracao]
    public async Task Marcar_parcelas_pagas_e_desmarcar_em_lote()
    {
        var loja = await NovaAsync();
        var conta = await loja.ContaPelaApiAsync();
        var grupo = await loja.ParceladoPelaApiAsync(conta, 3, 50m);
        var parcelas = await api.GrupoNoBancoAsync(grupo);
        string Url(Guid id) => $"{Rota}/lancamentos/{id}/parcelas/pagamento";

        var pagas = await loja.Painel.Post(Url(parcelas[0].Id), new { ids = new[] { parcelas[0].Id, parcelas[2].Id, parcelas[2].Id }, pago = true });

        Assert.Equal(200, pagas.Codigo);
        Assert.Equal(2, pagas.Json.GetProperty("alteradas").GetInt32());
        var depois = await api.GrupoNoBancoAsync(grupo);
        Assert.Equal(["pago", "pendente", "pago"], depois.Select(p => p.Status));
        Assert.Equal([true, false, true], depois.Select(p => p.PagoEm.HasValue));

        await loja.Painel.Post(Url(parcelas[0].Id), new { ids = new[] { parcelas[0].Id }, pago = false });
        Assert.Equal(["pendente", "pendente", "pago"], (await api.GrupoNoBancoAsync(grupo)).Select(p => p.Status));
        Assert.Null((await api.GrupoNoBancoAsync(grupo))[0].PagoEm);

        Assert.Equal((400, "Selecione as parcelas."), await Erro(loja.Painel.Post(Url(parcelas[0].Id), new { ids = Array.Empty<Guid>(), pago = true })));
        Assert.Equal((400, "Alguma parcela selecionada não foi encontrada."), await Erro(loja.Painel.Post(Url(parcelas[0].Id), new { ids = new[] { Guid.NewGuid() }, pago = true })));
        Assert.Equal((400, "Lançamento não encontrado."), await Erro(loja.Painel.Post(Url(Guid.NewGuid()), new { ids = new[] { parcelas[0].Id }, pago = true })));
    }

    [FatoIntegracao]
    public async Task Alterar_total_de_parcelas_reduz_removendo_as_ultimas_e_aumenta_continuando_a_serie_mensal()
    {
        var loja = await NovaAsync();
        var conta = await loja.ContaPelaApiAsync();
        var grupo = await loja.ParceladoPelaApiAsync(conta, 4, 50m, "2032-01-31");
        var id = await PrimeiraParcelaAsync(grupo);
        string Url() => $"{Rota}/lancamentos/{id}/parcelas/total";

        var reduzido = await loja.Painel.Post(Url(), new { total = 2 });
        Assert.Equal(2, reduzido.Json.GetProperty("total").GetInt32());
        var depoisReduzir = await api.GrupoNoBancoAsync(grupo);
        Assert.Equal([1, 2], depoisReduzir.Select(p => p.NumeroParcela!.Value));
        Assert.All(depoisReduzir, p => Assert.Equal(2, p.TotalParcelas));

        Assert.Equal(2, (await loja.Painel.Post(Url(), new { total = 2 })).Json.GetProperty("total").GetInt32());

        await loja.Painel.Patch($"{Rota}/lancamentos/{id}/avisar", new { avisar = false });
        var ultima = depoisReduzir[1];
        await api.NoBancoAsync(db => db.LancamentosFinanceiros.Where(l => l.Id == ultima.Id).ExecuteUpdateAsync(s => s.SetProperty(l => l.Avisar, false).SetProperty(l => l.Valor, 77m)));
        var aumentado = await loja.Painel.Post(Url(), new { total = 5 });
        Assert.Equal(5, aumentado.Json.GetProperty("total").GetInt32());
        var depoisAumentar = await api.GrupoNoBancoAsync(grupo);
        Assert.Equal([1, 2, 3, 4, 5], depoisAumentar.Select(p => p.NumeroParcela!.Value));
        Assert.All(depoisAumentar, p => Assert.Equal(5, p.TotalParcelas));
        Assert.Equal([Utc(2032, 1, 31), Utc(2032, 2, 29), Utc(2032, 3, 29), Utc(2032, 4, 29), Utc(2032, 5, 29)], depoisAumentar.Select(p => p.Vencimento));
        Assert.Equal([50m, 77m, 77m, 77m, 77m], depoisAumentar.Select(p => p.Valor));
        Assert.Equal(["pendente", "pendente", "pendente", "pendente", "pendente"], depoisAumentar.Select(p => p.Status));
        Assert.Equal([false, false, false, false], depoisAumentar.Skip(1).Select(p => p.Avisar));
        Assert.All(depoisAumentar, p => Assert.Equal(("parcelada", loja.Id, conta), (p.Modo, p.LojaId, p.ContaBancariaId)));
    }

    [FatoIntegracao]
    public async Task Alterar_total_de_parcelas_valida_faixa_e_recusa_remover_parcela_paga()
    {
        var loja = await NovaAsync();
        var conta = await loja.ContaPelaApiAsync();
        var grupo = await loja.ParceladoPelaApiAsync(conta, 4, 50m);
        var parcelas = await api.GrupoNoBancoAsync(grupo);
        string Url() => $"{Rota}/lancamentos/{parcelas[0].Id}/parcelas/total";
        await loja.Painel.Post($"{Rota}/lancamentos/{parcelas[0].Id}/parcelas/pagamento", new { ids = new[] { parcelas[2].Id, parcelas[3].Id }, pago = true });

        Assert.Equal((400, "O total de parcelas deve ficar entre 1 e 360."), await Erro(loja.Painel.Post(Url(), new { total = 0 })));
        Assert.Equal((400, "O total de parcelas deve ficar entre 1 e 360."), await Erro(loja.Painel.Post(Url(), new { total = 361 })));
        Assert.Equal((400, "As parcelas 3, 4 já estão pagas e seriam removidas. Desmarque o pagamento delas ou escolha as parcelas a excluir."), await Erro(loja.Painel.Post(Url(), new { total = 2 })));
        Assert.Equal(4, (await api.GrupoNoBancoAsync(grupo)).Count);
        Assert.Equal(1, (await loja.Painel.Post(Url(), new { total = 1 })).Codigo == 400 ? 1 : 0);
        Assert.Equal(360, (await loja.Painel.Post(Url(), new { total = 360 })).Json.GetProperty("total").GetInt32());
    }

    // ── Fixos ────────────────────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Criar_fixo_gera_60_meses_a_partir_do_mes_atual_e_corrige_o_dia_fora_de_1_a_28()
    {
        var loja = await NovaAsync();
        var conta = await loja.ContaPelaApiAsync();
        var inicio = MesAtual();

        var r = await loja.Painel.Post($"{Rota}/fixos", new { contaBancariaId = conta, tipo = "pagar", descricao = " Aluguel ", categoriaId = (Guid?)null, valor = 900m, diaVencimento = 29, observacao = "obs" });

        Assert.Equal(200, r.Codigo);
        Assert.Equal(["id", "geradoAte"], r.Json.EnumerateObject().Select(x => x.Name));
        Assert.Equal(inicio.AddMonths(59), r.Json.GetProperty("geradoAte").GetDateTime().ToUniversalTime());
        var fixo = Assert.Single(await api.FixosNoBancoAsync(loja.Id));
        Assert.Equal((r.Json.GetProperty("id").GetGuid(), "pagar", "Aluguel", 900m, 10, true, "obs", null), (fixo.Id, fixo.Tipo, fixo.Descricao, fixo.Valor, fixo.DiaVencimento, fixo.Ativa, fixo.Observacao, fixo.DataInicio));
        var geradas = await api.LancamentosDaLojaNoBancoAsync(loja.Id);
        Assert.Equal(60, geradas.Count);
        Assert.All(geradas, l => Assert.Equal(("fixa", "pendente", "Aluguel", 900m, fixo.Id, 10, 12), (l.Modo, l.Status, l.Descricao, l.Valor, l.LancamentoFixoId!.Value, l.Vencimento.Day, l.Vencimento.Hour)));
        Assert.Equal((inicio.Year, inicio.Month), (geradas[0].Vencimento.Year, geradas[0].Vencimento.Month));

        var dia28 = await loja.Painel.Post($"{Rota}/fixos", new { contaBancariaId = conta, tipo = "receber", descricao = "Outro", valor = 1m, diaVencimento = 28 });
        var zero = await loja.Painel.Post($"{Rota}/fixos", new { contaBancariaId = conta, tipo = "receber", descricao = "Zero", valor = 1m, diaVencimento = 0 });
        var fixos = await api.FixosNoBancoAsync(loja.Id);
        Assert.Equal(28, fixos.Single(f => f.Id == dia28.Json.GetProperty("id").GetGuid()).DiaVencimento);
        Assert.Equal(10, fixos.Single(f => f.Id == zero.Json.GetProperty("id").GetGuid()).DiaVencimento);

        await api.SemVinculoAsync(loja);
        Assert.Equal((400, "Loja não encontrada."), await Erro(loja.Painel.Post($"{Rota}/fixos", new { contaBancariaId = conta, tipo = "pagar", descricao = "x", valor = 1m, diaVencimento = 5 })));
    }

    [FatoIntegracao]
    public async Task Criar_fixo_com_data_inicio_pula_os_meses_anteriores_e_ja_pago_marca_a_primeira_ocorrencia()
    {
        var loja = await NovaAsync();
        var conta = await loja.ContaPelaApiAsync();
        var inicio = MesAtual().AddMonths(2);

        var r = await loja.Painel.Post($"{Rota}/fixos", new
        {
            contaBancariaId = conta, tipo = "pagar", descricao = "Seguro", valor = 50m, diaVencimento = 15, dataInicio = $"{inicio:yyyy-MM-dd}T20:00:00", jaPago = true,
        });

        Assert.Equal(200, r.Codigo);
        var fixo = Assert.Single(await api.FixosNoBancoAsync(loja.Id));
        Assert.Equal(inicio, fixo.DataInicio);
        var geradas = await api.LancamentosDaLojaNoBancoAsync(loja.Id);
        Assert.Equal(58, geradas.Count);
        Assert.Equal((inicio.Year, inicio.Month), (geradas[0].Vencimento.Year, geradas[0].Vencimento.Month));
        Assert.Equal(["pago"], geradas.Select(l => l.Status).Distinct().Take(1));
        Assert.Equal(1, geradas.Count(l => l.Status == "pago"));
        Assert.NotNull(geradas[0].PagoEm);
    }

    [FatoIntegracao]
    public async Task Atualizar_fixo_troca_os_dados_do_modelo_mas_hoje_perde_os_meses_futuros()
    {
        var loja = await NovaAsync();
        var outra = await NovaAsync();
        var conta = await loja.ContaPelaApiAsync();
        var nova = await loja.ContaPelaApiAsync("Nova");
        var criado = await loja.Painel.Post($"{Rota}/fixos", new { contaBancariaId = conta, tipo = "pagar", descricao = "Aluguel", valor = 900m, diaVencimento = 5, observacao = "original" });
        var id = criado.Json.GetProperty("id").GetGuid();
        var proximo = MesAtual().AddMonths(1);

        var r = await loja.Painel.Put($"{Rota}/fixos/{id}", new { contaBancariaId = nova, tipo = "receber", descricao = " Aluguel novo ", valor = 1000m, diaVencimento = 20, observacao = "ignorada" });

        Assert.Equal(200, r.Codigo);
        Assert.Equal(["id", "geradoAte"], r.Json.EnumerateObject().Select(x => x.Name));
        var fixo = Assert.Single(await api.FixosNoBancoAsync(loja.Id));
        Assert.Equal(("pagar", "Aluguel novo", 1000m, 20, nova, "original"), (fixo.Tipo, fixo.Descricao, fixo.Valor, fixo.DiaVencimento, fixo.ContaBancariaId, fixo.Observacao));
        // COMPORTAMENTO ANTIGO (defeito): apaga os pendentes futuros e tenta regerar sem salvar antes; a regeração enxerga os
        // registros ainda no banco e pula todos os meses, então só sobra o do mês atual quando o vencimento dele já passou.
        var depois = await api.LancamentosDaLojaNoBancoAsync(loja.Id);
        Assert.Equal(depois.Count(l => l.Vencimento < DateTime.UtcNow.Date), depois.Count);
        Assert.DoesNotContain(depois, l => l.Vencimento.Year == proximo.Year && l.Vencimento.Month == proximo.Month);

        Assert.Equal(404, (await outra.Painel.Put($"{Rota}/fixos/{id}", new { contaBancariaId = conta, tipo = "pagar", descricao = "x", valor = 1m, diaVencimento = 5 })).Codigo);
        Assert.Equal(404, (await loja.Painel.Put($"{Rota}/fixos/{Guid.NewGuid()}", new { contaBancariaId = conta, tipo = "pagar", descricao = "x", valor = 1m, diaVencimento = 5 })).Codigo);
    }

    [FatoIntegracao]
    public async Task Regenerar_fixo_recria_o_que_foi_apagado_mantendo_os_pagos_e_alternar_ativo_inverte_a_flag()
    {
        var loja = await NovaAsync();
        var outra = await NovaAsync();
        var conta = await loja.ContaPelaApiAsync();
        var id = (await loja.Painel.Post($"{Rota}/fixos", new { contaBancariaId = conta, tipo = "pagar", descricao = "Internet", valor = 100m, diaVencimento = 12 })).Json.GetProperty("id").GetGuid();
        var proximo = MesAtual().AddMonths(1);
        var dois = MesAtual().AddMonths(2);
        await api.NoBancoAsync(async db =>
        {
            await db.LancamentosFinanceiros.Where(l => l.LancamentoFixoId == id && l.Vencimento >= dois && l.Vencimento < dois.AddMonths(1)).ExecuteDeleteAsync();
            await db.LancamentosFinanceiros.Where(l => l.LancamentoFixoId == id && l.Vencimento >= proximo && l.Vencimento < dois)
                .ExecuteUpdateAsync(s => s.SetProperty(l => l.Status, "pago").SetProperty(l => l.Valor, 1m));
        });
        Assert.Equal(59, (await api.LancamentosDaLojaNoBancoAsync(loja.Id)).Count);

        var r = await loja.Painel.Post($"{Rota}/fixos/{id}/regenerar");

        Assert.Equal(200, r.Codigo);
        var lista = await api.LancamentosDaLojaNoBancoAsync(loja.Id);
        Assert.Equal(60, lista.Count);
        Assert.Equal(("pago", 1m), (lista.Single(l => l.Vencimento >= proximo && l.Vencimento < dois).Status, lista.Single(l => l.Vencimento >= proximo && l.Vencimento < dois).Valor));
        Assert.Equal(100m, lista.Single(l => l.Vencimento >= dois && l.Vencimento < dois.AddMonths(1)).Valor);

        var inativo = await loja.Painel.Patch($"{Rota}/fixos/{id}/ativo");
        Assert.Equal(["id", "ativa"], inativo.Json.EnumerateObject().Select(x => x.Name));
        Assert.False(inativo.Json.GetProperty("ativa").GetBoolean());
        Assert.True((await loja.Painel.Patch($"{Rota}/fixos/{id}/ativo")).Json.GetProperty("ativa").GetBoolean());
        Assert.Equal(404, (await outra.Painel.Patch($"{Rota}/fixos/{id}/ativo")).Codigo);
        Assert.Equal(404, (await outra.Painel.Post($"{Rota}/fixos/{id}/regenerar")).Codigo);
    }

    // ── Editar lançamento ────────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Editar_unica_troca_os_dados_e_o_vencimento_so_deste_lancamento()
    {
        var loja = await NovaAsync();
        var outra = await NovaAsync();
        var conta = await loja.ContaPelaApiAsync();
        var nova = await loja.ContaPelaApiAsync("Nova");
        var categoria = (await loja.Painel.Post($"{Rota}/categorias", new { nome = "Casa", tipo = "pagar" })).Json.GetProperty("id").GetGuid();
        var grupo = await loja.ParceladoPelaApiAsync(conta, 3, 50m);
        var parcelas = await api.GrupoNoBancoAsync(grupo);

        var r = await loja.Painel.Put($"{Rota}/lancamentos/{parcelas[1].Id}?modo=unica", new { descricao = " Novo ", categoriaId = categoria, contaBancariaId = nova, valor = 60m, vencimento = "2032-02-20T23:00:00", observacao = "n" });

        Assert.Equal(200, r.Codigo);
        Assert.Equal("Lançamento atualizado.", r.Json.GetProperty("mensagem").GetString());
        var depois = await api.GrupoNoBancoAsync(grupo);
        Assert.Equal(("Novo", categoria, nova, 60m, Utc(2032, 2, 20), "n"), (depois[1].Descricao, depois[1].CategoriaId, depois[1].ContaBancariaId, depois[1].Valor, depois[1].Vencimento, depois[1].Observacao));
        Assert.Equal([50m, 50m], new[] { depois[0].Valor, depois[2].Valor });
        Assert.Equal(404, (await outra.Painel.Put($"{Rota}/lancamentos/{parcelas[1].Id}?modo=unica", new { descricao = "x", contaBancariaId = conta, valor = 1m, vencimento = "2032-02-20" })).Codigo);
        Assert.Equal(404, (await loja.Painel.Put($"{Rota}/lancamentos/{Guid.NewGuid()}?modo=unica", new { descricao = "x", contaBancariaId = conta, valor = 1m, vencimento = "2032-02-20" })).Codigo);

        var avulso = await loja.AvulsoPelaApiAsync(conta);
        await loja.Painel.Put($"{Rota}/lancamentos/{avulso}?modo=todas", new { descricao = "Avulso novo", contaBancariaId = conta, valor = 7m, vencimento = "2032-09-09" });
        var a = await api.LancamentoNoBancoPorIdAsync(avulso);
        Assert.Equal(("Avulso novo", 7m, Utc(2032, 9, 9)), (a.Descricao, a.Valor, a.Vencimento));
    }

    [FatoIntegracao]
    public async Task Editar_parcelada_todas_muda_so_as_pendentes_e_futuras_muda_da_parcela_em_diante_ajustando_o_dia()
    {
        var loja = await NovaAsync();
        var conta = await loja.ContaPelaApiAsync();
        var todas = await loja.ParceladoPelaApiAsync(conta, 4, 50m, "2032-01-10");
        var p = await api.GrupoNoBancoAsync(todas);
        await loja.Painel.Post($"{Rota}/lancamentos/{p[0].Id}/parcelas/pagamento", new { ids = new[] { p[0].Id }, pago = true });

        await loja.Painel.Put($"{Rota}/lancamentos/{p[2].Id}?modo=todas", new { descricao = "Todas", contaBancariaId = conta, valor = 80m, vencimento = "2032-03-31", observacao = "o" });

        var depoisTodas = await api.GrupoNoBancoAsync(todas);
        Assert.Equal(["Financiamento", "Todas", "Todas", "Todas"], depoisTodas.Select(x => x.Descricao));
        Assert.Equal([50m, 80m, 80m, 80m], depoisTodas.Select(x => x.Valor));
        Assert.Equal([Utc(2032, 1, 10), Utc(2032, 2, 29), Utc(2032, 3, 31), Utc(2032, 4, 30)], depoisTodas.Select(x => x.Vencimento));

        var futuras = await loja.ParceladoPelaApiAsync(conta, 4, 50m, "2032-01-10");
        var f = await api.GrupoNoBancoAsync(futuras);
        await loja.Painel.Post($"{Rota}/lancamentos/{f[2].Id}/parcelas/pagamento", new { ids = new[] { f[2].Id }, pago = true });

        await loja.Painel.Put($"{Rota}/lancamentos/{f[1].Id}?modo=futuras", new { descricao = "Futuras", contaBancariaId = conta, valor = 90m, vencimento = "2032-02-05" });

        var depoisFuturas = await api.GrupoNoBancoAsync(futuras);
        Assert.Equal(["Financiamento", "Futuras", "Financiamento", "Futuras"], depoisFuturas.Select(x => x.Descricao));
        Assert.Equal([50m, 90m, 50m, 90m], depoisFuturas.Select(x => x.Valor));
        Assert.Equal([Utc(2032, 1, 10), Utc(2032, 2, 5), Utc(2032, 3, 10), Utc(2032, 4, 5)], depoisFuturas.Select(x => x.Vencimento));
    }

    [FatoIntegracao]
    public async Task Editar_fixo_todas_muda_o_modelo_e_regera_e_futuras_muda_da_data_em_diante()
    {
        var loja = await NovaAsync();
        var conta = await loja.ContaPelaApiAsync();
        var id = (await loja.Painel.Post($"{Rota}/fixos", new { contaBancariaId = conta, tipo = "pagar", descricao = "Aluguel", valor = 900m, diaVencimento = 5, observacao = "o" })).Json.GetProperty("id").GetGuid();
        var proximo = MesAtual().AddMonths(1);
        var segundo = MesAtual().AddMonths(2);
        var lista = await api.LancamentosDaLojaNoBancoAsync(loja.Id);
        var doProximo = lista.Single(l => l.Vencimento >= proximo && l.Vencimento < segundo);

        await loja.Painel.Put($"{Rota}/lancamentos/{doProximo.Id}?modo=todas", new { descricao = "Aluguel T", contaBancariaId = conta, valor = 1000m, vencimento = "2032-01-18", observacao = "novo" });

        var fixo = Assert.Single(await api.FixosNoBancoAsync(loja.Id));
        Assert.Equal(("Aluguel T", 1000m, 18, "novo"), (fixo.Descricao, fixo.Valor, fixo.DiaVencimento, fixo.Observacao));
        var aposTodas = (await api.LancamentosDaLojaNoBancoAsync(loja.Id)).Single(l => l.Vencimento >= proximo && l.Vencimento < segundo);
        Assert.Equal(("Aluguel T", 1000m, 18), (aposTodas.Descricao, aposTodas.Valor, aposTodas.Vencimento.Day));

        var terceiro = MesAtual().AddMonths(3);
        var doTerceiro = (await api.LancamentosDaLojaNoBancoAsync(loja.Id)).Single(l => l.Vencimento >= terceiro && l.Vencimento < terceiro.AddMonths(1));
        await loja.Painel.Put($"{Rota}/lancamentos/{doTerceiro.Id}?modo=futuras", new { descricao = "Aluguel F", contaBancariaId = conta, valor = 1200m, vencimento = "2032-01-25" });

        var depois = await api.LancamentosDaLojaNoBancoAsync(loja.Id);
        Assert.Equal("Aluguel T", depois.Single(l => l.Vencimento >= segundo && l.Vencimento < terceiro).Descricao);
        Assert.Equal(("Aluguel F", 1200m, 25), (depois.Last().Descricao, depois.Last().Valor, depois.Last().Vencimento.Day));
        Assert.Equal(("Aluguel F", 1200m, 25), (depois.Single(l => l.Id == doTerceiro.Id).Descricao, depois.Single(l => l.Id == doTerceiro.Id).Valor, depois.Single(l => l.Id == doTerceiro.Id).Vencimento.Day));
        var fixoFinal = Assert.Single(await api.FixosNoBancoAsync(loja.Id));
        Assert.Equal(("Aluguel F", 1200m, 25), (fixoFinal.Descricao, fixoFinal.Valor, fixoFinal.DiaVencimento));
    }

    // ── Excluir lançamento ───────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Excluir_unica_remove_so_o_lancamento_e_so_da_propria_loja()
    {
        var loja = await NovaAsync();
        var outra = await NovaAsync();
        var conta = await loja.ContaPelaApiAsync();
        var grupo = await loja.ParceladoPelaApiAsync(conta, 3, 50m);
        var p = await api.GrupoNoBancoAsync(grupo);

        Assert.Equal(404, (await outra.Painel.Delete($"{Rota}/lancamentos/{p[1].Id}")).Codigo);
        var r = await loja.Painel.Delete($"{Rota}/lancamentos/{p[1].Id}");

        Assert.Equal(200, r.Codigo);
        Assert.Equal("Lançamento excluído.", r.Json.GetProperty("mensagem").GetString());
        Assert.Equal([p[0].Id, p[2].Id], (await api.GrupoNoBancoAsync(grupo)).Select(x => x.Id));
        Assert.Equal(404, (await loja.Painel.Delete($"{Rota}/lancamentos/{p[1].Id}")).Codigo);
        var pago = await loja.AvulsoPelaApiAsync(conta, jaPago: true);
        Assert.Equal(200, (await loja.Painel.Delete($"{Rota}/lancamentos/{pago}?modo=qualquer")).Codigo);
    }

    [FatoIntegracao]
    public async Task Excluir_todas_de_um_parcelamento_remove_as_pendentes_e_preserva_as_pagas()
    {
        var loja = await NovaAsync();
        var conta = await loja.ContaPelaApiAsync();
        var grupo = await loja.ParceladoPelaApiAsync(conta, 4, 50m);
        var p = await api.GrupoNoBancoAsync(grupo);
        await loja.Painel.Post($"{Rota}/lancamentos/{p[0].Id}/parcelas/pagamento", new { ids = new[] { p[0].Id }, pago = true });

        var r = await loja.Painel.Delete($"{Rota}/lancamentos/{p[2].Id}?modo=todas");

        Assert.Equal(200, r.Codigo);
        Assert.Equal([p[0].Id], (await api.GrupoNoBancoAsync(grupo)).Select(x => x.Id));
    }

    [FatoIntegracao]
    public async Task Excluir_todas_de_um_fixo_desativa_o_modelo_e_remove_as_pendentes_a_partir_dele()
    {
        var loja = await NovaAsync();
        var conta = await loja.ContaPelaApiAsync();
        var id = (await loja.Painel.Post($"{Rota}/fixos", new { contaBancariaId = conta, tipo = "pagar", descricao = "Água", valor = 40m, diaVencimento = 7 })).Json.GetProperty("id").GetGuid();
        var lista = await api.LancamentosDaLojaNoBancoAsync(loja.Id);
        var alvo = lista[5];
        await api.NoBancoAsync(db => db.LancamentosFinanceiros.Where(l => l.Id == lista[7].Id).ExecuteUpdateAsync(s => s.SetProperty(l => l.Status, "pago")));

        var r = await loja.Painel.Delete($"{Rota}/lancamentos/{alvo.Id}?modo=todas");

        Assert.Equal(200, r.Codigo);
        Assert.False(Assert.Single(await api.FixosNoBancoAsync(loja.Id)).Ativa);
        var restantes = await api.LancamentosDaLojaNoBancoAsync(loja.Id);
        Assert.Equal(lista.Take(5).Select(l => l.Id).Append(lista[7].Id), restantes.Select(l => l.Id));
        Assert.All(restantes, l => Assert.Equal(id, l.LancamentoFixoId));
    }
}
