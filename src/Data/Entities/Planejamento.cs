using System.ComponentModel.DataAnnotations;

namespace LojaApi.Models;

// Planejamento financeiro mensal da loja (módulo financeiro): metas de gasto por categoria, receita esperada e % a guardar.
public class PlanejamentoFinanceiro
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid LojaId { get; set; }

    // Quanto a loja pretende receber no mês. Vazio = usa o que já está lançado como "a receber" no mês.
    public decimal? ReceitaPlanejada { get; set; }
    public decimal PercentualPoupanca { get; set; } = 30m;
}

// Quanto se quer gastar por mês numa categoria (o mesmo teto vale para todos os meses).
public class PlanejamentoCategoria
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid LojaId { get; set; }
    public Guid CategoriaId { get; set; }
    // pagar = teto de gasto | receber = meta de receita (uma categoria 'ambos' pode ter os dois)
    public string Tipo { get; set; } = "pagar";
    public decimal Valor { get; set; }
    public bool Ativo { get; set; } = true;
}

// Meta de guardar dinheiro (férias, carro novo, reserva...).
public class MetaPlanejamento
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid LojaId { get; set; }

    [Required, MaxLength(80)]
    public string Nome { get; set; } = "";

    [MaxLength(4)]
    public string? Icone { get; set; }

    public decimal ValorAlvo { get; set; }
    public decimal ValorGuardado { get; set; }
    public DateTime? DataAlvo { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}
