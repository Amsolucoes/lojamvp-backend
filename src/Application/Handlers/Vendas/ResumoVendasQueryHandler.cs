using LojaApi.Shared.Application;

namespace LojaApi.Application.Vendas;

// Total vendido, descontos, ticket médio e itens vendidos num período (só da loja do usuário).
public class ResumoVendasQueryHandler(IVendasRepository repositorio) : IQueryHandler<ResumoVendasQuery>
{
    public async Task<Resultado<object>> HandleAsync(ResumoVendasQuery query)
    {
        var vendas = await repositorio.ListarVendasComItensDoPeriodoAsync(query.LojaId, query.De, query.Ate);

        return Resultado.Ok(new ResumoVendasDto(
            TotalVendido:       vendas.Sum(v => v.TotalFinal),
            TotalDescontos:     vendas.Sum(v => v.Desconto),
            TicketMedio:        vendas.Count > 0 ? vendas.Sum(v => v.TotalFinal) / vendas.Count : 0,
            TotalItensVendidos: vendas.Sum(v => v.Itens.Sum(i => i.Quantidade)),
            TotalVendas:        vendas.Count
        ));
    }
}
