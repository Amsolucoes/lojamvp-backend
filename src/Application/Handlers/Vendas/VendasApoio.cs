using LojaApi.Models;

namespace LojaApi.Application.Vendas;

// O que vários handlers de Vendas compartilham.
public static class VendasApoio
{
    public static VendaDto ParaDto(Venda v) => new(
        v.Id, v.ClienteId, v.Cliente?.Nome,
        v.Total, v.Desconto, v.TotalFinal,
        v.FormaPagamento, v.FormasPagamento,
        v.Troco, v.CriadaEm,
        v.Itens.Select(i => new ItemVendaDto(
            i.Id, i.ProdutoId, i.NomeProduto,
            i.Quantidade, i.PrecoUnitario, i.Subtotal,
            i.ServicoId
        )).ToList(),
        v.OrigemNome,
        v.CreditoUsado,
        v.FuncionarioId,
        v.NomeFuncionario,
        v.ValorCarteira
    );
}
