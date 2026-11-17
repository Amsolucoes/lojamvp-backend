namespace LojaApi.Application.Financeiro;

// O que a API do Financeiro (faturas do cartão) RECEBE (os nomes das propriedades são o JSON do corpo)

public record PagarFaturaRequest(string Modo, decimal? ValorPago, int? TotalParcelas, decimal? ValorEntrada, DateTime? PrimeiraParcela, Guid? ContaBancariaId = null, DateTime? DataPagamento = null, bool NaoAfetaSaldo = false);
public record AntecipadoFaturaRequest(decimal Valor, DateTime Data, Guid ContaBancariaId, string? Observacao);
