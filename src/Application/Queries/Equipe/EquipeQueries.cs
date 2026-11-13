namespace LojaApi.Application.Equipe;

// Consultas da Equipe: só leem dados (cada uma tem o seu handler em Application/Handlers/Equipe).

public record ListarFuncionariosQuery(Guid? LojaId);
public record ListarFuncionariosAtivosQuery(Guid? LojaId);
public record ResumoComissoesQuery(Guid? LojaId, DateTime? De, DateTime? Ate);
public record DetalheComissoesProfissionalQuery(Guid? LojaId, Guid ProfissionalId, string Status);
public record ListarFechamentosQuery(Guid? LojaId, Guid? ProfissionalId);
