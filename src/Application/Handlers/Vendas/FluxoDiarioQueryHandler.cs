using LojaApi.Domain.Vendas;
using LojaApi.Shared.Application;

namespace LojaApi.Application.Vendas;

// Vendas dia a dia de um mês.
public class FluxoDiarioQueryHandler(IVendasRepository repositorio) : IQueryHandler<FluxoDiarioQuery>
{
    public async Task<Resultado<object>> HandleAsync(FluxoDiarioQuery query)
    {
        var (lojaId, mes, ano) = query;
        var de  = new DateTime(ano, mes, 1, 0, 0, 0, DateTimeKind.Utc);
        var ate = de.AddMonths(1);

        var vendas = await repositorio.ListarVendasEntreAsync(lojaId, de, ate);

        var dias = Enumerable.Range(1, DateTime.DaysInMonth(ano, mes))
            .Select(d =>
            {
                var data = new DateTime(ano, mes, d);
                var vs   = vendas.Where(v => v.CriadaEm.Date == data.Date).ToList();
                return new FluxoDiaDto(
                    Data:       data.ToString("dd/MM"),
                    QtdVendas:  vs.Count,
                    Entradas:   vs.Sum(v => v.TotalFinal),
                    Descontos:  vs.Sum(v => v.Desconto)
                );
            })
            .ToList();

        return Resultado.Ok(dias);
    }
}
