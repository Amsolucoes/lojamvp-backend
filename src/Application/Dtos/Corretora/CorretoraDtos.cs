namespace LojaApi.Application.Corretora;

public record SeguradoraDto(Guid Id, string Nome, bool Ativa);

public record OportunidadeDto(
    Guid Id, Guid ClienteId, string ClienteNome, string ClienteTelefone, string? SeguradoraNome,
    string? PlanoDesejado, decimal? ValorEstimado, string Etapa, int Ordem, string? Observacao,
    int? QuantidadeVidas, DateTime CriadoEm);

public record OportunidadeAbertaDto(Guid Id, string Etapa, string? PlanoDesejado);

public record ApoliceDto(
    Guid Id, Guid ClienteId, string ClienteNome, string ClienteTelefone, string SeguradoraNome,
    string NomePlano, string? NumeroApolice, decimal ValorPremio, decimal ValorComissao, decimal? PercentualComissao,
    DateTime VigenciaInicio, DateTime VigenciaFim, string Status, int DiasParaVencer);

public record FunilEtapaDto(string Etapa, int Qtd);
