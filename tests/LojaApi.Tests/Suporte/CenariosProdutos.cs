using LojaApi.Models;
using Microsoft.EntityFrameworkCore;

namespace LojaApi.Tests.Suporte;

// Preparos do módulo de Produtos (cadastro, variações, estoque, categorias, marcas, fornecedores e etiquetas).
public static class CenariosProdutos
{
    public const string Rota = "/api/produtos";

    public static object CorpoProduto(string nome = "Camiseta", string categoria = "outro", decimal custo = 20m, decimal venda = 50m, decimal estoque = 0m,
        decimal minimo = 3m, string? codigo = null, bool ativo = true, string tipo = "unidade", string unidade = "un", Guid? marca = null, Guid? fornecedor = null,
        string? descricao = null) => new
    {
        nome, descricao, categoria, precoCusto = custo, precoVenda = venda, estoque, estoqueMinimo = minimo, codigoBarras = codigo, ativo,
        tipoVenda = tipo, unidadeMedida = unidade, marcaId = marca, fornecedorId = fornecedor,
    };

    public static async Task<Guid> ProdutoPelaApiAsync(this LojaTeste loja, string nome = "Camiseta", decimal estoque = 0m, string? codigo = null,
        string categoria = "outro", decimal custo = 20m, decimal venda = 50m, bool ativo = true, Guid? marca = null, Guid? fornecedor = null)
    {
        var r = await loja.Painel.Post(Rota, CorpoProduto(nome, categoria, custo, venda, estoque, 3m, codigo, ativo, marca: marca, fornecedor: fornecedor));
        r.DeveSerSucesso();
        return r.Json.GetProperty("id").GetGuid();
    }

    public static object CorpoVariacao(string? tamanho = "M", string? cor = "Azul", string? outro = null, string? codigo = null, int estoque = 0, int minimo = 1) =>
        new { tamanho, cor, outroCampo = outro, codigoBarras = codigo, estoque, estoqueMinimo = minimo };

    public static async Task<Guid> VariacaoPelaApiAsync(this LojaTeste loja, Guid produto, string? tamanho = "M", string? cor = "Azul", int estoque = 0, string? codigo = null)
    {
        var r = await loja.Painel.Post($"{Rota}/{produto}/variacoes", CorpoVariacao(tamanho, cor, null, codigo, estoque));
        r.DeveSerSucesso();
        return r.Json.GetProperty("id").GetGuid();
    }

    public static async Task<Guid> MarcaIdAsync(this LojaTeste loja, string nome = "Bosch")
    {
        var r = await loja.Painel.Post("/api/marcas", new { nome });
        r.DeveSerSucesso();
        return r.Json.GetProperty("id").GetGuid();
    }

    public static async Task<Guid> FornecedorIdAsync(this LojaTeste loja, string nome = "Distribuidora Alfa", bool ativo = true)
    {
        var r = await loja.Painel.Post("/api/fornecedores", CorpoFornecedor(nome, ativo: ativo));
        r.DeveSerSucesso();
        return r.Json.GetProperty("id").GetGuid();
    }

    public static object CorpoFornecedor(string nome = "Distribuidora Alfa", string? cnpj = null, string? telefone = null, string? email = null, string? endereco = null,
        string? observacoes = null, bool ativo = true, string? cep = null, string? rua = null, string? numero = null, string? bairro = null, string? cidade = null, string? uf = null) =>
        new { nome, cnpjCpf = cnpj, telefone, email, endereco, observacoes, ativo, cep, rua, numero, bairro, cidade, uf };

    public static Task<ProdutoVariacao> VariacaoNoBancoDoProdutoAsync(this ApiFixture api, Guid id) =>
        api.NoBancoAsync(db => db.ProdutoVariacoes.AsNoTracking().FirstAsync(v => v.Id == id));

    public static Task<List<MovimentoEstoque>> MovimentosDoProdutoNoBancoAsync(this ApiFixture api, Guid produto) =>
        api.NoBancoAsync(db => db.Movimentos.AsNoTracking().Where(m => m.ProdutoId == produto).OrderBy(m => m.CriadoEm).ToListAsync());
}
