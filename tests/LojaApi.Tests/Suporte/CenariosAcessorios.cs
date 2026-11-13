using LojaApi.src.Models;
using Microsoft.EntityFrameworkCore;

namespace LojaApi.Tests.Suporte;

// Preparos da loja de acessórios (catálogo da própria plataforma, sem "loja": quem gerencia é o superadmin).
// Há duas famílias de rotas parecidas: /api/loja-acessorios (pedidos, frete) e /api/loja-acessorios/produtos (catálogo, pedidos, avaliações).
public static class CenariosAcessorios
{
    public const string Base = "/api/loja-acessorios";
    public const string Produtos = Base + "/produtos";

    // Superadmin (gerencia) e visitante (cliente que compra, sem login)
    public static async Task<(ClienteApi Super, ClienteApi Visitante, LojaTeste Loja)> AcessoriosAsync(this ApiFixture api)
    {
        var loja = await api.NovaLojaAsync();
        return (await api.SuperadminAsync(loja), loja.Site, loja);
    }

    public static object CorpoProduto(string nome = "Leitor de código", decimal preco = 100m, int estoque = 10, decimal? promo = null, string categoria = "outro",
        string? imagens = null, decimal? peso = null, bool ativo = true, bool destaque = false, int ordem = 0, string? descricao = null) => new
    {
        nome, descricao, preco, precoPromocional = promo, estoque, categoria, imagensUrls = imagens, pesoKg = peso, ativo, destaque, ordem,
    };

    public static async Task<Guid> ProdutoAcessorioAsync(this ClienteApi super, string? nome = null, decimal preco = 100m, int estoque = 10, decimal? promo = null,
        string categoria = "outro", bool ativo = true, bool destaque = false, int ordem = 0)
    {
        var r = await super.Post(CenariosAcessorios.Produtos, CorpoProduto(nome ?? $"Produto {Guid.NewGuid():N}"[..20], preco, estoque, promo, categoria, null, null, ativo, destaque, ordem));
        r.DeveSerSucesso();
        return r.Json.GetProperty("id").GetGuid();
    }

    public static object ItensDoPedido(params (Guid Produto, int Quantidade)[] itens) =>
        itens.Select(i => new { produtoId = i.Produto, quantidade = i.Quantidade }).ToArray();

    public static object CorpoPedido(Guid produto, int quantidade = 1, string uf = "MS", string? cpf = "12345678909", string nome = "  Maria Souza  ",
        string email = "  maria@teste.com ") => CorpoPedido([(produto, quantidade)], uf, cpf, nome, email);

    public static object CorpoPedido((Guid Produto, int Quantidade)[] itens, string uf = "MS", string? cpf = "12345678909", string nome = "  Maria Souza  ",
        string email = "  maria@teste.com ") => new
    {
        clienteNome = nome, clienteEmail = email, clienteTelefone = " 67999990000 ", clienteCpfCnpj = cpf,
        cep = " 79000-000 ", endereco = " Rua A ", numero = "10", complemento = "ap 2", bairro = "Centro", cidade = " Campo Grande ", uf,
        itens = ItensDoPedido(itens),
    };

    public static object CorpoPedidoCartao(Guid produto, int quantidade = 1, string uf = "MS", string? cpf = "12345678909", string token = "tok-ok") => new
    {
        clienteNome = "Maria Souza", clienteEmail = "maria@teste.com", clienteTelefone = "67999990000", clienteCpfCnpj = cpf,
        cep = "79000-000", endereco = "Rua A", numero = "10", complemento = (string?)null, bairro = "Centro", cidade = "Campo Grande", uf,
        itens = ItensDoPedido((produto, quantidade)),
        token, paymentMethodId = "visa", installments = 3, issuerId = "24",
    };

    public static Task<ProdutoAcessorio> ProdutoNoBancoAsync(this ApiFixture api, Guid id) =>
        api.NoBancoAsync(db => db.ProdutosAcessorio.AsNoTracking().FirstAsync(p => p.Id == id));

    public static Task<PedidoAcessorio> PedidoNoBancoAsync(this ApiFixture api, Guid id) =>
        api.NoBancoAsync(db => db.PedidosAcessorio.AsNoTracking().Include(p => p.Itens).FirstAsync(p => p.Id == id));

    // Cria um pedido pela API (Pix) e devolve o id; o pedido fica "aguardando_pagamento" com o estoque reservado.
    public static async Task<Guid> PedidoPixAsync(this ClienteApi visitante, Guid produto, int quantidade = 1, string rota = Produtos + "/pedidos")
    {
        var r = await visitante.Post(rota, CorpoPedido(produto, quantidade));
        r.DeveSerSucesso();
        return r.Json.GetProperty("id").GetGuid();
    }

    // Marca o pedido como vencido (passou do prazo de 30 min sem pagar)
    public static Task VencerPedidoAsync(this ApiFixture api, Guid pedido) =>
        api.NoBancoAsync(db => db.PedidosAcessorio.Where(p => p.Id == pedido).ExecuteUpdateAsync(s => s.SetProperty(p => p.ExpiraEm, DateTime.UtcNow.AddMinutes(-5))));
}
