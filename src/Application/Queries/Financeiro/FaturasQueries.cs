namespace LojaApi.Application.Financeiro;

// Consultas do Financeiro (fatura do cartão): só leem (cada uma tem o seu handler em Application/Handlers/Financeiro).
// LojaId nulo = usuário sem vínculo com loja.

public record VerFaturaQuery(Guid? LojaId, Guid CartaoId, int Ano, int Mes);
public record FaturasReferenciaQuery(Guid? LojaId, Guid CartaoId);
