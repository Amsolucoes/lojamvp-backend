using LojaApi.src.Models;

namespace LojaApi.src.Services;

public static class CalculadoraPrecoChacara
{
    public record ResultadoCalculo(decimal ValorEstadia, decimal ValorTaxaLimpeza, decimal ValorTotal, List<string> Detalhamento);

    public static ResultadoCalculo Calcular(DateTime dataInicio, DateTime dataFim, int pessoas, ConfiguracaoPrecoChacara cfg, List<PeriodoEspecialChacara>? periodosEspeciais = null)
    {
        var dias = new List<DateTime>();
        for (var d = dataInicio.Date; d <= dataFim.Date; d = d.AddDays(1))
            dias.Add(d);

        bool grande = pessoas > cfg.LimitePessoasPacotePequeno;
        decimal total = 0;
        var detalhamento = new List<string>();
        periodosEspeciais ??= new List<PeriodoEspecialChacara>();

        // Encontra, para um dia específico, o período especial que o contém (se houver)
        PeriodoEspecialChacara? PeriodoDoDia(DateTime dia) =>
            periodosEspeciais.FirstOrDefault(p => dia >= p.DataInicio.Date && dia <= p.DataFim.Date);

        int i = 0;
        while (i < dias.Count)
        {
            var atual = dias[i];
            var periodoEspecial = PeriodoDoDia(atual);

            if (periodoEspecial != null)
            {
                // Dia dentro de período especial: valor proporcional (valor total do período / dias do período)
                var diasDoPeriodo = (int)Math.Round((periodoEspecial.DataFim.Date - periodoEspecial.DataInicio.Date).TotalDays) + 1;
                var valorProporcional = Math.Round(periodoEspecial.ValorTotal / diasDoPeriodo, 2);
                total += valorProporcional;
                detalhamento.Add($"{atual:dd/MM} ({periodoEspecial.Nome}): {valorProporcional:C}");
                i += 1;
                continue;
            }

            bool temProximo = i + 1 < dias.Count;
            var proximo = temProximo ? dias[i + 1] : default;

            // Só forma par de fim de semana se o próximo dia também NÃO for período especial
            bool proximoEhEspecial = temProximo && PeriodoDoDia(proximo) != null;
            bool ehParFimSemana = temProximo && !proximoEhEspecial && EhParFimSemana(atual, proximo);

            if (ehParFimSemana)
            {
                var valorPacote = grande ? cfg.ValorPacote2DiasFimSemanaGrande : cfg.ValorPacote2DiasFimSemana;
                total += valorPacote;
                detalhamento.Add($"{atual:dd/MM} + {proximo:dd/MM} (pacote fim de semana): {valorPacote:C}");
                i += 2;
            }
            else
            {
                var ehFimSemana = EhDiaFimSemana(atual);
                var valorDia = ehFimSemana
                    ? (grande ? cfg.ValorDiariaFimSemanaGrande : cfg.ValorDiariaFimSemana)
                    : cfg.ValorDiariaSemana;
                total += valorDia;
                detalhamento.Add($"{atual:dd/MM} ({(ehFimSemana ? "fim de semana" : "semana")}): {valorDia:C}");
                i += 1;
            }
        }

        decimal taxaLimpeza = grande ? cfg.ValorTaxaLimpeza : 0;
        if (taxaLimpeza > 0)
            detalhamento.Add($"Taxa de limpeza (mais de {cfg.LimitePessoasPacotePequeno} pessoas): {taxaLimpeza:C}");

        return new ResultadoCalculo(total, taxaLimpeza, total + taxaLimpeza, detalhamento);
    }

    private static bool EhDiaFimSemana(DateTime d) =>
        d.DayOfWeek == DayOfWeek.Friday || d.DayOfWeek == DayOfWeek.Saturday || d.DayOfWeek == DayOfWeek.Sunday;

    private static bool EhParFimSemana(DateTime a, DateTime b) =>
        (a.DayOfWeek == DayOfWeek.Friday && b.DayOfWeek == DayOfWeek.Saturday) ||
        (a.DayOfWeek == DayOfWeek.Saturday && b.DayOfWeek == DayOfWeek.Sunday);
}