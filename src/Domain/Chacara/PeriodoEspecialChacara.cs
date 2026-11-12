using LojaApi.Models;

namespace LojaApi.Domain.Chacara;

public class PeriodoEspecialChacara
{
    public int Id { get; set; }
    public Guid LojaId { get; set; }

    public string Nome { get; set; } = string.Empty; // ex: "Natal", "Ano Novo"
    public DateTime DataInicio { get; set; }
    public DateTime DataFim { get; set; }
    public decimal ValorTotal { get; set; } // valor do período INTEIRO como cadastrado

    public Loja? Loja { get; set; }

    public static string? ValidarDatas(DateTime inicio, DateTime fim) =>
        fim.Date < inicio.Date ? "Data final não pode ser antes da data inicial." : null;

    public static PeriodoEspecialChacara Criar(Guid lojaId, string nome, DateTime inicio, DateTime fim, decimal valorTotal) => new()
    {
        LojaId = lojaId,
        Nome = nome.Trim(),
        DataInicio = DiaDeReserva.MeioDiaUtc(inicio),
        DataFim = DiaDeReserva.MeioDiaUtc(fim),
        ValorTotal = valorTotal,
    };

    public void Atualizar(string nome, DateTime inicio, DateTime fim, decimal valorTotal)
    {
        Nome = nome.Trim();
        DataInicio = DiaDeReserva.MeioDiaUtc(inicio);
        DataFim = DiaDeReserva.MeioDiaUtc(fim);
        ValorTotal = valorTotal;
    }
}
