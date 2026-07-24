using LojaApi.Models;

namespace LojaApi.src.Models;

public class ConfiguracaoPrecoChacara
{
    public int Id { get; set; }
    public Guid LojaId { get; set; }

    public int MinimoPessoas { get; set; } = 3;                    // reserva não pode ser feita com menos que isso
    public int LimitePessoasParaTaxaLimpeza { get; set; } = 50;    // acima disso, cobra a taxa de limpeza
    public decimal ValorTaxaLimpeza { get; set; } = 250m;
    public decimal ValorMultaNaoLimpeza { get; set; } = 250m;      // só informativo, avisado no checkout
    public decimal PercentualEntradaMinimo { get; set; } = 50m;    // sugestão de % mínimo de entrada, não é travado no backend

    public Loja? Loja { get; set; }
}