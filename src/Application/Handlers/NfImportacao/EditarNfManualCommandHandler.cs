using System.Text.Json;
using LojaApi.Domain.NfImportacao;
using LojaApi.Shared.Application;

namespace LojaApi.Application.NfImportacao;

// Edita uma nota lançada à mão: número, fornecedor, data, valor e quantidade/custo dos itens (não troca nem remove itens).
// O estoque anda só pela diferença de quantidade (sem ficar negativo) e os movimentos da nota ganham a etiqueta nova.
public class EditarNfManualCommandHandler(INfImportacaoRepository repositorio) : ICommandHandler<EditarNfManualCommand>
{
    public async Task<Resultado<object>> HandleAsync(EditarNfManualCommand command)
    {
        var (lojaIdOuNulo, id, req) = command;
        if (lojaIdOuNulo is not { } lojaId) return Resultado.Invalido("Loja não encontrada.");

        var nf = await repositorio.ObterNotaAsync(id, lojaId);
        if (nf is null) return Resultado.NaoEncontrado();
        if (nf.Desfeita) return Resultado.Invalido("Esta importação foi desfeita e não pode ser editada.");
        if (nf.Origem != NfImportacaoRegras.OrigemManual) return Resultado.Invalido("Apenas notas lançadas manualmente podem ser editadas.");
        if (string.IsNullOrWhiteSpace(req.NumeroNf)) return Resultado.Invalido("Informe o número da nota fiscal.");

        var fornecedor = await repositorio.ObterFornecedorAsync(req.FornecedorId, lojaId);
        if (fornecedor is null) return Resultado.Invalido("Fornecedor não encontrado.");

        var numeroEditado = req.NumeroNf.Trim();

        // Trava contra duas notas com o mesmo número pro mesmo fornecedor (ignora a própria)
        if (await repositorio.NotaDoFornecedorExisteAsync(lojaId, fornecedor.Id, numeroEditado, nf.Id))
            return Resultado.Conflito(NfImportacaoRegras.MensagemNotaDuplicada(numeroEditado, fornecedor.Nome));

        var itensAntigos = JsonSerializer.Deserialize<List<ItemImportadoDetalhe>>(nf.ItensJson) ?? [];
        if (itensAntigos.Count != req.Itens.Count)
            return Resultado.Invalido("Não é possível adicionar ou remover itens ao editar — desfaça e lance novamente.");

        var tagAntiga = NfImportacaoRegras.EtiquetaMovimentoManual(nf.NumeroNf);
        var tagNova = NfImportacaoRegras.EtiquetaMovimentoManual(numeroEditado);

        var detalhesNovos = new List<ItemImportadoDetalhe>();
        decimal custoTotal = 0, vendaTotal = 0, quantidadeTotal = 0;

        foreach (var antigo in itensAntigos)
        {
            var novo = req.Itens.FirstOrDefault(i => i.ProdutoId == antigo.ProdutoId && i.VariacaoId == antigo.VariacaoId);
            if (novo is null) return Resultado.Invalido("Os itens editados não correspondem aos itens originais desta nota.");

            var produto = await repositorio.ObterProdutoComVariacoesAsync(antigo.ProdutoId);
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

        foreach (var movimento in await repositorio.ListarMovimentosPorObservacaoAsync(lojaId, tagAntiga))
        {
            var correspondente = req.Itens.FirstOrDefault(i => i.ProdutoId == movimento.ProdutoId);
            if (correspondente != null) movimento.Quantidade = correspondente.Quantidade;
            movimento.Observacao = tagNova;
        }

        nf.FornecedorId = fornecedor.Id;
        nf.NomeFornecedor = fornecedor.Nome;
        nf.NumeroNf = numeroEditado;
        nf.DataEmissao = NfImportacaoRegras.DataEmissaoUtc(req.DataEmissao);
        nf.ValorTotal = req.ValorTotal ?? custoTotal;
        nf.ValorCustoTotal = custoTotal;
        nf.ValorVendaTotal = vendaTotal;
        nf.QuantidadeTotal = quantidadeTotal;
        nf.ItensJson = JsonSerializer.Serialize(detalhesNovos);
        await repositorio.SalvarAsync();

        return Resultado.Ok(new MensagemDto("Nota fiscal atualizada."));
    }
}
