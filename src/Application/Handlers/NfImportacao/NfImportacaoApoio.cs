using LojaApi.Domain.NfImportacao;
using LojaApi.Models;

namespace LojaApi.Application.NfImportacao;

public record ProdutoNovoDaNota(Guid ProdutoId, Guid? VariacaoId, bool CategoriaCriada, Guid CategoriaId);
public record ItensManuaisAplicados(List<ItemImportadoDetalhe> Detalhes, decimal CustoTotal, decimal VendaTotal, decimal QuantidadeTotal);

// O que vários handlers de NfImportacao compartilham.
public static class NfImportacaoApoio
{
    public static MovimentoEstoque Entrada(Guid produtoId, decimal quantidade, string observacao, Guid lojaId) => new()
    {
        ProdutoId = produtoId,
        Tipo = "entrada",
        Quantidade = quantidade,
        Observacao = observacao,
        LojaId = lojaId,
    };

    // Cria o produto novo (e a categoria, se ainda não existe) e grava na hora: a variação e a categoria
    // seguintes precisam achar o que este item criou.
    public static async Task<ProdutoNovoDaNota> CriarProdutoNovoAsync(
        INfImportacaoRepository repositorio, Guid lojaId, string nome, string? categoriaNome, string? cor, string? tamanho,
        decimal precoCusto, decimal precoVenda, decimal quantidade, string? gtin, string? tipoVenda = null, string? unidadeMedida = null)
    {
        var nomeCategoria = NfImportacaoRegras.NomeDaCategoria(categoriaNome);
        var categoria = await repositorio.ObterCategoriaAtivaPorNomeAsync(lojaId, nomeCategoria);

        var categoriaCriada = false;
        if (categoria is null)
        {
            categoria = new CategoriaLoja
            {
                LojaId = lojaId,
                Nome = nomeCategoria,
                Ordem = await repositorio.ProximaOrdemDeCategoriaAsync(lojaId),
                UsaTamanho = tamanho != null,
                UsaCor = cor != null,
            };
            repositorio.Adicionar(categoria);
            categoriaCriada = true;
        }

        var temVariacao = NfImportacaoRegras.TemVariacao(cor, tamanho);
        var tipoVendaFinal = NfImportacaoRegras.TipoDeVenda(tipoVenda);
        var produto = new Produto
        {
            Nome = nome,
            Categoria = categoria.Nome,
            PrecoCusto = precoCusto,
            PrecoVenda = precoVenda,
            Estoque = temVariacao ? 0 : quantidade,
            CodigoBarras = NfImportacaoRegras.GtinOuNulo(gtin),
            LojaId = lojaId,
            TipoVenda = tipoVendaFinal,
            UnidadeMedida = NfImportacaoRegras.UnidadeDeMedida(tipoVendaFinal, unidadeMedida),
        };
        repositorio.Adicionar(produto);
        await repositorio.SalvarAsync();

        Guid? variacaoId = null;
        if (temVariacao)
        {
            var variacao = new ProdutoVariacao { ProdutoId = produto.Id, Cor = cor, Tamanho = tamanho, Estoque = (int)quantidade };
            repositorio.Adicionar(variacao);
            await repositorio.SalvarAsync();
            variacaoId = variacao.Id;
        }

        return new ProdutoNovoDaNota(produto.Id, variacaoId, categoriaCriada, categoria.Id);
    }

    // Aplica os itens do lançamento manual (existentes ou novos): estoque, movimentos e o detalhe para editar/desfazer.
    // Confere TODOS os itens antes de mexer em qualquer coisa — um item inválido no meio não deixa produtos novos
    // dos itens anteriores gravados.
    public static async Task<(ItensManuaisAplicados? Aplicados, string? Erro)> AplicarItensManuaisAsync(
        INfImportacaoRepository repositorio, Guid lojaId, string numeroNf, List<ItemNfManualRequest> itens)
    {
        var produtos = new Produto?[itens.Count];
        for (var i = 0; i < itens.Count; i++)
        {
            var item = itens[i];
            if (item.Acao == NfImportacaoRegras.AcaoNovo)
            {
                if (string.IsNullOrWhiteSpace(item.NomeBase)) return (null, "Informe o nome do novo produto.");
                continue;
            }

            if (item.ProdutoId is null) return (null, "Produto não informado.");
            var produto = await repositorio.ObterProdutoComVariacoesAsync(item.ProdutoId.Value);
            if (produto is null || produto.LojaId != lojaId) return (null, "Produto não encontrado.");
            if (item.VariacaoId.HasValue && produto.Variacoes.All(v => v.Id != item.VariacaoId.Value))
                return (null, $"Variação não encontrada para '{produto.Nome}'.");
            produtos[i] = produto;
        }

        var detalhes = new List<ItemImportadoDetalhe>();
        decimal custoTotal = 0, vendaTotal = 0, quantidadeTotal = 0;
        for (var i = 0; i < itens.Count; i++)
        {
            var item = itens[i];
            Guid produtoId;
            Guid? variacaoId = null;
            bool produtoCriado = false, variacaoCriada = false, categoriaCriada = false;
            Guid? categoriaId = null;
            decimal custoUnit, vendaUnit;

            if (item.Acao == NfImportacaoRegras.AcaoNovo)
            {
                custoUnit = item.PrecoCusto ?? 0;
                vendaUnit = item.PrecoVenda ?? item.PrecoCusto ?? 0;

                var novo = await CriarProdutoNovoAsync(repositorio, lojaId, item.NomeBase!.Trim(), item.CategoriaNome, item.Cor, item.Tamanho,
                    custoUnit, vendaUnit, item.Quantidade, item.Gtin, item.TipoVenda, item.UnidadeMedida);

                produtoId = novo.ProdutoId;
                variacaoId = novo.VariacaoId;
                produtoCriado = true;
                variacaoCriada = novo.VariacaoId.HasValue;
                categoriaCriada = novo.CategoriaCriada;
                categoriaId = novo.CategoriaId;
            }
            else
            {
                var produto = produtos[i]!;
                if (item.VariacaoId.HasValue)
                {
                    var variacao = produto.Variacoes.First(v => v.Id == item.VariacaoId.Value);
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

            repositorio.Adicionar(Entrada(produtoId, item.Quantidade, NfImportacaoRegras.EtiquetaMovimentoManual(numeroNf), lojaId));
            detalhes.Add(new ItemImportadoDetalhe(produtoId, variacaoId, item.Quantidade, produtoCriado, variacaoCriada, categoriaCriada, categoriaId));
        }

        return (new ItensManuaisAplicados(detalhes, custoTotal, vendaTotal, quantidadeTotal), null);
    }
}
