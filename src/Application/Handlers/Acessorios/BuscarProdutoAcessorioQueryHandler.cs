using LojaApi.Domain.Acessorios;
using LojaApi.Shared.Application;
using LojaApi.src.Models;
using static LojaApi.Application.Acessorios.AcessoriosApoio;

namespace LojaApi.Application.Acessorios;

// Detalhe público de um produto ativo. Antes, libera o estoque de pedidos vencidos.
public class BuscarProdutoAcessorioQueryHandler(IAcessoriosRepository repositorio) : IQueryHandler<BuscarProdutoAcessorioQuery>
{
    public async Task<Resultado<object>> HandleAsync(BuscarProdutoAcessorioQuery query)
    {
        await LiberarPedidosExpiradosAsync(repositorio);

        var p = await repositorio.ObterProdutoAtivoAsync(query.Id);
        if (p is null) return Resultado.NaoEncontrado();

        var limiteNovo = AcessoriosRegras.LimiteProdutoNovo(DateTime.UtcNow);
        return Resultado.Ok(new
        {
            p.Id,
            p.Nome,
            p.Descricao,
            p.Preco,
            p.PrecoPromocional,
            p.Categoria,
            p.ImagensUrls,
            p.PesoKg,
            disponivel = p.Estoque > 0,
            estoque = p.Estoque, // opcional exibir quantidade exata; remova se preferir só "disponível/indisponível"
            destaque = p.Destaque,
            novo = p.CriadoEm >= limiteNovo,
        });
    }
}
