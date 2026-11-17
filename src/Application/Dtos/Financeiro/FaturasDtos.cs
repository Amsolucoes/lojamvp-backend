namespace LojaApi.Application.Financeiro;

// O que a API do Financeiro (fatura do cartão) DEVOLVE (os nomes das propriedades são o JSON da resposta)

public record FaturaItemDto(Guid Id, string Descricao, decimal Valor, DateTime DataCompra, string? CategoriaNome, Guid? CategoriaId, string Modo, string? Observacao);

public record ParcelaFinanciamentoDto(Guid Id, string Descricao, decimal Valor, DateTime Vencimento, string Status, int? NumeroParcela, int? TotalParcelas, Guid ContaBancariaId,
    Guid? CategoriaId, string? Observacao, string Modo, int? MesOrigemFatura, int? AnoOrigemFatura);

public record AntecipadoDto(Guid Id, decimal Valor, DateTime Data, Guid ContaBancariaId, string? Observacao);

// Fatura paga parcialmente (usada para absorver, em Contas a Pagar, as parcelas de financiamento do mês dela)
public record FaturaParcialDto(Guid CartaoCreditoId, DateTime MesReferencia, DateTime? PagoEm);
