namespace LojaApi.Application.Financeiro;

// Consultas do Financeiro (contas e categorias): só leem dados (cada uma tem o seu handler em Application/Handlers/Financeiro).

public record ListarContasQuery(Guid? LojaId);
public record ListarCategoriasFinanceirasQuery(Guid? LojaId, string? Tipo);
