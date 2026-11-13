namespace LojaApi.Application.Turmas;

public record SalvarTurmaRequest(string Nome, int DiaSemana, string Horario, int DuracaoMin, int Capacidade);

public record MatricularRequest(Guid ClienteId);

public record RemarcarRequest(Guid? SessaoDestinoId); // null = só avisa falta, sem remarcar ainda

public record MarcarPresencaRequest(bool Compareceu);

public record SalvarProfissionalRequest(string Nome);

public record DefinirProfissionalRequest(Guid? ProfissionalId);
