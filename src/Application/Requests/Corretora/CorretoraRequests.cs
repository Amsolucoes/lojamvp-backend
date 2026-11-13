namespace LojaApi.Application.Corretora;

public record SalvarSeguradoraRequest(string Nome);

public record AtualizarSeguradoraRequest(string Nome);

public record SalvarOportunidadeRequest(Guid ClienteId, Guid? SeguradoraId, string? PlanoDesejado, decimal? ValorEstimado, string? Observacao, int? QuantidadeVidas);

public record MoverEtapaRequest(string Etapa, int Ordem, string? MotivoPerda, decimal? Valor, Guid? ContaBancariaId);

// A tela manda o novo status no campo "nome" (aproveitava o mesmo formato da seguradora)
public record AlterarStatusApoliceRequest(string Nome);

public record SalvarApoliceRequest(
    Guid ClienteId, Guid? OportunidadeId, Guid SeguradoraId, string NomePlano, string? NumeroApolice,
    decimal ValorPremio, decimal ValorComissao, decimal? PercentualComissao,
    DateTime VigenciaInicio, DateTime VigenciaFim, Guid? ContaBancariaId, bool GerarNoFinanceiro);
