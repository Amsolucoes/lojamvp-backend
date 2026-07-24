using LojaApi.Models;

namespace LojaApi.src.Models;

public class FaixaPrecoChacara
{
    public int Id { get; set; }
    public Guid LojaId { get; set; }

    public int PessoasAte { get; set; }              // limite superior da faixa (ex: 50, 100, 200, 500, 1000)
    public decimal ValorDiariaSemana { get; set; }    // seg-qui, 1 dia
    public decimal ValorDiariaFimSemana { get; set; } // sex-dom, 1 dia
    public decimal ValorPacote2DiasFimSemana { get; set; } // sex+sáb ou sáb+dom

    public Loja? Loja { get; set; }
}