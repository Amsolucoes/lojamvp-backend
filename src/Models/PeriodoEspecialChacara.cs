using LojaApi.Models;

namespace LojaApi.src.Models;

public class PeriodoEspecialChacara
{
    public int Id { get; set; }
    public Guid LojaId { get; set; }

    public string Nome { get; set; } = string.Empty; // ex: "Natal", "Ano Novo"
    public DateTime DataInicio { get; set; }
    public DateTime DataFim { get; set; }
    public decimal ValorTotal { get; set; } // valor do período INTEIRO como cadastrado

    public Loja? Loja { get; set; }
}