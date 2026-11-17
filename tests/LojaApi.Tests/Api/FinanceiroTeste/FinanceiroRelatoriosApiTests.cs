using LojaApi.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using static LojaApi.Tests.Suporte.CenariosFinanceiro;

namespace LojaApi.Tests.Api.FinanceiroTeste;

// Financeiro, parte 4b: relatórios (resumo mensal e anual, balanço por categoria, detalhe e comparativo, alertas de vencimento, resumo dos cartões)
// e o GET do Planejamento. Usa 2024 (sempre no passado: o que está pendente vira "vencido") e 2032 (sempre no futuro: vira "pendente").
// Cartão de teste: fecha dia 10 e vence dia 15. A fatura de mar/2024 cobre as compras de 11/02 a 10/03/2024 e vence em 15/03/2024.
[Collection(ColecaoApi.Nome)]
public class FinanceiroRelatoriosApiTests(ApiFixture api)
{
    private Task<LojaTeste> NovaAsync() => api.NovaLojaAsync();

    private static DateTime Utc(int ano, int mes, int dia, int hora = 12) => new(ano, mes, dia, hora, 0, 0, DateTimeKind.Utc);

    private async Task<(LojaTeste Loja, Guid Conta, Guid Cartao)> CenarioAsync(string nome = "Cartão Rel")
    {
        var loja = await NovaAsync();
        var conta = await loja.ContaPelaApiAsync();
        return (loja, conta, await loja.CartaoPelaApiAsync(conta, nome));
    }

    private static decimal Num(JsonElement e, string campo) => e.GetProperty(campo).GetDecimal();
    private static string Texto(JsonElement e, string campo) => e.GetProperty(campo).GetString()!;

    private async Task<Guid> CategoriaAsync(LojaTeste loja, string nome, string tipo = "pagar") =>
        (await loja.Painel.Post($"{Rota}/categorias", new { nome, tipo })).Json.GetProperty("id").GetGuid();

    // Plano de R$ 200 com uma assinatura ativa e pagamentos nos meses informados
    private Task PlanoNoBancoAsync(LojaTeste loja, decimal valorPlano, params (DateTime Mes, string Status, decimal Valor)[] pagamentos) =>
        AssinaturaNoBancoAsync(loja, valorPlano, "ativa", pagamentos);

    private async Task AssinaturaNoBancoAsync(LojaTeste loja, decimal valorPlano, string statusDaAssinatura, params (DateTime Mes, string Status, decimal Valor)[] pagamentos)
    {
        var cliente = await loja.ClienteIdAsync($"Assinante {Guid.NewGuid():N}"[..14], "67999990001");
        await api.NoBancoAsync(async db =>
        {
            var plano = new Plano { LojaId = loja.Id, Nome = "Plano Rel", Valor = valorPlano };
            db.Planos.Add(plano);
            var assinatura = new AssinaturaCliente { LojaId = loja.Id, ClienteId = cliente, PlanoId = plano.Id, Status = statusDaAssinatura };
            db.AssinaturasCliente.Add(assinatura);
            await db.SaveChangesAsync();
            foreach (var p in pagamentos)
                db.PagamentosPlano.Add(new PagamentoPlano { AssinaturaId = assinatura.Id, LojaId = loja.Id, MesReferencia = p.Mes, Valor = p.Valor, Status = p.Status });
            await db.SaveChangesAsync();
        });
    }

    private Task CompraDiretaAsync(LojaTeste loja, Guid cartao, decimal valor, DateTime data, string modo = "avulsa", Guid? categoria = null, string descricao = "Compra direta") =>
        api.NoBancoAsync(async db =>
        {
            db.LancamentosCartao.Add(new LancamentoCartao { LojaId = loja.Id, CartaoCreditoId = cartao, Descricao = descricao, Valor = valor, DataCompra = data, Modo = modo, CategoriaId = categoria });
            await db.SaveChangesAsync();
        });

    private Task FaturaNoBancoAsync(LojaTeste loja, Guid cartao, int ano, int mes, string status, decimal total) =>
        api.NoBancoAsync(async db =>
        {
            db.FaturasCartao.Add(new FaturaCartao
            {
                LojaId = loja.Id, CartaoCreditoId = cartao, MesReferencia = Utc(ano, mes, 1, 0), Vencimento = Utc(ano, mes, 15),
                Total = total, ValorPago = status == "pendente" ? 0 : total, Status = status, PagoEm = status == "pendente" ? null : DateTime.UtcNow,
            });
            await db.SaveChangesAsync();
        });

    // ── Acesso ───────────────────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Exige_login_mas_qualquer_papel_da_loja_ve_os_relatorios_e_sem_vinculo_devolve_vazio()
    {
        var (loja, _, _) = await CenarioAsync();
        var operador = await api.OperadorAsync(loja);
        string[] rotas =
        [
            "resumo-mensal?ano=2024&mes=3", "balanco-por-categoria?ano=2024&mes=3", "balanco-detalhe?ano=2024&mes=3&categoria=Casa", "balanco-comparativo?ano=2024&mes=3",
            "resumo-anual?ano=2024", "alertas-vencimento", "cartoes-resumo", "planejamento?ano=2024&mes=3",
        ];

        foreach (var rota in rotas)
        {
            Assert.Equal(401, (await loja.Site.Get($"{Rota}/{rota}")).Codigo);
            Assert.Equal(200, (await operador.Get($"{Rota}/{rota}")).Codigo);
        }

        await api.SemVinculoAsync(loja);
        foreach (var rota in rotas.Where(r => !r.StartsWith("resumo-anual") && !r.StartsWith("alertas") && !r.StartsWith("cartoes")))
            Assert.Equal(JsonValueKind.Object, (await loja.Painel.Get($"{Rota}/{rota}")).Json.ValueKind);
        foreach (var rota in new[] { "resumo-anual?ano=2024", "alertas-vencimento", "cartoes-resumo" })
        {
            var r = await loja.Painel.Get($"{Rota}/{rota}");
            Assert.Equal(JsonValueKind.Array, r.Json.ValueKind);
            Assert.Equal(0, r.Json.GetArrayLength());
        }
        Assert.Empty((await loja.Painel.Get($"{Rota}/resumo-mensal?ano=2024&mes=3")).Json.EnumerateObject());
    }

    // ── Resumo mensal ────────────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Resumo_mensal_separa_pago_pendente_e_vencido_de_pagar_e_receber_e_soma_planos_e_cartoes()
    {
        var (loja, conta, cartao) = await CenarioAsync("Cartão Azul");
        var outra = await NovaAsync();
        await loja.AvulsoPelaApiAsync(conta, "pagar", "Pago", 100m, "2024-03-05", jaPago: true);
        await loja.AvulsoPelaApiAsync(conta, "pagar", "Vencido", 60m, "2024-03-10");
        await loja.AvulsoPelaApiAsync(conta, "pagar", "Outro mês", 7m, "2024-04-10");
        await loja.AvulsoPelaApiAsync(conta, "receber", "Recebido", 30m, "2024-03-06", jaPago: true);
        await loja.AvulsoPelaApiAsync(conta, "receber", "Atrasado", 20m, "2024-03-07");
        await outra.AvulsoPelaApiAsync(await outra.ContaPelaApiAsync(), "pagar", "Alheio", 999m, "2024-03-10");
        // mensalidades: uma paga e uma em atraso (o mês de referência já passou)
        await PlanoNoBancoAsync(loja, 200m, (Utc(2024, 3, 1, 0), "pago", 150m), (Utc(2024, 3, 1, 0), "pendente", 50m));
        // fatura de mar/2024 (compras de 11/02 a 10/03), vencida; e uma parcela de financiamento do mesmo cartão em aberto
        await loja.CompraNoCartaoAsync(cartao, 300m, "2024-02-20");
        await loja.CompraNoCartaoAsync(cartao, 11m, "2024-02-11");    // primeiro dia do ciclo: entra
        await loja.CompraNoCartaoAsync(cartao, 5m, "2024-03-10");     // dia do fechamento: entra
        await loja.CompraNoCartaoAsync(cartao, 7m, "2024-02-10");     // ciclo anterior: não entra
        await loja.CompraNoCartaoAsync(cartao, 10m, "2024-03-11");    // próximo ciclo: não entra
        await api.ParcelaDeFinanciamentoNoBancoAsync(loja.Id, conta, cartao, Utc(2024, 3, 15), 40m);

        var r = await loja.Painel.Get($"{Rota}/resumo-mensal?ano=2024&mes=3");

        Assert.Equal(200, r.Codigo);
        Assert.Equal(["pagar", "receber", "previsao", "detalhePagar"], r.Json.EnumerateObject().Select(x => x.Name));
        var pagar = r.Json.GetProperty("pagar");
        Assert.Equal(["totalPago", "qtdPago", "totalPendente", "qtdPendente", "totalVencido", "qtdVencido"], pagar.EnumerateObject().Select(x => x.Name));
        // vencido = avulso 60 + fatura 316 (300 + 11 + 5) + parcela de financiamento 40 (a fatura e a parcela contam como uma linha cada)
        Assert.Equal((100m, 1, 0m, 0, 416m, 2), (Num(pagar, "totalPago"), pagar.GetProperty("qtdPago").GetInt32(), Num(pagar, "totalPendente"), pagar.GetProperty("qtdPendente").GetInt32(),
            Num(pagar, "totalVencido"), pagar.GetProperty("qtdVencido").GetInt32()));
        var receber = r.Json.GetProperty("receber");
        Assert.Equal((180m, 2, 0m, 0, 70m, 2), (Num(receber, "totalPago"), receber.GetProperty("qtdPago").GetInt32(), Num(receber, "totalPendente"), receber.GetProperty("qtdPendente").GetInt32(),
            Num(receber, "totalVencido"), receber.GetProperty("qtdVencido").GetInt32()));
        var previsao = r.Json.GetProperty("previsao");
        Assert.Equal((250m, 516m, -266m), (Num(previsao, "receitaPrevista"), Num(previsao, "despesaPrevista"), Num(previsao, "saldoPrevisto")));
        var detalhe = r.Json.GetProperty("detalhePagar");
        Assert.Equal(60m, Num(detalhe, "lancamentos"));
        var cartoes = detalhe.GetProperty("cartoes").EnumerateArray().ToList();
        var linhaCartao = Assert.Single(cartoes);
        Assert.Equal(("Cartão Azul", 356m), (Texto(linhaCartao, "nome"), Num(linhaCartao, "valor")));
    }

    [FatoIntegracao]
    public async Task Resumo_mensal_no_futuro_conta_como_pendente_e_fatura_paga_conta_como_paga()
    {
        var (loja, conta, cartao) = await CenarioAsync("Cartão Verde");
        await loja.AvulsoPelaApiAsync(conta, "pagar", "Futuro", 80m, "2032-01-20");
        await loja.AvulsoPelaApiAsync(conta, "receber", "A receber", 25m, "2032-01-21");
        await loja.CompraNoCartaoAsync(cartao, 100m, "2031-12-20");
        await FaturaNoBancoAsync(loja, cartao, 2032, 1, "pago", 100m);

        var r = await loja.Painel.Get($"{Rota}/resumo-mensal?ano=2032&mes=1");

        var pagar = r.Json.GetProperty("pagar");
        Assert.Equal((100m, 1, 80m, 1, 0m, 0), (Num(pagar, "totalPago"), pagar.GetProperty("qtdPago").GetInt32(), Num(pagar, "totalPendente"), pagar.GetProperty("qtdPendente").GetInt32(),
            Num(pagar, "totalVencido"), pagar.GetProperty("qtdVencido").GetInt32()));
        Assert.Equal((25m, 1), (Num(r.Json.GetProperty("receber"), "totalPendente"), r.Json.GetProperty("receber").GetProperty("qtdPendente").GetInt32()));
        // fatura paga não aparece no detalhe de cartões; "contas" = só o que está em aberto
        Assert.Empty(r.Json.GetProperty("detalhePagar").GetProperty("cartoes").EnumerateArray());
        Assert.Equal(80m, Num(r.Json.GetProperty("detalhePagar"), "lancamentos"));
    }

    [FatoIntegracao]
    public async Task Resumo_mensal_conta_o_que_vence_hoje_como_pendente_e_considera_mensalidades_de_assinaturas_canceladas()
    {
        var loja = await NovaAsync();
        var conta = await loja.ContaPelaApiAsync();
        var hoje = DateTime.UtcNow;
        await loja.AvulsoPelaApiAsync(conta, "pagar", "Vence hoje", 33m, hoje.ToString("yyyy-MM-dd"));
        await PlanoNoBancoAsync(loja, 200m, (Utc(2024, 3, 1, 0), "pago", 150m));
        await AssinaturaNoBancoAsync(loja, 200m, "cancelada", (Utc(2024, 3, 1, 0), "pendente", 80m));

        var atual = await loja.Painel.Get($"{Rota}/resumo-mensal?ano={hoje.Year}&mes={hoje.Month}");
        var marco = await loja.Painel.Get($"{Rota}/resumo-mensal?ano=2024&mes=3");

        var pagar = atual.Json.GetProperty("pagar");
        Assert.Equal((33m, 1, 0m, 0), (Num(pagar, "totalPendente"), pagar.GetProperty("qtdPendente").GetInt32(), Num(pagar, "totalVencido"), pagar.GetProperty("qtdVencido").GetInt32()));
        // o resumo mensal soma as mensalidades de todas as assinaturas da loja (a cancelada em atraso também)
        var receber = marco.Json.GetProperty("receber");
        Assert.Equal((150m, 1, 80m, 1), (Num(receber, "totalPago"), receber.GetProperty("qtdPago").GetInt32(), Num(receber, "totalVencido"), receber.GetProperty("qtdVencido").GetInt32()));
    }

    // ── Balanço por categoria ────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Balanco_por_categoria_agrupa_receitas_e_despesas_e_acrescenta_plano_e_cartao()
    {
        var (loja, conta, cartao) = await CenarioAsync();
        var casa = await CategoriaAsync(loja, "Casa");
        var venda = await CategoriaAsync(loja, "Vendas", "receber");
        var mercado = await CategoriaAsync(loja, "Mercado");
        var extra = await CategoriaAsync(loja, "Extras", "receber");
        await loja.AvulsoPelaApiAsync(conta, "pagar", "Aluguel", 500m, "2024-03-05", casa);
        await loja.AvulsoPelaApiAsync(conta, "pagar", "Luz", 70m, "2024-03-08", casa);
        await loja.AvulsoPelaApiAsync(conta, "pagar", "Sem cat", 15m, "2024-03-09");
        await loja.AvulsoPelaApiAsync(conta, "receber", "Venda", 900m, "2024-03-12", venda);
        await loja.AvulsoPelaApiAsync(conta, "receber", "Extra", 50m, "2024-03-13", extra);
        await PlanoNoBancoAsync(loja, 200m, (Utc(2024, 3, 1, 0), "pago", 120m), (Utc(2024, 3, 1, 0), "pendente", 80m));
        // cartão: 300 de mercado na fatura de mar/2024; financiamento de 40 não entra como categoria própria no modo agrupado
        await CompraDiretaAsync(loja, cartao, 300m, Utc(2024, 2, 20), categoria: mercado);
        await api.ParcelaDeFinanciamentoNoBancoAsync(loja.Id, conta, cartao, Utc(2024, 3, 15), 40m);

        var r = await loja.Painel.Get($"{Rota}/balanco-por-categoria?ano=2024&mes=3");

        Assert.Equal(200, r.Codigo);
        Assert.Equal(["receitas", "despesas", "despesasDesmembradas", "totalReceitas", "totalDespesas", "saldo"], r.Json.EnumerateObject().Select(x => x.Name));
        var receitas = r.Json.GetProperty("receitas").EnumerateArray().ToList();
        // a mensalidade entra no fim e, como as receitas vêm da maior para a menor, ocupa o meio
        Assert.Equal([("Vendas", 900m), ("Mensalidade/Assinatura", 200m), ("Extras", 50m)], receitas.Select(x => (Texto(x, "nome"), Num(x, "valor"))));
        Assert.Equal("💳", Texto(receitas[1], "icone"));
        var despesas = r.Json.GetProperty("despesas").EnumerateArray().ToList();
        Assert.Equal([("Casa", 570m), ("Cartão de Crédito", 340m), ("Sem categoria", 15m)], despesas.Select(x => (Texto(x, "nome"), Num(x, "valor"))));
        Assert.Equal((1150m, 925m, 225m), (Num(r.Json, "totalReceitas"), Num(r.Json, "totalDespesas"), Num(r.Json, "saldo")));
    }

    [FatoIntegracao]
    public async Task Balanco_por_categoria_sem_plano_e_sem_cartao_nao_cria_as_linhas_extras()
    {
        var (loja, conta, _) = await CenarioAsync();
        await loja.AvulsoPelaApiAsync(conta, "pagar", "Aluguel", 500m, "2024-03-05");
        await loja.AvulsoPelaApiAsync(conta, "receber", "Venda", 900m, "2024-03-12");

        var r = await loja.Painel.Get($"{Rota}/balanco-por-categoria?ano=2024&mes=3");

        Assert.Equal(["Sem categoria"], r.Json.GetProperty("receitas").EnumerateArray().Select(x => Texto(x, "nome")));
        Assert.Equal(["Sem categoria"], r.Json.GetProperty("despesas").EnumerateArray().Select(x => Texto(x, "nome")));
        Assert.Equal((900m, 500m, 400m), (Num(r.Json, "totalReceitas"), Num(r.Json, "totalDespesas"), Num(r.Json, "saldo")));
    }

    [FatoIntegracao]
    public async Task Balanco_por_categoria_desmembrado_distribui_o_cartao_pela_data_da_compra_e_financiamento_vira_linha_propria()
    {
        var (loja, conta, cartao) = await CenarioAsync();
        var casa = await CategoriaAsync(loja, "Casa");
        var mercado = await CategoriaAsync(loja, "Mercado");
        await loja.AvulsoPelaApiAsync(conta, "pagar", "Aluguel", 500m, "2024-03-05", casa);
        await CompraDiretaAsync(loja, cartao, 120m, Utc(2024, 3, 3), categoria: mercado);
        await CompraDiretaAsync(loja, cartao, 30m, Utc(2024, 3, 20), categoria: casa);
        await CompraDiretaAsync(loja, cartao, 5m, Utc(2024, 3, 21));
        await CompraDiretaAsync(loja, cartao, 999m, Utc(2024, 4, 3), categoria: mercado);
        await api.ParcelaDeFinanciamentoNoBancoAsync(loja.Id, conta, cartao, Utc(2024, 3, 15), 40m);

        var r = await loja.Painel.Get($"{Rota}/balanco-por-categoria?ano=2024&mes=3");

        var desm = r.Json.GetProperty("despesasDesmembradas").EnumerateArray().ToList();
        Assert.Equal(["nome", "icone", "valor", "valorCartao", "valorAvulso"], desm[0].EnumerateObject().Select(x => x.Name));
        Assert.Equal([("Casa", 530m, 30m, 500m), ("Mercado", 120m, 120m, 0m), ("Financiamento de fatura", 40m, 40m, 0m), ("Sem categoria", 5m, 5m, 0m)],
            desm.Select(x => (Texto(x, "nome"), Num(x, "valor"), Num(x, "valorCartao"), Num(x, "valorAvulso"))));
        Assert.Equal("🏦", Texto(desm[2], "icone"));
    }

    // ── Balanço detalhe ──────────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Balanco_detalhe_lista_os_itens_de_cada_visao_e_confere_com_o_valor_do_balanco()
    {
        var (loja, conta, cartao) = await CenarioAsync("Cartão Detalhe");
        var casa = await CategoriaAsync(loja, "Casa");
        var mercado = await CategoriaAsync(loja, "Mercado");
        // criados fora da ordem de data, para provar que o detalhe sai ordenado por data
        await loja.AvulsoPelaApiAsync(conta, "pagar", "Luz", 70m, "2024-03-08", casa);
        var aluguel = await loja.AvulsoPelaApiAsync(conta, "pagar", "Aluguel", 500m, "2024-03-05", casa, jaPago: true);
        await loja.AvulsoPelaApiAsync(conta, "pagar", "Sem cat", 15m, "2024-03-09");
        await CompraDiretaAsync(loja, cartao, 30m, Utc(2024, 3, 20), categoria: casa, descricao: "Tinta");
        await CompraDiretaAsync(loja, cartao, 120m, Utc(2024, 3, 3), categoria: mercado, descricao: "Feira");
        await CompraDiretaAsync(loja, cartao, 5m, Utc(2024, 3, 4), descricao: "Chiclete");
        await CompraDiretaAsync(loja, cartao, 9m, new DateTime(2024, 4, 1, 0, 0, 0, DateTimeKind.Utc), categoria: casa, descricao: "Abril");
        await CompraDiretaAsync(loja, cartao, 300m, Utc(2024, 2, 20), descricao: "Geladeira");
        await api.ParcelaDeFinanciamentoNoBancoAsync(loja.Id, conta, cartao, Utc(2024, 3, 15), 40m);

        var avulsos = await loja.Painel.Get($"{Rota}/balanco-detalhe?ano=2024&mes=3&categoria=Casa");
        Assert.Equal(["categoria", "ano", "mes", "valorBalanco", "totalItens", "itens"], avulsos.Json.EnumerateObject().Select(x => x.Name));
        Assert.Equal(("Casa", 2024, 3, 570m, 570m), (Texto(avulsos.Json, "categoria"), avulsos.Json.GetProperty("ano").GetInt32(), avulsos.Json.GetProperty("mes").GetInt32(), Num(avulsos.Json, "valorBalanco"), Num(avulsos.Json, "totalItens")));
        var itens = avulsos.Json.GetProperty("itens").EnumerateArray().ToList();
        Assert.Equal(["origem", "descricao", "valor", "data", "status", "cartaoNome", "contaNome", "id", "modo"], itens[0].EnumerateObject().Select(x => x.Name));
        Assert.Equal([("avulso", "Aluguel", 500m, "pago"), ("avulso", "Luz", 70m, "pendente")], itens.Select(x => (Texto(x, "origem"), Texto(x, "descricao"), Num(x, "valor"), Texto(x, "status"))));
        Assert.Equal((aluguel, "Conta Teste", "avulsa"), (itens[0].GetProperty("id").GetGuid(), Texto(itens[0], "contaNome"), Texto(itens[0], "modo")));

        var semCategoria = await loja.Painel.Get($"{Rota}/balanco-detalhe?ano=2024&mes=3&categoria=Sem%20categoria");
        Assert.Equal(["Sem cat"], semCategoria.Json.GetProperty("itens").EnumerateArray().Select(x => Texto(x, "descricao")));
        var semCategoriaDesmembrado = await loja.Painel.Get($"{Rota}/balanco-detalhe?ano=2024&mes=3&categoria=Sem%20categoria&desmembrar=true");
        Assert.Equal(["Chiclete", "Sem cat"], semCategoriaDesmembrado.Json.GetProperty("itens").EnumerateArray().Select(x => Texto(x, "descricao")));

        var cartaoFatura = await loja.Painel.Get($"{Rota}/balanco-detalhe?ano=2024&mes=3&categoria=Cart%C3%A3o%20de%20Cr%C3%A9dito");
        var fatura = Assert.Single(cartaoFatura.Json.GetProperty("itens").EnumerateArray());
        Assert.Equal(("cartao_fatura", "Fatura Cartão Detalhe", 465m, "pendente", "Cartão Detalhe"), (Texto(fatura, "origem"), Texto(fatura, "descricao"), Num(fatura, "valor"), Texto(fatura, "status"), Texto(fatura, "cartaoNome")));
        Assert.Equal(465m, Num(cartaoFatura.Json, "valorBalanco"));

        var desmembrado = await loja.Painel.Get($"{Rota}/balanco-detalhe?ano=2024&mes=3&categoria=Casa&desmembrar=true");
        // só as compras da categoria feitas dentro do mês (a de 01/04 às 00:00 já é de abril)
        Assert.Equal(["Aluguel", "Luz", "Tinta"], desmembrado.Json.GetProperty("itens").EnumerateArray().Select(x => Texto(x, "descricao")));
        var tinta = desmembrado.Json.GetProperty("itens").EnumerateArray().Last();
        Assert.Equal(("cartao_item", "cartao", "Cartão Detalhe"), (Texto(tinta, "origem"), Texto(tinta, "status"), Texto(tinta, "cartaoNome")));
        Assert.Equal(600m, Num(desmembrado.Json, "valorBalanco"));

        var financiamento = await loja.Painel.Get($"{Rota}/balanco-detalhe?ano=2024&mes=3&categoria=Financiamento%20de%20fatura&desmembrar=true");
        var parcela = Assert.Single(financiamento.Json.GetProperty("itens").EnumerateArray());
        Assert.Equal(("cartao_parcela", 40m, "pendente"), (Texto(parcela, "origem"), Num(parcela, "valor"), Texto(parcela, "status")));
        Assert.Equal(40m, Num(financiamento.Json, "valorBalanco"));

        // fatura resolvida (paga) aparece como paga
        await FaturaNoBancoAsync(loja, cartao, 2024, 3, "pago", 300m);
        var paga = await loja.Painel.Get($"{Rota}/balanco-detalhe?ano=2024&mes=3&categoria=Cart%C3%A3o%20de%20Cr%C3%A9dito");
        Assert.Equal("pago", Texto(Assert.Single(paga.Json.GetProperty("itens").EnumerateArray()), "status"));

        // categoria desconhecida: lista vazia e valor zero
        var vazio = await loja.Painel.Get($"{Rota}/balanco-detalhe?ano=2024&mes=3&categoria=Nada");
        Assert.Empty(vazio.Json.GetProperty("itens").EnumerateArray());
        Assert.Equal(0m, Num(vazio.Json, "valorBalanco"));
    }

    // ── Balanço comparativo ──────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Balanco_comparativo_traz_mes_escolhido_mes_anterior_serie_do_ano_e_melhor_mes()
    {
        var (loja, conta, _) = await CenarioAsync();
        var casa = await CategoriaAsync(loja, "Casa");
        var lazer = await CategoriaAsync(loja, "Lazer");
        await loja.AvulsoPelaApiAsync(conta, "pagar", "Casa fev", 50m, "2024-02-10", casa);
        await loja.AvulsoPelaApiAsync(conta, "pagar", "Casa mar", 100m, "2024-03-10", casa);
        await loja.AvulsoPelaApiAsync(conta, "pagar", "Casa jun", 80m, "2024-06-10", casa);
        await loja.AvulsoPelaApiAsync(conta, "pagar", "Lazer mar", 10m, "2024-03-12", lazer);
        await loja.AvulsoPelaApiAsync(conta, "pagar", "Ano passado", 7m, "2023-12-10", casa);
        var zerada = await CategoriaAsync(loja, "Zerada");
        await api.NoBancoAsync(async db =>
        {
            db.LancamentosFinanceiros.Add(new LancamentoFinanceiro { LojaId = loja.Id, ContaBancariaId = conta, Tipo = "pagar", Descricao = "Zero", Valor = 0m, Vencimento = Utc(2024, 3, 10), Status = "pendente", CategoriaId = zerada });
            await db.SaveChangesAsync();
        });

        var r = await loja.Painel.Get($"{Rota}/balanco-comparativo?ano=2024&mes=3");

        Assert.Equal(200, r.Codigo);
        Assert.Equal(["ano", "mes", "anterior", "totalAtual", "totalAnterior", "totaisMes", "categorias"], r.Json.EnumerateObject().Select(x => x.Name));
        Assert.Equal((2024, 2), (r.Json.GetProperty("anterior").GetProperty("ano").GetInt32(), r.Json.GetProperty("anterior").GetProperty("mes").GetInt32()));
        Assert.Equal((110m, 50m), (Num(r.Json, "totalAtual"), Num(r.Json, "totalAnterior")));
        Assert.Equal([0m, 50m, 110m, 0m, 0m, 80m, 0m, 0m, 0m, 0m, 0m, 0m], r.Json.GetProperty("totaisMes").EnumerateArray().Select(x => x.GetDecimal()));
        var categorias = r.Json.GetProperty("categorias").EnumerateArray().ToList();
        Assert.Equal(["Casa", "Lazer"], categorias.Select(c => Texto(c, "nome")));
        var casaLinha = categorias[0];
        Assert.Equal(["nome", "icone", "atual", "anterior", "diferenca", "variacaoPct", "melhorMes", "melhorValor", "serie"], casaLinha.EnumerateObject().Select(x => x.Name));
        Assert.Equal((100m, 50m, 50m, 100m, 2, 50m), (Num(casaLinha, "atual"), Num(casaLinha, "anterior"), Num(casaLinha, "diferenca"), Num(casaLinha, "variacaoPct"),
            casaLinha.GetProperty("melhorMes").GetInt32(), Num(casaLinha, "melhorValor")));
        // Lazer não tinha gasto no mês anterior: sem variação percentual
        Assert.Equal(JsonValueKind.Null, categorias[1].GetProperty("variacaoPct").ValueKind);

        // janeiro compara com dezembro do ano anterior
        var janeiro = await loja.Painel.Get($"{Rota}/balanco-comparativo?ano=2024&mes=1");
        Assert.Equal((2023, 12), (janeiro.Json.GetProperty("anterior").GetProperty("ano").GetInt32(), janeiro.Json.GetProperty("anterior").GetProperty("mes").GetInt32()));
        Assert.Equal(7m, Num(janeiro.Json, "totalAnterior"));
    }

    [FatoIntegracao]
    public async Task Balanco_comparativo_so_considera_melhor_mes_os_meses_que_ja_aconteceram()
    {
        var (loja, conta, _) = await CenarioAsync();
        var casa = await CategoriaAsync(loja, "Casa");
        await loja.AvulsoPelaApiAsync(conta, "pagar", "Futuro barato", 1m, "2032-05-10", casa);
        await loja.AvulsoPelaApiAsync(conta, "pagar", "Futuro", 50m, "2032-06-10", casa);

        var r = await loja.Painel.Get($"{Rota}/balanco-comparativo?ano=2032&mes=6");

        var linha = Assert.Single(r.Json.GetProperty("categorias").EnumerateArray());
        Assert.Equal(JsonValueKind.Null, linha.GetProperty("melhorMes").ValueKind);
        Assert.Equal(JsonValueKind.Null, linha.GetProperty("melhorValor").ValueKind);
        Assert.Equal((50m, 1m), (Num(linha, "atual"), Num(linha, "anterior")));
    }

    // ── Resumo anual ─────────────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Resumo_anual_devolve_os_12_meses_com_pagar_receber_e_o_que_ja_foi_pago()
    {
        var (loja, conta, cartao) = await CenarioAsync();
        await loja.AvulsoPelaApiAsync(conta, "pagar", "Pago", 100m, "2024-03-05", jaPago: true);
        await loja.AvulsoPelaApiAsync(conta, "pagar", "Aberto", 60m, "2024-03-10");
        await loja.AvulsoPelaApiAsync(conta, "receber", "Receita", 500m, "2024-03-12");
        await loja.AvulsoPelaApiAsync(conta, "pagar", "Dezembro", 9m, "2024-12-12");
        await PlanoNoBancoAsync(loja, 200m, (Utc(2024, 3, 1, 0), "pago", 150m));
        await CompraDiretaAsync(loja, cartao, 300m, Utc(2024, 2, 20));
        await CompraDiretaAsync(loja, cartao, 70m, Utc(2023, 12, 20));   // ciclo da fatura de jan/2024 (começa em dezembro do ano anterior)
        await FaturaNoBancoAsync(loja, cartao, 2024, 3, "pago", 300m);
        await api.ParcelaDeFinanciamentoNoBancoAsync(loja.Id, conta, cartao, Utc(2024, 3, 15), 40m);
        await AssinaturaNoBancoAsync(loja, 999m, "cancelada", (Utc(2024, 3, 1, 0), "pago", 500m));   // assinatura cancelada não entra no resumo anual

        var r = await loja.Painel.Get($"{Rota}/resumo-anual?ano=2024");

        Assert.Equal(200, r.Codigo);
        var meses = r.Json.EnumerateArray().ToList();
        Assert.Equal(Enumerable.Range(1, 12), meses.Select(m => m.GetProperty("mes").GetInt32()));
        Assert.Equal(["mes", "pagar", "receber", "saldo", "pagarPago", "pagarAberto"], meses[0].EnumerateObject().Select(x => x.Name));
        var marco = meses[2];
        // pagar = 100 + 60 + fatura paga 300 + parcela de financiamento pendente (vencida) 40; receber = 500 + mensalidade paga 150
        Assert.Equal((500m, 650m, 150m, 400m, 100m), (Num(marco, "pagar"), Num(marco, "receber"), Num(marco, "saldo"), Num(marco, "pagarPago"), Num(marco, "pagarAberto")));
        Assert.Equal((9m, 0m), (Num(meses[11], "pagar"), Num(meses[11], "receber")));
        Assert.Equal(70m, Num(meses[0], "pagar"));
        // ano passado: meses sem pagamento de plano não projetam receita
        Assert.Equal(0m, Num(meses[3], "receber"));
    }

    [FatoIntegracao]
    public async Task Resumo_anual_projeta_a_receita_dos_planos_ativos_nos_meses_sem_pagamento_gerado()
    {
        var loja = await NovaAsync();
        await PlanoNoBancoAsync(loja, 200m, (Utc(2032, 3, 1, 0), "pago", 150m));

        var r = await loja.Painel.Get($"{Rota}/resumo-anual?ano=2032");

        var meses = r.Json.EnumerateArray().ToList();
        Assert.Equal(150m, Num(meses[2], "receber"));                 // março: usa o pagamento gerado
        Assert.Equal(200m, Num(meses[0], "receber"));                 // demais meses de 2032: projeção do plano
        Assert.Equal(200m, Num(meses[11], "receber"));
        Assert.All(meses, m => Assert.Equal(0m, Num(m, "pagar")));
    }

    // ── Alertas de vencimento ────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Alertas_de_vencimento_listam_pendentes_dentro_da_janela_e_a_fatura_do_cartao()
    {
        var (loja, conta, cartao) = await CenarioAsync("Cartão Alerta");
        var outra = await NovaAsync();
        var hoje = DateTime.UtcNow.Date;
        string Dia(int dias) => hoje.AddDays(dias).ToString("yyyy-MM-dd");
        await loja.AvulsoPelaApiAsync(conta, "pagar", "Amanhã", 10m, Dia(1));
        await loja.AvulsoPelaApiAsync(conta, "receber", "Em 5 dias", 20m, Dia(5));
        await loja.AvulsoPelaApiAsync(conta, "pagar", "Em 20 dias", 30m, Dia(20));
        await loja.AvulsoPelaApiAsync(conta, "pagar", "Já pago", 40m, Dia(2), jaPago: true);
        await loja.AvulsoPelaApiAsync(conta, "pagar", "Ontem", 50m, Dia(-1));
        await outra.AvulsoPelaApiAsync(await outra.ContaPelaApiAsync(), "pagar", "Alheio", 60m, Dia(1));
        // compra de hoje cai no ciclo em aberto do cartão, cujo vencimento ainda não passou
        await CompraDiretaAsync(loja, cartao, 77m, DateTime.UtcNow);

        var curto = await loja.Painel.Get($"{Rota}/alertas-vencimento?dias=7");
        var longo = await loja.Painel.Get($"{Rota}/alertas-vencimento?dias=400");

        Assert.Equal(200, curto.Codigo);
        var linhas = curto.Json.EnumerateArray().ToList();
        Assert.Equal(["Amanhã", "Em 5 dias"], linhas.Where(l => Texto(l, "origem") == "lancamento").Select(l => Texto(l, "descricao")));
        Assert.Equal(["id", "descricao", "tipo", "valor", "vencimento", "origem"], linhas[0].EnumerateObject().Select(x => x.Name));
        Assert.Equal(("pagar", 10m), (Texto(linhas[0], "tipo"), Num(linhas[0], "valor")));
        var longoLancamentos = longo.Json.EnumerateArray().Where(l => Texto(l, "origem") == "lancamento").Select(l => Texto(l, "descricao")).ToList();
        Assert.Equal(["Amanhã", "Em 5 dias", "Em 20 dias"], longoLancamentos);
        var alertaCartao = Assert.Single(longo.Json.EnumerateArray(), l => Texto(l, "origem") == "cartao");
        Assert.Equal((cartao, "Fatura Cartão Alerta", "pagar", 77m), (alertaCartao.GetProperty("id").GetGuid(), Texto(alertaCartao, "descricao"), Texto(alertaCartao, "tipo"), Num(alertaCartao, "valor")));
    }

    [FatoIntegracao]
    public async Task Alertas_de_vencimento_nao_avisam_fatura_ja_paga_nem_cartao_sem_compras()
    {
        var (loja, _, cartao) = await CenarioAsync("Cartão Quieto");
        var conta2 = await loja.ContaPelaApiAsync("Outra conta");
        await loja.CartaoPelaApiAsync(conta2, "Cartão Vazio");
        var hoje = DateTime.UtcNow;
        await CompraDiretaAsync(loja, cartao, 77m, hoje);
        // descobre o vencimento da fatura em aberto pelo próprio alerta e a marca como paga
        var antes = await loja.Painel.Get($"{Rota}/alertas-vencimento?dias=400");
        var alerta = Assert.Single(antes.Json.EnumerateArray());
        var venc = alerta.GetProperty("vencimento").GetDateTime();
        await FaturaNoBancoAsync(loja, cartao, venc.Year, venc.Month, "pago", 77m);

        var depois = await loja.Painel.Get($"{Rota}/alertas-vencimento?dias=400");

        Assert.Empty(depois.Json.EnumerateArray());
    }

    // ── Resumo dos cartões ───────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Cartoes_resumo_soma_ciclo_aberto_compras_futuras_e_financiamento_pendente_no_limite_usado()
    {
        var (loja, conta, cartao) = await CenarioAsync("Cartão Limite");
        var outra = await NovaAsync();
        await outra.CartaoPelaApiAsync(await outra.ContaPelaApiAsync(), "Cartão Alheio");
        var inativo = await loja.CartaoPelaApiAsync(conta, "Cartão Inativo");
        await api.NoBancoAsync(db => db.CartoesCredito.Where(c => c.Id == inativo).ExecuteUpdateAsync(s => s.SetProperty(c => c.Ativo, false)));
        var hoje = DateTime.UtcNow;
        await CompraDiretaAsync(loja, cartao, 100m, hoje);
        await CompraDiretaAsync(loja, cartao, 60m, Utc(2032, 5, 20), "parcelada");
        await CompraDiretaAsync(loja, cartao, 25m, Utc(2032, 6, 20), "fixa");
        await api.ParcelaDeFinanciamentoNoBancoAsync(loja.Id, conta, cartao, Utc(2024, 3, 15), 40m);
        await api.ParcelaDeFinanciamentoNoBancoAsync(loja.Id, conta, cartao, Utc(2024, 4, 15), 15m, "pago");

        var r = await loja.Painel.Get($"{Rota}/cartoes-resumo");

        Assert.Equal(200, r.Codigo);
        var linha = Assert.Single(r.Json.EnumerateArray());
        Assert.Equal(["id", "nome", "bandeira", "limite", "usado", "disponivel", "vencimentoAtual", "status", "qtdCompras"], linha.EnumerateObject().Select(x => x.Name));
        // usado = 100 (ciclo em aberto) + 60 (compra parcelada futura; a fixa futura não conta) + 40 (financiamento pendente; a parcela paga não)
        Assert.Equal((cartao, "Cartão Limite", 1000m, 200m, 800m, "pendente", 1), (linha.GetProperty("id").GetGuid(), Texto(linha, "nome"), Num(linha, "limite"), Num(linha, "usado"),
            Num(linha, "disponivel"), Texto(linha, "status"), linha.GetProperty("qtdCompras").GetInt32()));
        Assert.Equal(JsonValueKind.Null, linha.GetProperty("bandeira").ValueKind);
    }

    [FatoIntegracao]
    public async Task Cartoes_resumo_nao_conta_fatura_paga_mas_conta_o_que_foi_comprado_depois_do_pagamento()
    {
        var (loja, _, cartao) = await CenarioAsync("Cartão Quitado");
        // compra de ~3 meses atrás, num ciclo cuja fatura já foi paga com total menor que o comprado depois
        var dataCompra = DateTime.UtcNow.Date.AddMonths(-3);
        var vencimento = dataCompra.Day <= 10 ? dataCompra : dataCompra.AddMonths(1);
        await CompraDiretaAsync(loja, cartao, 100m, dataCompra.AddHours(12), descricao: "Antes do pagamento");
        await FaturaNoBancoAsync(loja, cartao, vencimento.Year, vencimento.Month, "pago", 100m);

        var quitada = await loja.Painel.Get($"{Rota}/cartoes-resumo");
        Assert.Equal(0m, Num(Assert.Single(quitada.Json.EnumerateArray()), "usado"));

        await CompraDiretaAsync(loja, cartao, 30m, dataCompra.AddHours(13), descricao: "Depois do pagamento");

        var depois = await loja.Painel.Get($"{Rota}/cartoes-resumo");
        Assert.Equal(30m, Num(Assert.Single(depois.Json.EnumerateArray()), "usado"));
    }

    // ── Lacunas apontadas pelo revisor ───────────────────────────────────────

    [FatoIntegracao]
    public async Task Relatorios_usam_a_fatura_e_o_adiantamento_de_cada_cartao_sem_misturar_os_da_mesma_loja()
    {
        var (loja, conta, cartaoA) = await CenarioAsync("Cartão A");
        var cartaoB = await loja.CartaoPelaApiAsync(conta, "Cartão B");
        await CompraDiretaAsync(loja, cartaoA, 100m, Utc(2024, 2, 20));
        await CompraDiretaAsync(loja, cartaoB, 200m, Utc(2024, 2, 20));
        await FaturaNoBancoAsync(loja, cartaoA, 2024, 3, "pago", 100m);   // só a do cartão A está paga
        await api.NoBancoAsync(async db =>
        {
            var faturaB = new FaturaCartao { LojaId = loja.Id, CartaoCreditoId = cartaoB, MesReferencia = Utc(2024, 3, 1, 0), Vencimento = Utc(2024, 3, 15), Total = 200m, Status = "pendente" };
            db.FaturasCartao.Add(faturaB);
            db.PagamentosAntecipadosFatura.Add(new PagamentoAntecipadoFatura { LojaId = loja.Id, FaturaCartaoId = faturaB.Id, ContaBancariaId = conta, Valor = 50m, Data = Utc(2024, 3, 1) });
            await db.SaveChangesAsync();
        });

        var mensal = await loja.Painel.Get($"{Rota}/resumo-mensal?ano=2024&mes=3");
        var balanco = await loja.Painel.Get($"{Rota}/balanco-por-categoria?ano=2024&mes=3");
        var anual = await loja.Painel.Get($"{Rota}/resumo-anual?ano=2024");

        // A: fatura paga (100 pago). B: pendente e vencida, com 50 já adiantados (150 vencido)
        var pagar = mensal.Json.GetProperty("pagar");
        Assert.Equal((100m, 150m), (Num(pagar, "totalPago"), Num(pagar, "totalVencido")));
        Assert.Equal(250m, Num(Assert.Single(balanco.Json.GetProperty("despesas").EnumerateArray()), "valor"));
        var marco = anual.Json.EnumerateArray().ElementAt(2);
        Assert.Equal((250m, 100m), (Num(marco, "pagar"), Num(marco, "pagarPago")));
    }

    [FatoIntegracao]
    public async Task Balanco_por_categoria_soma_o_cartao_e_o_plano_as_categorias_de_mesmo_nome()
    {
        var (loja, conta, cartao) = await CenarioAsync();
        var cartaoCategoria = await CategoriaAsync(loja, "Cartão de Crédito");
        var mensalidade = await CategoriaAsync(loja, "Mensalidade/Assinatura", "receber");
        await loja.AvulsoPelaApiAsync(conta, "pagar", "Taxa", 50m, "2024-03-05", cartaoCategoria);
        await loja.AvulsoPelaApiAsync(conta, "receber", "Avulsa", 40m, "2024-03-06", mensalidade);
        await CompraDiretaAsync(loja, cartao, 300m, Utc(2024, 2, 20));
        await PlanoNoBancoAsync(loja, 200m, (Utc(2024, 3, 1, 0), "pago", 120m));

        var r = await loja.Painel.Get($"{Rota}/balanco-por-categoria?ano=2024&mes=3");

        Assert.Equal([("Cartão de Crédito", 350m)], r.Json.GetProperty("despesas").EnumerateArray().Select(x => (Texto(x, "nome"), Num(x, "valor"))));
        Assert.Equal([("Mensalidade/Assinatura", 160m)], r.Json.GetProperty("receitas").EnumerateArray().Select(x => (Texto(x, "nome"), Num(x, "valor"))));
    }

    [FatoIntegracao]
    public async Task Comparativo_desmembrado_separa_os_anos_e_so_cria_financiamento_quando_ha_parcela()
    {
        var (loja, _, cartao) = await CenarioAsync();
        var mercado = await CategoriaAsync(loja, "Mercado");
        await CompraDiretaAsync(loja, cartao, 50m, Utc(2023, 12, 10), categoria: mercado);
        await CompraDiretaAsync(loja, cartao, 20m, Utc(2024, 12, 10), categoria: mercado);

        var r = await loja.Painel.Get($"{Rota}/balanco-comparativo?ano=2024&mes=1&desmembrar=true");

        var linha = Assert.Single(r.Json.GetProperty("categorias").EnumerateArray());
        Assert.Equal(("Mercado", 0m, 50m), (Texto(linha, "nome"), Num(linha, "atual"), Num(linha, "anterior")));
        Assert.Equal(20m, linha.GetProperty("serie").EnumerateArray().Last().GetDecimal());
    }

    [FatoIntegracao]
    public async Task Resumo_anual_projeta_a_receita_dos_planos_no_mes_atual_mas_nao_nos_anteriores()
    {
        var loja = await NovaAsync();
        await PlanoNoBancoAsync(loja, 200m);
        var hoje = DateTime.UtcNow;

        var r = await loja.Painel.Get($"{Rota}/resumo-anual?ano={hoje.Year}");

        var meses = r.Json.EnumerateArray().ToList();
        Assert.Equal(200m, Num(meses[hoje.Month - 1], "receber"));
        if (hoje.Month > 1) Assert.Equal(0m, Num(meses[hoje.Month - 2], "receber"));
    }

    [FatoIntegracao]
    public async Task Cartoes_resumo_conta_compra_antiga_de_ciclo_ainda_nao_pago_dentro_da_janela()
    {
        var (loja, _, cartao) = await CenarioAsync("Cartão Antigo");
        await CompraDiretaAsync(loja, cartao, 100m, DateTime.UtcNow.Date.AddMonths(-8).AddHours(12));

        var r = await loja.Painel.Get($"{Rota}/cartoes-resumo");

        Assert.Equal(100m, Num(Assert.Single(r.Json.EnumerateArray()), "usado"));
    }

    [FatoIntegracao]
    public async Task Planejamento_com_meta_para_o_mes_atual_guarda_o_que_falta_de_uma_vez()
    {
        var loja = await NovaAsync();
        await loja.Painel.Post($"{Rota}/planejamento/metas", new { nome = "Agora", valorAlvo = 900m, valorGuardado = 300m, dataAlvo = DateTime.UtcNow.ToString("yyyy-MM-dd") });

        var r = await loja.Painel.Get($"{Rota}/planejamento?ano=2024&mes=3");

        Assert.Equal(600m, Num(Assert.Single(r.Json.GetProperty("metas").EnumerateArray()), "porMes"));
    }

    // ── Planejamento: GET ────────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Planejamento_valida_o_mes_e_cria_o_planejamento_da_loja_na_primeira_leitura()
    {
        var loja = await NovaAsync();

        Assert.Equal(400, (await loja.Painel.Get($"{Rota}/planejamento?ano=1999&mes=3")).Codigo);
        Assert.Equal(400, (await loja.Painel.Get($"{Rota}/planejamento?ano=2024&mes=0")).Codigo);
        var invalido = await loja.Painel.Get($"{Rota}/planejamento?ano=2024&mes=13");
        Assert.Equal(400, invalido.Codigo);
        Assert.Equal("Mês inválido.", Texto(invalido.Json, "erro"));
        Assert.Empty(await api.NoBancoAsync(db => db.PlanejamentosFinanceiros.AsNoTracking().Where(p => p.LojaId == loja.Id).ToListAsync()));

        var r = await loja.Painel.Get($"{Rota}/planejamento?ano=2024&mes=3");

        Assert.Equal(200, r.Codigo);
        Assert.Equal(["ano", "mes", "receitaPlanejada", "receitaPrevista", "receita", "percentualPoupanca", "poupar", "disponivelParaGastar", "totalPlanejado", "totalPlanejadoReceber",
            "recebidoPlanejadas", "aReceberPlanejadas", "gastoPlanejadas", "gastoTotal", "categorias", "categoriasReceita", "metas"], r.Json.EnumerateObject().Select(x => x.Name));
        Assert.Single(await api.NoBancoAsync(db => db.PlanejamentosFinanceiros.AsNoTracking().Where(p => p.LojaId == loja.Id).ToListAsync()));
        // o planejamento padrão traz as categorias padrão da loja (ativas), sem tetos
        Assert.All(r.Json.GetProperty("categorias").EnumerateArray(), c => Assert.False(c.GetProperty("ativo").GetBoolean()));
    }

    [FatoIntegracao]
    public async Task Planejamento_calcula_receita_poupanca_gastos_por_categoria_tetos_e_metas()
    {
        var (loja, conta, cartao) = await CenarioAsync();
        var casa = await CategoriaAsync(loja, "Casa");
        var venda = await CategoriaAsync(loja, "Vendas", "receber");
        var mercado = await CategoriaAsync(loja, "Mercado");
        await loja.AvulsoPelaApiAsync(conta, "pagar", "Aluguel", 500m, "2024-03-05", casa);
        await loja.AvulsoPelaApiAsync(conta, "receber", "Venda paga", 900m, "2024-03-12", venda, jaPago: true);
        await loja.AvulsoPelaApiAsync(conta, "receber", "Venda aberta", 100m, "2024-03-20", venda);
        await CompraDiretaAsync(loja, cartao, 120m, Utc(2024, 3, 3), categoria: mercado);
        await loja.Painel.Put($"{Rota}/planejamento/config", new { receitaPlanejada = 4000m, percentualPoupanca = 10m });
        await loja.Painel.Put($"{Rota}/planejamento/categorias", new
        {
            itens = new object[]
            {
                new { categoriaId = casa, ativo = true, valor = 600m },
                new { categoriaId = mercado, ativo = true, valor = 300m },
                new { categoriaId = venda, ativo = true, valor = 2000m, tipo = "receber" },
            },
        });
        var hoje = DateTime.UtcNow;
        await loja.Painel.Post($"{Rota}/planejamento/metas", new { nome = "Viagem", icone = "✈️", valorAlvo = 1200m, valorGuardado = 200m, dataAlvo = new DateTime(hoje.Year, hoje.Month, 1).AddMonths(5).ToString("yyyy-MM-dd") });
        await loja.Painel.Post($"{Rota}/planejamento/metas", new { nome = "Sem data", valorAlvo = 500m, valorGuardado = 100m });

        var r = await loja.Painel.Get($"{Rota}/planejamento?ano=2024&mes=3");

        Assert.Equal(200, r.Codigo);
        // com teto de receita ativo, a receita planejada é a soma dos tetos de receber (2000), não a configurada
        Assert.Equal((4000m, 1000m, 2000m, 10m, 200m, 1800m), (Num(r.Json, "receitaPlanejada"), Num(r.Json, "receitaPrevista"), Num(r.Json, "receita"), Num(r.Json, "percentualPoupanca"),
            Num(r.Json, "poupar"), Num(r.Json, "disponivelParaGastar")));
        Assert.Equal((900m, 100m, 2000m), (Num(r.Json, "recebidoPlanejadas"), Num(r.Json, "aReceberPlanejadas"), Num(r.Json, "totalPlanejadoReceber")));
        Assert.Equal((900m, 620m, 620m), (Num(r.Json, "totalPlanejado"), Num(r.Json, "gastoPlanejadas"), Num(r.Json, "gastoTotal")));
        var categorias = r.Json.GetProperty("categorias").EnumerateArray().ToList();
        var casaLinha = categorias.Single(c => Texto(c, "nome") == "Casa");
        Assert.Equal(["categoriaId", "nome", "icone", "ativo", "valor", "gastoAvulso", "gastoCartao", "gasto"], casaLinha.EnumerateObject().Select(x => x.Name));
        Assert.Equal((true, 600m, 500m, 0m, 500m), (casaLinha.GetProperty("ativo").GetBoolean(), Num(casaLinha, "valor"), Num(casaLinha, "gastoAvulso"), Num(casaLinha, "gastoCartao"), Num(casaLinha, "gasto")));
        var mercadoLinha = categorias.Single(c => Texto(c, "nome") == "Mercado");
        Assert.Equal((300m, 0m, 120m), (Num(mercadoLinha, "valor"), Num(mercadoLinha, "gastoAvulso"), Num(mercadoLinha, "gastoCartao")));
        // categorias só de receber não aparecem entre as de pagar, e vice-versa
        Assert.DoesNotContain(categorias, c => Texto(c, "nome") == "Vendas");
        var receitas = r.Json.GetProperty("categoriasReceita").EnumerateArray().ToList();
        var vendas = receitas.Single(c => Texto(c, "nome") == "Vendas");
        Assert.Equal(["categoriaId", "nome", "icone", "ativo", "valor", "recebido", "aReceber", "total"], vendas.EnumerateObject().Select(x => x.Name));
        Assert.Equal((true, 2000m, 900m, 100m, 1000m), (vendas.GetProperty("ativo").GetBoolean(), Num(vendas, "valor"), Num(vendas, "recebido"), Num(vendas, "aReceber"), Num(vendas, "total")));
        Assert.DoesNotContain(receitas, c => Texto(c, "nome") == "Mercado");
        // metas na ordem de criação; sem data alvo não há valor por mês; com data (5 meses à frente): (1200 - 200) / 5 = 200
        var metas = r.Json.GetProperty("metas").EnumerateArray().ToList();
        Assert.Equal(["Viagem", "Sem data"], metas.Select(m => Texto(m, "nome")));
        Assert.Equal(["id", "nome", "icone", "valorAlvo", "valorGuardado", "dataAlvo", "porMes"], metas[0].EnumerateObject().Select(x => x.Name));
        Assert.Equal((1200m, 200m, 200m), (Num(metas[0], "valorAlvo"), Num(metas[0], "valorGuardado"), Num(metas[0], "porMes")));
        Assert.Equal(JsonValueKind.Null, metas[1].GetProperty("porMes").ValueKind);
        Assert.Equal("🎯", Texto(metas[1], "icone"));
    }

    [FatoIntegracao]
    public async Task Planejamento_com_categoria_ambos_usa_o_teto_de_cada_tipo_na_sua_lista()
    {
        var loja = await NovaAsync();
        var extras = await CategoriaAsync(loja, "Extras", "ambos");
        var bonus = await CategoriaAsync(loja, "Bônus", "ambos");
        // gravados em chamadas separadas e em ordens opostas, para nenhuma busca depender de qual teto foi criado primeiro
        await loja.Painel.Put($"{Rota}/planejamento/categorias", new { itens = new object[] { new { categoriaId = extras, ativo = true, valor = 100m, tipo = "pagar" }, new { categoriaId = bonus, ativo = false, valor = 400m, tipo = "receber" } } });
        await loja.Painel.Put($"{Rota}/planejamento/categorias", new { itens = new object[] { new { categoriaId = extras, ativo = false, valor = 300m, tipo = "receber" }, new { categoriaId = bonus, ativo = true, valor = 200m, tipo = "pagar" } } });

        var r = await loja.Painel.Get($"{Rota}/planejamento?ano=2024&mes=3");

        JsonElement Linha(string lista, string nome) => r.Json.GetProperty(lista).EnumerateArray().Single(c => Texto(c, "nome") == nome);
        Assert.Equal((true, 100m), (Linha("categorias", "Extras").GetProperty("ativo").GetBoolean(), Num(Linha("categorias", "Extras"), "valor")));
        Assert.Equal((false, 300m), (Linha("categoriasReceita", "Extras").GetProperty("ativo").GetBoolean(), Num(Linha("categoriasReceita", "Extras"), "valor")));
        Assert.Equal((true, 200m), (Linha("categorias", "Bônus").GetProperty("ativo").GetBoolean(), Num(Linha("categorias", "Bônus"), "valor")));
        Assert.Equal((false, 400m), (Linha("categoriasReceita", "Bônus").GetProperty("ativo").GetBoolean(), Num(Linha("categoriasReceita", "Bônus"), "valor")));
    }

    [FatoIntegracao]
    public async Task Planejamento_sem_tetos_de_receita_usa_a_receita_planejada_ou_a_prevista_do_mes()
    {
        var loja = await NovaAsync();
        var conta = await loja.ContaPelaApiAsync();
        await loja.AvulsoPelaApiAsync(conta, "receber", "Venda", 800m, "2024-03-12");

        var semConfig = await loja.Painel.Get($"{Rota}/planejamento?ano=2024&mes=3");
        Assert.Equal(JsonValueKind.Null, semConfig.Json.GetProperty("receitaPlanejada").ValueKind);
        // sem configuração, o percentual a guardar padrão é 30%
        Assert.Equal((800m, 800m, 30m, 240m), (Num(semConfig.Json, "receitaPrevista"), Num(semConfig.Json, "receita"), Num(semConfig.Json, "percentualPoupanca"), Num(semConfig.Json, "poupar")));

        await loja.Painel.Put($"{Rota}/planejamento/config", new { receitaPlanejada = 3000m, percentualPoupanca = 12.5m });
        var comConfig = await loja.Painel.Get($"{Rota}/planejamento?ano=2024&mes=3");
        Assert.Equal((3000m, 3000m, 375m, 2625m), (Num(comConfig.Json, "receitaPlanejada"), Num(comConfig.Json, "receita"), Num(comConfig.Json, "poupar"), Num(comConfig.Json, "disponivelParaGastar")));
    }
}
