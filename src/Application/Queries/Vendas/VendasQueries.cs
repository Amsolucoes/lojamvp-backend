namespace LojaApi.Application.Vendas;

// Consultas de Vendas: só leem dados (cada uma tem o seu handler em Application/Handlers/Vendas).

public record ListarVendasQuery(Guid? LojaId, DateTime? De, DateTime? Ate, Guid? ClienteId);
public record BuscarVendaQuery(Guid? LojaId, Guid Id);

public record ListarTrocasQuery(Guid? LojaId);

public record ListarMovimentosCaixaQuery(Guid? LojaId, DateTime? De, DateTime? Ate);

public record ListarOrigensVendaQuery(Guid? LojaId);
public record ListarTodasOrigensVendaQuery(Guid? LojaId);

public record ResumoVendasQuery(Guid? LojaId, DateTime? De, DateTime? Ate);
public record ProdutosRankingQuery(Guid? LojaId, DateTime? De, DateTime? Ate, int Top);
public record FluxoDiarioQuery(Guid? LojaId, int Mes, int Ano);
public record FluxoMensalQuery(Guid? LojaId, int Ano);
