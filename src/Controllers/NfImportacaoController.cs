using LojaApi.Data;
using LojaApi.Models;
using LojaApi.src.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.RegularExpressions;
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

        // A chave de acesso vem no atributo Id="NFe<44 dígitos>" do infNFe
        var idAttr = infNFe.Attribute("Id")?.Value ?? "";
        var chaveAcesso = idAttr.StartsWith("NFe") ? idAttr[3..] : idAttr;

        if (string.IsNullOrWhiteSpace(chaveAcesso) || chaveAcesso.Length != 44)
            return BadRequest(new { erro = "Não foi possível identificar a chave de acesso da nota." });

        var jaImportada = await db.NfsImportadas.FirstOrDefaultAsync(n => n.LojaId == lojaId && n.ChaveAcesso == chaveAcesso && !n.Desfeita);
        if (jaImportada != null)
            return Conflict(new
            {
                erro = $"Esta nota já foi importada em {jaImportada.ImportadoEm:dd/MM/yyyy HH:mm} ({jaImportada.QtdItens} item(ns))."
            });

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

        return Ok(new NfPreviewResponse(cnpjFornecedor, nomeFornecedor, numeroNf, chaveAcesso, itens));
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
    [HttpPost("confirmar")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> Confirmar([FromBody] ConfirmarImportacaoRequest req)
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return BadRequest(new { erro = "Loja não encontrada." });

        // Trava definitiva contra reenvio da mesma nota, mesmo se o preview foi burlado
        var jaImportada = await db.NfsImportadas.AnyAsync(n => n.LojaId == lojaId && n.ChaveAcesso == req.ChaveAcesso && !n.Desfeita);
        if (jaImportada)
            return Conflict(new { erro = "Esta nota já foi importada anteriormente." });

        var criados = 0;
        var atualizados = 0;
        var categoriasCriadas = 0;
        var detalhesParaDesfazer = new List<ItemImportadoDetalhe>();
        decimal custoTotal = 0, vendaTotal = 0, quantidadeTotal = 0;

        foreach (var item in req.Itens)
        {
            Guid produtoId;
            var temVariacao = item.Cor != null || item.Tamanho != null;
            bool produtoCriado = false, variacaoCriada = false, categoriaCriadaAgora = false;
            Guid? categoriaId = null;
            Guid? variacaoId = null;
            decimal custoUnit, vendaUnit;

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
                    categoriaCriadaAgora = true;
                }
                categoriaId = categoria.Id;

                custoUnit = item.PrecoCusto ?? 0;
                vendaUnit = item.PrecoVenda ?? item.PrecoCusto ?? 0;

                var novoProduto = new Produto
                {
                    Nome = item.NomeBase,
                    Categoria = categoria.Nome,
                    PrecoCusto = custoUnit,
                    PrecoVenda = vendaUnit,
                    Estoque = temVariacao ? 0 : item.Quantidade,
                    CodigoBarras = string.IsNullOrWhiteSpace(item.Gtin) ? null : item.Gtin,
                    LojaId = lojaId,
                };
                db.Produtos.Add(novoProduto);
                await db.SaveChangesAsync(); // precisa do Id antes de criar variação

                produtoCriado = true;

                if (temVariacao)
                {
                    var novaVariacao = new ProdutoVariacao
                    {
                        ProdutoId = novoProduto.Id,
                        Cor = item.Cor,
                        Tamanho = item.Tamanho,
                        Estoque = (int)item.Quantidade,
                    };
                    db.ProdutoVariacoes.Add(novaVariacao);
                    await db.SaveChangesAsync();
                    variacaoId = novaVariacao.Id;
                    variacaoCriada = true;
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
                        variacaoId = variacao.Id;
                    }
                    else
                    {
                        var novaVariacao = new ProdutoVariacao
                        {
                            ProdutoId = produto.Id,
                            Cor = item.Cor,
                            Tamanho = item.Tamanho,
                            Estoque = (int)item.Quantidade,
                        };
                        db.ProdutoVariacoes.Add(novaVariacao);
                        await db.SaveChangesAsync();
                        variacaoId = novaVariacao.Id;
                        variacaoCriada = true;
                    }
                }
                else
                {
                    produto.Estoque += item.Quantidade;
                }

                produto.AtualizadoEm = DateTime.UtcNow;
                produtoId = produto.Id;
                atualizados++;
                custoUnit = produto.PrecoCusto;
                vendaUnit = produto.PrecoVenda;

                db.Movimentos.Add(new MovimentoEstoque
                {
                    ProdutoId = produtoId,
                    Tipo = "entrada",
                    Quantidade = item.Quantidade,
                    Observacao = $"Importação NF {req.NumeroNf}",
                    LojaId = lojaId,
                });
            }

            custoTotal += custoUnit * item.Quantidade;
            vendaTotal += vendaUnit * item.Quantidade;
            quantidadeTotal += item.Quantidade;

            detalhesParaDesfazer.Add(new ItemImportadoDetalhe(
                produtoId, variacaoId, item.Quantidade,
                produtoCriado, variacaoCriada, categoriaCriadaAgora, categoriaId
            ));

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

        db.NfsImportadas.Add(new NfImportada
        {
            LojaId = lojaId.Value,
            ChaveAcesso = req.ChaveAcesso,
            NumeroNf = req.NumeroNf,
            NomeFornecedor = req.NomeFornecedor,
            ValorTotal = custoTotal,
            ValorCustoTotal = custoTotal,
            ValorVendaTotal = vendaTotal,
            QuantidadeTotal = quantidadeTotal,
            QtdItens = req.Itens.Count,
            ItensJson = System.Text.Json.JsonSerializer.Serialize(detalhesParaDesfazer),
        });

        await db.SaveChangesAsync();

        return Ok(new
        {
            mensagem = "Importação concluída.",
            produtosNovos = criados,
            produtosAtualizados = atualizados,
            categoriasCriadas,
        });
    }

    // ── Cria um produto novo (e categoria, se preciso) a partir de um item de NF manual ──
    private async Task<(Guid ProdutoId, Guid? VariacaoId, bool CategoriaCriada, Guid? CategoriaId)> CriarProdutoNovo(
        Guid lojaId, string nomeBase, string? categoriaNome, string? cor, string? tamanho,
        decimal precoCusto, decimal precoVenda, decimal quantidade, string? gtin,
        string? tipoVenda = null, string? unidadeMedida = null)
    {
        var nomeCategoria = string.IsNullOrWhiteSpace(categoriaNome) ? "Outro" : categoriaNome.Trim();

        var categoria = await db.CategoriasLoja
            .FirstOrDefaultAsync(c => c.LojaId == lojaId && c.Ativo && c.Nome.ToLower() == nomeCategoria.ToLower());

        bool categoriaCriadaAgora = false;
        if (categoria is null)
        {
            var maxOrdem = await db.CategoriasLoja.Where(c => c.LojaId == lojaId).Select(c => (int?)c.Ordem).MaxAsync() ?? -1;
            categoria = new CategoriaLoja
            {
                LojaId = lojaId,
                Nome = nomeCategoria,
                Ordem = maxOrdem + 1,
                UsaTamanho = tamanho != null,
                UsaCor = cor != null,
            };
            db.CategoriasLoja.Add(categoria);
            categoriaCriadaAgora = true;
        }

        var temVariacao = cor != null || tamanho != null;
        var tipoVendaFinal = tipoVenda == "fracionado" ? "fracionado" : "unidade";
        var novoProduto = new Produto
        {
            Nome = nomeBase,
            Categoria = categoria.Nome,
            PrecoCusto = precoCusto,
            PrecoVenda = precoVenda,
            Estoque = temVariacao ? 0 : quantidade,
            CodigoBarras = string.IsNullOrWhiteSpace(gtin) ? null : gtin,
            LojaId = lojaId,
            TipoVenda = tipoVendaFinal,
            UnidadeMedida = tipoVendaFinal == "fracionado" && !string.IsNullOrWhiteSpace(unidadeMedida) ? unidadeMedida : (tipoVendaFinal == "fracionado" ? "kg" : "un"),
        };
        db.Produtos.Add(novoProduto);
        await db.SaveChangesAsync(); // precisa do Id antes de criar variação

        Guid? variacaoId = null;
        if (temVariacao)
        {
            var novaVariacao = new ProdutoVariacao
            {
                ProdutoId = novoProduto.Id,
                Cor = cor,
                Tamanho = tamanho,
                Estoque = (int)quantidade,
            };
            db.ProdutoVariacoes.Add(novaVariacao);
            await db.SaveChangesAsync();
            variacaoId = novaVariacao.Id;
        }

        return (novoProduto.Id, variacaoId, categoriaCriadaAgora, categoria.Id);
    }

    // ── Aplica uma lista de itens de NF manual (existentes ou novos), gerando os
    // movimentos de estoque e o detalhe usado depois pro Desfazer/Editar ──────
    private async Task<(List<ItemImportadoDetalhe>? Detalhes, decimal CustoTotal, decimal VendaTotal, decimal QuantidadeTotal, string? Erro)> AplicarItensManual(
        Guid lojaId, string numeroNf, List<ItemNfManualRequest> itensReq)
    {
        var detalhes = new List<ItemImportadoDetalhe>();
        decimal custoTotal = 0, vendaTotal = 0, quantidadeTotal = 0;

        foreach (var item in itensReq)
        {
            Guid produtoId;
            Guid? variacaoId = null;
            bool produtoCriado = false, variacaoCriada = false, categoriaCriadaAgora = false;
            Guid? categoriaId = null;
            decimal custoUnit, vendaUnit;

            if (item.Acao == "novo")
            {
                if (string.IsNullOrWhiteSpace(item.NomeBase))
                    return (null, 0, 0, 0, "Informe o nome do novo produto.");

                custoUnit = item.PrecoCusto ?? 0;
                vendaUnit = item.PrecoVenda ?? item.PrecoCusto ?? 0;

                var (novoProdutoId, novaVariacaoId, catCriada, catId) = await CriarProdutoNovo(
                    lojaId, item.NomeBase!.Trim(), item.CategoriaNome, item.Cor, item.Tamanho,
                    custoUnit, vendaUnit, item.Quantidade, item.Gtin,
                    item.TipoVenda, item.UnidadeMedida);

                produtoId = novoProdutoId;
                variacaoId = novaVariacaoId;
                produtoCriado = true;
                variacaoCriada = novaVariacaoId.HasValue;
                categoriaCriadaAgora = catCriada;
                categoriaId = catId;
            }
            else
            {
                if (item.ProdutoId is null) return (null, 0, 0, 0, "Produto não informado.");
                var produto = await db.Produtos.Include(p => p.Variacoes).FirstOrDefaultAsync(p => p.Id == item.ProdutoId.Value);
                if (produto is null || produto.LojaId != lojaId) return (null, 0, 0, 0, "Produto não encontrado.");

                if (item.VariacaoId.HasValue)
                {
                    var variacao = produto.Variacoes.FirstOrDefault(v => v.Id == item.VariacaoId.Value);
                    if (variacao is null) return (null, 0, 0, 0, $"Variação não encontrada para '{produto.Nome}'.");
                    variacao.Estoque += (int)item.Quantidade;
                    variacao.AtualizadoEm = DateTime.UtcNow;
                    variacaoId = variacao.Id;
                }
                else
                {
                    produto.Estoque += item.Quantidade;
                }

                if (item.PrecoCusto.HasValue) produto.PrecoCusto = item.PrecoCusto.Value;
                produto.AtualizadoEm = DateTime.UtcNow;
                produtoId = produto.Id;
                custoUnit = produto.PrecoCusto;
                vendaUnit = produto.PrecoVenda;
            }

            custoTotal += custoUnit * item.Quantidade;
            vendaTotal += vendaUnit * item.Quantidade;
            quantidadeTotal += item.Quantidade;

            db.Movimentos.Add(new MovimentoEstoque
            {
                ProdutoId = produtoId,
                Tipo = "entrada",
                Quantidade = item.Quantidade,
                Observacao = $"Nota fiscal manual {numeroNf}",
                LojaId = lojaId,
            });

            detalhes.Add(new ItemImportadoDetalhe(
                produtoId, variacaoId, item.Quantidade,
                produtoCriado, variacaoCriada, categoriaCriadaAgora, categoriaId
            ));
        }

        return (detalhes, custoTotal, vendaTotal, quantidadeTotal, null);
    }

    // ── Lançamento manual de nota fiscal (sem XML), vinculado a um Fornecedor ──
    [HttpPost("manual")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> LancarManual([FromBody] LancarNfManualRequest req)
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return BadRequest(new { erro = "Loja não encontrada." });

        if (string.IsNullOrWhiteSpace(req.NumeroNf))
            return BadRequest(new { erro = "Informe o número da nota fiscal." });
        if (req.Itens.Count == 0)
            return BadRequest(new { erro = "Adicione ao menos um item." });

        var fornecedor = await db.Fornecedores.FirstOrDefaultAsync(f => f.Id == req.FornecedorId && f.LojaId == lojaId);
        if (fornecedor is null) return BadRequest(new { erro = "Fornecedor não encontrado." });

        var numeroNf = req.NumeroNf.Trim();

        // Trava contra lançar a mesma NF (número) duas vezes pro mesmo fornecedor
        var duplicada = await db.NfsImportadas.AnyAsync(n =>
            n.LojaId == lojaId && n.FornecedorId == fornecedor.Id && !n.Desfeita && n.NumeroNf.ToLower() == numeroNf.ToLower());
        if (duplicada)
            return Conflict(new { erro = $"Já existe uma nota {numeroNf} lançada para o fornecedor {fornecedor.Nome}." });

        // Postgres exige DateTimeKind.Utc pra gravar em coluna timestamptz — o valor
        // desserializado do JSON vem com Kind=Unspecified e quebra o SaveChanges.
        DateTime? dataEmissaoUtc = req.DataEmissao.HasValue
            ? DateTime.SpecifyKind(req.DataEmissao.Value.Date, DateTimeKind.Utc)
            : null;

        var (detalhes, custoTotal, vendaTotal, quantidadeTotal, erro) = await AplicarItensManual(lojaId.Value, numeroNf, req.Itens);
        if (erro != null) return BadRequest(new { erro });

        db.NfsImportadas.Add(new NfImportada
        {
            LojaId = lojaId.Value,
            ChaveAcesso = null,
            NumeroNf = numeroNf,
            NomeFornecedor = fornecedor.Nome,
            FornecedorId = fornecedor.Id,
            Origem = "manual",
            DataEmissao = dataEmissaoUtc,
            ValorTotal = req.ValorTotal ?? custoTotal,
            ValorCustoTotal = custoTotal,
            ValorVendaTotal = vendaTotal,
            QuantidadeTotal = quantidadeTotal,
            QtdItens = req.Itens.Count,
            ItensJson = System.Text.Json.JsonSerializer.Serialize(detalhes),
        });

        await db.SaveChangesAsync();

        return Ok(new { mensagem = "Nota fiscal lançada.", produtosAtualizados = req.Itens.Count });
    }

    // ── Detalhe de uma NF manual (pra preencher o formulário de edição) ────
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Detalhe(Guid id)
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return NotFound();

        var nf = await db.NfsImportadas.FirstOrDefaultAsync(n => n.Id == id && n.LojaId == lojaId);
        if (nf is null) return NotFound();
        if (nf.Origem != "manual") return BadRequest(new { erro = "Apenas notas lançadas manualmente têm detalhe editável." });

        var itensDetalhe = System.Text.Json.JsonSerializer.Deserialize<List<ItemImportadoDetalhe>>(nf.ItensJson) ?? new();
        var itens = new List<object>();

        foreach (var item in itensDetalhe)
        {
            var produto = await db.Produtos.Include(p => p.Variacoes).FirstOrDefaultAsync(p => p.Id == item.ProdutoId);
            if (produto is null) continue; // produto pode ter sido excluído depois (ex: por outro Desfazer)

            string? variacaoLabel = null;
            if (item.VariacaoId.HasValue)
            {
                var variacao = produto.Variacoes.FirstOrDefault(v => v.Id == item.VariacaoId.Value);
                if (variacao != null)
                    variacaoLabel = string.Join(" / ", new[] { variacao.Cor, variacao.Tamanho }.Where(s => !string.IsNullOrWhiteSpace(s)));
            }

            itens.Add(new
            {
                produtoId = item.ProdutoId,
                nomeProduto = produto.Nome,
                variacaoId = item.VariacaoId,
                variacaoLabel,
                quantidade = item.Quantidade,
                precoCusto = produto.PrecoCusto,
            });
        }

        return Ok(new
        {
            nf.Id,
            nf.FornecedorId,
            nf.NumeroNf,
            nf.DataEmissao,
            nf.ValorTotal,
            Itens = itens,
        });
    }

    // ── Edita uma NF manual já lançada (número, fornecedor, data, valor e
    // quantidade/custo dos itens — não permite adicionar/remover itens) ────
    [HttpPut("{id:guid}/editar")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> Editar(Guid id, [FromBody] EditarNfManualRequest req)
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return BadRequest(new { erro = "Loja não encontrada." });

        var nf = await db.NfsImportadas.FirstOrDefaultAsync(n => n.Id == id && n.LojaId == lojaId);
        if (nf is null) return NotFound();
        if (nf.Desfeita) return BadRequest(new { erro = "Esta importação foi desfeita e não pode ser editada." });
        if (nf.Origem != "manual") return BadRequest(new { erro = "Apenas notas lançadas manualmente podem ser editadas." });
        if (string.IsNullOrWhiteSpace(req.NumeroNf)) return BadRequest(new { erro = "Informe o número da nota fiscal." });

        var fornecedor = await db.Fornecedores.FirstOrDefaultAsync(f => f.Id == req.FornecedorId && f.LojaId == lojaId);
        if (fornecedor is null) return BadRequest(new { erro = "Fornecedor não encontrado." });

        var numeroNfEditado = req.NumeroNf.Trim();

        // Trava contra deixar duas notas com o mesmo número pro mesmo fornecedor
        // (ignora a própria nota sendo editada)
        var duplicada = await db.NfsImportadas.AnyAsync(n =>
            n.LojaId == lojaId && n.Id != nf.Id && n.FornecedorId == fornecedor.Id && !n.Desfeita
            && n.NumeroNf.ToLower() == numeroNfEditado.ToLower());
        if (duplicada)
            return Conflict(new { erro = $"Já existe uma nota {numeroNfEditado} lançada para o fornecedor {fornecedor.Nome}." });

        var itensAntigos = System.Text.Json.JsonSerializer.Deserialize<List<ItemImportadoDetalhe>>(nf.ItensJson) ?? new();
        if (itensAntigos.Count != req.Itens.Count)
            return BadRequest(new { erro = "Não é possível adicionar ou remover itens ao editar — desfaça e lance novamente." });

        var tagAntiga = $"Nota fiscal manual {nf.NumeroNf}";
        var tagNova = $"Nota fiscal manual {numeroNfEditado}";

        var detalhesNovos = new List<ItemImportadoDetalhe>();
        decimal custoTotal = 0, vendaTotal = 0, quantidadeTotal = 0;

        foreach (var antigo in itensAntigos)
        {
            var novo = req.Itens.FirstOrDefault(i => i.ProdutoId == antigo.ProdutoId && i.VariacaoId == antigo.VariacaoId);
            if (novo is null)
                return BadRequest(new { erro = "Os itens editados não correspondem aos itens originais desta nota." });

            var produto = await db.Produtos.Include(p => p.Variacoes).FirstOrDefaultAsync(p => p.Id == antigo.ProdutoId);
            if (produto != null)
            {
                var delta = novo.Quantidade - antigo.Quantidade;

                if (antigo.VariacaoId.HasValue)
                {
                    var variacao = produto.Variacoes.FirstOrDefault(v => v.Id == antigo.VariacaoId.Value);
                    if (variacao != null)
                    {
                        variacao.Estoque = Math.Max(0, variacao.Estoque + (int)delta);
                        variacao.AtualizadoEm = DateTime.UtcNow;
                    }
                }
                else
                {
                    produto.Estoque = Math.Max(0, produto.Estoque + delta);
                }

                if (novo.PrecoCusto.HasValue) produto.PrecoCusto = novo.PrecoCusto.Value;
                produto.AtualizadoEm = DateTime.UtcNow;

                custoTotal += produto.PrecoCusto * novo.Quantidade;
                vendaTotal += produto.PrecoVenda * novo.Quantidade;
            }

            quantidadeTotal += novo.Quantidade;
            detalhesNovos.Add(antigo with { Quantidade = novo.Quantidade });
        }

        var movimentos = await db.Movimentos.Where(m => m.LojaId == lojaId && m.Observacao == tagAntiga).ToListAsync();
        foreach (var mov in movimentos)
        {
            var correspondente = req.Itens.FirstOrDefault(i => i.ProdutoId == mov.ProdutoId);
            if (correspondente != null) mov.Quantidade = correspondente.Quantidade;
            mov.Observacao = tagNova;
        }

        nf.FornecedorId = fornecedor.Id;
        nf.NomeFornecedor = fornecedor.Nome;
        nf.NumeroNf = numeroNfEditado;
        nf.DataEmissao = req.DataEmissao.HasValue ? DateTime.SpecifyKind(req.DataEmissao.Value.Date, DateTimeKind.Utc) : null;
        nf.ValorTotal = req.ValorTotal ?? custoTotal;
        nf.ValorCustoTotal = custoTotal;
        nf.ValorVendaTotal = vendaTotal;
        nf.QuantidadeTotal = quantidadeTotal;
        nf.ItensJson = System.Text.Json.JsonSerializer.Serialize(detalhesNovos);

        await db.SaveChangesAsync();

        return Ok(new { mensagem = "Nota fiscal atualizada." });
    }

    // ── Histórico de importações (XML e lançamentos manuais) ───────
    [HttpGet("historico")]
    public async Task<IActionResult> Historico()
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return Ok(Array.Empty<object>());

        var lista = await db.NfsImportadas
            .Where(n => n.LojaId == lojaId)
            .OrderByDescending(n => n.ImportadoEm)
            .Select(n => new
            {
                n.Id,
                n.NumeroNf,
                n.NomeFornecedor,
                n.FornecedorId,
                n.Origem,
                n.DataEmissao,
                n.ValorTotal,
                n.ValorCustoTotal,
                n.ValorVendaTotal,
                n.QuantidadeTotal,
                n.QtdItens,
                n.ImportadoEm,
                n.Desfeita,
            })
            .ToListAsync();

        return Ok(lista);
    }

    // ── Desfazer uma importação ────────────────────────────────────
    [HttpPost("{id:guid}/desfazer")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> Desfazer(Guid id)
    {
        var lojaId = await GetLojaId();
        if (lojaId is null) return BadRequest(new { erro = "Loja não encontrada." });

        var nf = await db.NfsImportadas.FirstOrDefaultAsync(n => n.Id == id && n.LojaId == lojaId);
        if (nf is null) return NotFound();
        if (nf.Desfeita) return BadRequest(new { erro = "Esta importação já foi desfeita." });

        var itens = System.Text.Json.JsonSerializer.Deserialize<List<ItemImportadoDetalhe>>(nf.ItensJson) ?? new();

        var produtosParaExcluir = new List<Guid>();
        var categoriasParaChecar = new HashSet<Guid>();

        foreach (var item in itens)
        {
            if (item.ProdutoCriado)
            {
                // Produto inteiro foi criado só por essa importação — remove tudo
                produtosParaExcluir.Add(item.ProdutoId);
                continue;
            }

            var produto = await db.Produtos.Include(p => p.Variacoes).FirstOrDefaultAsync(p => p.Id == item.ProdutoId);
            if (produto is null) continue;

            if (item.VariacaoId.HasValue)
            {
                var variacao = produto.Variacoes.FirstOrDefault(v => v.Id == item.VariacaoId.Value);
                if (variacao != null)
                {
                    if (item.VariacaoCriada)
                    {
                        db.ProdutoVariacoes.Remove(variacao);
                    }
                    else
                    {
                        variacao.Estoque = Math.Max(0, variacao.Estoque - (int)item.Quantidade);
                        variacao.AtualizadoEm = DateTime.UtcNow;
                    }
                }
            }
            else
            {
                produto.Estoque = Math.Max(0, produto.Estoque - item.Quantidade);
                produto.AtualizadoEm = DateTime.UtcNow;
            }

            if (item.CategoriaId.HasValue) categoriasParaChecar.Add(item.CategoriaId.Value);
        }

        // Remove os produtos criados exclusivamente por essa importação
        // (só remove se não tiverem vendas registradas, por segurança)
        foreach (var produtoId in produtosParaExcluir.Distinct())
        {
            var temVendas = await db.ItensVenda.AnyAsync(iv => iv.ProdutoId == produtoId);
            if (temVendas) continue; // não mexe se já foi vendido

            await db.ProdutoVariacoes.Where(v => v.ProdutoId == produtoId).ExecuteDeleteAsync();
            await db.Movimentos.Where(m => m.ProdutoId == produtoId).ExecuteDeleteAsync();
            await db.Produtos.Where(p => p.Id == produtoId).ExecuteDeleteAsync();
        }

        // Remove categorias que foram criadas só por essa importação e continuam sem produtos
        foreach (var catId in categoriasParaChecar)
        {
            var cat = await db.CategoriasLoja.FindAsync(catId);
            if (cat is null) continue;
            var criadaNestaImportacao = itens.Any(i => i.CategoriaId == catId && i.CategoriaCriada);
            if (!criadaNestaImportacao) continue;

            var aindaTemProduto = await db.Produtos.AnyAsync(p => p.LojaId == lojaId && p.Categoria == cat.Nome && p.Ativo);
            if (!aindaTemProduto)
                cat.Ativo = false;
        }

        // Remove os movimentos de estoque criados por essa importação (todos com a mesma observação) —
        // a tag muda conforme a origem (importação por XML ou lançamento manual).
        var tagMovimento = nf.Origem == "manual" ? $"Nota fiscal manual {nf.NumeroNf}" : $"Importação NF {nf.NumeroNf}";
        await db.Movimentos
            .Where(m => m.LojaId == lojaId && m.Observacao == tagMovimento)
            .ExecuteDeleteAsync();

        // Remove o mapeamento fornecedor->produto criado por essa nota (só se apontar pra produto agora excluído)
        // Mantemos os mapeamentos que ainda apontam pra produtos existentes — mais seguro.

        nf.Desfeita = true;
        await db.SaveChangesAsync();

        return Ok(new { mensagem = "Importação desfeita. Você já pode reimportar esta nota se quiser." });
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

    public record NfPreviewResponse(string CnpjFornecedor, string NomeFornecedor, string NumeroNf, string ChaveAcesso, List<ItemNfPreview> Itens);

    public record ItemConfirmacao(
        string CodigoFornecedor, string? Gtin,
        string NomeBase, string? Cor, string? Tamanho,
        decimal Quantidade,
        string Acao, // "existente" | "novo"
        Guid? ProdutoId, // obrigatório se Acao == "existente"
        decimal? PrecoCusto, // obrigatório se Acao == "novo" — editável, pré-preenchido com o valor da nota
        decimal? PrecoVenda, // obrigatório se Acao == "novo"
        string? CategoriaNome // obrigatório se Acao == "novo"
    );

    public record ConfirmarImportacaoRequest(string CnpjFornecedor, string NumeroNf, string ChaveAcesso, string NomeFornecedor, List<ItemConfirmacao> Itens);

    public record ItemNfManualRequest(
        Guid? ProdutoId, Guid? VariacaoId, decimal Quantidade, decimal? PrecoCusto,
        string? Acao, // null/"existente" (padrão) | "novo"
        string? NomeBase, string? CategoriaNome, string? Cor, string? Tamanho, decimal? PrecoVenda, string? Gtin,
        string? TipoVenda = null, // "unidade" (padrão) | "fracionado" — só usado quando Acao == "novo"
        string? UnidadeMedida = null
    );

    public record LancarNfManualRequest(
        Guid FornecedorId, string NumeroNf, DateTime? DataEmissao, decimal? ValorTotal,
        List<ItemNfManualRequest> Itens
    );

    public record ItemNfEditRequest(Guid ProdutoId, Guid? VariacaoId, decimal Quantidade, decimal? PrecoCusto);

    public record EditarNfManualRequest(
        Guid FornecedorId, string NumeroNf, DateTime? DataEmissao, decimal? ValorTotal,
        List<ItemNfEditRequest> Itens
    );

    public record ItemImportadoDetalhe(
    Guid ProdutoId, Guid? VariacaoId, decimal Quantidade,
    bool ProdutoCriado, bool VariacaoCriada, bool CategoriaCriada, Guid? CategoriaId
    );
}