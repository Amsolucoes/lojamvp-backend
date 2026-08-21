using LojaApi.src.Models.Funcionarios;
using System.ComponentModel.DataAnnotations;

namespace LojaApi.src.Models;

// ── Turma (a aula recorrente em si — ex: "Pilates Solo — Segunda 8h") ──
public class Turma
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid LojaId { get; set; }

    [Required, MaxLength(100)]
    public string Nome { get; set; } = "";

    public int DiaSemana { get; set; } // 0=domingo ... 6=sábado
    public TimeSpan Horario { get; set; }
    public int DuracaoMin { get; set; } = 60;
    public int Capacidade { get; set; } = 8;

    public bool Ativa { get; set; } = true;
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}

// ── Matrícula fixa do aluno numa turma ──────────────────────────────
public class MatriculaTurma
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid LojaId { get; set; }
    public Guid TurmaId { get; set; }
    public Turma? Turma { get; set; }
    public Guid ClienteId { get; set; }

    public bool Ativa { get; set; } = true;
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}

// ── Instância concreta de uma turma num dia específico ──────────────
public class SessaoTurma
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid LojaId { get; set; }
    public Guid TurmaId { get; set; }
    public Turma? Turma { get; set; }

    public DateTime DataHora { get; set; }

    [MaxLength(20)]
    public string Status { get; set; } = "aberta"; // aberta | realizada | cancelada

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}

// ── Presença/vaga de um aluno numa sessão específica ─────────────────
public class InscricaoSessao
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid LojaId { get; set; }
    public Guid SessaoTurmaId { get; set; }
    public SessaoTurma? SessaoTurma { get; set; }
    public Guid ClienteId { get; set; }

    [MaxLength(20)]
    public string Tipo { get; set; } = "fixo"; // fixo | remarcacao

    [MaxLength(20)]
    public string Status { get; set; } = "confirmado"; // confirmado | falta_avisada | compareceu | faltou

    // Se essa vaga foi remarcada, aponta pra sessão de destino
    public Guid? RemarcadoParaSessaoId { get; set; }

    // Profissional que atende esse aluno NESSA sessão específica — pode variar semana a semana
    public Guid? ProfissionalId { get; set; }
    public Profissional? Profissional { get; set; }

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}