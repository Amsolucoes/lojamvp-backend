using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using LojaApi.Data;
using LojaApi.Models;

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

    // ── Extrai "Cor:X;Tamanho:Y" do nome do produto, se existir ────
    private static (string NomeBase, string? Cor, string? Tamanho) ExtrairVariacao(string xProd)
    {
        var match = Regex.Match(xProd, @"^(?<base>.*?)\s*(?<vars>(?:Cor|Tamanho)\s*:.*)$", RegexOptions.IgnoreCase);
        if (!match.Success) return (xProd.Trim(), null, null);

        var nomeBase = match.Groups["base"].Value.Trim().TrimEnd(':', ';', '-', ',');
        var varsPart = match.Groups["vars"].Value;

        string? cor = null, tamanho = null;
        foreach (var par in varsPart.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var kv = par.Split(':', 2);
            if (kv.Length != 2) continue;
            var chave = kv[0].Trim().ToLowerInvariant();
            var valor = kv[1].Trim();
            if (chave == "cor") cor = valor;
            else if (chave == "tamanho") tamanho = valor;
        }

        return (nomeBase, cor, tamanho);
    }

    // Primeira palavra do nome-base vira sugestão de categoria (ex: "Blusa de Renda..." -> "Blusa")
    private static string SugerirCategoria(string nomeBase)
    {
        var primeira = nomeBase.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "Outro";
        return char.ToUpper(primeira[0]) + primeira[1..].ToLowerInvariant();
    }

    public record ItemNfPreview(
        string CodigoFornecedor, string? Gtin, string Descricao,
        string NomeBase, string? Cor, string? Tamanho,
        decimal Quantidade, decimal ValorUnitario, decimal ValorTotal,
        string StatusMatch, // "gtin" | "mapeamento" | "nome_exato" | "sugestao" | "novo"
        Guid? ProdutoSugeridoId, string? ProdutoSugeridoNome,
        bool VariacaoJaExiste, int? EstoqueVariacaoAtual,
        string CategoriaSugerida, bool CategoriaJaExiste
    );

    public record NfPreviewResponse(string CnpjFornecedor, string NomeFornecedor, string NumeroNf, List<ItemNfPreview> Itens);

    [HttpPost("preview")]
    [Authorize(Roles = "admin,superadmin")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Preview([FromForm] IFormFile arquivo)
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

        var produtosDaLoja = await db.Produtos
            .Include(p => p.Variacoes)
            .Where(p => p.LojaId == lojaId && p.Ativo)
            .ToListAsync();

        var categoriasDaLoja = await db.CategoriasLoja
            .Where(c => c.LojaId == lojaId && c.Ativo)
            .ToListAsync();

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

            var (nomeBase, cor, tamanho) = ExtrairVariacao(xProd);

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

            // 3. Match exato por nome-base (ex: já existe produto "Blusa de Renda Manga Longa Cloe")
            if (match is null)
            {
                match = produtosDaLoja.FirstOrDefault(p => p.Nome.Equals(nomeBase, StringComparison.OrdinalIgnoreCase));
                if (match != null) status = "nome_exato";
            }

            // 4. Sugestão por nome aproximado (sempre precisa confirmação manual)
            if (match is null)
            {
                var candidato = MelhorCandidatoPorNome(nomeBase, produtosDaLoja);
                if (candidato != null)
                {
                    match = candidato;
                    status = "sugestao";
                }
            }

            bool variacaoJaExiste = false;
            int? estoqueVariacaoAtual = null;
            if (match != null && (cor != null || tamanho != null))
            {
                var variacao = match.Variacoes.FirstOrDefault(v => v.Ativo
                    && string.Equals(v.Cor ?? "", cor ?? "", StringComparison.OrdinalIgnoreCase)
                    && string.Equals(v.Tamanho ?? "", tamanho ?? "", StringComparison.OrdinalIgnoreCase));
                if (variacao != null)
                {
                    variacaoJaExiste = true;
                    estoqueVariacaoAtual = variacao.Estoque;
                }
            }

            var categoriaSugerida = match?.Categoria ?? SugerirCategoria(nomeBase);
            var categoriaJaExiste = categoriasDaLoja.Any(c => c.Nome.Equals(categoriaSugerida, StringComparison.OrdinalIgnoreCase));

            itens.Add(new ItemNfPreview(
                cProd, gtin, xProd, nomeBase, cor, tamanho,
                qCom, vUnCom, vProd,
                status, match?.Id, match?.Nome,
                variacaoJaExiste, estoqueVariacaoAtual,
                categoriaSugerida, categoriaJaExiste
            ));
        }

        return Ok(new NfPreviewResponse(cnpjFornecedor, nomeFornecedor, numeroNf, itens));
    }

    // Similaridade simples por palavras em comum — suficiente pra sugestão, não pra match automático
    private static Produto? MelhorCandidatoPorNome(string nomeBase, List<Produto> produtos)
    {
        var palavrasNf = nomeBase.ToLowerInvariant()
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
            if (score > melhorScore && score >= 2)
            {
                melhorScore = score;
                melhor = p;
            }
        }

        return melhor;
    }

    // ── Confirmação da importação ────────────────────────────────────
    public record ItemConfirmacao(
        string CodigoFornecedor, string? Gtin,
        string NomeBase, string? Cor, string? Tamanho,
        decimal Quantidade, decimal ValorUnitario,
        string Acao, // "existente" | "novo"
        Guid? ProdutoId, // obrigatório se Acao == "existente"
        decimal? PrecoVenda, // obrigatório se Acao == "novo"
        string? CategoriaNome // obrigatório se Acao == "novo"
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
        var categoriasCriadas = 0;

        foreach (var item in req.Itens)
        {
            Guid produtoId;
            var temVariacao = item.Cor != null || item.Tamanho != null;

            if (item.Acao == "novo")
            {
                var nomeCategoria = string.IsNullOrWhiteSpace(item.CategoriaNome) ? "Outro" : item.CategoriaNome!.Trim();

                var categoria = await db.CategoriasLoja
                    .FirstOrDefaultAsync(c => c.LojaId == lojaId && c.Ativo && c.Nome.ToLower() == nomeCategoria.ToLower());

                if (categoria is null)
                {
                    var maxOrdem = await db.CategoriasLoja.Where(c => c.LojaId == lojaId).Select(c => (int?)c.Ordem).MaxAsync() ?? -1;
                    categoria = new CategoriaLoja
                    {
                        LojaId = lojaId.Value,
                        Nome = nomeCategoria,
                        Ordem = maxOrdem + 1,
                        UsaTamanho = item.Tamanho != null,
                        UsaCor = item.Cor != null,
                    };
                    db.CategoriasLoja.Add(categoria);
                    categoriasCriadas++;
                }

                var novoProduto = new Produto
                {
                    Nome = item.NomeBase,
                    Categoria = categoria.Nome,
                    PrecoCusto = item.ValorUnitario,
                    PrecoVenda = item.PrecoVenda ?? item.ValorUnitario,
                    Estoque = temVariacao ? 0 : item.Quantidade,
                    CodigoBarras = string.IsNullOrWhiteSpace(item.Gtin) ? null : item.Gtin,
                    LojaId = lojaId,
                };
                db.Produtos.Add(novoProduto);
                await db.SaveChangesAsync(); // precisa do Id antes de criar variação

                if (temVariacao)
                {
                    db.ProdutoVariacoes.Add(new ProdutoVariacao
                    {
                        ProdutoId = novoProduto.Id,
                        Cor = item.Cor,
                        Tamanho = item.Tamanho,
                        Estoque = (int)item.Quantidade,
                    });
                }

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
                var produto = await db.Produtos.Include(p => p.Variacoes).FirstOrDefaultAsync(p => p.Id == item.ProdutoId.Value);
                if (produto is null || produto.LojaId != lojaId) continue;

                if (temVariacao)
                {
                    var variacao = produto.Variacoes.FirstOrDefault(v => v.Ativo
                        && string.Equals(v.Cor ?? "", item.Cor ?? "", StringComparison.OrdinalIgnoreCase)
                        && string.Equals(v.Tamanho ?? "", item.Tamanho ?? "", StringComparison.OrdinalIgnoreCase));

                    if (variacao != null)
                    {
                        variacao.Estoque += (int)item.Quantidade;
                        variacao.AtualizadoEm = DateTime.UtcNow;
                    }
                    else
                    {
                        db.ProdutoVariacoes.Add(new ProdutoVariacao
                        {
                            ProdutoId = produto.Id,
                            Cor = item.Cor,
                            Tamanho = item.Tamanho,
                            Estoque = (int)item.Quantidade,
                        });
                    }
                }
                else
                {
                    produto.Estoque += item.Quantidade;
                }

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

        return Ok(new
        {
            mensagem = "Importação concluída.",
            produtosNovos = criados,
            produtosAtualizados = atualizados,
            categoriasCriadas,
        });
    }
}