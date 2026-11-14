using LojaApi.Domain.Acessorios;
using LojaApi.Shared.Application;
using LojaApi.src.Models;
using static LojaApi.Application.Acessorios.AcessoriosApoio;

namespace LojaApi.Application.Acessorios;

// Frete pela UF (rota /api/loja-acessorios/frete), sem olhar o subtotal.
public class CalcularFreteQueryHandler : IQueryHandler<CalcularFreteQuery>
{
    public Task<Resultado<object>> HandleAsync(CalcularFreteQuery query)
    {
        var valor = AcessoriosRegras.FreteDaUf(query.Uf.ToUpper());
        return Task.FromResult(Resultado.Ok(new { valorFrete = valor }));
    }
}
