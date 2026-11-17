using LojaApi.Domain.Financeiro;
using Microsoft.EntityFrameworkCore;
using static LojaApi.Tests.Suporte.CenariosFinanceiro;

namespace LojaApi.Tests.Api.FinanceiroTeste;

// Financeiro, parte 3b: fatura do cartão (ver, pagamento antecipado, pagar total/parcial/parcelado/desfazer) e faturas de referência.
// Cartão de teste: fecha dia 10 e vence dia 15. A fatura de jan/2032 cobre as compras de 11/12/2031 a 10/01/2032.
[Collection(ColecaoApi.Nome)]
public class FinanceiroFaturasApiTests(ApiFixture api)
{
    private Task<LojaTeste> NovaAsync() => api.NovaLojaAsync();

    private static async Task<(int, string)> Erro(Task<Resposta> r) { var x = await r; return (x.Codigo, x.Erro); }

    private static DateTime Utc(int ano, int mes, int dia, int hora = 12) => new(ano, mes, dia, hora, 0, 0, DateTimeKind.Utc);

    private async Task<(LojaTeste Loja, Guid Conta, Guid Cartao)> CenarioAsync(decimal juros = 0m)
    {
        var loja = await NovaAsync();
        var conta = await loja.ContaPelaApiAsync();
        var r = await loja.Painel.Post($"{Rota}/cartoes", new { nome = "Cartão Fatura", limite = 5000m, diaFechamento = 10, diaVencimento = 15, contaBancariaId = conta, taxaJurosMensal = juros });
        return (loja, conta, r.Json.GetProperty("id").GetGuid());
    }

    private static string Fatura(Guid cartao, string rota = "fatura", int ano = 2032, int mes = 1) => $"{Rota}/cartoes/{cartao}/{rota}?ano={ano}&mes={mes}";

    // ── Ver fatura ───────────────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Exige_login_mas_qualquer_papel_da_loja_usa_as_faturas()
    {
        var (loja, _, cartao) = await CenarioAsync();
        var operador = await api.OperadorAsync(loja);

        Assert.Equal(401, (await loja.Site.Get(Fatura(cartao))).Codigo);
        Assert.Equal(401, (await loja.Site.Post(Fatura(cartao, "fatura/pagamento"), new { modo = "total" })).Codigo);
        Assert.Equal(401, (await loja.Site.Delete($"{Rota}/cartoes/fatura/antecipado/{Guid.NewGuid()}")).Codigo);
        Assert.Equal(401, (await loja.Site.Get($"{Rota}/cartoes/{cartao}/faturas-referencia")).Codigo);
        Assert.Equal(200, (await operador.Get(Fatura(cartao))).Codigo);
    }

    [FatoIntegracao]
    public async Task Ver_fatura_soma_as_compras_do_ciclo_e_as_parcelas_de_financiamento_do_mes()
    {
        var (loja, conta, cartao) = await CenarioAsync();
        var outra = await NovaAsync();
        await loja.CompraNoCartaoAsync(cartao, 100m, "2031-12-20", "Antes");
        await loja.CompraNoCartaoAsync(cartao, 50m, "2032-01-10", "No fechamento");
        await loja.CompraNoCartaoAsync(cartao, 30m, "2032-01-11", "Próxima fatura");
        await loja.CompraNoCartaoAsync(cartao, 5m, "2031-12-10", "Fatura anterior");
        await api.ParcelaDeFinanciamentoNoBancoAsync(loja.Id, conta, cartao, Utc(2032, 1, 15), 25m);

        var r = await loja.Painel.Get(Fatura(cartao));

        Assert.Equal(200, r.Codigo);
        Assert.Equal(["vencimento", "total", "totalAntecipado", "restante", "status", "pagoEm", "valorEntrada", "itens", "parcelasFinanciamento", "antecipados"], r.Json.EnumerateObject().Select(x => x.Name));
        Assert.Equal((Utc(2032, 1, 15), 175m, 0m, 175m, "pendente"), (r.Json.GetProperty("vencimento").GetDateTime().ToUniversalTime(), r.Json.GetProperty("total").GetDecimal(),
            r.Json.GetProperty("totalAntecipado").GetDecimal(), r.Json.GetProperty("restante").GetDecimal(), r.Json.GetProperty("status").GetString()!));
        Assert.Equal(System.Text.Json.JsonValueKind.Null, r.Json.GetProperty("pagoEm").ValueKind);
        Assert.Equal(System.Text.Json.JsonValueKind.Null, r.Json.GetProperty("valorEntrada").ValueKind);
        var itens = r.Json.GetProperty("itens").EnumerateArray().ToList();
        Assert.Equal(["Antes", "No fechamento"], itens.Select(x => x.GetProperty("descricao").GetString()));
        Assert.Equal(["id", "descricao", "valor", "dataCompra", "categoriaNome", "categoriaId", "modo", "observacao"], itens[0].EnumerateObject().Select(x => x.Name));
        var parcela = Assert.Single(r.Json.GetProperty("parcelasFinanciamento").EnumerateArray());
        Assert.Equal(["id", "descricao", "valor", "vencimento", "status", "numeroParcela", "totalParcelas", "contaBancariaId", "categoriaId", "observacao", "modo", "mesOrigemFatura", "anoOrigemFatura"],
            parcela.EnumerateObject().Select(x => x.Name));
        Assert.Equal(25m, parcela.GetProperty("valor").GetDecimal());

        Assert.Equal(404, (await outra.Painel.Get(Fatura(cartao))).Codigo);
        Assert.Equal(404, (await loja.Painel.Get(Fatura(Guid.NewGuid()))).Codigo);
        Assert.Equal(0m, (await loja.Painel.Get(Fatura(cartao, ano: 2032, mes: 6))).Json.GetProperty("total").GetDecimal());
    }

    // ── Pagamento antecipado ─────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Antecipado_cria_a_fatura_se_nao_existir_abate_do_restante_e_pode_ser_excluido()
    {
        var (loja, conta, cartao) = await CenarioAsync();
        var outra = await NovaAsync();
        await loja.CompraNoCartaoAsync(cartao, 100m, "2032-01-05");
        string Url() => Fatura(cartao, "fatura/antecipado");

        var r = await loja.Painel.Post(Url(), new { valor = 40m, data = "2025-01-10T23:00:00", contaBancariaId = conta, observacao = "adiantei" });

        Assert.Equal(200, r.Codigo);
        Assert.Equal("Pagamento antecipado registrado.", r.Json.GetProperty("mensagem").GetString());
        var fatura = Assert.Single(await api.FaturasDoCartaoNoBancoAsync(cartao));
        Assert.Equal((loja.Id, Utc(2032, 1, 1, 0), Utc(2032, 1, 15), 100m, "pendente"), (fatura.LojaId, fatura.MesReferencia, fatura.Vencimento, fatura.Total, fatura.Status));
        var antecipado = Assert.Single(await api.AntecipadosDaFaturaNoBancoAsync(fatura.Id));
        Assert.Equal((loja.Id, conta, 40m, Utc(2025, 1, 10), "adiantei"), (antecipado.LojaId, antecipado.ContaBancariaId, antecipado.Valor, antecipado.Data, antecipado.Observacao));

        var ver = await loja.Painel.Get(Fatura(cartao));
        Assert.Equal((40m, 60m), (ver.Json.GetProperty("totalAntecipado").GetDecimal(), ver.Json.GetProperty("restante").GetDecimal()));
        Assert.Equal(40m, ver.Json.GetProperty("antecipados").EnumerateArray().Single().GetProperty("valor").GetDecimal());

        Assert.Equal((400, "O valor adiantado não pode ultrapassar o total da fatura."), await Erro(loja.Painel.Post(Url(), new { valor = 60.01m, data = "2025-01-10", contaBancariaId = conta })));
        Assert.Equal(200, (await loja.Painel.Post(Url(), new { valor = 60m, data = "2025-01-10", contaBancariaId = conta })).Codigo);

        Assert.Equal(404, (await outra.Painel.Delete($"{Rota}/cartoes/fatura/antecipado/{antecipado.Id}")).Codigo);
        var excluido = await loja.Painel.Delete($"{Rota}/cartoes/fatura/antecipado/{antecipado.Id}");
        Assert.Equal(200, excluido.Codigo);
        Assert.Equal("Pagamento antecipado excluído.", excluido.Json.GetProperty("mensagem").GetString());
        Assert.Single(await api.AntecipadosDaFaturaNoBancoAsync(fatura.Id));
        Assert.Equal(404, (await loja.Painel.Delete($"{Rota}/cartoes/fatura/antecipado/{antecipado.Id}")).Codigo);
    }

    [FatoIntegracao]
    public async Task Antecipado_valida_valor_data_conta_e_cartao()
    {
        var (loja, conta, cartao) = await CenarioAsync();
        var outra = await NovaAsync();
        var contaAlheia = await outra.ContaPelaApiAsync();
        await loja.CompraNoCartaoAsync(cartao, 100m, "2032-01-05");
        string Url(Guid id) => Fatura(id, "fatura/antecipado");

        Assert.Equal((400, "Valor deve ser maior que zero."), await Erro(loja.Painel.Post(Url(cartao), new { valor = 0m, data = "2025-01-10", contaBancariaId = conta })));
        Assert.Equal((400, "A data não pode ser no futuro."), await Erro(loja.Painel.Post(Url(cartao), new { valor = 10m, data = DateTime.UtcNow.AddDays(2).ToString("yyyy-MM-dd"), contaBancariaId = conta })));
        Assert.Equal((400, "Conta bancária inválida."), await Erro(loja.Painel.Post(Url(cartao), new { valor = 10m, data = "2025-01-10", contaBancariaId = contaAlheia })));
        Assert.Equal(404, (await outra.Painel.Post(Url(cartao), new { valor = 10m, data = "2025-01-10", contaBancariaId = contaAlheia })).Codigo);
        Assert.Empty(await api.FaturasDoCartaoNoBancoAsync(cartao));
    }

    // ── Pagar fatura ─────────────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Pagar_total_paga_o_restante_mais_as_parcelas_antigas_do_mes_na_data_informada()
    {
        var (loja, conta, cartao) = await CenarioAsync();
        await loja.CompraNoCartaoAsync(cartao, 100m, "2032-01-05");
        await loja.CompraNoCartaoAsync(cartao, 50m, "2032-01-08");
        var parcela = await api.ParcelaDeFinanciamentoNoBancoAsync(loja.Id, conta, cartao, Utc(2032, 1, 15), 25m);
        await loja.Painel.Post(Fatura(cartao, "fatura/antecipado"), new { valor = 30m, data = "2025-01-10", contaBancariaId = conta });

        var r = await loja.Painel.Post(Fatura(cartao, "fatura/pagamento"), new { modo = "total", contaBancariaId = conta, dataPagamento = "2032-01-12T22:00:00", naoAfetaSaldo = true });

        Assert.Equal(200, r.Codigo);
        Assert.Equal(["id", "status", "total", "valorPago"], r.Json.EnumerateObject().Select(x => x.Name));
        Assert.Equal(("pago", 150m, 145m), (r.Json.GetProperty("status").GetString()!, r.Json.GetProperty("total").GetDecimal(), r.Json.GetProperty("valorPago").GetDecimal()));
        var fatura = Assert.Single(await api.FaturasDoCartaoNoBancoAsync(cartao));
        Assert.Equal((conta, "pago", 145m, Utc(2032, 1, 12), true), (fatura.ContaBancariaId!.Value, fatura.Status, fatura.ValorPago, fatura.PagoEm!.Value, fatura.NaoAfetaSaldo));
        var paga = await api.LancamentoNoBancoPorIdAsync(parcela);
        Assert.Equal(("pago", Utc(2032, 1, 12), true), (paga.Status, paga.PagoEm!.Value, paga.NaoAfetaSaldo));
    }

    [FatoIntegracao]
    public async Task Pagar_total_sem_data_usa_agora_e_sem_nao_afeta_saldo_a_parcela_continua_afetando()
    {
        var (loja, conta, cartao) = await CenarioAsync();
        await loja.CompraNoCartaoAsync(cartao, 100m, "2032-01-05");
        var parcela = await api.ParcelaDeFinanciamentoNoBancoAsync(loja.Id, conta, cartao, Utc(2032, 1, 15), 25m);

        var r = await loja.Painel.Post(Fatura(cartao, "fatura/pagamento"), new { modo = "total" });

        Assert.Equal(("pago", 125m), (r.Json.GetProperty("status").GetString()!, r.Json.GetProperty("valorPago").GetDecimal()));
        var fatura = Assert.Single(await api.FaturasDoCartaoNoBancoAsync(cartao));
        Assert.InRange((DateTime.UtcNow - fatura.PagoEm!.Value).TotalMinutes, -1, 5);
        Assert.Null(fatura.ContaBancariaId);
        Assert.False(fatura.NaoAfetaSaldo);
        Assert.False((await api.LancamentoNoBancoPorIdAsync(parcela)).NaoAfetaSaldo);
    }

    [FatoIntegracao]
    public async Task Pagar_parcial_lanca_o_saldo_com_juros_na_proxima_fatura_e_absorve_as_parcelas_antigas()
    {
        var (loja, conta, cartao) = await CenarioAsync(juros: 10m);
        await loja.CompraNoCartaoAsync(cartao, 150m, "2032-01-05");
        var parcela = await api.ParcelaDeFinanciamentoNoBancoAsync(loja.Id, conta, cartao, Utc(2032, 1, 15), 50m);
        string Url() => Fatura(cartao, "fatura/pagamento");

        Assert.Equal((400, "Valor parcial deve ser maior que zero e menor que o total (já descontado o que foi antecipado)."), await Erro(loja.Painel.Post(Url(), new { modo = "parcial", valorPago = 0m })));
        Assert.Equal((400, "Valor parcial deve ser maior que zero e menor que o total (já descontado o que foi antecipado)."), await Erro(loja.Painel.Post(Url(), new { modo = "parcial", valorPago = 200m })));
        Assert.Empty(await api.FaturasDoCartaoNoBancoAsync(cartao));

        var r = await loja.Painel.Post(Url(), new { modo = "parcial", valorPago = 100m, dataPagamento = "2032-01-12" });

        Assert.Equal(200, r.Codigo);
        Assert.Equal(("parcial", 150m, 100m), (r.Json.GetProperty("status").GetString()!, r.Json.GetProperty("total").GetDecimal(), r.Json.GetProperty("valorPago").GetDecimal()));
        var rotativo = (await api.ComprasDoCartaoNoBancoAsync(cartao)).Single(c => c.EhJurosRotativo);
        Assert.Equal(("Saldo rotativo (fatura Jan) + juros", 110m, Utc(2032, 1, 11, 0)), (rotativo.Descricao, rotativo.Valor, rotativo.DataCompra));
        var absorvida = await api.LancamentoNoBancoPorIdAsync(parcela);
        Assert.Equal(("pago", true, "Financiamento antigo (absorvida no pagamento parcial de 12/01/2032)"), (absorvida.Status, absorvida.NaoAfetaSaldo, absorvida.Descricao));
        var fatura = Assert.Single(await api.FaturasDoCartaoNoBancoAsync(cartao));
        Assert.Equal(("parcial", 100m), (fatura.Status, fatura.ValorPago));
    }

    [FatoIntegracao]
    public async Task Pagar_parcelado_gera_as_parcelas_com_juros_na_conta_do_cartao_e_encerra_as_antigas()
    {
        var (loja, conta, cartao) = await CenarioAsync(juros: 2m);
        await loja.CompraNoCartaoAsync(cartao, 100m, "2032-01-05");
        var antiga = await api.ParcelaDeFinanciamentoNoBancoAsync(loja.Id, conta, cartao, Utc(2032, 1, 15), 50m);
        string Url() => Fatura(cartao, "fatura/pagamento");
        const string msgEntrada = "O valor de entrada deve ser maior ou igual a zero e menor que o total (já descontado o que foi antecipado).";

        Assert.Equal((400, "Escolha entre 2 e 24 parcelas."), await Erro(loja.Painel.Post(Url(), new { modo = "parcelado", totalParcelas = 1 })));
        Assert.Equal((400, "Escolha entre 2 e 24 parcelas."), await Erro(loja.Painel.Post(Url(), new { modo = "parcelado", totalParcelas = 25 })));
        Assert.Equal((400, msgEntrada), await Erro(loja.Painel.Post(Url(), new { modo = "parcelado", totalParcelas = 3, valorEntrada = 150m })));
        Assert.Equal((400, msgEntrada), await Erro(loja.Painel.Post(Url(), new { modo = "parcelado", totalParcelas = 3, valorEntrada = -1m })));

        var r = await loja.Painel.Post(Url(), new { modo = "parcelado", totalParcelas = 3, valorEntrada = 30m, dataPagamento = "2032-01-20" });

        Assert.Equal(200, r.Codigo);
        Assert.Equal(("financiada", 100m, 30m), (r.Json.GetProperty("status").GetString()!, r.Json.GetProperty("total").GetDecimal(), r.Json.GetProperty("valorPago").GetDecimal()));
        var fatura = Assert.Single(await api.FaturasDoCartaoNoBancoAsync(cartao));
        var parcelas = (await api.LancamentosDaLojaNoBancoAsync(loja.Id)).Where(l => l.FaturaCartaoId == fatura.Id).OrderBy(l => l.NumeroParcela).ToList();
        // (100 + 50 antiga - 30 de entrada) = 120; com 2% ao mês em 3 parcelas = 127,20; cada parcela 42,40
        Assert.Equal([Utc(2032, 2, 15), Utc(2032, 3, 15), Utc(2032, 4, 15)], parcelas.Select(p => p.Vencimento));
        Assert.All(parcelas, p => Assert.Equal(("Financiamento fatura Cartão Fatura (Jan)", 42.40m, "pagar", "parcelada", cartao, conta, 3), (p.Descricao, p.Valor, p.Tipo, p.Modo, p.CartaoOrigemId!.Value, p.ContaBancariaId, p.TotalParcelas!.Value)));
        var encerrada = await api.LancamentoNoBancoPorIdAsync(antiga);
        Assert.Equal(("pago", Utc(2032, 1, 20), "Financiamento antigo (absorvida no refinanciamento de 20/01/2032)"), (encerrada.Status, encerrada.PagoEm!.Value, encerrada.Descricao));

        await loja.CompraNoCartaoAsync(cartao, 100m, "2032-03-05");
        var primeira = await loja.Painel.Post(Fatura(cartao, "fatura/pagamento", mes: 3), new { modo = "parcelado", totalParcelas = 2, primeiraParcela = "2033-05-20" });
        Assert.Equal(200, primeira.Codigo);
        var marco = (await api.LancamentosDaLojaNoBancoAsync(loja.Id)).Where(l => l.Descricao.EndsWith("(Mar)")).OrderBy(l => l.NumeroParcela).ToList();
        // a 1ª parcela usa só o mês/ano da data escolhida; o dia é o do vencimento do cartão.
        // Financia a compra (100) + a parcela de 42,40 do financiamento anterior que vence em março: 142,40 + 2% x 2 = 148,096 -> 74,05 cada
        Assert.Equal([Utc(2033, 5, 15), Utc(2033, 6, 15)], marco.Select(l => l.Vencimento));
        Assert.Equal([74.05m, 74.05m], marco.Select(l => l.Valor));
        Assert.Equal(2, (await api.FaturasDoCartaoNoBancoAsync(cartao)).Count);
    }

    [FatoIntegracao]
    public async Task Desfazer_volta_a_fatura_para_pendente_apaga_as_parcelas_pendentes_e_reabre_as_pagas_do_mes()
    {
        var (loja, conta, cartao) = await CenarioAsync();
        await loja.CompraNoCartaoAsync(cartao, 100m, "2032-01-05");
        string Url() => Fatura(cartao, "fatura/pagamento");
        await loja.Painel.Post(Url(), new { modo = "parcelado", totalParcelas = 2, contaBancariaId = conta, naoAfetaSaldo = true });
        var fatura = Assert.Single(await api.FaturasDoCartaoNoBancoAsync(cartao));
        Assert.Equal(2, (await api.LancamentosDaLojaNoBancoAsync(loja.Id)).Count(l => l.FaturaCartaoId == fatura.Id));
        var paga = await api.ParcelaDeFinanciamentoNoBancoAsync(loja.Id, conta, cartao, Utc(2032, 1, 15), 10m, status: "pago");
        await api.NoBancoAsync(db => db.LancamentosFinanceiros.Where(l => l.Id == paga).ExecuteUpdateAsync(s => s.SetProperty(l => l.Descricao, "Antigo (absorvida no pagamento parcial de 12/01/2032)").SetProperty(l => l.NaoAfetaSaldo, true)));

        var r = await loja.Painel.Post(Url(), new { modo = "desfazer" });

        Assert.Equal(200, r.Codigo);
        Assert.Equal(("pendente", 0m), (r.Json.GetProperty("status").GetString()!, r.Json.GetProperty("valorPago").GetDecimal()));
        var depois = Assert.Single(await api.FaturasDoCartaoNoBancoAsync(cartao));
        Assert.Equal(("pendente", 0m, null, false), (depois.Status, depois.ValorPago, depois.ContaBancariaId, depois.NaoAfetaSaldo));
        Assert.Null(depois.PagoEm);
        var restantes = await api.LancamentosDaLojaNoBancoAsync(loja.Id);
        var reaberta = Assert.Single(restantes);
        Assert.Equal((paga, "pendente", false, "Antigo"), (reaberta.Id, reaberta.Status, reaberta.NaoAfetaSaldo, reaberta.Descricao));
        Assert.Null(reaberta.PagoEm);
    }

    [FatoIntegracao]
    public async Task Pagar_fatura_valida_modo_conta_e_cartao()
    {
        var (loja, conta, cartao) = await CenarioAsync();
        var outra = await NovaAsync();
        var contaAlheia = await outra.ContaPelaApiAsync();
        string Url(Guid id) => Fatura(id, "fatura/pagamento");

        Assert.Equal((400, "Modo inválido."), await Erro(loja.Painel.Post(Url(cartao), new { modo = "pix" })));
        Assert.Equal((400, "Conta bancária inválida."), await Erro(loja.Painel.Post(Url(cartao), new { modo = "total", contaBancariaId = contaAlheia })));
        Assert.Equal(404, (await outra.Painel.Post(Url(cartao), new { modo = "total" })).Codigo);
        Assert.Equal(404, (await loja.Painel.Post(Url(Guid.NewGuid()), new { modo = "total" })).Codigo);
        Assert.Empty(await api.FaturasDoCartaoNoBancoAsync(cartao));

        var vazia = await loja.Painel.Post(Url(cartao), new { modo = "total", contaBancariaId = conta });
        Assert.Equal(("pago", 0m, 0m), (vazia.Json.GetProperty("status").GetString()!, vazia.Json.GetProperty("total").GetDecimal(), vazia.Json.GetProperty("valorPago").GetDecimal()));
    }

    // ── Faturas de referência ────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Faturas_de_referencia_mostra_o_ciclo_aberto_e_a_fatura_fechada_pendente()
    {
        var (loja, _, cartao) = await CenarioAsync();
        var outra = await NovaAsync();
        var hoje = DateTime.UtcNow;

        var vazio = await loja.Painel.Get($"{Rota}/cartoes/{cartao}/faturas-referencia");

        Assert.Equal(200, vazio.Codigo);
        Assert.Equal(["aberta", "fechada"], vazio.Json.EnumerateObject().Select(x => x.Name));
        Assert.Equal(["ano", "mes"], vazio.Json.GetProperty("aberta").EnumerateObject().Select(x => x.Name));
        Assert.Equal(["ano", "mes", "total", "status"], vazio.Json.GetProperty("fechada").EnumerateObject().Select(x => x.Name));
        Assert.Equal((0m, "pendente"), (vazio.Json.GetProperty("fechada").GetProperty("total").GetDecimal(), vazio.Json.GetProperty("fechada").GetProperty("status").GetString()!));
        var venc = CartaoRegras.VencimentoDaFaturaDaCompra(10, 15, hoje.Date);
        Assert.Equal((venc.Year, venc.Month), (vazio.Json.GetProperty("aberta").GetProperty("ano").GetInt32(), vazio.Json.GetProperty("aberta").GetProperty("mes").GetInt32()));

        var compra = hoje.Date.AddDays(-40);
        await loja.CompraNoCartaoAsync(cartao, 80m, compra.ToString("yyyy-MM-dd"));
        var vencDaCompra = CartaoRegras.VencimentoDaFaturaDaCompra(10, 15, compra.AddHours(12));

        var comCompra = await loja.Painel.Get($"{Rota}/cartoes/{cartao}/faturas-referencia");

        var fechada = comCompra.Json.GetProperty("fechada");
        Assert.Equal((vencDaCompra.Year, vencDaCompra.Month, 80m, "pendente"), (fechada.GetProperty("ano").GetInt32(), fechada.GetProperty("mes").GetInt32(), fechada.GetProperty("total").GetDecimal(), fechada.GetProperty("status").GetString()!));

        Assert.Equal(404, (await outra.Painel.Get($"{Rota}/cartoes/{cartao}/faturas-referencia")).Codigo);
        Assert.Equal(404, (await loja.Painel.Get($"{Rota}/cartoes/{Guid.NewGuid()}/faturas-referencia")).Codigo);
    }
}
