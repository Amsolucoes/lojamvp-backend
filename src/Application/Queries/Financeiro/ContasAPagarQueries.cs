namespace LojaApi.Application.Financeiro;

// Consultas do Financeiro (Contas a Pagar): só leem (cada uma tem o seu handler em Application/Handlers/Financeiro).
// LojaId nulo = usuário sem vínculo com loja.

public record ListarLancamentosQuery(Guid? LojaId, string Tipo, int? Ano, int? Mes);
public record PagarUnificadoQuery(Guid? LojaId, int? Ano, int? Mes, string Modo, DateTime? De, DateTime? Ate);
public record FaturasDoMesQuery(Guid? LojaId, int Ano, int Mes);
