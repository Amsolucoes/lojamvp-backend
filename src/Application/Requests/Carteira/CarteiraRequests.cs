namespace LojaApi.Application.Carteira;

public record ReceberCarteiraRequest(decimal Valor, string FormaPagamento, DateTime? Data, string? Observacao, Guid? ContaBancariaId);

public record DefinirLimiteCarteiraRequest(decimal? Limite);
