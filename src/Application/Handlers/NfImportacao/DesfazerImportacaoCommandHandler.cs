using System.Text.Json;
using LojaApi.Domain.NfImportacao;
using LojaApi.Shared.Application;

namespace LojaApi.Application.NfImportacao;

// Desfaz uma nota: tira o estoque que ela somou, apaga os produtos que só ela criou (se nunca foram vendidos),
// inativa categorias que só ela criou e ficaram vazias, apaga os movimentos dela e libera o número para importar de novo.
public class DesfazerImportacaoCommandHandler(INfImportacaoRepository repositorio) : ICommandHandler<DesfazerImportacaoCommand>
{
    public async Task<Resultado<object>> HandleAsync(DesfazerImportacaoCommand command)
    {
        var (lojaIdOuNulo, id) = command;
        if (lojaIdOuNulo is not { } lojaId) return Resultado.Invalido("Loja não encontrada.");

        var nf = await repositorio.ObterNotaAsync(id, lojaId);
        if (nf is null) return Resultado.NaoEncontrado();
        if (nf.Desfeita) return Resultado.Invalido("Esta importação já foi desfeita.");

        var itens = JsonSerializer.Deserialize<List<ItemImportadoDetalhe>>(nf.ItensJson) ?? [];
        var produtosParaExcluir = new List<Guid>();
        var categoriasParaChecar = new HashSet<Guid>();

        foreach (var item in itens)
        {
            if (item.ProdutoCriado)
            {
                // Produto inteiro foi criado só por essa nota — remove tudo
                produtosParaExcluir.Add(item.ProdutoId);
                continue;
            }

            var produto = await repositorio.ObterProdutoComVariacoesAsync(item.ProdutoId);
            if (produto is null) continue;

            if (item.VariacaoId.HasValue)
            {
                var variacao = produto.Variacoes.FirstOrDefault(v => v.Id == item.VariacaoId.Value);
                if (variacao != null)
                {
                    if (item.VariacaoCriada)
                    {
                        repositorio.Remover(variacao);
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

        // Só remove produto sem vendas registradas, por segurança
        foreach (var produtoId in produtosParaExcluir.Distinct())
        {
            if (await repositorio.ProdutoTemVendasAsync(produtoId)) continue;
            await repositorio.ExcluirProdutoComDependenciasAsync(produtoId);
        }

        foreach (var categoriaId in categoriasParaChecar)
        {
            var categoria = await repositorio.ObterCategoriaAsync(categoriaId);
            if (categoria is null) continue;
            if (!itens.Any(i => i.CategoriaId == categoriaId && i.CategoriaCriada)) continue;

            if (!await repositorio.CategoriaTemProdutoAtivoAsync(lojaId, categoria.Nome))
                categoria.Ativo = false;
        }

        // Os movimentos da nota têm todos a mesma observação (muda conforme a origem: XML ou manual)
        await repositorio.ExcluirMovimentosPorObservacaoAsync(lojaId, NfImportacaoRegras.EtiquetaMovimento(nf.Origem, nf.NumeroNf));

        // O mapeamento fornecedor→produto fica (ainda aponta para produtos que existem)
        nf.Desfeita = true;
        await repositorio.SalvarAsync();

        return Resultado.Ok(new MensagemDto("Importação desfeita. Você já pode reimportar esta nota se quiser."));
    }
}
