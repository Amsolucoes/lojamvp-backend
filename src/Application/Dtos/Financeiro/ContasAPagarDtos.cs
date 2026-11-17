namespace LojaApi.Application.Financeiro;

// Dados lidos para montar Contas a Pagar e as faturas do mês (os nomes das propriedades casam com as colunas lidas no Dapper)

public record LancamentoListadoDto(Guid Id, Guid ContaBancariaId, string Descricao, string? CategoriaNome, string Modo, decimal Valor, DateTime Vencimento, string Status,
    DateTime? PagoEm, int? NumeroParcela, int? TotalParcelas, string Origem);

public record LancamentoAPagarDto(Guid Id, string Descricao, string? Observacao, string? CategoriaNome, Guid? CategoriaId, Guid ContaBancariaId, string Modo, bool Avisar, decimal Valor,
    DateTime Vencimento, string Status, DateTime? PagoEm, string? FormaPagamento, int? NumeroParcela, int? TotalParcelas);

public record CartaoAtivoDto(Guid Id, string Nome, int DiaFechamento, int DiaVencimento);

public record ItemDeCartaoDto(Guid Id, Guid CartaoId, string Descricao, decimal Valor, DateTime DataCompra, string? CategoriaNome);

public record CompraDeCartaoDto(Guid CartaoId, DateTime DataCompra, decimal Valor);

// Parcela de financiamento de fatura (lançamento a pagar ligado a um cartão)
public record ParcelaDoCartaoDto(Guid Id, Guid? CartaoOrigemId, string Descricao, decimal Valor, DateTime Vencimento, string Status, int? NumeroParcela, int? TotalParcelas,
    Guid? FaturaCartaoId, DateTime? PagoEm);

public record FaturaLeveDto(Guid Id, Guid CartaoCreditoId, DateTime MesReferencia, string Status, decimal ValorPago, DateTime? PagoEm);

public record AntecipadoPorFaturaDto(Guid FaturaId, decimal Total);
