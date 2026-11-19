namespace LojaApi.Application.Bonificacoes;

// Consultas de Bonificações: só leem (cada uma tem o seu handler em Application/Handlers/Bonificacoes).

public record ListarBonificacoesQuery(Guid? LojaId, Guid? ClienteId, Guid? ProdutoId, DateTime? De, DateTime? Ate, bool IncluirEstornadas);
public record ResumoBonificacoesQuery(Guid? LojaId, DateTime? De, DateTime? Ate);
