namespace LojaApi.Application.Financeiro;

// O que a API do Financeiro (lançamentos, parcelas e fixos) RECEBE (os nomes das propriedades são o JSON do corpo)

public record SalvarLancamentoAvulsoRequest(Guid ContaBancariaId, string Tipo, string Descricao, Guid? CategoriaId, decimal Valor, DateTime Vencimento, string? Observacao, bool JaPago = false, bool Avisar = true);
public record SalvarLancamentoParceladoRequest(Guid ContaBancariaId, string Tipo, string Descricao, Guid? CategoriaId, decimal ValorParcela, int? TotalParcelas, DateTime PrimeiroVencimento, DateTime? DataFim, string? Observacao, bool JaPago = false, bool Avisar = true);
public record SalvarLancamentoFixoRequest(Guid ContaBancariaId, string Tipo, string Descricao, Guid? CategoriaId, decimal Valor, int DiaVencimento, string? Observacao, DateTime? DataInicio = null, bool JaPago = false, bool Avisar = true);
public record MarcarPagamentoRequest(bool Pago, bool AbsorvidaSemConta = false);
public record EditarLancamentoRequest(string Descricao, Guid? CategoriaId, Guid ContaBancariaId, decimal Valor, DateTime Vencimento, string? Observacao);
public record ToggleAvisarRequest(bool Avisar);
public record ParcelasIdsRequest(List<Guid> Ids);
public record ParcelasPagamentoRequest(List<Guid> Ids, bool Pago);
public record ParcelasTotalRequest(int Total);
public record PagarLoteRequest(List<Guid> Ids, string Forma, Guid? ContaBancariaId, Guid? CartaoId, int Parcelas = 1, DateTime? Data = null);
