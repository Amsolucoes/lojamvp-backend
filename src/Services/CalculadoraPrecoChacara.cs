using LojaApi.src.Models;

namespace LojaApi.src.Services;

public static class CalculadoraPrecoChacara
{
    public record ResultadoCalculo(decimal ValorEstadia, decimal ValorTaxaLimpeza, decimal ValorTotal, List<string> Detalhamento);

    public static ResultadoCalculo Calcular(
        DateTime dataInicio, DateTime dataFim, int pessoas,
        ConfiguracaoPrecoChacara cfg, List<FaixaPrecoChacara> faixas,
        List<PeriodoEspecialChacara>? periodosEspeciais = null)
    {
        periodosEspeciais ??= new List<PeriodoEspecialChacara>();

        var faixasOrdenadas = faixas.OrderBy(f => f.PessoasAte).ToList();
        var faixa = faixasOrdenadas.FirstOrDefault(f => pessoas <= f.PessoasAte) ?? faixasOrdenadas.LastOrDefault();
        if (faixa is null)
            throw new InvalidOperationException("Nenhuma faixa de preço configurada para esta chácara.");

        var matchExato = periodosEspeciais.FirstOrDefault(p =>
            p.DataInicio.Date == dataInicio.Date && p.DataFim.Date == dataFim.Date);

        if (matchExato != null)
        {
            var taxaLimpezaExata = pessoas > cfg.LimitePessoasParaTaxaLimpeza ? cfg.ValorTaxaLimpeza : 0;
            var detalhamentoExato = new List<string> { $"{matchExato.Nome} (pacote fechado): {matchExato.ValorTotal:C}" };
            if (taxaLimpezaExata > 0)
                detalhamentoExato.Add($"Taxa de limpeza (mais de {cfg.LimitePessoasParaTaxaLimpeza} pessoas): {taxaLimpezaExata:C}");
            return new ResultadoCalculo(matchExato.ValorTotal, taxaLimpezaExata, matchExato.ValorTotal + taxaLimpezaExata, detalhamentoExato);
        }

        var dias = new List<DateTime>();
        for (var d = dataInicio.Date; d <= dataFim.Date; d = d.AddDays(1))
            dias.Add(d);

        decimal total = 0;
        var detalhamento = new List<string>();

        PeriodoEspecialChacara? PeriodoDoDia(DateTime dia) =>
            periodosEspeciais
                .Where(p => dia >= p.DataInicio.Date && dia <= p.DataFim.Date)
                .OrderBy(p => (p.DataFim.Date - p.DataInicio.Date).TotalDays)
                .FirstOrDefault();

        int i = 0;
        while (i < dias.Count)
        {
            var atual = dias[i];
            var periodoEspecial = PeriodoDoDia(atual);

            if (periodoEspecial != null)
            {
                var diasDoPeriodo = (int)Math.Round((periodoEspecial.DataFim.Date - periodoEspecial.DataInicio.Date).TotalDays) + 1;
                var valorProporcional = Math.Round(periodoEspecial.ValorTotal / diasDoPeriodo, 2);
                total += valorProporcional;
                detalhamento.Add($"{atual:dd/MM} ({periodoEspecial.Nome}): {valorProporcional:C}");
                i += 1;
                continue;
            }

            bool temProximo = i + 1 < dias.Count;
            var proximo = temProximo ? dias[i + 1] : default;
            bool proximoEhEspecial = temProximo && PeriodoDoDia(proximo) != null;
            bool ehParFimSemana = temProximo && !proximoEhEspecial && EhParFimSemana(atual, proximo);

            if (ehParFimSemana)
            {
                total += faixa.ValorPacote2DiasFimSemana;
                detalhamento.Add($"{atual:dd/MM} + {proximo:dd/MM} (pacote fim de semana): {faixa.ValorPacote2DiasFimSemana:C}");
                i += 2;
            }
            else
            {
                var ehFimSemana = EhDiaFimSemana(atual);
                var valorDia = ehFimSemana ? faixa.ValorDiariaFimSemana : faixa.ValorDiariaSemana;
                total += valorDia;
                detalhamento.Add($"{atual:dd/MM} ({(ehFimSemana ? "fim de semana" : "semana")}): {valorDia:C}");
                i += 1;
            }
        }

        decimal taxaLimpeza = pessoas > cfg.LimitePessoasParaTaxaLimpeza ? cfg.ValorTaxaLimpeza : 0;
        if (taxaLimpeza > 0)
            detalhamento.Add($"Taxa de limpeza (mais de {cfg.LimitePessoasParaTaxaLimpeza} pessoas): {taxaLimpeza:C}");

        return new ResultadoCalculo(total, taxaLimpeza, total + taxaLimpeza, detalhamento);
    }

    private static bool EhDiaFimSemana(DateTime d) =>
        d.DayOfWeek == DayOfWeek.Friday || d.DayOfWeek == DayOfWeek.Saturday || d.DayOfWeek == DayOfWeek.Sunday;

    private static bool EhParFimSemana(DateTime a, DateTime b) =>
        (a.DayOfWeek == DayOfWeek.Friday && b.DayOfWeek == DayOfWeek.Saturday) ||
        (a.DayOfWeek == DayOfWeek.Saturday && b.DayOfWeek == DayOfWeek.Sunday);
}