using System.Text.Json;
using LojaApi.Domain.NfImportacao;
using LojaApi.Models;
using LojaApi.Shared.Application;
using LojaApi.src.Models;

namespace LojaApi.Application.NfImportacao;

// Confirma a importação da NF-e: cria produtos/categorias novos, soma estoque dos existentes, registra os movimentos,
// guarda o mapeamento código-do-fornecedor → produto e a nota (com o detalhe para poder desfazer).
public class ConfirmarImportacaoCommandHandler(INfImportacaoRepository repositorio) : ICommandHandler<ConfirmarImportacaoCommand>
{
    public async Task<Resultado<object>> HandleAsync(ConfirmarImportacaoCommand command)
    {
        var (lojaIdOuNulo, req) = command;
        if (lojaIdOuNulo is not { } lojaId) return Resultado.Invalido("Loja não encontrada.");

        // Trava definitiva contra reenvio da mesma nota, mesmo se o preview foi burlado
        if (await repositorio.ObterNotaAtivaPorChaveAsync(lojaId, req.ChaveAcesso) != null)
            return Resultado.Conflito("Esta nota já foi importada anteriormente.");

        var criados = 0;
        var atualizados = 0;
        var categoriasCriadas = 0;
        var detalhes = new List<ItemImportadoDetalhe>();
        decimal custoTotal = 0, vendaTotal = 0, quantidadeTotal = 0;
        var observacao = NfImportacaoRegras.EtiquetaMovimentoXml(req.NumeroNf);

        foreach (var item in req.Itens)
        {
            Guid produtoId;
            var temVariacao = NfImportacaoRegras.TemVariacao(item.Cor, item.Tamanho);
            bool produtoCriado = false, variacaoCriada = false, categoriaCriadaAgora = false;
            Guid? categoriaId = null;
            Guid? variacaoId = null;
            decimal custoUnit, vendaUnit;

            if (item.Acao == NfImportacaoRegras.AcaoNovo)
            {
                custoUnit = item.PrecoCusto ?? 0;
                vendaUnit = item.PrecoVenda ?? item.PrecoCusto ?? 0;

                var novo = await NfImportacaoApoio.CriarProdutoNovoAsync(repositorio, lojaId, item.NomeBase, item.CategoriaNome, item.Cor, item.Tamanho,
                    custoUnit, vendaUnit, item.Quantidade, item.Gtin);

                produtoId = novo.ProdutoId;
                variacaoId = novo.VariacaoId;
                produtoCriado = true;
                variacaoCriada = novo.VariacaoId.HasValue;
                categoriaCriadaAgora = novo.CategoriaCriada;
                categoriaId = novo.CategoriaId;
                if (novo.CategoriaCriada) categoriasCriadas++;
                criados++;
            }
            else
            {
                if (item.ProdutoId is null) continue;
                var produto = await repositorio.ObterProdutoComVariacoesAsync(item.ProdutoId.Value);
                if (produto is null || produto.LojaId != lojaId) continue;

                if (temVariacao)
                {
                    var variacao = produto.Variacoes.FirstOrDefault(v => v.Ativo && NfImportacaoRegras.MesmaVariacao(v.Cor, v.Tamanho, item.Cor, item.Tamanho));
                    if (variacao != null)
                    {
                        variacao.Estoque += (int)item.Quantidade;
                        variacao.AtualizadoEm = DateTime.UtcNow;
                        variacaoId = variacao.Id;
                    }
                    else
                    {
                        var nova = new ProdutoVariacao { ProdutoId = produto.Id, Cor = item.Cor, Tamanho = item.Tamanho, Estoque = (int)item.Quantidade };
                        repositorio.Adicionar(nova);
                        await repositorio.SalvarAsync();
                        variacaoId = nova.Id;
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
            }

            repositorio.Adicionar(NfImportacaoApoio.Entrada(produtoId, item.Quantidade, observacao, lojaId));

            custoTotal += custoUnit * item.Quantidade;
            vendaTotal += vendaUnit * item.Quantidade;
            quantidadeTotal += item.Quantidade;
            detalhes.Add(new ItemImportadoDetalhe(produtoId, variacaoId, item.Quantidade, produtoCriado, variacaoCriada, categoriaCriadaAgora, categoriaId));

            // Guarda o de-para código-do-fornecedor → produto, pra próxima nota casar direto
            var mapeamento = await repositorio.ObterMapeamentoAsync(lojaId, req.CnpjFornecedor, item.CodigoFornecedor);
            if (mapeamento is null)
                repositorio.Adicionar(new NfProdutoMapeamento { LojaId = lojaId, CnpjFornecedor = req.CnpjFornecedor, CodigoFornecedor = item.CodigoFornecedor, ProdutoId = produtoId });
            else if (mapeamento.ProdutoId != produtoId)
                mapeamento.ProdutoId = produtoId;
        }

        repositorio.Adicionar(new NfImportada
        {
            LojaId = lojaId,
            ChaveAcesso = req.ChaveAcesso,
            NumeroNf = req.NumeroNf,
            NomeFornecedor = req.NomeFornecedor,
            ValorTotal = custoTotal,
            ValorCustoTotal = custoTotal,
            ValorVendaTotal = vendaTotal,
            QuantidadeTotal = quantidadeTotal,
            QtdItens = req.Itens.Count,
            ItensJson = JsonSerializer.Serialize(detalhes),
        });
        await repositorio.SalvarAsync();

        return Resultado.Ok(new ImportacaoConcluidaDto("Importação concluída.", criados, atualizados, categoriasCriadas));
    }
}
