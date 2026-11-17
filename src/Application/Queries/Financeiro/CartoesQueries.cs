namespace LojaApi.Application.Financeiro;

// Consultas do Financeiro (cartões): só leem (cada uma tem o seu handler em Application/Handlers/Financeiro).
// LojaId nulo = usuário sem vínculo com loja.

public record ListarCartoesQuery(Guid? LojaId);
public record ListarCartaoFixosQuery(Guid? LojaId, Guid CartaoId);
public record ListarDescricoesRecentesCartaoQuery(Guid? LojaId);
