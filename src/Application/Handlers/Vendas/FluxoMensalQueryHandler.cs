using LojaApi.Domain.Vendas;
using LojaApi.Shared.Application;

namespace LojaApi.Application.Vendas;

// Vendas mês a mês de um ano.
public class FluxoMensalQueryHandler(IVendasRepository repositorio) : IQueryHandler<FluxoMensalQuery>
{
    public async Task<Resultado<object>> HandleAsync(FluxoMensalQuery query)
    {
        var (lojaId, ano) = query;
        var de  = new DateTime(ano, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var ate = de.AddYears(1);

        var vendas = await repositorio.ListarVendasEntreAsync(lojaId, de, ate);

        var meses = Enumerable.Range(1, 12)
            .Select(m =>
            {
                var vs = vendas.Where(v => v.CriadaEm.Month == m).ToList();
                return new FluxoMesDto(
                    Mes:       m,
                    NomeMes:   VendasRegras.NomesDosMeses[m - 1],
                    QtdVendas: vs.Count,
                    Entradas:  vs.Sum(v => v.TotalFinal),
                    Descontos: vs.Sum(v => v.Desconto)
                );
            })
            .ToList();

        return Resultado.Ok(meses);
    }
}
