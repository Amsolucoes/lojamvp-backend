namespace LojaApi.Application.Vendas;

// O que Vendas RECEBE no corpo da requisição

public record ItemVendaRequest(
    Guid? ProdutoId,
    decimal Quantidade,
    decimal PrecoUnitario,
    Guid? VariacaoId = null,
    Guid? ServicoId = null,
    Guid? AgendamentoId = null,
    Guid? AssinaturaId = null
);

public record CriarVendaRequest(
    List<ItemVendaRequest> Itens,
    Guid? ClienteId,
    decimal Desconto,
    string FormaPagamento,
    string? FormasPagamento,
    decimal? Troco,
    decimal? CreditoUsado,
    DateTime? DataVenda,
    Guid? OrigemVendaId,
    Guid? FuncionarioId = null
);

public record ItemTrocaRequest(
    Guid ProdutoId, string NomeProduto,
    Guid? VariacaoId, int Quantidade,
    decimal PrecoUnitario, bool VoltaEstoque
);

public record CriarTrocaRequest(
    Guid ClienteId,
    List<ItemTrocaRequest> Devolvidos,
    List<ItemTrocaRequest> Novos,
    string? FormaPagamento
);

public record CriarMovimentoRequest(string Tipo, decimal Valor, DateTime Data, Guid? OrigemVendaId, string? Observacao, Guid? ContaBancariaId);

public record EditarMovimentoRequest(string Tipo, decimal Valor, DateTime Data, Guid? OrigemVendaId, string? Observacao, Guid? ContaBancariaId);

public record SalvarOrigemRequest(string Nome, int Ordem, bool Ativa);
