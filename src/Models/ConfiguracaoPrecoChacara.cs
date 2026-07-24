using LojaApi.Models;

namespace LojaApi.src.Models;

public class ConfiguracaoPrecoChacara
{
    public int Id { get; set; }
    public Guid LojaId { get; set; }

    public decimal ValorDiariaSemana { get; set; } = 350m;          // seg-qui, 1 dia
    public decimal ValorDiariaFimSemana { get; set; } = 500m;        // sex-dom, 1 dia, até 50 pessoas
    public decimal ValorDiariaFimSemanaGrande { get; set; } = 650m;  // sex-dom, 1 dia, 50-100 pessoas
    public decimal ValorPacote2DiasFimSemana { get; set; } = 800m;       // sex+sab ou sab+dom, até 50 pessoas
    public decimal ValorPacote2DiasFimSemanaGrande { get; set; } = 1000m; // sex+sab ou sab+dom, 50-100 pessoas

    public int LimitePessoasPacotePequeno { get; set; } = 50; // acima disso usa os valores "Grande"
    public int MinimoPessoas { get; set; } = 3;               // reserva não pode ser feita com menos que isso
    public decimal ValorTaxaLimpeza { get; set; } = 250m;     // cobrada junto se pessoas > este limite
    public decimal ValorMultaNaoLimpeza { get; set; } = 250m; // só informativo, avisado no checkout
    public decimal PercentualEntradaMinimo { get; set; } = 50m; // sugestão de % mínimo de entrada, não é travado no backend

    public Loja? Loja { get; set; }
}