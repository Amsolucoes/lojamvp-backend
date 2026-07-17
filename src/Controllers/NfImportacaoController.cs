using LojaApi.Data;
using LojaApi.Models;
using LojaApi.src.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Xml.Linq;

namespace LojaApi.src.Controllers;

[ApiController]
[Route("api/nf-importacao")]
[Authorize]
public class NfImportacaoController(AppDbContext db) : ControllerBase
{
    private Guid UsuarioId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private async Task<Guid?> GetLojaId()
    {
        var vinculo = await db.UsuariosLoja.FirstOrDefaultAsync(ul => ul.UsuarioId == UsuarioId && ul.Ativo);
        return vinculo?.LojaId;
    }

    private static readonly XNamespace Ns = "http://www.portalfiscal.inf.br/nfe";

    public record ItemNfPreview(
        string CodigoFornecedor, string? Gtin, string Descricao,
        decimal Quantidade, decimal ValorUnitario, decimal ValorTotal,
        string StatusMatch, // "gtin" | "mapeamento" | "sugestao" | "novo"
        Guid? ProdutoSugeridoId, string? ProdutoSugeridoNome, decimal? ProdutoSugeridoEstoqueAtual
    );

    public record NfPreviewResponse(string CnpjFornecedor, string NomeFornecedor, string NumeroNf, List<ItemNfPreview> Itens);

    [HttpPost("preview")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> Preview(IFormFile arquivo)
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return BadRequest(new { erro = "Loja não encontrada." });

        if (arquivo is null || arquivo.Length == 0)
            return BadRequest(new { erro = "Envie o arquivo XML da nota fiscal." });

        XDocument doc;
        try
        {
            using var stream = arquivo.OpenReadStream();
            doc = await XDocument.LoadAsync(stream, LoadOptions.None, HttpContext.RequestAborted);
        }
        catch
        {
            return BadRequest(new { erro = "Arquivo XML inválido." });
        }

        var infNFe = doc.Descendants(Ns + "infNFe").FirstOrDefault();
        if (infNFe is null) return BadRequest(new { erro = "XML não parece ser uma NF-e válida." });

        var emit = infNFe.Element(Ns + "emit");
        var cnpjFornecedor = emit?.Element(Ns + "CNPJ")?.Value ?? "";
        var nomeFornecedor = emit?.Element(Ns + "xNome")?.Value ?? "Fornecedor";
        var numeroNf = infNFe.Element(Ns + "ide")?.Element(Ns + "nNF")?.Value ?? "";

        var produtosDaLoja = await db.Produtos.Where(p => p.LojaId == lojaId && p.Ativo).ToListAsync();
        var mapeamentos = await db.NfProdutoMapeamentos
            .Where(m => m.LojaId == lojaId && m.CnpjFornecedor == cnpjFornecedor)
            .ToListAsync();

        var itens = new List<ItemNfPreview>();

        foreach (var det in infNFe.Elements(Ns + "det"))
        {
            var prod = det.Element(Ns + "prod");
            if (prod is null) continue;

            var cProd = prod.Element(Ns + "cProd")?.Value ?? "";
            var cEan = prod.Element(Ns + "cEAN")?.Value ?? "";
            var gtin = (string.IsNullOrWhiteSpace(cEan) || cEan == "SEM GTIN") ? null : cEan;
            var xProd = prod.Element(Ns + "xProd")?.Value ?? "";
            var qCom = decimal.Parse(prod.Element(Ns + "qCom")?.Value ?? "0", System.Globalization.CultureInfo.InvariantCulture);
            var vUnCom = decimal.Parse(prod.Element(Ns + "vUnCom")?.Value ?? "0", System.Globalization.CultureInfo.InvariantCulture);
            var vProd = decimal.Parse(prod.Element(Ns + "vProd")?.Value ?? "0", System.Globalization.CultureInfo.InvariantCulture);

            Produto? match = null;
            string status = "novo";

            // 1. Match por GTIN
            if (gtin != null)
            {
                match = produtosDaLoja.FirstOrDefault(p => p.CodigoBarras == gtin);
                if (match != null) status = "gtin";
            }

            // 2. Match por mapeamento salvo (código do fornecedor já visto antes)
            if (match is null)
            {
                var mapeado = mapeamentos.FirstOrDefault(m => m.CodigoFornecedor == cProd);
                if (mapeado != null)
                {
                    match = produtosDaLoja.FirstOrDefault(p => p.Id == mapeado.ProdutoId);
                    if (match != null) status = "mapeamento";
                }
            }

            // 3. Sugestão por nome aproximado (sempre precisa confirmação manual)
            if (match is null)
            {
                var candidato = MelhorCandidatoPorNome(xProd, produtosDaLoja);
                if (candidato != null)
                {
                    match = candidato;
                    status = "sugestao";
                }
            }

            itens.Add(new ItemNfPreview(
                cProd, gtin, xProd, qCom, vUnCom, vProd,
                status, match?.Id, match?.Nome, match?.Estoque
            ));
        }

        return Ok(new NfPreviewResponse(cnpjFornecedor, nomeFornecedor, numeroNf, itens));
    }

    // Similaridade simples por palavras em comum — suficiente pra sugestão, não pra match automático
    private static Produto? MelhorCandidatoPorNome(string nomeNf, List<Produto> produtos)
    {
        var palavrasNf = nomeNf.ToLowerInvariant()
            .Split(new[] { ' ', ':', ';', ',', '-' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(p => p.Length > 2)
            .ToHashSet();

        if (palavrasNf.Count == 0) return null;

        Produto? melhor = null;
        int melhorScore = 0;

        foreach (var p in produtos)
        {
            var palavrasProd = p.Nome.ToLowerInvariant()
                .Split(new[] { ' ', ':', ';', ',', '-' }, StringSplitOptions.RemoveEmptyEntries)
                .ToHashSet();

            var score = palavrasNf.Intersect(palavrasProd).Count();
            if (score > melhorScore && score >= 2) // exige pelo menos 2 palavras em comum
            {
                melhorScore = score;
                melhor = p;
            }
        }

        return melhor;
    }

    // ── Confirmação da importação ────────────────────────────────────
    public record ItemConfirmacao(
        string CodigoFornecedor, string? Gtin, string Descricao,
        decimal Quantidade, decimal ValorUnitario,
        string Acao, // "existente" | "novo"
        Guid? ProdutoId, // obrigatório se Acao == "existente"
        decimal? PrecoVenda // obrigatório se Acao == "novo" (preço de custo vem de ValorUnitario)
    );

    public record ConfirmarImportacaoRequest(string CnpjFornecedor, string NumeroNf, List<ItemConfirmacao> Itens);

    [HttpPost("confirmar")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> Confirmar([FromBody] ConfirmarImportacaoRequest req)
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return BadRequest(new { erro = "Loja não encontrada." });

        var criados = 0;
        var atualizados = 0;

        foreach (var item in req.Itens)
        {
            Guid produtoId;

            if (item.Acao == "novo")
            {
                var novoProduto = new Produto
                {
                    Nome = item.Descricao,
                    Categoria = "outro",
                    PrecoCusto = item.ValorUnitario,
                    PrecoVenda = item.PrecoVenda ?? item.ValorUnitario,
                    Estoque = item.Quantidade,
                    CodigoBarras = string.IsNullOrWhiteSpace(item.Gtin) ? null : item.Gtin,
                    LojaId = lojaId,
                };
                db.Produtos.Add(novoProduto);
                produtoId = novoProduto.Id;
                criados++;

                db.Movimentos.Add(new MovimentoEstoque
                {
                    ProdutoId = produtoId,
                    Tipo = "entrada",
                    Quantidade = item.Quantidade,
                    Observacao = $"Importação NF {req.NumeroNf}",
                    LojaId = lojaId,
                });
            }
            else
            {
                if (item.ProdutoId is null) continue;
                var produto = await db.Produtos.FindAsync(item.ProdutoId.Value);
                if (produto is null || produto.LojaId != lojaId) continue;

                produto.Estoque += item.Quantidade;
                produto.AtualizadoEm = DateTime.UtcNow;
                produtoId = produto.Id;
                atualizados++;

                db.Movimentos.Add(new MovimentoEstoque
                {
                    ProdutoId = produtoId,
                    Tipo = "entrada",
                    Quantidade = item.Quantidade,
                    Observacao = $"Importação NF {req.NumeroNf}",
                    LojaId = lojaId,
                });
            }

            // Salva/atualiza o mapeamento código-do-fornecedor -> produto, pra próxima nota casar direto
            var mapeamentoExistente = await db.NfProdutoMapeamentos.FirstOrDefaultAsync(m =>
                m.LojaId == lojaId && m.CnpjFornecedor == req.CnpjFornecedor && m.CodigoFornecedor == item.CodigoFornecedor);

            if (mapeamentoExistente is null)
            {
                db.NfProdutoMapeamentos.Add(new NfProdutoMapeamento
                {
                    LojaId = lojaId.Value,
                    CnpjFornecedor = req.CnpjFornecedor,
                    CodigoFornecedor = item.CodigoFornecedor,
                    ProdutoId = produtoId,
                });
            }
            else if (mapeamentoExistente.ProdutoId != produtoId)
            {
                mapeamentoExistente.ProdutoId = produtoId;
            }
        }

        await db.SaveChangesAsync();

        return Ok(new { mensagem = "Importação concluída.", produtosNovos = criados, produtosAtualizados = atualizados });
    }
}