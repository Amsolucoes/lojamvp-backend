using Microsoft.EntityFrameworkCore;
using static LojaApi.Tests.Suporte.CenariosFinanceiro;

namespace LojaApi.Tests.Api.FinanceiroTeste;

// Financeiro, parte 2: lançamentos avulsos e parcelados, listagens, pagamento (um e em lote), aviso e descrições recentes.
[Collection(ColecaoApi.Nome)]
public class FinanceiroLancamentosApiTests(ApiFixture api)
{
    private Task<LojaTeste> NovaAsync() => api.NovaLojaAsync();

    private static async Task<(int, string)> Erro(Task<Resposta> r) { var x = await r; return (x.Codigo, x.Erro); }

    private static DateTime Utc(int ano, int mes, int dia, int hora = 12) => new(ano, mes, dia, hora, 0, 0, DateTimeKind.Utc);

    [FatoIntegracao]
    public async Task Exige_login_mas_qualquer_papel_da_loja_usa_os_lancamentos()
    {
        var loja = await NovaAsync();
        var operador = await api.OperadorAsync(loja);
        var conta = await loja.ContaPelaApiAsync();
        var id = await loja.AvulsoPelaApiAsync(conta);

        Assert.Equal(401, (await loja.Site.Get($"{Rota}/lancamentos?tipo=pagar")).Codigo);
        Assert.Equal(401, (await loja.Site.Post($"{Rota}/lancamentos/avulso", new { })).Codigo);
        Assert.Equal(401, (await loja.Site.Delete($"{Rota}/lancamentos/{id}")).Codigo);
        Assert.Equal(200, (await operador.Get($"{Rota}/lancamentos?tipo=pagar")).Codigo);
        Assert.Equal(200, (await operador.Patch($"{Rota}/lancamentos/{id}/avisar", new { avisar = false })).Codigo);
    }

    // ── Avulso ───────────────────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Criar_avulso_grava_com_vencimento_ao_meio_dia_descricao_cortada_e_ignora_o_avisar_do_corpo()
    {
        var loja = await NovaAsync();
        var conta = await loja.ContaPelaApiAsync();

        var r = await loja.Painel.Post($"{Rota}/lancamentos/avulso", new
        {
            contaBancariaId = conta, tipo = "pagar", descricao = "  Luz  ", categoriaId = (Guid?)null, valor = 80.5m,
            vencimento = "2032-03-10T23:30:00", observacao = "obs", jaPago = false, avisar = false,
        });

        Assert.Equal(200, r.Codigo);
        Assert.Equal(["id"], r.Json.EnumerateObject().Select(x => x.Name));
        var l = await api.LancamentoNoBancoPorIdAsync(r.Json.GetProperty("id").GetGuid());
        Assert.Equal((loja.Id, conta, "pagar", "avulsa", "Luz", 80.5m, "obs"), (l.LojaId, l.ContaBancariaId, l.Tipo, l.Modo, l.Descricao, l.Valor, l.Observacao));
        Assert.Equal((Utc(2032, 3, 10), "pendente", null, true), (l.Vencimento, l.Status, l.PagoEm, l.Avisar));
    }

    [FatoIntegracao]
    public async Task Criar_avulso_ja_pago_marca_pago_agora_e_sem_loja_devolve_400()
    {
        var loja = await NovaAsync();
        var conta = await loja.ContaPelaApiAsync();

        var id = await loja.AvulsoPelaApiAsync(conta, jaPago: true);

        var l = await api.LancamentoNoBancoPorIdAsync(id);
        Assert.Equal("pago", l.Status);
        Assert.InRange((DateTime.UtcNow - l.PagoEm!.Value).TotalMinutes, -1, 5);

        await api.SemVinculoAsync(loja);
        Assert.Equal((400, "Loja não encontrada."), await Erro(loja.Painel.Post($"{Rota}/lancamentos/avulso",
            new { contaBancariaId = conta, tipo = "pagar", descricao = "x", valor = 1m, vencimento = "2032-03-10" })));
    }

    // ── Parcelado ────────────────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Criar_parcelado_gera_uma_parcela_por_mes_com_o_mesmo_grupo_e_so_a_primeira_paga_se_ja_pago()
    {
        var loja = await NovaAsync();
        var conta = await loja.ContaPelaApiAsync();

        var r = await loja.Painel.Post($"{Rota}/lancamentos/parcelado", new
        {
            contaBancariaId = conta, tipo = "pagar", descricao = " Geladeira ", categoriaId = (Guid?)null, valorParcela = 120m, totalParcelas = 3,
            primeiroVencimento = "2032-01-31", observacao = "x", jaPago = true,
        });

        Assert.Equal(200, r.Codigo);
        Assert.Equal(["grupoParcelamentoId", "totalGerado"], r.Json.EnumerateObject().Select(x => x.Name));
        Assert.Equal(3, r.Json.GetProperty("totalGerado").GetInt32());
        var grupo = await api.GrupoNoBancoAsync(r.Json.GetProperty("grupoParcelamentoId").GetGuid());
        Assert.Equal([1, 2, 3], grupo.Select(p => p.NumeroParcela!.Value));
        Assert.All(grupo, p => Assert.Equal((loja.Id, "parcelada", "Geladeira", 120m, 3, "x"), (p.LojaId, p.Modo, p.Descricao, p.Valor, p.TotalParcelas!.Value, p.Observacao)));
        Assert.Equal([Utc(2032, 1, 31), Utc(2032, 2, 29), Utc(2032, 3, 31)], grupo.Select(p => p.Vencimento));
        Assert.Equal(["pago", "pendente", "pendente"], grupo.Select(p => p.Status));
        Assert.NotNull(grupo[0].PagoEm);
        Assert.Null(grupo[1].PagoEm);
    }

    [FatoIntegracao]
    public async Task Criar_parcelado_com_data_fim_calcula_o_total_pelos_meses_e_valida_de_2_a_120()
    {
        var loja = await NovaAsync();
        var conta = await loja.ContaPelaApiAsync();
        object Corpo(int? total, string? fim) => new
        {
            contaBancariaId = conta, tipo = "pagar", descricao = "Carro", valorParcela = 10m, totalParcelas = total, primeiroVencimento = "2032-01-10", dataFim = fim,
        };
        const string mensagem = "Número de parcelas deve ficar entre 2 e 120. Ajuste a quantidade ou a data fim.";

        var porFim = await loja.Painel.Post($"{Rota}/lancamentos/parcelado", Corpo(99, "2032-06-01"));
        Assert.Equal(6, porFim.Json.GetProperty("totalGerado").GetInt32());

        Assert.Equal((400, mensagem), await Erro(loja.Painel.Post($"{Rota}/lancamentos/parcelado", Corpo(1, null))));
        Assert.Equal((400, mensagem), await Erro(loja.Painel.Post($"{Rota}/lancamentos/parcelado", Corpo(121, null))));
        Assert.Equal((400, mensagem), await Erro(loja.Painel.Post($"{Rota}/lancamentos/parcelado", Corpo(null, null))));
        Assert.Equal((400, mensagem), await Erro(loja.Painel.Post($"{Rota}/lancamentos/parcelado", Corpo(5, "2032-01-20"))));
        Assert.Equal(120, (await loja.Painel.Post($"{Rota}/lancamentos/parcelado", Corpo(120, null))).Json.GetProperty("totalGerado").GetInt32());
        Assert.Equal(126, (await api.LancamentosDaLojaNoBancoAsync(loja.Id)).Count);

        await api.SemVinculoAsync(loja);
        Assert.Equal((400, "Loja não encontrada."), await Erro(loja.Painel.Post($"{Rota}/lancamentos/parcelado", Corpo(3, null))));
    }

    // ── Listar ───────────────────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Listar_filtra_por_tipo_e_mes_ordena_por_vencimento_e_so_traz_da_loja()
    {
        var loja = await NovaAsync();
        var outra = await NovaAsync();
        var conta = await loja.ContaPelaApiAsync();
        var categoria = await loja.Painel.Post($"{Rota}/categorias", new { nome = "Moradia", tipo = "pagar" });
        var categoriaId = categoria.Json.GetProperty("id").GetGuid();
        var tarde = await loja.AvulsoPelaApiAsync(conta, "pagar", "Tarde", 20m, "2032-03-25", categoriaId);
        var cedo = await loja.AvulsoPelaApiAsync(conta, "pagar", "Cedo", 10m, "2032-03-05", jaPago: true);
        await loja.AvulsoPelaApiAsync(conta, "pagar", "Outro mês", 30m, "2032-04-05");
        await loja.AvulsoPelaApiAsync(conta, "receber", "Recebimento", 40m, "2032-03-10");
        await outra.AvulsoPelaApiAsync(await outra.ContaPelaApiAsync(), "pagar", "Alheio", 50m, "2032-03-10");

        var marco = (await loja.Painel.Get($"{Rota}/lancamentos?tipo=pagar&ano=2032&mes=3")).Json.EnumerateArray().ToList();
        var todos = (await loja.Painel.Get($"{Rota}/lancamentos?tipo=pagar")).Json.EnumerateArray().ToList();

        Assert.Equal([cedo, tarde], marco.Select(x => x.GetProperty("id").GetGuid()));
        Assert.Equal(["Cedo", "Tarde", "Outro mês"], todos.Select(x => x.GetProperty("descricao").GetString()));
        Assert.Equal(["id", "contaBancariaId", "descricao", "categoriaNome", "modo", "valor", "vencimento", "status", "pagoEm", "numeroParcela", "totalParcelas", "origem"],
            marco[0].EnumerateObject().Select(x => x.Name));
        Assert.Equal((conta, "avulsa", 10m, "pago", "lancamento"), (marco[0].GetProperty("contaBancariaId").GetGuid(), marco[0].GetProperty("modo").GetString()!,
            marco[0].GetProperty("valor").GetDecimal(), marco[0].GetProperty("status").GetString()!, marco[0].GetProperty("origem").GetString()!));
        Assert.Equal(JsonValueKindNulo, marco[0].GetProperty("categoriaNome").ValueKind);
        Assert.Equal("Moradia", marco[1].GetProperty("categoriaNome").GetString());
        Assert.Empty((await loja.Painel.Get($"{Rota}/lancamentos?tipo=pagar&ano=2032&mes=9")).Json.EnumerateArray());
        Assert.Empty((await loja.Painel.Get($"{Rota}/lancamentos?tipo=nenhum")).Json.EnumerateArray());

        await api.SemVinculoAsync(loja);
        Assert.Empty((await loja.Painel.Get($"{Rota}/lancamentos?tipo=pagar")).Json.EnumerateArray());
    }

    private static readonly System.Text.Json.JsonValueKind JsonValueKindNulo = System.Text.Json.JsonValueKind.Null;

    [FatoIntegracao]
    public async Task Receber_unificado_junta_lancamentos_a_receber_e_mensalidades_dos_planos_ordenados_por_vencimento()
    {
        var loja = await NovaAsync();
        var outra = await NovaAsync();
        var conta = await loja.ContaPelaApiAsync();
        await loja.AvulsoPelaApiAsync(conta, "pagar", "Despesa", 5m, "2032-03-10");
        var avulso = await loja.AvulsoPelaApiAsync(conta, "receber", "Aluguel recebido", 300m, "2032-03-20");
        var foraDoPeriodo = await loja.AvulsoPelaApiAsync(conta, "receber", "Antigo", 1m, "2031-12-20");
        await api.NoBancoAsync(async db =>
        {
            var cliente = new LojaApi.Models.Cliente { LojaId = loja.Id, Nome = "Maria", Telefone = "11999990000" };
            var assinatura = new LojaApi.Models.AssinaturaCliente { LojaId = loja.Id, ClienteId = cliente.Id, PlanoId = Guid.NewGuid() };
            db.Clientes.Add(cliente);
            db.AssinaturasCliente.Add(assinatura);
            db.PagamentosPlano.Add(new LojaApi.Models.PagamentoPlano { LojaId = loja.Id, AssinaturaId = assinatura.Id, MesReferencia = Utc(2032, 3, 1, 0), Valor = 99m });
            await db.SaveChangesAsync();
        });

        var resp = await loja.Painel.Get($"{Rota}/receber-unificado?de=2032-03-01T00:00:00Z&ate=2032-03-31T00:00:00Z");
        Assert.True(resp.Sucesso, resp.Codigo + " " + resp.Corpo);
        var lista = resp.Json.EnumerateArray().ToList();

        Assert.Equal(["plano", "avulso"], lista.Select(x => x.GetProperty("origem").GetString()));
        Assert.Equal("Mensalidade - Maria", lista[0].GetProperty("descricao").GetString());
        Assert.Equal(99m, lista[0].GetProperty("valor").GetDecimal());
        Assert.Equal(["id", "descricao", "observacao", "categoriaNome", "modo", "categoriaId", "contaBancariaId", "numeroParcela", "totalParcelas", "valor", "vencimento", "status", "pagoEm", "origem"],
            lista[1].EnumerateObject().Select(x => x.Name));
        Assert.Equal(avulso, lista[1].GetProperty("id").GetGuid());
        Assert.Equal(3, (await loja.Painel.Get($"{Rota}/receber-unificado")).Json.GetArrayLength());
        Assert.Equal([foraDoPeriodo], (await loja.Painel.Get($"{Rota}/receber-unificado?ate=2031-12-20T00:00:00Z")).Json.EnumerateArray().Select(x => x.GetProperty("id").GetGuid()));
        Assert.Empty((await outra.Painel.Get($"{Rota}/receber-unificado")).Json.EnumerateArray());

        await api.SemVinculoAsync(loja);
        Assert.Empty((await loja.Painel.Get($"{Rota}/receber-unificado")).Json.EnumerateArray());
    }

    // ── Pagamento ────────────────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Marcar_pagamento_alterna_pago_e_pendente_e_absorvida_sem_conta_tira_do_saldo()
    {
        var loja = await NovaAsync();
        var outra = await NovaAsync();
        var conta = await loja.ContaPelaApiAsync("Principal", 100m);
        var id = await loja.AvulsoPelaApiAsync(conta, "pagar", "Boleto", 30m);

        var pago = await loja.Painel.Post($"{Rota}/lancamentos/{id}/pagamento", new { pago = true });
        Assert.Equal(200, pago.Codigo);
        Assert.Equal(["id", "status"], pago.Json.EnumerateObject().Select(x => x.Name));
        Assert.Equal("pago", pago.Json.GetProperty("status").GetString());
        var l = await api.LancamentoNoBancoPorIdAsync(id);
        Assert.Equal(("pago", false), (l.Status, l.NaoAfetaSaldo));
        Assert.NotNull(l.PagoEm);
        Assert.Equal(70m, (await api.ContaNoBancoAsync(conta)).SaldoInicial - 30m);

        await loja.Painel.Post($"{Rota}/lancamentos/{id}/pagamento", new { pago = false, absorvidaSemConta = true });
        l = await api.LancamentoNoBancoPorIdAsync(id);
        Assert.Equal(("pendente", null, false), (l.Status, l.PagoEm, l.NaoAfetaSaldo));

        await loja.Painel.Post($"{Rota}/lancamentos/{id}/pagamento", new { pago = true, absorvidaSemConta = true });
        l = await api.LancamentoNoBancoPorIdAsync(id);
        Assert.Equal(("pago", true), (l.Status, l.NaoAfetaSaldo));
        var saldo = (await loja.Painel.Get($"{Rota}/contas")).Json.EnumerateArray().Single().GetProperty("saldoAtual").GetDecimal();
        Assert.Equal(100m, saldo);

        Assert.Equal(404, (await outra.Painel.Post($"{Rota}/lancamentos/{id}/pagamento", new { pago = true })).Codigo);
        Assert.Equal(404, (await loja.Painel.Post($"{Rota}/lancamentos/{Guid.NewGuid()}/pagamento", new { pago = true })).Codigo);
    }

    [FatoIntegracao]
    public async Task Alternar_avisar_so_na_propria_loja()
    {
        var loja = await NovaAsync();
        var outra = await NovaAsync();
        var id = await loja.AvulsoPelaApiAsync(await loja.ContaPelaApiAsync());

        var r = await loja.Painel.Patch($"{Rota}/lancamentos/{id}/avisar", new { avisar = false });

        Assert.Equal(200, r.Codigo);
        Assert.Equal(["id", "avisar"], r.Json.EnumerateObject().Select(x => x.Name));
        Assert.False(r.Json.GetProperty("avisar").GetBoolean());
        Assert.False((await api.LancamentoNoBancoPorIdAsync(id)).Avisar);
        Assert.Equal(404, (await outra.Painel.Patch($"{Rota}/lancamentos/{id}/avisar", new { avisar = true })).Codigo);
        Assert.False((await api.LancamentoNoBancoPorIdAsync(id)).Avisar);
    }

    [FatoIntegracao]
    public async Task Descricoes_recentes_sao_distintas_do_tipo_da_loja_da_mais_nova_para_a_mais_antiga()
    {
        var loja = await NovaAsync();
        var outra = await NovaAsync();
        var conta = await loja.ContaPelaApiAsync();
        await loja.AvulsoPelaApiAsync(conta, "pagar", "Primeira");
        await loja.AvulsoPelaApiAsync(conta, "pagar", "Segunda");
        await loja.AvulsoPelaApiAsync(conta, "pagar", "Primeira");
        await loja.AvulsoPelaApiAsync(conta, "receber", "Recebida");
        await outra.AvulsoPelaApiAsync(await outra.ContaPelaApiAsync(), "pagar", "Alheia");

        var pagar = (await loja.Painel.Get($"{Rota}/lancamentos/descricoes?tipo=pagar")).Json.EnumerateArray().Select(x => x.GetString()).ToList();

        Assert.Equal(["Primeira", "Segunda"], pagar.Order().ToList());
        Assert.Equal(2, pagar.Count);
        Assert.Equal(["Recebida"], (await loja.Painel.Get($"{Rota}/lancamentos/descricoes?tipo=receber")).Json.EnumerateArray().Select(x => x.GetString()));

        await api.SemVinculoAsync(loja);
        Assert.Empty((await loja.Painel.Get($"{Rota}/lancamentos/descricoes?tipo=pagar")).Json.EnumerateArray());
    }

    // ── Pagar em lote ────────────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Pagar_lote_na_conta_marca_pago_na_data_pedida_e_troca_a_conta_do_lancamento()
    {
        var loja = await NovaAsync();
        var original = await loja.ContaPelaApiAsync("Original");
        var destino = await loja.ContaPelaApiAsync("Destino");
        var a = await loja.AvulsoPelaApiAsync(original, "pagar", "A", 10.5m);
        var b = await loja.AvulsoPelaApiAsync(original, "pagar", "B", 20m);

        var r = await loja.Painel.Post($"{Rota}/lancamentos/pagar-lote", new { ids = new[] { a, b, a }, forma = "conta", contaBancariaId = destino, data = "2032-05-20T22:00:00" });

        Assert.Equal(200, r.Codigo);
        Assert.Equal(["pagos", "total", "forma"], r.Json.EnumerateObject().Select(x => x.Name));
        Assert.Equal((2, 30.5m, "conta"), (r.Json.GetProperty("pagos").GetInt32(), r.Json.GetProperty("total").GetDecimal(), r.Json.GetProperty("forma").GetString()!));
        foreach (var id in new[] { a, b })
        {
            var l = await api.LancamentoNoBancoPorIdAsync(id);
            Assert.Equal((destino, "pago", Utc(2032, 5, 20)), (l.ContaBancariaId, l.Status, l.PagoEm));
        }

        var c = await loja.AvulsoPelaApiAsync(original, "pagar", "C", 1m);
        await loja.Painel.Post($"{Rota}/lancamentos/pagar-lote", new { ids = new[] { c }, forma = "conta", contaBancariaId = destino });
        Assert.InRange((DateTime.UtcNow - (await api.LancamentoNoBancoPorIdAsync(c)).PagoEm!.Value).TotalMinutes, -1, 5);
    }

    [FatoIntegracao]
    public async Task Pagar_lote_valida_selecao_status_tipo_conta_e_forma()
    {
        var loja = await NovaAsync();
        var outra = await NovaAsync();
        var conta = await loja.ContaPelaApiAsync();
        var contaAlheia = await outra.ContaPelaApiAsync();
        var pendente = await loja.AvulsoPelaApiAsync(conta, "pagar", "Pendente");
        var paga = await loja.AvulsoPelaApiAsync(conta, "pagar", "Paga", jaPago: true);
        var receber = await loja.AvulsoPelaApiAsync(conta, "receber", "Receber");
        var alheia = await outra.AvulsoPelaApiAsync(contaAlheia, "pagar", "Alheia");
        const string naoPendente = "Alguma conta selecionada não está mais pendente. Atualize a lista e tente de novo.";

        Assert.Equal((400, "Selecione ao menos uma conta."), await Erro(loja.Painel.Post($"{Rota}/lancamentos/pagar-lote", new { ids = Array.Empty<Guid>(), forma = "conta", contaBancariaId = conta })));
        Assert.Equal((400, naoPendente), await Erro(loja.Painel.Post($"{Rota}/lancamentos/pagar-lote", new { ids = new[] { pendente, paga }, forma = "conta", contaBancariaId = conta })));
        Assert.Equal((400, naoPendente), await Erro(loja.Painel.Post($"{Rota}/lancamentos/pagar-lote", new { ids = new[] { receber }, forma = "conta", contaBancariaId = conta })));
        Assert.Equal((400, naoPendente), await Erro(loja.Painel.Post($"{Rota}/lancamentos/pagar-lote", new { ids = new[] { alheia }, forma = "conta", contaBancariaId = contaAlheia })));
        Assert.Equal((400, "Escolha a conta de onde sai o pagamento."), await Erro(loja.Painel.Post($"{Rota}/lancamentos/pagar-lote", new { ids = new[] { pendente }, forma = "conta", contaBancariaId = contaAlheia })));
        Assert.Equal((400, "Escolha a conta de onde sai o pagamento."), await Erro(loja.Painel.Post($"{Rota}/lancamentos/pagar-lote", new { ids = new[] { pendente }, forma = "conta" })));
        Assert.Equal((400, "Escolha o cartão de crédito."), await Erro(loja.Painel.Post($"{Rota}/lancamentos/pagar-lote", new { ids = new[] { pendente }, forma = "cartao" })));
        Assert.Equal((400, "Forma de pagamento inválida."), await Erro(loja.Painel.Post($"{Rota}/lancamentos/pagar-lote", new { ids = new[] { pendente }, forma = "pix" })));
        Assert.Equal("pendente", (await api.LancamentoNoBancoPorIdAsync(pendente)).Status);

        await api.SemVinculoAsync(loja);
        Assert.Equal((400, "Loja não encontrada."), await Erro(loja.Painel.Post($"{Rota}/lancamentos/pagar-lote", new { ids = new[] { pendente }, forma = "conta", contaBancariaId = conta })));
    }

    [FatoIntegracao]
    public async Task Pagar_lote_no_cartao_a_vista_vira_compra_do_cartao_e_some_de_contas_a_pagar()
    {
        var loja = await NovaAsync();
        var conta = await loja.ContaPelaApiAsync();
        var cartao = await api.CartaoNoBancoAsync(loja.Id, conta);
        var categoria = (await loja.Painel.Post($"{Rota}/categorias", new { nome = "Mercado", tipo = "pagar" })).Json.GetProperty("id").GetGuid();
        var id = await loja.AvulsoPelaApiAsync(conta, "pagar", "Compra grande", 90m, categoriaId: categoria);

        var r = await loja.Painel.Post($"{Rota}/lancamentos/pagar-lote", new { ids = new[] { id }, forma = "cartao", cartaoId = cartao, data = "2032-05-20" });

        Assert.Equal(200, r.Codigo);
        Assert.Equal((1, 90m, "cartao"), (r.Json.GetProperty("pagos").GetInt32(), r.Json.GetProperty("total").GetDecimal(), r.Json.GetProperty("forma").GetString()!));
        Assert.Empty(await api.LancamentosDaLojaNoBancoAsync(loja.Id));
        var compra = Assert.Single(await api.LancamentosDeCartaoNoBancoAsync(loja.Id));
        Assert.Equal((cartao, "Compra grande", 90m, Utc(2032, 5, 20), "avulsa", categoria), (compra.CartaoCreditoId, compra.Descricao, compra.Valor, compra.DataCompra, compra.Modo, compra.CategoriaId));
        Assert.Null(compra.GrupoParcelamentoId);
    }

    [FatoIntegracao]
    public async Task Pagar_lote_no_cartao_parcelado_divide_em_parcelas_e_a_ultima_absorve_os_centavos()
    {
        var loja = await NovaAsync();
        var conta = await loja.ContaPelaApiAsync();
        var cartao = await api.CartaoNoBancoAsync(loja.Id, conta);
        var id = await loja.AvulsoPelaApiAsync(conta, "pagar", "Sofá", 100m);

        var r = await loja.Painel.Post($"{Rota}/lancamentos/pagar-lote", new { ids = new[] { id }, forma = "cartao", cartaoId = cartao, parcelas = 3, data = "2032-05-31" });

        Assert.Equal(200, r.Codigo);
        var compras = await api.LancamentosDeCartaoNoBancoAsync(loja.Id);
        Assert.Equal(["Sofá (1/3)", "Sofá (2/3)", "Sofá (3/3)"], compras.Select(c => c.Descricao));
        Assert.Equal([33.33m, 33.33m, 33.34m], compras.Select(c => c.Valor));
        Assert.Equal([Utc(2032, 5, 31), Utc(2032, 6, 30), Utc(2032, 7, 31)], compras.Select(c => c.DataCompra));
        Assert.All(compras, c => Assert.Equal(("parcelada", 3), (c.Modo, c.TotalParcelas!.Value)));
        Assert.Single(compras.Select(c => c.GrupoParcelamentoId).Distinct());
        Assert.Equal([1, 2, 3], compras.Select(c => c.NumeroParcela!.Value));

        var outro = await loja.AvulsoPelaApiAsync(conta, "pagar", "Outro", 10m);
        Assert.Equal((400, "Escolha entre 1 e 24 parcelas."), await Erro(loja.Painel.Post($"{Rota}/lancamentos/pagar-lote", new { ids = new[] { outro }, forma = "cartao", cartaoId = cartao, parcelas = 25 })));
        Assert.Equal((400, "Escolha entre 1 e 24 parcelas."), await Erro(loja.Painel.Post($"{Rota}/lancamentos/pagar-lote", new { ids = new[] { outro }, forma = "cartao", cartaoId = cartao, parcelas = 0 })));
        Assert.Equal("pendente", (await api.LancamentoNoBancoPorIdAsync(outro)).Status);
    }
}
