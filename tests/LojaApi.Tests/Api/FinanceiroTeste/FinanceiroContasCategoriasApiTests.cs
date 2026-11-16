using Microsoft.EntityFrameworkCore;
using static LojaApi.Tests.Suporte.CenariosFinanceiro;

namespace LojaApi.Tests.Api.FinanceiroTeste;

// Financeiro, parte 1: contas bancárias (com o saldo), ajuste de saldo, transferência entre contas e categorias financeiras.
[Collection(ColecaoApi.Nome)]
public class FinanceiroContasCategoriasApiTests(ApiFixture api)
{
    private Task<LojaTeste> NovaAsync() => api.NovaLojaAsync();

    private static async Task<(int, string)> Erro(Task<Resposta> r) { var x = await r; return (x.Codigo, x.Erro); }

    // ── Contas ───────────────────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Exige_login_mas_qualquer_papel_da_loja_usa_as_contas()
    {
        var loja = await NovaAsync();
        var operador = await api.OperadorAsync(loja);

        Assert.Equal(401, (await loja.Site.Get($"{Rota}/contas")).Codigo);
        Assert.Equal(401, (await loja.Site.Post($"{Rota}/contas", new { nome = "x", saldoInicial = 0m })).Codigo);
        Assert.Equal(401, (await loja.Site.Get($"{Rota}/categorias")).Codigo);
        Assert.Equal(200, (await operador.Get($"{Rota}/contas")).Codigo);
        Assert.Equal(200, (await operador.Post($"{Rota}/contas", new { nome = "Conta do operador", saldoInicial = 0m })).Codigo);
    }

    [FatoIntegracao]
    public async Task Criar_conta_corta_o_nome_e_o_saldo_atual_comeca_no_saldo_inicial()
    {
        var loja = await NovaAsync();

        var r = await loja.Painel.Post($"{Rota}/contas", new { nome = "  Caixa  ", saldoInicial = 150.5m, banco = "Banco X", limite = 300m });

        Assert.Equal(200, r.Codigo);
        Assert.Equal(["id", "nome", "saldoInicial", "ativa", "banco", "limite", "saldoAtual"], r.Json.EnumerateObject().Select(x => x.Name));
        Assert.Equal(("Caixa", 150.5m, true, "Banco X", 300m, 150.5m), (r.Json.GetProperty("nome").GetString()!, r.Json.GetProperty("saldoInicial").GetDecimal(), r.Json.GetProperty("ativa").GetBoolean(),
            r.Json.GetProperty("banco").GetString()!, r.Json.GetProperty("limite").GetDecimal(), r.Json.GetProperty("saldoAtual").GetDecimal()));
        var banco = await api.ContaNoBancoAsync(r.Json.GetProperty("id").GetGuid());
        Assert.Equal((loja.Id, "Caixa"), (banco.LojaId, banco.Nome));

        await api.SemVinculoAsync(loja);
        Assert.Equal((400, "Loja não encontrada."), await Erro(loja.Painel.Post($"{Rota}/contas", new { nome = "Sem loja", saldoInicial = 0m })));
    }

    [FatoIntegracao]
    public async Task Listar_mostra_so_as_contas_da_loja_com_o_saldo_e_sem_loja_devolve_lista_vazia()
    {
        var loja = await NovaAsync();
        var outra = await NovaAsync();
        var minha = await loja.ContaPelaApiAsync("Minha", 10m);
        await outra.ContaPelaApiAsync("Alheia", 99m);

        var lista = (await loja.Painel.Get($"{Rota}/contas")).Json.EnumerateArray().ToList();

        Assert.Equal([minha], lista.Select(c => c.GetProperty("id").GetGuid()));
        Assert.Equal(10m, lista[0].GetProperty("saldoAtual").GetDecimal());

        await api.SemVinculoAsync(loja);
        Assert.Empty((await loja.Painel.Get($"{Rota}/contas")).Json.EnumerateArray());
    }

    [FatoIntegracao]
    public async Task Saldo_soma_lancamentos_pagos_ajustes_e_desconta_faturas_de_cartao_e_pagamentos_antecipados()
    {
        var loja = await NovaAsync();
        var conta = await loja.ContaPelaApiAsync("Principal", 100m);
        var outraConta = await loja.ContaPelaApiAsync("Secundária", 0m);

        await api.LancamentoNoBancoAsync(loja.Id, conta, "receber", 50m);                                              // +50
        await api.LancamentoNoBancoAsync(loja.Id, conta, "pagar", 20m);                                                // -20
        await api.LancamentoNoBancoAsync(loja.Id, conta, "receber", 1000m, status: "pendente");                        // pendente não conta
        await api.LancamentoNoBancoAsync(loja.Id, conta, "pagar", 1000m, naoAfetaSaldo: true);                         // "não afeta saldo" não conta
        await api.AjusteNoBancoAsync(loja.Id, conta, "entrada", 10m);                                                  // +10
        await api.AjusteNoBancoAsync(loja.Id, conta, "saida", 5m);                                                     // -5
        await api.AjusteNoBancoAsync(loja.Id, conta, "ajuste", 3m);                                                    // +3 (a diferença já calculada)
        await api.FaturaPagaNoBancoAsync(loja.Id, contaDoCartao: conta, valorPago: 7m);                                // fatura sem conta própria: debita a do cartão
        await api.FaturaPagaNoBancoAsync(loja.Id, contaDoCartao: conta, valorPago: 1000m, contaDaFatura: outraConta); // fatura paga em outra conta não mexe nesta
        await api.FaturaPagaNoBancoAsync(loja.Id, contaDoCartao: conta, valorPago: 1000m, naoAfetaSaldo: true);        // "não afeta saldo" não conta
        await api.FaturaPagaNoBancoAsync(loja.Id, contaDoCartao: outraConta, valorPago: 0m, antecipado: 4m, contaDoAntecipado: conta);   // antecipado -4

        // 100 + 50 - 20 + 10 - 5 + 3 - 7 - 4
        var lista = (await loja.Painel.Get($"{Rota}/contas")).Json.EnumerateArray().ToList();
        Assert.Equal(127m, lista.Single(c => c.GetProperty("id").GetGuid() == conta).GetProperty("saldoAtual").GetDecimal());
        Assert.Equal(-1000m, lista.Single(c => c.GetProperty("id").GetGuid() == outraConta).GetProperty("saldoAtual").GetDecimal());

        var atualizada = await loja.Painel.Put($"{Rota}/contas/{conta}", new { nome = "Principal", saldoInicial = 100m });      // o mesmo saldo pelo caminho da conta única
        Assert.Equal(127m, atualizada.Json.GetProperty("saldoAtual").GetDecimal());
    }

    [FatoIntegracao]
    public async Task Atualizar_conta_troca_os_dados_recalcula_o_saldo_e_recusa_conta_de_outra_loja()
    {
        var loja = await NovaAsync();
        var outra = await NovaAsync();
        var conta = await loja.ContaPelaApiAsync("Antiga", 10m);
        var alheia = await outra.ContaPelaApiAsync("Alheia", 10m);

        var r = await loja.Painel.Put($"{Rota}/contas/{conta}", new { nome = "  Nova  ", saldoInicial = 40m, banco = "B", limite = 5m });

        Assert.Equal(("Nova", 40m, true, 40m), (r.Json.GetProperty("nome").GetString()!, r.Json.GetProperty("saldoInicial").GetDecimal(), r.Json.GetProperty("ativa").GetBoolean(), r.Json.GetProperty("saldoAtual").GetDecimal()));
        Assert.Equal(404, (await loja.Painel.Put($"{Rota}/contas/{alheia}", new { nome = "Invadida", saldoInicial = 0m })).Codigo);
        Assert.Equal("Alheia", (await api.ContaNoBancoAsync(alheia)).Nome);
        Assert.Equal(404, (await loja.Painel.Put($"{Rota}/contas/{Guid.NewGuid()}", new { nome = "x", saldoInicial = 0m })).Codigo);
    }

    [FatoIntegracao]
    public async Task Alternar_ativa_liga_e_desliga_e_recusa_conta_de_outra_loja()
    {
        var loja = await NovaAsync();
        var outra = await NovaAsync();
        var conta = await loja.ContaPelaApiAsync();
        var alheia = await outra.ContaPelaApiAsync();

        var desligou = await loja.Painel.Patch($"{Rota}/contas/{conta}/ativo");
        Assert.Equal(["id", "ativa"], desligou.Json.EnumerateObject().Select(x => x.Name));
        Assert.False(desligou.Json.GetProperty("ativa").GetBoolean());
        Assert.True((await loja.Painel.Patch($"{Rota}/contas/{conta}/ativo")).Json.GetProperty("ativa").GetBoolean());
        Assert.Equal(404, (await loja.Painel.Patch($"{Rota}/contas/{alheia}/ativo")).Codigo);
        Assert.True((await api.ContaNoBancoAsync(alheia)).Ativa);
    }

    // ── Ajuste de saldo ──────────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Ajustar_saldo_registra_a_diferenca_a_entrada_ou_a_saida_e_devolve_o_novo_saldo()
    {
        var loja = await NovaAsync();
        var conta = await loja.ContaPelaApiAsync("Conta", 100m);

        var ajuste = await loja.Painel.Post($"{Rota}/contas/{conta}/ajuste", new { tipo = "ajuste", novoSaldo = 180m, observacao = "Conferência do caixa" });
        Assert.Equal(["saldoAtual"], ajuste.Json.EnumerateObject().Select(x => x.Name));
        Assert.Equal(180m, ajuste.Json.GetProperty("saldoAtual").GetDecimal());
        var registrado = Assert.Single(await api.AjustesDaContaNoBancoAsync(conta));
        Assert.Equal(("ajuste", 80m, "Conferência do caixa", loja.Id), (registrado.Tipo, registrado.Valor, registrado.Observacao, registrado.LojaId));        // guarda a diferença (180 - 100)

        Assert.Equal(190m, (await loja.Painel.Post($"{Rota}/contas/{conta}/ajuste", new { tipo = "entrada", valor = 10m, novoSaldo = 0m })).Json.GetProperty("saldoAtual").GetDecimal());
        Assert.Equal(185m, (await loja.Painel.Post($"{Rota}/contas/{conta}/ajuste", new { tipo = "saida", valor = 5m, novoSaldo = 0m })).Json.GetProperty("saldoAtual").GetDecimal());
        Assert.Equal(185m, (await loja.Painel.Post($"{Rota}/contas/{conta}/ajuste", new { tipo = "entrada", novoSaldo = 0m })).Json.GetProperty("saldoAtual").GetDecimal());   // sem valor: registra zero
        Assert.Equal(4, (await api.AjustesDaContaNoBancoAsync(conta)).Count);
    }

    [FatoIntegracao]
    public async Task Ajustar_saldo_de_conta_de_outra_loja_ou_inexistente_da_404_sem_registrar()
    {
        var loja = await NovaAsync();
        var outra = await NovaAsync();
        var alheia = await outra.ContaPelaApiAsync("Alheia", 10m);

        Assert.Equal(404, (await loja.Painel.Post($"{Rota}/contas/{alheia}/ajuste", new { tipo = "entrada", valor = 5m, novoSaldo = 0m })).Codigo);
        Assert.Equal(404, (await loja.Painel.Post($"{Rota}/contas/{Guid.NewGuid()}/ajuste", new { tipo = "entrada", valor = 5m, novoSaldo = 0m })).Codigo);
        Assert.Empty(await api.AjustesDaContaNoBancoAsync(alheia));
    }

    // ── Transferência ────────────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Transferencia_valida_contas_valor_e_loja()
    {
        var loja = await NovaAsync();
        var outra = await NovaAsync();
        var a = await loja.ContaPelaApiAsync("A", 100m);
        var b = await loja.ContaPelaApiAsync("B", 0m);
        var alheia = await outra.ContaPelaApiAsync("Alheia", 0m);

        Assert.Equal((400, "Escolha duas contas diferentes."), await Erro(loja.Painel.Post($"{Rota}/contas/transferencia", new { contaOrigemId = a, contaDestinoId = a, valor = 10m, registrar = true })));
        Assert.Equal((400, "Valor deve ser maior que zero."), await Erro(loja.Painel.Post($"{Rota}/contas/transferencia", new { contaOrigemId = a, contaDestinoId = b, valor = 0m, registrar = true })));
        Assert.Equal((404, "Conta não encontrada."), await Erro(loja.Painel.Post($"{Rota}/contas/transferencia", new { contaOrigemId = a, contaDestinoId = alheia, valor = 10m, registrar = true })));
        Assert.Equal((404, "Conta não encontrada."), await Erro(loja.Painel.Post($"{Rota}/contas/transferencia", new { contaOrigemId = Guid.NewGuid(), contaDestinoId = b, valor = 10m, registrar = true })));
        Assert.Equal(100m, (await api.ContaNoBancoAsync(a)).SaldoInicial);
        Assert.Empty(await api.AjustesDaContaNoBancoAsync(alheia));

        await api.SemVinculoAsync(loja);
        Assert.Equal((400, "Loja não encontrada."), await Erro(loja.Painel.Post($"{Rota}/contas/transferencia", new { contaOrigemId = a, contaDestinoId = b, valor = 10m, registrar = true })));
    }

    [FatoIntegracao]
    public async Task Transferencia_registrada_cria_saida_e_entrada_no_mesmo_grupo_e_devolve_os_saldos()
    {
        var loja = await NovaAsync();
        var origem = await loja.ContaPelaApiAsync("Origem", 100m);
        var destino = await loja.ContaPelaApiAsync("Destino", 5m);

        var r = await loja.Painel.Post($"{Rota}/contas/transferencia", new { contaOrigemId = origem, contaDestinoId = destino, valor = 30m, registrar = true, observacao = "Reforço" });

        Assert.Equal(["saldoOrigem", "saldoDestino"], r.Json.EnumerateObject().Select(x => x.Name));
        Assert.Equal((70m, 35m), (r.Json.GetProperty("saldoOrigem").GetDecimal(), r.Json.GetProperty("saldoDestino").GetDecimal()));
        var saida = Assert.Single(await api.AjustesDaContaNoBancoAsync(origem));
        var entrada = Assert.Single(await api.AjustesDaContaNoBancoAsync(destino));
        Assert.Equal(("saida", 30m, "Transferência para Destino — Reforço"), (saida.Tipo, saida.Valor, saida.Observacao));
        Assert.Equal(("entrada", 30m, "Transferência de Origem — Reforço"), (entrada.Tipo, entrada.Valor, entrada.Observacao));
        Assert.NotNull(saida.TransferenciaGrupoId);
        Assert.Equal(saida.TransferenciaGrupoId, entrada.TransferenciaGrupoId);
        Assert.Equal((100m, 5m), ((await api.ContaNoBancoAsync(origem)).SaldoInicial, (await api.ContaNoBancoAsync(destino)).SaldoInicial));         // o saldo inicial não muda

        await loja.Painel.Post($"{Rota}/contas/transferencia", new { contaOrigemId = origem, contaDestinoId = destino, valor = 1m, registrar = true });
        Assert.Equal("Transferência para Destino", (await api.AjustesDaContaNoBancoAsync(origem))[1].Observacao);                                    // sem observação, sem o traço
    }

    [FatoIntegracao]
    public async Task Transferencia_sem_registro_mexe_direto_no_saldo_inicial_e_nao_deixa_historico()
    {
        var loja = await NovaAsync();
        var origem = await loja.ContaPelaApiAsync("Origem", 100m);
        var destino = await loja.ContaPelaApiAsync("Destino", 5m);

        var r = await loja.Painel.Post($"{Rota}/contas/transferencia", new { contaOrigemId = origem, contaDestinoId = destino, valor = 30m, registrar = false });

        Assert.Equal((70m, 35m), (r.Json.GetProperty("saldoOrigem").GetDecimal(), r.Json.GetProperty("saldoDestino").GetDecimal()));
        Assert.Equal((70m, 35m), ((await api.ContaNoBancoAsync(origem)).SaldoInicial, (await api.ContaNoBancoAsync(destino)).SaldoInicial));
        Assert.Empty(await api.AjustesDaContaNoBancoAsync(origem));
        Assert.Empty(await api.AjustesDaContaNoBancoAsync(destino));
    }

    // ── Categorias ───────────────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Categorias_lista_so_as_ativas_da_loja_por_nome_e_filtra_pelo_tipo_incluindo_ambos()
    {
        var loja = await NovaAsync();
        var outra = await NovaAsync();
        await loja.Painel.Post($"{Rota}/categorias", new { nome = "Zebra", tipo = "pagar", icone = "🦓" });
        await loja.Painel.Post($"{Rota}/categorias", new { nome = "Aluguel", tipo = "pagar", icone = "🏠" });
        await loja.Painel.Post($"{Rota}/categorias", new { nome = "Vendas", tipo = "receber", icone = "🛒" });
        await loja.Painel.Post($"{Rota}/categorias", new { nome = "Outros", tipo = "ambos", icone = "📁" });
        await outra.Painel.Post($"{Rota}/categorias", new { nome = "Alheia", tipo = "pagar" });
        await api.NoBancoAsync(db => db.CategoriasFinanceiras.Where(c => c.LojaId == loja.Id && c.Nome == "Zebra").ExecuteUpdateAsync(s => s.SetProperty(c => c.Ativa, false)));

        var todas = (await loja.Painel.Get($"{Rota}/categorias")).Json.EnumerateArray().ToList();
        Assert.Equal(["Aluguel", "Outros", "Vendas"], todas.Select(c => c.GetProperty("nome").GetString()));
        Assert.Equal(["id", "nome", "tipo", "icone"], todas[0].EnumerateObject().Select(x => x.Name));

        var pagar = (await loja.Painel.Get($"{Rota}/categorias?tipo=pagar")).Json.EnumerateArray().Select(c => c.GetProperty("nome").GetString()).ToList();
        Assert.Equal(["Aluguel", "Outros"], pagar);

        await api.SemVinculoAsync(loja);
        Assert.Empty((await loja.Painel.Get($"{Rota}/categorias")).Json.EnumerateArray());
    }

    [FatoIntegracao]
    public async Task Criar_categoria_devolve_a_que_ja_existe_sem_diferenciar_maiuscula_nem_acento()
    {
        var loja = await NovaAsync();

        var primeira = await loja.Painel.Post($"{Rota}/categorias", new { nome = "  Água  ", tipo = "pagar", icone = "💧" });
        Assert.Equal("Água", primeira.Json.GetProperty("nome").GetString());
        var id = primeira.Json.GetProperty("id").GetGuid();

        var repetida = await loja.Painel.Post($"{Rota}/categorias", new { nome = "AGUA", tipo = "pagar" });
        Assert.Equal(id, repetida.Json.GetProperty("id").GetGuid());                                                   // devolve a existente, sem duplicar
        var deOutroTipo = await loja.Painel.Post($"{Rota}/categorias", new { nome = "agua", tipo = "receber" });
        Assert.NotEqual(id, deOutroTipo.Json.GetProperty("id").GetGuid());                                              // tipo incompatível: cria outra
        var ambos = await loja.Painel.Post($"{Rota}/categorias", new { nome = "agua", tipo = "ambos" });
        Assert.Contains(ambos.Json.GetProperty("id").GetGuid(), new[] { id, deOutroTipo.Json.GetProperty("id").GetGuid() });   // "ambos" casa com qualquer tipo: devolve uma das que já existem

        Assert.Equal(2, (await api.CategoriasFinanceirasNoBancoAsync(loja.Id)).Count);

        await api.SemVinculoAsync(loja);
        Assert.Equal((400, "Loja não encontrada."), await Erro(loja.Painel.Post($"{Rota}/categorias", new { nome = "Nova", tipo = "pagar" })));
    }

    [FatoIntegracao]
    public async Task Atualizar_categoria_troca_nome_tipo_e_icone_e_recusa_a_de_outra_loja()
    {
        var loja = await NovaAsync();
        var outra = await NovaAsync();
        var id = (await loja.Painel.Post($"{Rota}/categorias", new { nome = "Antiga", tipo = "pagar", icone = "🏠" })).Json.GetProperty("id").GetGuid();
        var alheia = (await outra.Painel.Post($"{Rota}/categorias", new { nome = "Alheia", tipo = "pagar" })).Json.GetProperty("id").GetGuid();

        var r = await loja.Painel.Put($"{Rota}/categorias/{id}", new { nome = "  Nova  ", tipo = "ambos", icone = "📁" });

        Assert.Equal(("Nova", "ambos", "📁"), (r.Json.GetProperty("nome").GetString()!, r.Json.GetProperty("tipo").GetString()!, r.Json.GetProperty("icone").GetString()!));
        Assert.Equal(404, (await loja.Painel.Put($"{Rota}/categorias/{alheia}", new { nome = "Invadida", tipo = "pagar" })).Codigo);
        Assert.Equal("Alheia", (await api.CategoriasFinanceirasNoBancoAsync(outra.Id)).Single().Nome);
    }

    [FatoIntegracao]
    public async Task Excluir_categoria_apaga_a_sem_uso_e_so_desativa_a_que_tem_lancamento_ou_fixo()
    {
        var loja = await NovaAsync();
        var outra = await NovaAsync();
        var conta = await loja.ContaPelaApiAsync();
        var livre = (await loja.Painel.Post($"{Rota}/categorias", new { nome = "Livre", tipo = "pagar" })).Json.GetProperty("id").GetGuid();
        var usada = (await loja.Painel.Post($"{Rota}/categorias", new { nome = "Usada", tipo = "pagar" })).Json.GetProperty("id").GetGuid();
        var alheia = (await outra.Painel.Post($"{Rota}/categorias", new { nome = "Alheia", tipo = "pagar" })).Json.GetProperty("id").GetGuid();
        await api.LancamentoNoBancoAsync(loja.Id, conta, "pagar", 10m, categoriaId: usada);

        Assert.Equal("Categoria excluída.", (await loja.Painel.Delete($"{Rota}/categorias/{livre}")).Json.GetProperty("mensagem").GetString());
        Assert.Equal("Categoria em uso — foi desativada em vez de excluída.", (await loja.Painel.Delete($"{Rota}/categorias/{usada}")).Json.GetProperty("mensagem").GetString());
        Assert.Equal(404, (await loja.Painel.Delete($"{Rota}/categorias/{alheia}")).Codigo);
        Assert.Equal(404, (await loja.Painel.Delete($"{Rota}/categorias/{Guid.NewGuid()}")).Codigo);

        var restantes = await api.CategoriasFinanceirasNoBancoAsync(loja.Id);
        var u = Assert.Single(restantes);
        Assert.Equal(("Usada", false), (u.Nome, u.Ativa));
    }

    [FatoIntegracao]
    public async Task Categoria_com_lancamento_fixo_tambem_so_e_desativada()
    {
        var loja = await NovaAsync();
        var conta = await loja.ContaPelaApiAsync();
        var id = (await loja.Painel.Post($"{Rota}/categorias", new { nome = "Com fixo", tipo = "pagar" })).Json.GetProperty("id").GetGuid();
        await api.NoBancoAsync(async db =>
        {
            db.LancamentosFixos.Add(new LojaApi.Models.LancamentoFixo { LojaId = loja.Id, ContaBancariaId = conta, Tipo = "pagar", Descricao = "Fixo", Valor = 10m, DiaVencimento = 5, CategoriaId = id });
            await db.SaveChangesAsync();
        });

        Assert.Equal("Categoria em uso — foi desativada em vez de excluída.", (await loja.Painel.Delete($"{Rota}/categorias/{id}")).Json.GetProperty("mensagem").GetString());
        Assert.False((await api.CategoriasFinanceirasNoBancoAsync(loja.Id)).Single().Ativa);
    }

    [FatoIntegracao]
    public async Task Seed_cria_as_10_categorias_padrao_uma_vez_so()
    {
        var loja = await NovaAsync();

        var r = await loja.Painel.Post($"{Rota}/categorias/seed-padrao");

        Assert.Equal("Categorias padrão criadas.", r.Json.GetProperty("mensagem").GetString());
        var cats = await api.CategoriasFinanceirasNoBancoAsync(loja.Id);
        Assert.Equal(10, cats.Count);
        Assert.Equal(("pagar", "🏠"), (cats.Single(c => c.Nome == "Aluguel").Tipo, cats.Single(c => c.Nome == "Aluguel").Icone));
        Assert.Equal("receber", cats.Single(c => c.Nome == "Venda avulsa").Tipo);
        Assert.Equal("ambos", cats.Single(c => c.Nome == "Outros").Tipo);
        Assert.Equal((400, "Você já tem categorias cadastradas."), await Erro(loja.Painel.Post($"{Rota}/categorias/seed-padrao")));

        var semLoja = await NovaAsync();
        await api.SemVinculoAsync(semLoja);
        Assert.Equal((400, "Loja não encontrada."), await Erro(semLoja.Painel.Post($"{Rota}/categorias/seed-padrao")));
    }
}
