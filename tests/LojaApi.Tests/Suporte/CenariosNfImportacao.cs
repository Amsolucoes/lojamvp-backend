using System.Text;
using LojaApi.src.Models;
using Microsoft.EntityFrameworkCore;

namespace LojaApi.Tests.Suporte;

// Preparos da importação de nota fiscal (XML da NF-e e lançamento manual).
public static class CenariosNfImportacao
{
    public const string Rota = "/api/nf-importacao";
    public const string CnpjFornecedor = "11222333000181";

    private static int _contador;

    // Chave de acesso de 44 dígitos, diferente a cada chamada
    public static string NovaChave() =>
        DateTime.UtcNow.Ticks.ToString("D19") + Interlocked.Increment(ref _contador).ToString("D10") + Random.Shared.NextInt64(0, 1_000_000_000_000_000L).ToString("D15");     // 19 + 10 + 15 = 44 dígitos

    public record ItemXml(string CodigoProduto, string? Ean, string Descricao, string Quantidade = "1.0000", string ValorUnitario = "10.00", string ValorTotal = "10.00");

    public static string Xml(string chave, string numero = "123", string cnpj = CnpjFornecedor, string fornecedor = "Distribuidora Alfa", params ItemXml[] itens)
    {
        var sb = new StringBuilder();
        sb.Append($"<?xml version=\"1.0\" encoding=\"UTF-8\"?><nfeProc xmlns=\"http://www.portalfiscal.inf.br/nfe\"><NFe><infNFe Id=\"NFe{chave}\">");
        sb.Append($"<ide><nNF>{numero}</nNF></ide><emit><CNPJ>{cnpj}</CNPJ><xNome>{fornecedor}</xNome></emit>");
        var n = 1;
        foreach (var i in itens)
        {
            sb.Append($"<det nItem=\"{n++}\"><prod><cProd>{i.CodigoProduto}</cProd><cEAN>{i.Ean ?? "SEM GTIN"}</cEAN><xProd>{i.Descricao}</xProd>");
            sb.Append($"<qCom>{i.Quantidade}</qCom><vUnCom>{i.ValorUnitario}</vUnCom><vProd>{i.ValorTotal}</vProd></prod></det>");
        }
        sb.Append("</infNFe></NFe></nfeProc>");
        return sb.ToString();
    }

    public static Task<Resposta> PreviewAsync(this LojaTeste loja, string xml) => loja.Painel.PostArquivo($"{Rota}/preview", "arquivo", "nota.xml", xml);

    public static object ItemConfirmacao(string codigo, string nomeBase, decimal quantidade, string acao = "novo", Guid? produtoId = null, decimal? custo = null, decimal? venda = null,
        string? categoria = null, string? cor = null, string? tamanho = null, string? gtin = null) =>
        new { codigoFornecedor = codigo, gtin, nomeBase, cor, tamanho, quantidade, acao, produtoId, precoCusto = custo, precoVenda = venda, categoriaNome = categoria };

    public static Task<Resposta> ConfirmarAsync(this LojaTeste loja, string chave, string numero, params object[] itens) =>
        loja.Painel.Post($"{Rota}/confirmar", new { cnpjFornecedor = CnpjFornecedor, numeroNf = numero, chaveAcesso = chave, nomeFornecedor = "Distribuidora Alfa", itens });

    public static object ItemManual(Guid? produto, decimal quantidade, decimal? custo = null, Guid? variacao = null, string? acao = null, string? nomeBase = null,
        string? categoria = null, string? cor = null, string? tamanho = null, decimal? venda = null, string? gtin = null, string? tipoVenda = null, string? unidade = null) =>
        new { produtoId = produto, variacaoId = variacao, quantidade, precoCusto = custo, acao, nomeBase, categoriaNome = categoria, cor, tamanho, precoVenda = venda, gtin, tipoVenda, unidadeMedida = unidade };

    public static Task<Resposta> LancarManualAsync(this LojaTeste loja, Guid fornecedor, string numero, DateTime? emissao, decimal? valorTotal, params object[] itens) =>
        loja.Painel.Post($"{Rota}/manual", new { fornecedorId = fornecedor, numeroNf = numero, dataEmissao = emissao, valorTotal, itens });

    public static async Task<Guid> NfManualAsync(this LojaTeste loja, Guid fornecedor, string numero, params object[] itens)
    {
        (await loja.LancarManualAsync(fornecedor, numero, null, null, itens)).DeveSerSucesso();
        return (await loja.Painel.Get($"{Rota}/historico")).Json.EnumerateArray().First(n => n.GetProperty("numeroNf").GetString() == numero).GetProperty("id").GetGuid();
    }

    public static Task<NfImportada> NfNoBancoAsync(this ApiFixture api, Guid id) => api.NoBancoAsync(db => db.NfsImportadas.AsNoTracking().FirstAsync(n => n.Id == id));

    public static Task<NfImportada> NfPorNumeroNoBancoAsync(this ApiFixture api, Guid loja, string numero) =>
        api.NoBancoAsync(db => db.NfsImportadas.AsNoTracking().Where(n => n.LojaId == loja && n.NumeroNf == numero).OrderByDescending(n => n.ImportadoEm).FirstAsync());

    public static Task<List<NfProdutoMapeamento>> MapeamentosNoBancoAsync(this ApiFixture api, Guid loja) =>
        api.NoBancoAsync(db => db.NfProdutoMapeamentos.AsNoTracking().Where(m => m.LojaId == loja).ToListAsync());

    public static Task MapearAsync(this ApiFixture api, Guid loja, string cnpj, string codigo, Guid produto) => api.NoBancoAsync(async db =>
    {
        db.NfProdutoMapeamentos.Add(new NfProdutoMapeamento { LojaId = loja, CnpjFornecedor = cnpj, CodigoFornecedor = codigo, ProdutoId = produto });
        await db.SaveChangesAsync();
    });

    public static Task<List<LojaApi.Models.CategoriaLoja>> CategoriasNoBancoAsync(this ApiFixture api, Guid loja) =>
        api.NoBancoAsync(db => db.CategoriasLoja.AsNoTracking().Where(c => c.LojaId == loja).OrderBy(c => c.Ordem).ToListAsync());

    public static Task<List<LojaApi.Models.Produto>> ProdutosPorNomeNoBancoAsync(this ApiFixture api, Guid loja, string nome) =>
        api.NoBancoAsync(db => db.Produtos.AsNoTracking().Where(p => p.LojaId == loja && p.Nome == nome).ToListAsync());

    public static Task<List<LojaApi.Models.ProdutoVariacao>> VariacoesDoProdutoNoBancoAsync(this ApiFixture api, Guid produto) =>
        api.NoBancoAsync(db => db.ProdutoVariacoes.AsNoTracking().Where(v => v.ProdutoId == produto).ToListAsync());

    public static Task<List<LojaApi.Models.MovimentoEstoque>> MovimentosDaLojaNoBancoAsync(this ApiFixture api, Guid loja) =>
        api.NoBancoAsync(db => db.Movimentos.AsNoTracking().Where(m => m.LojaId == loja).OrderBy(m => m.CriadoEm).ToListAsync());
}
