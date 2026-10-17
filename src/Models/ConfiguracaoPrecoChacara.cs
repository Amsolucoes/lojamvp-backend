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

    // Pagamento no cartão (por chácara, pois cada uma usa a própria conta do Mercado Pago)
    public decimal PercentualTaxaCartao { get; set; } = 5m;        // % somado ao valor cobrado no cartão para repassar a taxa
    public decimal ValorLimiteParcelas { get; set; } = 800m;       // até este valor vale ParcelasAteLimite
    public int ParcelasAteLimite { get; set; } = 2;
    public int ParcelasAcimaLimite { get; set; } = 3;

    public Loja? Loja { get; set; }
}