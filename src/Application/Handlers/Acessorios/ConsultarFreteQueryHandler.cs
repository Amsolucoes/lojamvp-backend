using LojaApi.Domain.Acessorios;
using LojaApi.Shared.Application;
using LojaApi.src.Models;
using static LojaApi.Application.Acessorios.AcessoriosApoio;

namespace LojaApi.Application.Acessorios;

// Frete pela UF e pelo subtotal (rota /api/loja-acessorios/produtos/frete), com o indicador de frete grátis.
public class ConsultarFreteQueryHandler : IQueryHandler<ConsultarFreteQuery>
{
    public Task<Resultado<object>> HandleAsync(ConsultarFreteQuery query)
    {
        var valor = AcessoriosRegras.CalcularFrete(query.Subtotal, query.Uf.ToUpper());
        return Task.FromResult(Resultado.Ok(new { valorFrete = valor, gratis = valor == 0 }));
    }
}
