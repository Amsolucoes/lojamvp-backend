namespace LojaApi.Application.Financeiro;

// O que a API do Financeiro (lançamentos) DEVOLVE nas listagens (os nomes das propriedades são o JSON da resposta)

// Linha de lançamento avulso/parcelado/fixo a receber, na visão unificada de Contas a Receber
public record LancamentoAReceberDto(Guid Id, string Descricao, string? Observacao, string? CategoriaNome, string Modo, Guid? CategoriaId, Guid ContaBancariaId,
    int? NumeroParcela, int? TotalParcelas, decimal Valor, DateTime Vencimento, string Status, DateTime? PagoEm, string Origem);

// Linha de mensalidade de plano de cliente, na mesma visão
public record MensalidadeDePlanoDto(Guid Id, string Descricao, decimal Valor, DateTime Vencimento, string Status, DateTime? PagoEm, string Origem);
