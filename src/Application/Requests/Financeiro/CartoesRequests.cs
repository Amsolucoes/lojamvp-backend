namespace LojaApi.Application.Financeiro;

// O que a API do Financeiro (cartões de crédito) RECEBE (os nomes das propriedades são o JSON do corpo)

public record SalvarCartaoRequest(string Nome, decimal Limite, int DiaFechamento, int DiaVencimento, Guid ContaBancariaId, decimal TaxaJurosMensal = 0, string? Bandeira = null);
public record SalvarLancamentoCartaoRequest(string Descricao, decimal Valor, DateTime DataCompra, Guid? CategoriaId, string? Observacao = null);
public record CompraParceladaCartaoRequest(string Descricao, decimal ValorParcela, int TotalParcelas, DateTime DataCompra, Guid? CategoriaId, string? Observacao = null);
public record CartaoFixoRequest(string Descricao, decimal Valor, Guid? CategoriaId, string? Observacao = null, int DiaCompra = 1);
public record EditarLancamentoCartaoRequest(string Descricao, decimal Valor, DateTime DataCompra, Guid? CategoriaId, string? Observacao = null);
