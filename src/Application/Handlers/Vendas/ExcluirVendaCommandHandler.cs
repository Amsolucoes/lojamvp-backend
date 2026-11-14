using LojaApi.Application.Carteira;
using LojaApi.Domain.Vendas;
using LojaApi.Models;
using LojaApi.Shared.Application;

namespace LojaApi.Application.Vendas;

// Exclui uma venda (só do mês atual), estornando estoque, agendamento, uso do plano, crédito, carteira e comissão pendente.
public class ExcluirVendaCommandHandler(IVendasRepository repositorio, CarteiraService carteira) : ICommandHandler<ExcluirVendaCommand>
{
    public async Task<Resultado<object>> HandleAsync(ExcluirVendaCommand command)
    {
        var (lojaId, id) = command;
        var venda = await repositorio.ObterVendaComItensAsync(id);

        if (venda is null || (lojaId.HasValue && venda.LojaId != lojaId)) return Resultado.NaoEncontrado();

        if (!VendasRegras.PodeExcluirNoMes(venda.CriadaEm, DateTime.UtcNow.Date))
            return Resultado.Invalido("Só é possível excluir vendas do mês atual.");

        var temNfceAutorizada = await repositorio.TemNotaFiscalAutorizadaAsync(venda.Id);
        if (temNfceAutorizada)
            return Resultado.Invalido("Não é possível excluir: já existe uma nota fiscal autorizada para esta venda. Cancele a nota antes.");

        // Tira da carteira a parte que o cliente ficou devendo nessa venda
        var erroCarteira = await carteira.RemoverCompraDaVendaAsync(lojaId ?? Guid.Empty, venda.Id);
        if (erroCarteira != null) return Resultado.Invalido(erroCarteira);

        // Comissão gerada por essa venda: se já foi paga num fechamento, não dá
        // pra excluir sem bagunçar o fechamento — se ainda tá pendente, estorna junto.
        var comissaoVenda = await repositorio.ObterComissaoDaVendaAsync(venda.Id);
        if (comissaoVenda != null)
        {
            if (comissaoVenda.Status != "pendente")
                return Resultado.Invalido("Não é possível excluir: a comissão desta venda já foi paga.");
            repositorio.Remover(comissaoVenda);
        }

        // Estorna estoque. Observação: o ItemVenda não guarda qual variação
        // (tamanho/cor) foi vendida quando o produto tem grade — só o produto
        // pai. Por isso, pra produtos com grade, o estorno soma no total do
        // produto, mas não sabe em qual variação específica devolver.
        foreach (var item in venda.Itens.Where(i => i.ProdutoId.HasValue))
        {
            var produto = await repositorio.ObterProdutoAsync(item.ProdutoId!.Value, null);
            if (produto is null) continue;

            produto.Estoque += item.Quantidade;
            produto.AtualizadoEm = DateTime.UtcNow;

            repositorio.Adicionar(new MovimentoEstoque
            {
                ProdutoId = produto.Id,
                Tipo = "entrada",
                Quantidade = item.Quantidade,
                Observacao = $"Estorno da venda #{venda.Id.ToString()[..8]} (excluída) - {item.NomeProduto}",
                LojaId = lojaId,
            });
        }

        // Reverte agendamentos marcados como pagos por essa venda
        var agendamentos = await repositorio.ListarAgendamentosDaVendaAsync(venda.Id);
        foreach (var ag in agendamentos)
        {
            ag.Pago = false;
            ag.VendaId = null;
        }

        // Remove consumos de plano gerados por essa venda (devolve o uso do plano)
        await repositorio.RemoverConsumosDePlanoDaVendaAsync(venda.Id);

        // Devolve crédito de loja usado pelo cliente nessa venda
        if (venda.CreditoUsado.HasValue && venda.CreditoUsado.Value > 0 && venda.ClienteId.HasValue)
        {
            var cliente = await repositorio.ObterClienteAsync(venda.ClienteId.Value, null);
            if (cliente != null) cliente.CreditoLoja += venda.CreditoUsado.Value;
        }

        repositorio.RemoverItens(venda.Itens);
        repositorio.Remover(venda);

        await repositorio.SalvarAsync();
        return Resultado.Ok(new { mensagem = "Venda excluída. Estoque, agendamento e crédito foram estornados." });
    }
}
