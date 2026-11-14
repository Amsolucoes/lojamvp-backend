using LojaApi.Models;
using Microsoft.EntityFrameworkCore;

namespace LojaApi.Tests.Suporte;

// Preparos do módulo de Vendas (venda, troca, caixa, origens e relatórios).
public static class CenariosVendas
{
    // Produto direto no banco (o cadastro pela API tem regras que não importam aqui)
    public static Task<Guid> ProdutoAsync(this ApiFixture api, Guid lojaId, string nome = "Camiseta", decimal estoque = 10m, decimal preco = 50m,
        decimal custo = 20m, bool ativo = true) => api.NoBancoAsync(async db =>
    {
        var p = new Produto { LojaId = lojaId, Nome = nome, Categoria = "outro", Estoque = estoque, PrecoVenda = preco, PrecoCusto = custo, Ativo = ativo };
        db.Produtos.Add(p);
        await db.SaveChangesAsync();
        return p.Id;
    });

    public static Task<Guid> VariacaoAsync(this ApiFixture api, Guid produtoId, string? tamanho = "M", string? cor = "Azul", int estoque = 5) =>
        api.NoBancoAsync(async db =>
        {
            var v = new ProdutoVariacao { ProdutoId = produtoId, Tamanho = tamanho, Cor = cor, Estoque = estoque };
            db.ProdutoVariacoes.Add(v);
            await db.SaveChangesAsync();
            return v.Id;
        });

    public static object ItemProduto(Guid produto, decimal quantidade = 1m, decimal preco = 50m, Guid? variacao = null) =>
        new { produtoId = produto, quantidade, precoUnitario = preco, variacaoId = variacao, servicoId = (Guid?)null, agendamentoId = (Guid?)null, assinaturaId = (Guid?)null };

    public static object ItemSemProdutoNemServico(decimal quantidade = 1m, decimal preco = 50m) =>
        new { produtoId = (Guid?)null, quantidade, precoUnitario = preco, variacaoId = (Guid?)null, servicoId = (Guid?)null, agendamentoId = (Guid?)null, assinaturaId = (Guid?)null };

    public static object ItemServico(Guid servico, decimal quantidade = 1m, decimal preco = 80m, Guid? agendamento = null, Guid? assinatura = null) =>
        new { produtoId = (Guid?)null, quantidade, precoUnitario = preco, variacaoId = (Guid?)null, servicoId = servico, agendamentoId = agendamento, assinaturaId = assinatura };

    public static object CorpoVenda(object[] itens, Guid? cliente = null, decimal desconto = 0m, string forma = "pix", string? formas = null, decimal? troco = null,
        decimal? credito = null, DateTime? data = null, Guid? origem = null, Guid? funcionario = null) => new
    {
        itens, clienteId = cliente, desconto, formaPagamento = forma, formasPagamento = formas, troco, creditoUsado = credito, dataVenda = data,
        origemVendaId = origem, funcionarioId = funcionario,
    };

    public static async Task<Guid> VenderAsync(this LojaTeste loja, object[] itens, Guid? cliente = null, decimal desconto = 0m, DateTime? data = null,
        Guid? origem = null, Guid? funcionario = null, decimal? credito = null)
    {
        var r = await loja.Painel.Post("/api/vendas", CorpoVenda(itens, cliente, desconto, data: data, origem: origem, funcionario: funcionario, credito: credito));
        r.DeveSerSucesso();
        return r.Json.GetProperty("id").GetGuid();
    }

    public static Task<Produto> ProdutoDaLojaNoBancoAsync(this ApiFixture api, Guid id) => api.NoBancoAsync(db => db.Produtos.AsNoTracking().FirstAsync(p => p.Id == id));
    public static Task<ProdutoVariacao> VariacaoNoBancoAsync(this ApiFixture api, Guid id) => api.NoBancoAsync(db => db.ProdutoVariacoes.AsNoTracking().FirstAsync(v => v.Id == id));
    public static Task<Venda> VendaNoBancoAsync(this ApiFixture api, Guid id) => api.NoBancoAsync(db => db.Vendas.AsNoTracking().FirstAsync(v => v.Id == id));
    public static Task<List<MovimentoEstoque>> MovimentosDoProdutoAsync(this ApiFixture api, Guid produto) =>
        api.NoBancoAsync(db => db.Movimentos.AsNoTracking().Where(m => m.ProdutoId == produto).OrderBy(m => m.CriadoEm).ToListAsync());
    public static Task<decimal> CreditoDoClienteAsync(this ApiFixture api, Guid cliente) => api.NoBancoAsync(db => db.Clientes.Where(c => c.Id == cliente).Select(c => c.CreditoLoja).FirstAsync());
    public static Task DarCreditoAsync(this ApiFixture api, Guid cliente, decimal valor) =>
        api.NoBancoAsync(db => db.Clientes.Where(c => c.Id == cliente).ExecuteUpdateAsync(s => s.SetProperty(c => c.CreditoLoja, valor)));
    public static Task ConfigurarDataDaVendaAsync(this ApiFixture api, Guid venda, DateTime quando) =>
        api.NoBancoAsync(db => db.Vendas.Where(v => v.Id == venda).ExecuteUpdateAsync(s => s.SetProperty(v => v.CriadaEm, quando)));
}
