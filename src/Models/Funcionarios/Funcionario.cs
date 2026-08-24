using LojaApi.Models;
using LojaApi.src.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LojaApi.src.Models.Funcionarios;

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
    public string TipoRemuneracao { get; set; } = "comissao"; // comissao | salario_fixo | diaria

    [Column(TypeName = "decimal(10,2)")]
    public decimal? SalarioFixo { get; set; }

    // Valor da diária, usado quando TipoRemuneracao = "diaria" — pago junto com a comissão
    // no fechamento (ex: R$100/dia + comissão sobre serviço), sem virar lançamento fixo mensal.
    [Column(TypeName = "decimal(10,2)")]
    public decimal? ValorDiaria { get; set; }

    // Base de cálculo da comissão em Ordem de Serviço: "total" (peça+serviço, comportamento
    // padrão) ou "servico" (só mão de obra, ignora peça). Não afeta comissão de Agendamento,
    // que já é só sobre o preço do serviço em si.
    [MaxLength(20)]
    public string ComissaoBaseCalculo { get; set; } = "total";

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

    // Origem da comissão — "agendamento" (Serviços/Barbearia) ou "ordem_servico" (Ordem de Serviço)
    [MaxLength(20)]
    public string OrigemTipo { get; set; } = "agendamento";
    public Guid OrigemId { get; set; } // aponta pro Agendamento.Id OU OrcamentoServico.Id, conforme OrigemTipo

    // Mantido por compatibilidade com código/consultas existentes que usam AgendamentoId direto — opcional agora
    public Guid? AgendamentoId { get; set; }
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
    public decimal ValorTotal { get; set; } // comissão + diárias, já somado
    public int QtdAtendimentos { get; set; }

    // Diárias incluídas manualmente neste fechamento (funcionário tipo "diaria")
    public int QtdDiarias { get; set; } = 0;
    [Column(TypeName = "decimal(10,2)")]
    public decimal ValorDiarias { get; set; } = 0;

    public DateTime PagoEm { get; set; } = DateTime.UtcNow;
    // Vínculo opcional com o Financeiro (lançamento "a pagar" gerado, se a loja tiver módulo Financeiro)
    public Guid? LancamentoFinanceiroId { get; set; }
}