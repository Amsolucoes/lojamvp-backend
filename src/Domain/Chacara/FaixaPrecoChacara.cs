using LojaApi.Models;

namespace LojaApi.Domain.Chacara;

public class FaixaPrecoChacara
{
    public int Id { get; set; }
    public Guid LojaId { get; set; }

    public int PessoasAte { get; set; }              // limite superior da faixa (ex: 50, 100, 200, 500, 1000)
    public decimal ValorDiariaSemana { get; set; }    // seg-qui, 1 dia
    public decimal ValorDiariaFimSemana { get; set; } // sex-dom, 1 dia
    public decimal ValorPacote2DiasFimSemana { get; set; } // sex+sáb ou sáb+dom

    public Loja? Loja { get; set; }

    public static FaixaPrecoChacara Criar(Guid lojaId, int pessoasAte, decimal semana, decimal fimSemana, decimal pacote2Dias) => new()
    {
        LojaId = lojaId,
        PessoasAte = pessoasAte,
        ValorDiariaSemana = semana,
        ValorDiariaFimSemana = fimSemana,
        ValorPacote2DiasFimSemana = pacote2Dias,
    };

    public void Atualizar(int pessoasAte, decimal semana, decimal fimSemana, decimal pacote2Dias)
    {
        PessoasAte = pessoasAte;
        ValorDiariaSemana = semana;
        ValorDiariaFimSemana = fimSemana;
        ValorPacote2DiasFimSemana = pacote2Dias;
    }
}
