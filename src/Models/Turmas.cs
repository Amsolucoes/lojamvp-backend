using LojaApi.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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

// ── Profissional que atende os alunos nas sessões (Turmas) ou clientes (Serviços/Barbearia) ──
public class Profissional
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid LojaId { get; set; }
    [Required, MaxLength(100)]
    public string Nome { get; set; } = "";
    public bool Ativo { get; set; } = true;
    // Comissão padrão (%), usada quando não há exceção específica por serviço
    [Column(TypeName = "decimal(5,2)")]
    public decimal? ComissaoPadraoPercentual { get; set; }
    public int? DiaPagamentoPadrao { get; set; } // dia do mês sugerido pra pagar comissão ou salário (ex: 5)

    [MaxLength(20)]
    public string TipoRemuneracao { get; set; } = "comissao"; // comissao | salario_fixo

    [Column(TypeName = "decimal(10,2)")]
    public decimal? SalarioFixo { get; set; }

    [MaxLength(20)]
    public string? Telefone { get; set; }

    [MaxLength(9)]
    public string? Cep { get; set; }

    [MaxLength(200)]
    public string? Endereco { get; set; }

    // Vínculo com o lançamento fixo gerado no Financeiro, se TipoRemuneracao = salario_fixo
    public Guid? LancamentoFixoId { get; set; }

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    // Navegação
    public ICollection<ComissaoServicoProfissional> ComissoesPorServico { get; set; } = [];
}

// ── Exceção de comissão: percentual diferente pra um serviço específico ──
public class ComissaoServicoProfissional
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid LojaId { get; set; }
    public Guid ProfissionalId { get; set; }
    public Profissional? Profissional { get; set; }
    public Guid ServicoId { get; set; }
    [Column(TypeName = "decimal(5,2)")]
    public decimal ComissaoPercentual { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}

// ── Lançamento de comissão gerado ao concluir um atendimento ────────
public class ComissaoFuncionario
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid LojaId { get; set; }
    public Guid ProfissionalId { get; set; }
    public Profissional? Profissional { get; set; }
    public Guid AgendamentoId { get; set; }
    public Agendamento? Agendamento { get; set; }
    [Column(TypeName = "decimal(10,2)")]
    public decimal ValorServico { get; set; } // preço do atendimento no momento da conclusão
    [Column(TypeName = "decimal(5,2)")]
    public decimal ComissaoPercentual { get; set; } // percentual aplicado (registrado, caso mude depois)
    [Column(TypeName = "decimal(10,2)")]
    public decimal ValorComissao { get; set; }
    [MaxLength(20)]
    public string Status { get; set; } = "pendente"; // pendente | pago
    public Guid? FechamentoId { get; set; } // agrupa comissões pagas juntas num fechamento
    public DateTime? PagoEm { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}

// ── Fechamento de comissão: agrupa e paga várias comissões de um período ──
public class FechamentoComissao
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid LojaId { get; set; }
    public Guid ProfissionalId { get; set; }
    public Profissional? Profissional { get; set; }
    public DateTime PeriodoInicio { get; set; }
    public DateTime PeriodoFim { get; set; }
    [Column(TypeName = "decimal(10,2)")]
    public decimal ValorTotal { get; set; }
    public int QtdAtendimentos { get; set; }
    public DateTime PagoEm { get; set; } = DateTime.UtcNow;
    // Vínculo opcional com o Financeiro (lançamento "a pagar" gerado, se a loja tiver módulo Financeiro)
    public Guid? LancamentoFinanceiroId { get; set; }
}