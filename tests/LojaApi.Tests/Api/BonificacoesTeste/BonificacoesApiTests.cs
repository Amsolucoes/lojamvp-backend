using System.Text.Json;
using LojaApi.Domain.Bonificacoes;
using LojaApi.Models;
using Microsoft.EntityFrameworkCore;

namespace LojaApi.Tests.Api.BonificacoesTeste;

// Bonificação (brinde ao cliente): registrar (avulso), estornar, listar e o relatório de custo. Só o administrador registra e estorna.
[Collection(ColecaoApi.Nome)]
public class BonificacoesApiTests(ApiFixture api)
{
    private const string Rota = "/api/bonificacoes";

    private Task<LojaTeste> NovaAsync() => api.NovaLojaDeServicosAsync();

    private static Guid Id(JsonElement e) => e.GetProperty("id").GetGuid();
    private static decimal Num(JsonElement e, string campo) => e.GetProperty(campo).GetDecimal();
    private static string Texto(JsonElement e, string campo) => e.GetProperty(campo).GetString()!;

    private static object Corpo(Guid cliente, Guid produto, decimal quantidade = 1m, Guid? variacao = null, string? observacao = null) =>
        new { clienteId = cliente, produtoId = produto, quantidade, variacaoId = variacao, observacao };

    private Task<List<Bonificacao>> NoBancoAsync(Guid lojaId) =>
        api.NoBancoAsync(db => db.Bonificacoes.AsNoTracking().Where(b => b.LojaId == lojaId).OrderBy(b => b.CriadoEm).ToListAsync());

    // ── Permissões ───────────────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Exige_login_e_so_admin_registra_e_estorna_mas_qualquer_papel_consulta()
    {
        var loja = await NovaAsync();
        var cliente = await loja.ClienteIdAsync();
        var produto = await api.ProdutoAsync(loja.Id, estoque: 5);
        var bonificacao = Id((await loja.Painel.Post(Rota, Corpo(cliente, produto))).Json);

        Assert.Equal(401, (await loja.Site.Get(Rota)).Codigo);
        Assert.Equal(401, (await loja.Site.Get($"{Rota}/resumo")).Codigo);
        Assert.Equal(401, (await loja.Site.Post(Rota, Corpo(cliente, produto))).Codigo);
        Assert.Equal(401, (await loja.Site.Post($"{Rota}/{bonificacao}/estornar")).Codigo);

        var operador = await api.OperadorAsync(loja);
        Assert.Equal(200, (await operador.Get(Rota)).Codigo);
        Assert.Equal(200, (await operador.Get($"{Rota}/resumo")).Codigo);
        Assert.Equal(403, (await operador.Post(Rota, Corpo(cliente, produto))).Codigo);
        Assert.Equal(403, (await operador.Post($"{Rota}/{bonificacao}/estornar")).Codigo);
        Assert.Equal(4m, (await api.ProdutoDaLojaNoBancoAsync(produto)).Estoque);
        Assert.Single(await NoBancoAsync(loja.Id));
    }

    // ── Registrar ────────────────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Registrar_baixa_o_estoque_grava_o_movimento_com_a_observacao_e_guarda_o_custo_e_os_nomes()
    {
        var loja = await NovaAsync();
        var cliente = await loja.ClienteIdAsync("Maria Souza");
        var produto = await api.ProdutoAsync(loja.Id, "Pulseira", estoque: 10, preco: 30m, custo: 8m);

        var r = await loja.Painel.Post(Rota, Corpo(cliente, produto, 2m, observacao: "  aniversário  "));

        Assert.Equal(201, r.Codigo);
        Assert.Equal(["id", "clienteId", "nomeCliente", "produtoId", "variacaoId", "nomeProduto", "quantidade", "custoUnitario", "custoTotal", "observacao", "vendaId", "usuarioId", "criadoEm", "estornadaEm"],
            r.Json.EnumerateObject().Select(x => x.Name));
        Assert.Equal((cliente, "Maria Souza", produto, "Pulseira", 2m, 8m, 16m, "aniversário"),
            (r.Json.GetProperty("clienteId").GetGuid(), Texto(r.Json, "nomeCliente"), r.Json.GetProperty("produtoId").GetGuid(), Texto(r.Json, "nomeProduto"),
                Num(r.Json, "quantidade"), Num(r.Json, "custoUnitario"), Num(r.Json, "custoTotal"), Texto(r.Json, "observacao")));
        Assert.Equal(JsonValueKind.Null, r.Json.GetProperty("vendaId").ValueKind);
        Assert.Equal(JsonValueKind.Null, r.Json.GetProperty("estornadaEm").ValueKind);
        Assert.NotEqual(JsonValueKind.Null, r.Json.GetProperty("usuarioId").ValueKind);

        Assert.Equal(8m, (await api.ProdutoDaLojaNoBancoAsync(produto)).Estoque);
        var movimento = Assert.Single(await api.MovimentosDoProdutoAsync(produto));
        Assert.Equal(("saida", 2m, "Bonificação - Maria Souza: aniversário", loja.Id), (movimento.Tipo, movimento.Quantidade, movimento.Observacao, movimento.LojaId));
        var guardada = Assert.Single(await NoBancoAsync(loja.Id));
        Assert.Equal((cliente, produto, 2m, 8m, loja.Id), (guardada.ClienteId, guardada.ProdutoId, guardada.Quantidade, guardada.CustoUnitario, guardada.LojaId));
    }

    [FatoIntegracao]
    public async Task Registrar_pode_dar_o_estoque_todo_mas_nao_mais_que_isso()
    {
        var loja = await NovaAsync();
        var cliente = await loja.ClienteIdAsync();
        var produto = await api.ProdutoAsync(loja.Id, estoque: 2);

        var demais = await loja.Painel.Post(Rota, Corpo(cliente, produto, 3m));
        var tudo = await loja.Painel.Post(Rota, Corpo(cliente, produto, 2m));

        Assert.Equal(400, demais.Codigo);
        Assert.Equal("A quantidade (3) é maior que o estoque atual (2.000).", demais.Erro);
        Assert.Equal(201, tudo.Codigo);
        Assert.Equal(0m, (await api.ProdutoDaLojaNoBancoAsync(produto)).Estoque);
    }

    [FatoIntegracao]
    public async Task Registrar_valida_cliente_produto_variacao_e_quantidade_sem_gravar_nada()
    {
        var loja = await NovaAsync();
        var outra = await NovaAsync();
        var cliente = await loja.ClienteIdAsync();
        var clienteAlheio = await outra.ClienteIdAsync("Alheia");
        var produto = await api.ProdutoAsync(loja.Id, estoque: 5);
        var produtoAlheio = await api.ProdutoAsync(outra.Id, estoque: 5);
        var inativo = await api.ProdutoAsync(loja.Id, "Inativo", estoque: 5, ativo: false);
        var comGrade = await api.ProdutoAsync(loja.Id, "Anel", estoque: 0);
        var variacao = await api.VariacaoAsync(comGrade, "M", "Azul", estoque: 2);

        var semCliente = await loja.Painel.Post(Rota, Corpo(Guid.NewGuid(), produto));
        var clienteDeOutra = await loja.Painel.Post(Rota, Corpo(clienteAlheio, produto));
        var produtoDeOutra = await loja.Painel.Post(Rota, Corpo(cliente, produtoAlheio));
        var produtoInexistente = await loja.Painel.Post(Rota, Corpo(cliente, Guid.NewGuid()));
        var produtoInativo = await loja.Painel.Post(Rota, Corpo(cliente, inativo));
        var zero = await loja.Painel.Post(Rota, Corpo(cliente, produto, 0m));
        var negativa = await loja.Painel.Post(Rota, Corpo(cliente, produto, -1m));
        var variacaoInexistente = await loja.Painel.Post(Rota, Corpo(cliente, comGrade, 1m, Guid.NewGuid()));
        var variacaoFracionada = await loja.Painel.Post(Rota, Corpo(cliente, comGrade, 1.5m, variacao));
        var variacaoDemais = await loja.Painel.Post(Rota, Corpo(cliente, comGrade, 3m, variacao));

        Assert.Equal((400, "Cliente não encontrado."), (semCliente.Codigo, semCliente.Erro));
        Assert.Equal((400, "Cliente não encontrado."), (clienteDeOutra.Codigo, clienteDeOutra.Erro));
        Assert.Equal(404, produtoDeOutra.Codigo);
        Assert.Equal(404, produtoInexistente.Codigo);
        Assert.Equal((400, "Produto 'Inativo' está inativo."), (produtoInativo.Codigo, produtoInativo.Erro));
        Assert.Equal((400, "Informe uma quantidade maior que zero."), (zero.Codigo, zero.Erro));
        Assert.Equal((400, "Informe uma quantidade maior que zero."), (negativa.Codigo, negativa.Erro));
        Assert.Equal(404, variacaoInexistente.Codigo);
        Assert.Equal((400, "Para produto com variação, a quantidade deve ser um número inteiro."), (variacaoFracionada.Codigo, variacaoFracionada.Erro));
        Assert.Equal((400, "A quantidade (3) é maior que o estoque atual desta variação (2)."), (variacaoDemais.Codigo, variacaoDemais.Erro));

        Assert.Empty(await NoBancoAsync(loja.Id));
        Assert.Empty(await api.MovimentosDoProdutoAsync(produto));
        Assert.Equal((5m, 2), ((await api.ProdutoDaLojaNoBancoAsync(produto)).Estoque, (await api.VariacaoNoBancoAsync(variacao)).Estoque));
        Assert.Equal(5m, (await api.ProdutoDaLojaNoBancoAsync(produtoAlheio)).Estoque);
    }

    [FatoIntegracao]
    public async Task Registrar_produto_com_variacao_baixa_so_a_variacao_e_leva_o_tamanho_e_a_cor_no_nome()
    {
        var loja = await NovaAsync();
        var cliente = await loja.ClienteIdAsync();
        var produto = await api.ProdutoAsync(loja.Id, "Anel", estoque: 7, custo: 12m);
        var variacao = await api.VariacaoAsync(produto, "M", "Azul", estoque: 4);

        var r = await loja.Painel.Post(Rota, Corpo(cliente, produto, 3m, variacao));

        Assert.Equal(201, r.Codigo);
        Assert.Equal(("Anel (M / Azul)", variacao, 36m), (Texto(r.Json, "nomeProduto"), r.Json.GetProperty("variacaoId").GetGuid(), Num(r.Json, "custoTotal")));
        Assert.Equal((7m, 1), ((await api.ProdutoDaLojaNoBancoAsync(produto)).Estoque, (await api.VariacaoNoBancoAsync(variacao)).Estoque));
        Assert.Equal("saida", Assert.Single(await api.MovimentosDoProdutoAsync(produto)).Tipo);
    }

    // ── Listar e relatório ───────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Listar_traz_so_as_da_loja_da_mais_nova_para_a_mais_antiga_e_filtra_por_cliente_produto_e_periodo()
    {
        var loja = await NovaAsync();
        var outra = await NovaAsync();
        var ana = await loja.ClienteIdAsync("Ana", "67999990001");
        var bia = await loja.ClienteIdAsync("Bia", "67999990002");
        var pulseira = await api.ProdutoAsync(loja.Id, "Pulseira", estoque: 20);
        var colar = await api.ProdutoAsync(loja.Id, "Colar", estoque: 20);
        var primeira = Id((await loja.Painel.Post(Rota, Corpo(ana, pulseira))).Json);
        var segunda = Id((await loja.Painel.Post(Rota, Corpo(bia, colar))).Json);
        var terceira = Id((await loja.Painel.Post(Rota, Corpo(ana, colar))).Json);
        await outra.Painel.Post(Rota, Corpo(await outra.ClienteIdAsync(), await api.ProdutoAsync(outra.Id, estoque: 5)));
        await api.NoBancoAsync(db => db.Bonificacoes.Where(b => b.Id == primeira).ExecuteUpdateAsync(s => s.SetProperty(b => b.CriadoEm, new DateTime(2025, 1, 10, 12, 0, 0, DateTimeKind.Utc))));
        await api.NoBancoAsync(db => db.Bonificacoes.Where(b => b.Id == segunda).ExecuteUpdateAsync(s => s.SetProperty(b => b.CriadoEm, new DateTime(2025, 2, 10, 12, 0, 0, DateTimeKind.Utc))));

        var todas = (await loja.Painel.Get(Rota)).Json.EnumerateArray().Select(Id).ToList();
        var daAna = (await loja.Painel.Get($"{Rota}?clienteId={ana}")).Json.EnumerateArray().Select(Id).ToList();
        var doColar = (await loja.Painel.Get($"{Rota}?produtoId={colar}")).Json.EnumerateArray().Select(Id).ToList();
        var janeiro = (await loja.Painel.Get($"{Rota}?de=2025-01-01&ate=2025-01-10")).Json.EnumerateArray().Select(Id).ToList();
        var foraDoPeriodo = (await loja.Painel.Get($"{Rota}?de=2025-03-01&ate=2025-03-31")).Json.EnumerateArray().ToList();

        Assert.Equal([terceira, segunda, primeira], todas);
        Assert.Equal([terceira, primeira], daAna);
        Assert.Equal([terceira, segunda], doColar);
        Assert.Equal([primeira], janeiro);      // o dia "ate" entra inteiro
        Assert.Empty(foraDoPeriodo);
    }

    [FatoIntegracao]
    public async Task Resumo_soma_o_custo_por_produto_e_por_cliente_sem_as_estornadas_e_filtra_o_periodo()
    {
        var loja = await NovaAsync();
        var outra = await NovaAsync();
        var ana = await loja.ClienteIdAsync("Ana", "67999990001");
        var bia = await loja.ClienteIdAsync("Bia", "67999990002");
        var pulseira = await api.ProdutoAsync(loja.Id, "Pulseira", estoque: 20, custo: 8m);
        var colar = await api.ProdutoAsync(loja.Id, "Colar", estoque: 20, custo: 30m);
        await loja.Painel.Post(Rota, Corpo(ana, pulseira, 2m));       // 16 (Ana)
        await loja.Painel.Post(Rota, Corpo(bia, pulseira, 1m));       // 8 (Bia)
        await loja.Painel.Post(Rota, Corpo(ana, colar, 1m));          // 30 (Ana)
        var desfeita = Id((await loja.Painel.Post(Rota, Corpo(bia, colar, 5m))).Json);   // estornada: não conta
        await loja.Painel.Post($"{Rota}/{desfeita}/estornar");
        await outra.Painel.Post(Rota, Corpo(await outra.ClienteIdAsync(), await api.ProdutoAsync(outra.Id, estoque: 5, custo: 100m)));

        var r = await loja.Painel.Get($"{Rota}/resumo");

        Assert.Equal(200, r.Codigo);
        Assert.Equal(["totalBonificacoes", "totalQuantidade", "totalCusto", "porProduto", "porCliente"], r.Json.EnumerateObject().Select(x => x.Name));
        Assert.Equal((3, 4m, 54m), (r.Json.GetProperty("totalBonificacoes").GetInt32(), Num(r.Json, "totalQuantidade"), Num(r.Json, "totalCusto")));
        var porProduto = r.Json.GetProperty("porProduto").EnumerateArray().ToList();
        Assert.Equal(["produtoId", "nomeProduto", "quantidade", "custo"], porProduto[0].EnumerateObject().Select(x => x.Name));
        Assert.Equal([("Colar", 1m, 30m), ("Pulseira", 3m, 24m)], porProduto.Select(p => (Texto(p, "nomeProduto"), Num(p, "quantidade"), Num(p, "custo"))));
        var porCliente = r.Json.GetProperty("porCliente").EnumerateArray().ToList();
        Assert.Equal(["clienteId", "nomeCliente", "bonificacoes", "quantidade", "custo"], porCliente[0].EnumerateObject().Select(x => x.Name));
        Assert.Equal([("Ana", 2, 3m, 46m), ("Bia", 1, 1m, 8m)], porCliente.Select(c => (Texto(c, "nomeCliente"), c.GetProperty("bonificacoes").GetInt32(), Num(c, "quantidade"), Num(c, "custo"))));

        var vazio = await loja.Painel.Get($"{Rota}/resumo?de=2001-01-01&ate=2001-01-31");
        Assert.Equal((0, 0m, 0m), (vazio.Json.GetProperty("totalBonificacoes").GetInt32(), Num(vazio.Json, "totalQuantidade"), Num(vazio.Json, "totalCusto")));
        Assert.Empty(vazio.Json.GetProperty("porProduto").EnumerateArray());
    }

    [FatoIntegracao]
    public async Task Listar_esconde_as_estornadas_por_padrao_e_mostra_com_incluirEstornadas()
    {
        var loja = await NovaAsync();
        var cliente = await loja.ClienteIdAsync();
        var produto = await api.ProdutoAsync(loja.Id, estoque: 5);
        var mantida = Id((await loja.Painel.Post(Rota, Corpo(cliente, produto))).Json);
        var desfeita = Id((await loja.Painel.Post(Rota, Corpo(cliente, produto))).Json);
        await loja.Painel.Post($"{Rota}/{desfeita}/estornar");

        var padrao = (await loja.Painel.Get(Rota)).Json.EnumerateArray().Select(Id).ToList();
        var todas = (await loja.Painel.Get($"{Rota}?incluirEstornadas=true")).Json.EnumerateArray().ToList();

        Assert.Equal([mantida], padrao);
        Assert.Equal([desfeita, mantida], todas.Select(Id));
        Assert.NotEqual(JsonValueKind.Null, todas[0].GetProperty("estornadaEm").ValueKind);
        Assert.Equal(JsonValueKind.Null, todas[1].GetProperty("estornadaEm").ValueKind);
    }

    [FatoIntegracao]
    public async Task Sem_vinculo_com_loja_listar_e_resumo_voltam_vazios_e_registrar_recusa()
    {
        var loja = await NovaAsync();
        var cliente = await loja.ClienteIdAsync();
        var produto = await api.ProdutoAsync(loja.Id, estoque: 5);
        await api.SemVinculoAsync(loja);

        var lista = await loja.Painel.Get(Rota);
        var resumo = await loja.Painel.Get($"{Rota}/resumo");
        var registrar = await loja.Painel.Post(Rota, Corpo(cliente, produto));

        Assert.Equal(0, lista.Json.GetArrayLength());
        Assert.Equal(0, resumo.Json.GetProperty("totalBonificacoes").GetInt32());
        Assert.Equal((400, "Loja não encontrada."), (registrar.Codigo, registrar.Erro));
        Assert.Equal(5m, (await api.ProdutoDaLojaNoBancoAsync(produto)).Estoque);
    }

    // ── Estornar ─────────────────────────────────────────────────────────────

    [FatoIntegracao]
    public async Task Estornar_devolve_o_estoque_com_um_movimento_de_entrada_e_marca_a_bonificacao()
    {
        var loja = await NovaAsync();
        var cliente = await loja.ClienteIdAsync("Maria");
        var produto = await api.ProdutoAsync(loja.Id, "Pulseira", estoque: 10);
        var bonificacao = Id((await loja.Painel.Post(Rota, Corpo(cliente, produto, 4m))).Json);

        var r = await loja.Painel.Post($"{Rota}/{bonificacao}/estornar");

        Assert.Equal(200, r.Codigo);
        Assert.NotEqual(JsonValueKind.Null, r.Json.GetProperty("estornadaEm").ValueKind);
        Assert.Equal(10m, (await api.ProdutoDaLojaNoBancoAsync(produto)).Estoque);
        var entrada = (await api.MovimentosDoProdutoAsync(produto)).Last();
        Assert.Equal(("entrada", 4m, "Estorno da bonificação - Maria - Pulseira"), (entrada.Tipo, entrada.Quantidade, entrada.Observacao));
        Assert.True((await NoBancoAsync(loja.Id)).Single().Estornada());

        var denovo = await loja.Painel.Post($"{Rota}/{bonificacao}/estornar");
        Assert.Equal((400, "Esta bonificação já foi estornada."), (denovo.Codigo, denovo.Erro));
        Assert.Equal(10m, (await api.ProdutoDaLojaNoBancoAsync(produto)).Estoque);
    }

    [FatoIntegracao]
    public async Task Estornar_de_variacao_volta_para_a_variacao_e_de_outra_loja_ou_inexistente_da_404()
    {
        var loja = await NovaAsync();
        var outra = await NovaAsync();
        var cliente = await loja.ClienteIdAsync();
        var produto = await api.ProdutoAsync(loja.Id, "Anel", estoque: 3);
        var variacao = await api.VariacaoAsync(produto, "M", "Azul", estoque: 5);
        var bonificacao = Id((await loja.Painel.Post(Rota, Corpo(cliente, produto, 2m, variacao))).Json);
        Assert.Equal(3, (await api.VariacaoNoBancoAsync(variacao)).Estoque);

        Assert.Equal(404, (await outra.Painel.Post($"{Rota}/{bonificacao}/estornar")).Codigo);
        Assert.Equal(404, (await loja.Painel.Post($"{Rota}/{Guid.NewGuid()}/estornar")).Codigo);
        Assert.Equal(3, (await api.VariacaoNoBancoAsync(variacao)).Estoque);

        await loja.Painel.Post($"{Rota}/{bonificacao}/estornar");

        Assert.Equal((5, 3m), ((await api.VariacaoNoBancoAsync(variacao)).Estoque, (await api.ProdutoDaLojaNoBancoAsync(produto)).Estoque));
    }

    [FatoIntegracao]
    public async Task Estornar_recusa_quando_o_produto_foi_excluido()
    {
        var loja = await NovaAsync();
        var cliente = await loja.ClienteIdAsync();
        var produto = await api.ProdutoAsync(loja.Id, "Descontinuado", estoque: 3);
        var bonificacao = Id((await loja.Painel.Post(Rota, Corpo(cliente, produto))).Json);
        await api.NoBancoAsync(db => db.Produtos.Where(p => p.Id == produto).ExecuteDeleteAsync());

        var r = await loja.Painel.Post($"{Rota}/{bonificacao}/estornar");

        Assert.Equal((400, "O produto desta bonificação foi excluído; não dá para devolver o estoque."), (r.Codigo, r.Erro));
        Assert.False((await NoBancoAsync(loja.Id)).Single().Estornada());
    }

    // ── O histórico sobrevive à exclusão do cliente e do produto ─────────────

    [FatoIntegracao]
    public async Task Excluir_o_cliente_ou_o_produto_mantem_a_bonificacao_com_os_nomes_e_o_custo()
    {
        var loja = await NovaAsync();
        var cliente = await loja.ClienteIdAsync("Cliente Saiu");
        var produto = await api.ProdutoAsync(loja.Id, "Produto Saiu", estoque: 5, custo: 10m);
        await loja.Painel.Post(Rota, Corpo(cliente, produto, 2m));

        await api.NoBancoAsync(db => db.Clientes.Where(c => c.Id == cliente).ExecuteDeleteAsync());
        await api.NoBancoAsync(db => db.Produtos.Where(p => p.Id == produto).ExecuteDeleteAsync());

        var item = Assert.Single((await loja.Painel.Get(Rota)).Json.EnumerateArray());
        Assert.Equal(("Cliente Saiu", "Produto Saiu", 20m), (Texto(item, "nomeCliente"), Texto(item, "nomeProduto"), Num(item, "custoTotal")));
        Assert.Equal(JsonValueKind.Null, item.GetProperty("clienteId").ValueKind);
        Assert.Equal(JsonValueKind.Null, item.GetProperty("produtoId").ValueKind);
        Assert.Equal(20m, Num((await loja.Painel.Get($"{Rota}/resumo")).Json, "totalCusto"));
    }
}
