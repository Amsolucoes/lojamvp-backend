using System.Xml.Linq;
using LojaApi.Domain.NfImportacao;
using LojaApi.Models;
using LojaApi.Shared.Application;

namespace LojaApi.Application.NfImportacao;

// Lê o XML da NF-e e mostra, item a item, com qual produto da loja ele casa (sem gravar nada).
// Ordem de casamento: GTIN, mapeamento salvo do fornecedor, nome exato, nome parecido (sugestão), novo.
public class PreviewNfImportacaoQueryHandler(INfImportacaoRepository repositorio) : IQueryHandler<PreviewNfImportacaoQuery>
{
    public async Task<Resultado<object>> HandleAsync(PreviewNfImportacaoQuery query)
    {
        if (query.LojaId is not { } lojaId) return Resultado.Invalido("Loja não encontrada.");
        if (query.Arquivo is null) return Resultado.Invalido("Envie o arquivo XML da nota fiscal.");

        XDocument doc;
        try
        {
            doc = await XDocument.LoadAsync(query.Arquivo, LoadOptions.None, CancellationToken.None);
        }
        catch
        {
            return Resultado.Invalido("Arquivo XML inválido.");
        }

        var (infNFe, chaveAcesso, erro) = NfImportacaoRegras.Identificar(doc);
        if (erro != null) return Resultado.Invalido(erro);

        var jaImportada = await repositorio.ObterNotaAtivaPorChaveAsync(lojaId, chaveAcesso!);
        if (jaImportada != null)
            return Resultado.Conflito(NfImportacaoRegras.MensagemJaImportada(jaImportada.ImportadoEm, jaImportada.QtdItens));

        var nota = NfImportacaoRegras.LerNota(infNFe!, chaveAcesso!);
        var produtosDaLoja = await repositorio.ListarProdutosAtivosComVariacoesAsync(lojaId);
        var categoriasDaLoja = await repositorio.ListarCategoriasAtivasAsync(lojaId);
        var mapeamentos = await repositorio.ListarMapeamentosAsync(lojaId, nota.CnpjFornecedor);

        var itens = new List<ItemNfPreview>();
        foreach (var item in nota.Itens)
        {
            var (nomeBase, cor, tamanho) = NfImportacaoRegras.ExtrairVariacao(item.Descricao);

            Produto? match = null;
            var status = "novo";

            if (item.Gtin != null)
            {
                match = produtosDaLoja.FirstOrDefault(p => p.CodigoBarras == item.Gtin);
                if (match != null) status = "gtin";
            }

            if (match is null)
            {
                var mapeado = mapeamentos.FirstOrDefault(m => m.CodigoFornecedor == item.CodigoFornecedor);
                if (mapeado != null)
                {
                    match = produtosDaLoja.FirstOrDefault(p => p.Id == mapeado.ProdutoId);
                    if (match != null) status = "mapeamento";
                }
            }

            if (match is null)
            {
                match = produtosDaLoja.FirstOrDefault(p => p.Nome.Equals(nomeBase, StringComparison.OrdinalIgnoreCase));
                if (match != null) status = "nome_exato";
            }

            if (match is null)
            {
                // Sugestão por nome aproximado: sempre precisa de confirmação manual
                var candidato = NfImportacaoRegras.MelhorCandidatoPorNome(nomeBase, produtosDaLoja, p => p.Nome);
                if (candidato != null)
                {
                    match = candidato;
                    status = "sugestao";
                }
            }

            var variacaoJaExiste = false;
            int? estoqueVariacaoAtual = null;
            if (match != null && NfImportacaoRegras.TemVariacao(cor, tamanho))
            {
                var variacao = match.Variacoes.FirstOrDefault(v => v.Ativo && NfImportacaoRegras.MesmaVariacao(v.Cor, v.Tamanho, cor, tamanho));
                if (variacao != null)
                {
                    variacaoJaExiste = true;
                    estoqueVariacaoAtual = variacao.Estoque;
                }
            }

            var categoriaSugerida = match?.Categoria ?? NfImportacaoRegras.SugerirCategoria(nomeBase);
            var categoriaJaExiste = categoriasDaLoja.Any(c => c.Nome.Equals(categoriaSugerida, StringComparison.OrdinalIgnoreCase));

            itens.Add(new ItemNfPreview(
                item.CodigoFornecedor, item.Gtin, item.Descricao, nomeBase, cor, tamanho,
                item.Quantidade, item.ValorUnitario, item.ValorTotal,
                status, match?.Id, match?.Nome,
                variacaoJaExiste, estoqueVariacaoAtual,
                categoriaSugerida, categoriaJaExiste));
        }

        return Resultado.Ok(new NfPreviewResponse(nota.CnpjFornecedor, nota.NomeFornecedor, nota.NumeroNf, nota.ChaveAcesso, itens));
    }
}
