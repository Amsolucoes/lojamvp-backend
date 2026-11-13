using LojaApi.src.Models;

namespace LojaApi.Application.Turmas;

public record TurmaComAlunosDto(Turma Turma, int QtdAlunos);

public record AlunoDaTurmaDto(Guid Id, Guid ClienteId, string Nome, string Telefone);

public record InscricaoDaSessaoDto(Guid Id, Guid ClienteId, string Nome, string Tipo, string Status,
    Guid? RemarcadoParaSessaoId, Guid? ProfissionalId, string? ProfissionalNome);

public record ProfissionalDaTurmaDto(Guid Id, string Nome);

public record TurmaDaListaDto(Guid Id, string Nome, int DiaSemana, string Horario, int DuracaoMin, int Capacidade, bool Ativa, int QtdAlunos);
