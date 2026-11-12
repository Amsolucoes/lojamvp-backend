using LojaApi.Models;

namespace LojaApi.Modules.Chacara.Domain;

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

    // Regras dos campos opcionais do cartão. Devolve a mensagem de erro, ou null se estiver tudo certo.
    public static string? ValidarCartao(decimal? percentualTaxaCartao, int? parcelasAteLimite, int? parcelasAcimaLimite)
    {
        if (percentualTaxaCartao is < 0 or > 15)
            return "A taxa do cartão deve ficar entre 0% e 15%.";
        if (parcelasAteLimite is < 1 or > 12 || parcelasAcimaLimite is < 1 or > 12)
            return "O número de parcelas deve ficar entre 1 e 12.";
        return null;
    }

    // Os campos do cartão são opcionais: o que não vier, mantém o valor atual.
    public void Atualizar(
        int minimoPessoas, int limitePessoasParaTaxaLimpeza, decimal valorTaxaLimpeza, decimal valorMultaNaoLimpeza,
        decimal percentualEntradaMinimo, decimal? percentualTaxaCartao, decimal? valorLimiteParcelas,
        int? parcelasAteLimite, int? parcelasAcimaLimite)
    {
        MinimoPessoas = minimoPessoas;
        LimitePessoasParaTaxaLimpeza = limitePessoasParaTaxaLimpeza;
        ValorTaxaLimpeza = valorTaxaLimpeza;
        ValorMultaNaoLimpeza = valorMultaNaoLimpeza;
        PercentualEntradaMinimo = percentualEntradaMinimo;
        if (percentualTaxaCartao.HasValue) PercentualTaxaCartao = percentualTaxaCartao.Value;
        if (valorLimiteParcelas.HasValue) ValorLimiteParcelas = valorLimiteParcelas.Value;
        if (parcelasAteLimite.HasValue) ParcelasAteLimite = parcelasAteLimite.Value;
        if (parcelasAcimaLimite.HasValue) ParcelasAcimaLimite = parcelasAcimaLimite.Value;
    }
}
