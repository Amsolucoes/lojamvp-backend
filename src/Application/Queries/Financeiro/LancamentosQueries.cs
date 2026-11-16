namespace LojaApi.Application.Financeiro;

// Consultas do Financeiro (lançamentos e parcelas): só leem (cada uma tem o seu handler em Application/Handlers/Financeiro).
// LojaId nulo = usuário sem vínculo com loja.

public record ReceberUnificadoQuery(Guid? LojaId, DateTime? De, DateTime? Ate);
public record ListarParcelasQuery(Guid? LojaId, Guid Id);
public record ListarDescricoesRecentesQuery(Guid? LojaId, string Tipo);
