namespace LojaApi.Application.OrdemServico;

// Consultas de OrdemServico: só leem dados (cada uma tem o seu handler em Application/Handlers/OrdemServico).

public record ListarChecklistCategoriasQuery(Guid? LojaId);

public record ListarOrcamentosQuery(Guid? LojaId, string? Status, string? Placa);
public record BuscarOrcamentoQuery(Guid? LojaId, Guid Id);
public record ListarOrcamentosDoClienteQuery(Guid? LojaId, Guid ClienteId);
public record ResumoClientesOrdemQuery(Guid? LojaId);
public record BaixarOrcamentoPdfQuery(Guid? LojaId, Guid Id);

public record ListarItensPatrimonioQuery(Guid? LojaId, bool IncluirInativos);
public record ListarContagensPatrimonioQuery(Guid? LojaId);
public record BuscarContagemPatrimonioQuery(Guid? LojaId, Guid Id);
public record RelatorioPatrimonioPdfQuery(Guid? LojaId);
public record ContagemPatrimonioPdfQuery(Guid? LojaId, Guid Id);
