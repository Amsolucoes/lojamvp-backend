namespace LojaApi.Application.Vendas;

// O que Vendas DEVOLVE (os nomes das propriedades viram o JSON da API)

public record ItemVendaDto(
    Guid Id, Guid? ProdutoId, string NomeProduto,
    decimal Quantidade, decimal PrecoUnitario, decimal Subtotal,
    Guid? ServicoId = null
);

public record VendaDto(
    Guid Id,
    Guid? ClienteId, string? NomeCliente,
    decimal Total, decimal Desconto, decimal TotalFinal,
    string FormaPagamento, string? FormasPagamento,
    decimal? Troco,
    DateTime CriadaEm,
    List<ItemVendaDto> Itens,
    string? OrigemNome,
    decimal? CreditoUsado = null,
    Guid? FuncionarioId = null,
    string? NomeFuncionario = null,
    decimal? ValorCarteira = null
);

public record ProdutoRankingDto(
    Guid Id, string Nome, string Categoria,
    decimal QtdVendida, decimal Receita, decimal LucroEstimado
);

public record FluxoDiaDto(string Data, int QtdVendas, decimal Entradas, decimal Descontos);
public record FluxoMesDto(int Mes, string NomeMes, int QtdVendas, decimal Entradas, decimal Descontos);

public record ResumoVendasDto(
    decimal TotalVendido, decimal TotalDescontos,
    decimal TicketMedio, decimal TotalItensVendidos,
    int TotalVendas
);

// Lista de origens ativas (só estes campos)
public record OrigemVendaResumoDto(Guid Id, string Nome, int Ordem);

// Linha da lista de movimentos de caixa
public record MovimentoCaixaDto(
    Guid Id, string Tipo, decimal Valor, DateTime Data, Guid? OrigemVendaId, string? OrigemNome,
    string? Observacao, Guid? ContaBancariaId, DateTime CriadoEm);

// Lista de trocas
public record TrocaItemDto(string NomeProduto, int Quantidade, decimal PrecoUnitario, string Tipo, bool VoltaEstoque);

public record TrocaListaDto(
    Guid Id, Guid ClienteId, string NomeCliente, decimal TotalDevolvido, decimal TotalNovo, decimal Diferenca,
    decimal CreditoGerado, string? FormaPagamento, DateTime CriadaEm, List<TrocaItemDto> Itens);
