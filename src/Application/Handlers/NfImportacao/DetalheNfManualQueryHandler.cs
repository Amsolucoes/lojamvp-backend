using System.Text.Json;
using LojaApi.Domain.NfImportacao;
using LojaApi.Shared.Application;

namespace LojaApi.Application.NfImportacao;

// Detalhe de uma nota lançada à mão (para preencher o formulário de edição). Produto excluído depois some da lista.
public class DetalheNfManualQueryHandler(INfImportacaoRepository repositorio) : IQueryHandler<DetalheNfManualQuery>
{
    public async Task<Resultado<object>> HandleAsync(DetalheNfManualQuery query)
    {
        var (lojaId, id) = query;
        if (lojaId is null) return Resultado.NaoEncontrado();

        var nf = await repositorio.ObterNotaAsync(id, lojaId.Value);
        if (nf is null) return Resultado.NaoEncontrado();
        if (nf.Origem != NfImportacaoRegras.OrigemManual) return Resultado.Invalido("Apenas notas lançadas manualmente têm detalhe editável.");

        var detalhes = JsonSerializer.Deserialize<List<ItemImportadoDetalhe>>(nf.ItensJson) ?? [];
        var itens = new List<NfManualItemDto>();
        foreach (var item in detalhes)
        {
            var produto = await repositorio.ObterProdutoComVariacoesAsync(item.ProdutoId);
            if (produto is null) continue;

            string? rotulo = null;
            if (item.VariacaoId.HasValue)
            {
                var variacao = produto.Variacoes.FirstOrDefault(v => v.Id == item.VariacaoId.Value);
                if (variacao != null) rotulo = NfImportacaoRegras.RotuloDaVariacao(variacao.Cor, variacao.Tamanho);
            }

            itens.Add(new NfManualItemDto(item.ProdutoId, produto.Nome, item.VariacaoId, rotulo, item.Quantidade, produto.PrecoCusto));
        }

        return Resultado.Ok(new NfManualDetalheDto(nf.Id, nf.FornecedorId, nf.NumeroNf, nf.DataEmissao, nf.ValorTotal, itens));
    }
}
