using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using static LojaApi.Tests.Suporte.CenariosFinanceiro;

namespace LojaApi.Tests.Api.FinanceiroTeste;

// Financeiro, parte 3c: Contas a Pagar unificado (lançamentos + faturas de cartão) e faturas do mês.
// Cartão de teste: fecha dia 10 e vence dia 15. A fatura de jan/2032 cobre as compras de 11/12/2031 a 10/01/2032 e vence em 15/01/2032.
[Collection(ColecaoApi.Nome)]
public class FinanceiroContasAPagarApiTests(ApiFixture api)
{
    private Task<LojaTeste> NovaAsync() => api.NovaLojaAsync();

    private static DateTime Utc(int ano, int mes, int dia, int hora = 12) => new(ano, mes, dia, hora, 0, 0, DateTimeKind.Utc);

    private async Task<(LojaTeste Loja, Guid Conta, Guid Cartao)> CenarioAsync(string nome = "Cartão Pagar")
    {
        var loja = await NovaAsync();
        var conta = await loja.ContaPelaApiAsync();
        return (loja, conta, await loja.CartaoPelaApiAsync(conta, nome));
    }

    private static string Pagar(string modo = "agrupado", int ano = 2032, int mes = 1) => $"{Rota}/pagar-unificado?ano={ano}&mes={mes}&modo={modo}";

    private static string Faturas(int ano = 2032, int mes = 1) => $"{Rota}/faturas-do-mes?ano={ano}&mes={mes}";

    private static List<JsonElement> Linhas(Resposta r) => r.Json.EnumerateArray().ToList();

    private static string Texto(JsonElement e, string campo) => e.GetProperty(campo).GetString()!;

    // ── Pagar unificado: lançamentos ─────────────────────────────────────────

    [FatoIntegracao]
    public async Task Exige_login_mas_qualquer_papel_da_loja_ve_contas_a_pagar()
    {
        var (loja, _, _) = await CenarioAsync();
        var operador = await api.OperadorAsync(loja);

        Assert.Equal(401, (await loja.Site.Get(Pagar())).Codigo);
        Assert.Equal(401, (await loja.Site.Get(Faturas())).Codigo);
        Assert.Equal(200, (await operador.Get(Pagar())).Codigo);
        Assert.Equal(200, (await operador.Get(Faturas())).Codigo);
    }

    [FatoIntegracao]
    public async Task Pagar_unificado_traz_os_lancamentos_a_pagar_do_mes_da_loja_com_a_forma_de_pagamento()
    {
        var (loja, conta, _) = await CenarioAsync();
        var outra = await NovaAsync();
        var categoria = (await loja.Painel.Post($"{Rota}/categorias", new { nome = "Casa", tipo = "pagar" })).Json.GetProperty("id").GetGuid();
        var luz = await loja.AvulsoPelaApiAsync(conta, "pagar", "Luz", 80m, "2032-01-20", categoria);
        var agua = await loja.AvulsoPelaApiAsync(conta, "pagar", "Água", 40m, "2032-01-05", jaPago: true);
        await loja.AvulsoPelaApiAsync(conta, "pagar", "Outro mês", 1m, "2032-02-05");
        await loja.AvulsoPelaApiAsync(conta, "receber", "A receber", 1m, "2032-01-10");
        await outra.AvulsoPelaApiAsync(await outra.ContaPelaApiAsync(), "pagar", "Alheia", 1m, "2032-01-10");
        await api.NoBancoAsync(db => db.LancamentosFinanceiros.Where(l => l.Id == agua).ExecuteUpdateAsync(s => s.SetProperty(l => l.FormaPagamento, "pix").SetProperty(l => l.Observacao, "obs")));
        // parcela de financiamento de cartão não aparece como lançamento avulso
        await api.ParcelaDeFinanciamentoNoBancoAsync(loja.Id, conta, Guid.NewGuid(), Utc(2032, 1, 15), 9m);

        var r = await loja.Painel.Get(Pagar());

        Assert.Equal(200, r.Codigo);
        var linhas = Linhas(r);
        Assert.Equal(["Água", "Luz"], linhas.Select(x => Texto(x, "descricao")));
        Assert.Equal(["id", "descricao", "observacao", "categoriaNome", "categoriaId", "contaBancariaId", "modo", "avisar", "valor", "vencimento", "status", "pagoEm", "formaPagamento",
            "numeroParcela", "totalParcelas", "origem", "cartaoId", "cartaoNome"], linhas[0].EnumerateObject().Select(x => x.Name));
        Assert.Equal((agua, "obs", "pix", "pago", "avulso"), (linhas[0].GetProperty("id").GetGuid(), Texto(linhas[0], "observacao"), Texto(linhas[0], "formaPagamento"), Texto(linhas[0], "status"), Texto(linhas[0], "origem")));
        Assert.Equal((luz, "Casa", categoria, conta, "avulsa", true, 80m), (linhas[1].GetProperty("id").GetGuid(), Texto(linhas[1], "categoriaNome"), linhas[1].GetProperty("categoriaId").GetGuid(),
            linhas[1].GetProperty("contaBancariaId").GetGuid(), Texto(linhas[1], "modo"), linhas[1].GetProperty("avisar").GetBoolean(), linhas[1].GetProperty("valor").GetDecimal()));
        Assert.Equal(JsonValueKind.Null, linhas[1].GetProperty("cartaoId").ValueKind);
        Assert.Equal(JsonValueKind.Null, linhas[1].GetProperty("formaPagamento").ValueKind);
        Assert.Equal(JsonValueKind.Null, linhas[1].GetProperty("pagoEm").ValueKind);
    }

    [FatoIntegracao]
    public async Task Pagar_unificado_filtra_por_periodo_de_ate_inclusive_e_sem_parametros_usa_o_mes_atual()
    {
        var (loja, conta, _) = await CenarioAsync();
        await loja.AvulsoPelaApiAsync(conta, "pagar", "Dia 05", 1m, "2032-01-05");
        await loja.AvulsoPelaApiAsync(conta, "pagar", "Dia 10", 1m, "2032-01-10");
        await loja.AvulsoPelaApiAsync(conta, "pagar", "Dia 20", 1m, "2032-01-20");
        await loja.AvulsoPelaApiAsync(conta, "pagar", "Dia 21", 1m, "2032-01-21");
        var hoje = DateTime.UtcNow;
        await loja.AvulsoPelaApiAsync(conta, "pagar", "Deste mês", 1m, $"{hoje:yyyy-MM}-01");

        var periodo = await loja.Painel.Get($"{Rota}/pagar-unificado?de=2032-01-10&ate=2032-01-20");
        var atual = await loja.Painel.Get($"{Rota}/pagar-unificado");

        Assert.Equal(["Dia 10", "Dia 20"], Linhas(periodo).Select(x => Texto(x, "descricao")));
        Assert.Equal(["Deste mês"], Linhas(atual).Select(x => Texto(x, "descricao")));

        await api.SemVinculoAsync(loja);
        Assert.Empty(Linhas(await loja.Painel.Get(Pagar())));
    }

    // ── Pagar unificado: cartões ─────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Pagar_unificado_agrupado_mostra_uma_linha_por_fatura_do_cartao_ja_descontado_o_antecipado()
    {
        var (loja, conta, cartao) = await CenarioAsync();
        await loja.AvulsoPelaApiAsync(conta, "pagar", "Aluguel", 500m, "2032-01-05");
        await loja.CompraNoCartaoAsync(cartao, 100m, "2031-12-20");
        await loja.CompraNoCartaoAsync(cartao, 50m, "2032-01-10");
        await loja.CompraNoCartaoAsync(cartao, 9m, "2032-01-11");
        await loja.Painel.Post($"{Rota}/cartoes/{cartao}/fatura/antecipado?ano=2032&mes=1", new { valor = 30m, data = "2025-01-10", contaBancariaId = conta });

        var r = await loja.Painel.Get(Pagar());

        var linhas = Linhas(r);
        Assert.Equal(2, linhas.Count);
        Assert.Equal("Aluguel", Texto(linhas[0], "descricao"));
        var fatura = linhas[1];
        Assert.Equal(["id", "descricao", "categoriaNome", "valor", "vencimento", "status", "pagoEm", "numeroParcela", "totalParcelas", "origem", "cartaoId", "cartaoNome"], fatura.EnumerateObject().Select(x => x.Name));
        Assert.Equal((cartao, 120m, Utc(2032, 1, 15), "pendente", "cartao_fatura", cartao, "Cartão Pagar"), (fatura.GetProperty("id").GetGuid(), fatura.GetProperty("valor").GetDecimal(),
            fatura.GetProperty("vencimento").GetDateTime().ToUniversalTime(), Texto(fatura, "status"), Texto(fatura, "origem"), fatura.GetProperty("cartaoId").GetGuid(), Texto(fatura, "cartaoNome")));
        Assert.StartsWith("Fatura Cartão Pagar (já antecipado ", Texto(fatura, "descricao"));
        Assert.Equal(JsonValueKind.Null, fatura.GetProperty("categoriaNome").ValueKind);
        Assert.Equal(JsonValueKind.Null, fatura.GetProperty("numeroParcela").ValueKind);
    }

    [FatoIntegracao]
    public async Task Pagar_unificado_agrupado_sem_antecipado_chama_so_de_fatura_e_o_valor_e_o_total_das_compras()
    {
        var (loja, _, cartao) = await CenarioAsync();
        await loja.CompraNoCartaoAsync(cartao, 100m, "2032-01-05");

        var fatura = Assert.Single(Linhas(await loja.Painel.Get(Pagar())));

        Assert.Equal(("Fatura Cartão Pagar", 100m), (Texto(fatura, "descricao"), fatura.GetProperty("valor").GetDecimal()));
    }

    [FatoIntegracao]
    public async Task Pagar_unificado_nao_mostra_a_fatura_ja_paga_parcial_ou_financiada()
    {
        var loja = await NovaAsync();
        var conta = await loja.ContaPelaApiAsync();
        foreach (var status in new[] { "pago", "parcial", "financiada" })
        {
            var cartao = await loja.CartaoPelaApiAsync(conta, $"Cartão {status}");
            await loja.CompraNoCartaoAsync(cartao, 100m, "2032-01-05");
            await api.FaturaPagaDoMesNoBancoAsync(loja.Id, cartao, 2032, 1, status);
        }
        var pendente = await loja.CartaoPelaApiAsync(conta, "Cartão pendente");
        await loja.CompraNoCartaoAsync(pendente, 70m, "2032-01-05");

        var linhas = Linhas(await loja.Painel.Get(Pagar()));

        Assert.Equal(["Fatura Cartão pendente"], linhas.Select(x => Texto(x, "descricao")));
    }

    [FatoIntegracao]
    public async Task Pagar_unificado_soma_as_parcelas_de_financiamento_pendentes_na_fatura_do_cartao()
    {
        var (loja, conta, cartao) = await CenarioAsync();
        var soParcela = await loja.CartaoPelaApiAsync(conta, "Só parcela");
        await loja.CompraNoCartaoAsync(cartao, 100m, "2032-01-05");
        await api.ParcelaDeFinanciamentoNoBancoAsync(loja.Id, conta, cartao, Utc(2032, 1, 15), 25m);
        await api.ParcelaDeFinanciamentoNoBancoAsync(loja.Id, conta, soParcela, Utc(2032, 1, 15), 40m);
        await api.ParcelaDeFinanciamentoNoBancoAsync(loja.Id, conta, soParcela, Utc(2032, 1, 15), 5m, status: "pago");

        var linhas = Linhas(await loja.Painel.Get(Pagar()));

        var porNome = linhas.ToDictionary(x => Texto(x, "cartaoNome"));
        Assert.Equal(("Fatura Cartão Pagar", 125m), (Texto(porNome["Cartão Pagar"], "descricao"), porNome["Cartão Pagar"].GetProperty("valor").GetDecimal()));
        Assert.Equal(("Fatura Só parcela (parcela de financiamento)", 40m), (Texto(porNome["Só parcela"], "descricao"), porNome["Só parcela"].GetProperty("valor").GetDecimal()));
    }

    [FatoIntegracao]
    public async Task Pagar_unificado_detalhado_lista_cada_compra_e_cada_parcela_de_financiamento()
    {
        var (loja, conta, cartao) = await CenarioAsync();
        var categoria = (await loja.Painel.Post($"{Rota}/categorias", new { nome = "Mercado", tipo = "pagar" })).Json.GetProperty("id").GetGuid();
        await loja.Painel.Post($"{Rota}/cartoes/{cartao}/lancamentos", new { descricao = "Padaria", valor = 30m, dataCompra = "2032-01-05", categoriaId = categoria });
        await loja.CompraNoCartaoAsync(cartao, 20m, "2031-12-20", "Farmácia");
        var parcela = await api.ParcelaDeFinanciamentoNoBancoAsync(loja.Id, conta, cartao, Utc(2032, 1, 15), 25m, numero: 1, total: 3);
        var compras = await api.ComprasDoCartaoNoBancoAsync(cartao);

        var linhas = Linhas(await loja.Painel.Get(Pagar("detalhado")));

        Assert.Equal(["Cartão Pagar — Farmácia", "Cartão Pagar — Padaria", "Cartão Pagar — Financiamento antigo"], linhas.Select(x => Texto(x, "descricao")));
        Assert.All(linhas, x => Assert.Equal(("cartao_item", "pendente", cartao, Utc(2032, 1, 15)), (Texto(x, "origem"), Texto(x, "status"), x.GetProperty("cartaoId").GetGuid(), x.GetProperty("vencimento").GetDateTime().ToUniversalTime())));
        Assert.Equal((compras.Single(c => c.Descricao == "Padaria").Id, "Mercado", 30m), (linhas[1].GetProperty("id").GetGuid(), Texto(linhas[1], "categoriaNome"), linhas[1].GetProperty("valor").GetDecimal()));
        Assert.Equal((parcela, 1, 3, 25m), (linhas[2].GetProperty("id").GetGuid(), linhas[2].GetProperty("numeroParcela").GetInt32(), linhas[2].GetProperty("totalParcelas").GetInt32(), linhas[2].GetProperty("valor").GetDecimal()));
        Assert.Equal(JsonValueKind.Null, linhas[2].GetProperty("categoriaNome").ValueKind);
    }

    [FatoIntegracao]
    public async Task Pagar_unificado_detalhado_esconde_as_compras_de_fatura_resolvida_mas_mantem_parcelas_pendentes()
    {
        var (loja, conta, cartao) = await CenarioAsync();
        await loja.CompraNoCartaoAsync(cartao, 100m, "2032-01-05");
        await api.FaturaPagaDoMesNoBancoAsync(loja.Id, cartao, 2032, 1, "pago");
        await api.ParcelaDeFinanciamentoNoBancoAsync(loja.Id, conta, cartao, Utc(2032, 1, 15), 25m);

        var linhas = Linhas(await loja.Painel.Get(Pagar("detalhado")));

        Assert.Equal(["Cartão Pagar — Financiamento antigo"], linhas.Select(x => Texto(x, "descricao")));
    }

    [FatoIntegracao]
    public async Task Pagar_unificado_ignora_cartao_inativo_fatura_fora_do_periodo_e_cartao_de_outra_loja()
    {
        var (loja, conta, cartao) = await CenarioAsync();
        var (outra, _, cartaoAlheio) = await CenarioAsync("Alheio");
        await loja.CompraNoCartaoAsync(cartao, 100m, "2032-01-05");
        await outra.CompraNoCartaoAsync(cartaoAlheio, 100m, "2032-01-05");
        var inativo = await loja.CartaoPelaApiAsync(conta, "Inativo");
        await loja.CompraNoCartaoAsync(inativo, 100m, "2032-01-05");
        await api.NoBancoAsync(db => db.CartoesCredito.Where(c => c.Id == inativo).ExecuteUpdateAsync(s => s.SetProperty(c => c.Ativo, false)));

        Assert.Equal(["Fatura Cartão Pagar"], Linhas(await loja.Painel.Get(Pagar())).Select(x => Texto(x, "descricao")));
        Assert.Empty(Linhas(await loja.Painel.Get(Pagar(mes: 2))));
        Assert.Equal(["Fatura Cartão Pagar"], Linhas(await loja.Painel.Get($"{Rota}/pagar-unificado?de=2032-01-14&ate=2032-01-15")).Select(x => Texto(x, "descricao")));
        Assert.Empty(Linhas(await loja.Painel.Get($"{Rota}/pagar-unificado?de=2032-01-16&ate=2032-01-31")));
    }

    // ── Faturas do mês ───────────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Faturas_do_mes_sem_cartao_ou_sem_loja_devolve_lista_vazia()
    {
        var loja = await NovaAsync();

        Assert.Empty(Linhas(await loja.Painel.Get(Faturas())));

        var (comCartao, _, _) = await CenarioAsync();
        await api.SemVinculoAsync(comCartao);
        Assert.Empty(Linhas(await comCartao.Painel.Get(Faturas())));
    }

    [FatoIntegracao]
    public async Task Faturas_do_mes_soma_compras_e_parcelas_de_financiamento_e_abate_o_antecipado()
    {
        var (loja, conta, cartao) = await CenarioAsync();
        await loja.CompraNoCartaoAsync(cartao, 100m, "2031-12-20");
        await loja.CompraNoCartaoAsync(cartao, 50m, "2032-01-10");
        await loja.CompraNoCartaoAsync(cartao, 9m, "2032-01-11");
        await api.ParcelaDeFinanciamentoNoBancoAsync(loja.Id, conta, cartao, Utc(2032, 1, 15), 25m);
        await loja.Painel.Post($"{Rota}/cartoes/{cartao}/fatura/antecipado?ano=2032&mes=1", new { valor = 30m, data = "2025-01-10", contaBancariaId = conta });

        var r = await loja.Painel.Get(Faturas());

        var f = Assert.Single(Linhas(r));
        Assert.Equal(["cartaoId", "cartaoNome", "ano", "mes", "vencimento", "totalCompras", "totalParcelasFinanciamento", "total", "totalAntecipado", "restante", "status", "valorPago", "pagoEm", "parcelas"],
            f.EnumerateObject().Select(x => x.Name));
        Assert.Equal((cartao, "Cartão Pagar", 2032, 1, Utc(2032, 1, 15)), (f.GetProperty("cartaoId").GetGuid(), Texto(f, "cartaoNome"), f.GetProperty("ano").GetInt32(), f.GetProperty("mes").GetInt32(),
            f.GetProperty("vencimento").GetDateTime().ToUniversalTime()));
        Assert.Equal((150m, 25m, 175m, 30m, 145m, "pendente", 0m), (f.GetProperty("totalCompras").GetDecimal(), f.GetProperty("totalParcelasFinanciamento").GetDecimal(), f.GetProperty("total").GetDecimal(),
            f.GetProperty("totalAntecipado").GetDecimal(), f.GetProperty("restante").GetDecimal(), Texto(f, "status"), f.GetProperty("valorPago").GetDecimal()));
        Assert.Equal(JsonValueKind.Null, f.GetProperty("pagoEm").ValueKind);
        Assert.Empty(f.GetProperty("parcelas").EnumerateArray());
    }

    [FatoIntegracao]
    public async Task Faturas_do_mes_ordena_o_cronograma_pelo_numero_da_parcela_mesmo_inserido_fora_de_ordem()
    {
        var (loja, conta, cartao) = await CenarioAsync();
        await loja.CompraNoCartaoAsync(cartao, 90m, "2032-01-05");
        var fatura = await api.NoBancoAsync(async db =>
        {
            var f = new LojaApi.Models.FaturaCartao { LojaId = loja.Id, CartaoCreditoId = cartao, MesReferencia = Utc(2032, 1, 1, 0), Vencimento = Utc(2032, 1, 15), Total = 90m, ValorPago = 0m, Status = "financiada" };
            db.FaturasCartao.Add(f);
            await db.SaveChangesAsync();
            return f.Id;
        });
        foreach (var numero in new[] { 3, 1, 2 })
            await api.ParcelaDeFinanciamentoNoBancoAsync(loja.Id, conta, cartao, Utc(2032, 1 + numero, 15), 30m, numero: numero, total: 3, faturaId: fatura);

        var f = Assert.Single(Linhas(await loja.Painel.Get(Faturas())));

        Assert.Equal([1, 2, 3], f.GetProperty("parcelas").EnumerateArray().Select(p => p.GetProperty("numeroParcela").GetInt32()));
    }

    [FatoIntegracao]
    public async Task Faturas_do_mes_mostra_o_cronograma_das_parcelas_da_fatura_financiada()
    {
        var (loja, _, cartao) = await CenarioAsync();
        await loja.CompraNoCartaoAsync(cartao, 100m, "2032-01-05");
        await loja.Painel.Post($"{Rota}/cartoes/{cartao}/fatura/pagamento?ano=2032&mes=1", new { modo = "parcelado", totalParcelas = 2, valorEntrada = 20m });

        var f = Assert.Single(Linhas(await loja.Painel.Get(Faturas())));

        Assert.Equal(("financiada", 20m, 100m), (Texto(f, "status"), f.GetProperty("valorPago").GetDecimal(), f.GetProperty("total").GetDecimal()));
        Assert.NotEqual(JsonValueKind.Null, f.GetProperty("pagoEm").ValueKind);
        var parcelas = f.GetProperty("parcelas").EnumerateArray().ToList();
        Assert.Equal(["id", "numeroParcela", "totalParcelas", "valor", "vencimento", "status", "pagoEm"], parcelas[0].EnumerateObject().Select(x => x.Name));
        Assert.Equal([1, 2], parcelas.Select(p => p.GetProperty("numeroParcela").GetInt32()));
        Assert.All(parcelas, p => Assert.Equal((2, 40m, "pendente"), (p.GetProperty("totalParcelas").GetInt32(), p.GetProperty("valor").GetDecimal(), Texto(p, "status"))));
    }

    [FatoIntegracao]
    public async Task Faturas_do_mes_omite_cartao_sem_compras_nem_fatura_mas_mostra_fatura_existente_zerada_e_ignora_inativo_e_outra_loja()
    {
        var (loja, conta, vazio) = await CenarioAsync("Sem nada");
        var (outra, _, alheio) = await CenarioAsync("Alheio");
        var comFatura = await loja.CartaoPelaApiAsync(conta, "Fatura zerada");
        var inativo = await loja.CartaoPelaApiAsync(conta, "Inativo");
        await api.FaturaPagaDoMesNoBancoAsync(loja.Id, comFatura, 2032, 1, "pago");
        await loja.CompraNoCartaoAsync(inativo, 10m, "2032-01-05");
        await outra.CompraNoCartaoAsync(alheio, 10m, "2032-01-05");
        await api.NoBancoAsync(db => db.CartoesCredito.Where(c => c.Id == inativo).ExecuteUpdateAsync(s => s.SetProperty(c => c.Ativo, false)));

        var lista = Linhas(await loja.Painel.Get(Faturas()));

        var f = Assert.Single(lista);
        Assert.Equal(("Fatura zerada", 0m, "pago", 1m), (Texto(f, "cartaoNome"), f.GetProperty("total").GetDecimal(), Texto(f, "status"), f.GetProperty("valorPago").GetDecimal()));
        Assert.DoesNotContain(lista, x => x.GetProperty("cartaoId").GetGuid() == vazio);
    }

    // ── Autocorreção, vários cartões e adiantamentos ─────────────────────────

    [FatoIntegracao]
    public async Task Pagar_unificado_e_listar_lancamentos_corrigem_antes_o_vencimento_das_parcelas_de_financiamento()
    {
        var (loja, conta, cartao) = await CenarioAsync();
        async Task<Guid> ParcelaErradaAsync(string descricao) => await api.NoBancoAsync(async db =>
        {
            // nasceu em 02/01/2032 (antes do fechamento de 10/01): deveria vencer em 15/01, mas está com a data de junho
            var l = new LojaApi.Models.LancamentoFinanceiro
            {
                LojaId = loja.Id, ContaBancariaId = conta, Tipo = "pagar", Modo = "parcelada", Descricao = descricao, Valor = 5m, Vencimento = Utc(2032, 6, 15), Status = "pendente",
                GrupoParcelamentoId = Guid.NewGuid(), NumeroParcela = 1, TotalParcelas = 1, CartaoOrigemId = cartao, CriadoEm = Utc(2032, 1, 2),
            };
            db.LancamentosFinanceiros.Add(l);
            await db.SaveChangesAsync();
            return l.Id;
        });
        var viaPagar = await ParcelaErradaAsync("Errada pagar");

        var linhas = Linhas(await loja.Painel.Get(Pagar()));

        Assert.Equal(Utc(2032, 1, 15), (await api.LancamentoNoBancoPorIdAsync(viaPagar)).Vencimento);
        var fatura = Assert.Single(linhas);
        Assert.Equal(("Fatura Cartão Pagar (parcela de financiamento)", 5m), (Texto(fatura, "descricao"), fatura.GetProperty("valor").GetDecimal()));

        var viaLista = await ParcelaErradaAsync("Errada lista");
        await loja.Painel.Get($"{Rota}/lancamentos?tipo=pagar");
        Assert.Equal(Utc(2032, 1, 15), (await api.LancamentoNoBancoPorIdAsync(viaLista)).Vencimento);
    }

    [FatoIntegracao]
    public async Task Faturas_do_mes_com_varios_cartoes_nao_mistura_parcelas_nem_cronogramas_nem_adiantamentos()
    {
        var (loja, conta, cartaoA) = await CenarioAsync("Cartão A");
        var cartaoB = await loja.CartaoPelaApiAsync(conta, "Cartão B");
        await loja.CompraNoCartaoAsync(cartaoA, 100m, "2032-01-05");
        await loja.CompraNoCartaoAsync(cartaoB, 10m, "2032-01-05");
        await api.ParcelaDeFinanciamentoNoBancoAsync(loja.Id, conta, cartaoA, Utc(2032, 1, 15), 25m);
        // adiantamentos: dois na fatura do A (somam), nenhum na do B
        await loja.Painel.Post($"{Rota}/cartoes/{cartaoA}/fatura/antecipado?ano=2032&mes=1", new { valor = 10m, data = "2025-01-10", contaBancariaId = conta });
        await loja.Painel.Post($"{Rota}/cartoes/{cartaoA}/fatura/antecipado?ano=2032&mes=1", new { valor = 20m, data = "2025-01-11", contaBancariaId = conta });
        // parcela exatamente à meia-noite do mês seguinte: pertence a fevereiro, não entra na fatura de janeiro
        await api.NoBancoAsync(async db =>
        {
            db.LancamentosFinanceiros.Add(new LojaApi.Models.LancamentoFinanceiro
            {
                LojaId = loja.Id, ContaBancariaId = conta, Tipo = "pagar", Modo = "parcelada", Descricao = "De fevereiro", Valor = 7m, Vencimento = Utc(2032, 2, 1, 0), Status = "pendente",
                GrupoParcelamentoId = Guid.NewGuid(), NumeroParcela = 1, TotalParcelas = 1, CartaoOrigemId = cartaoB,
            });
            await db.SaveChangesAsync();
        });

        var lista = Linhas(await loja.Painel.Get(Faturas())).ToDictionary(x => Texto(x, "cartaoNome"));

        Assert.Equal((100m, 25m, 125m, 30m, 95m), (lista["Cartão A"].GetProperty("totalCompras").GetDecimal(), lista["Cartão A"].GetProperty("totalParcelasFinanciamento").GetDecimal(),
            lista["Cartão A"].GetProperty("total").GetDecimal(), lista["Cartão A"].GetProperty("totalAntecipado").GetDecimal(), lista["Cartão A"].GetProperty("restante").GetDecimal()));
        Assert.Equal((10m, 0m, 10m, 0m, 10m), (lista["Cartão B"].GetProperty("totalCompras").GetDecimal(), lista["Cartão B"].GetProperty("totalParcelasFinanciamento").GetDecimal(),
            lista["Cartão B"].GetProperty("total").GetDecimal(), lista["Cartão B"].GetProperty("totalAntecipado").GetDecimal(), lista["Cartão B"].GetProperty("restante").GetDecimal()));

        var agrupado = Linhas(await loja.Painel.Get(Pagar())).ToDictionary(x => Texto(x, "cartaoNome"));
        Assert.Equal(95m, agrupado["Cartão A"].GetProperty("valor").GetDecimal());
        Assert.Equal(10m, agrupado["Cartão B"].GetProperty("valor").GetDecimal());
    }

    [FatoIntegracao]
    public async Task Faturas_do_mes_mostra_so_o_cronograma_da_fatura_de_cada_cartao()
    {
        var (loja, conta, cartaoA) = await CenarioAsync("Cartão A");
        var cartaoB = await loja.CartaoPelaApiAsync(conta, "Cartão B");
        foreach (var cartao in new[] { cartaoA, cartaoB })
        {
            await loja.CompraNoCartaoAsync(cartao, 100m, "2032-01-05");
            await loja.Painel.Post($"{Rota}/cartoes/{cartao}/fatura/pagamento?ano=2032&mes=1", new { modo = "parcelado", totalParcelas = 2 });
        }

        var lista = Linhas(await loja.Painel.Get(Faturas()));

        Assert.Equal(2, lista.Count);
        Assert.All(lista, f => Assert.Equal(2, f.GetProperty("parcelas").GetArrayLength()));
        var idsA = (await api.LancamentosDaLojaNoBancoAsync(loja.Id)).Where(l => l.CartaoOrigemId == cartaoA).Select(l => l.Id).ToHashSet();
        var doA = lista.Single(f => Texto(f, "cartaoNome") == "Cartão A").GetProperty("parcelas").EnumerateArray().Select(p => p.GetProperty("id").GetGuid());
        Assert.All(doA, id => Assert.Contains(id, idsA));
    }
}
