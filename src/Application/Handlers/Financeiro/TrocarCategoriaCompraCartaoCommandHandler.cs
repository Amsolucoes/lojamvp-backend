using LojaApi.Models;
using LojaApi.Shared.Application;

namespace LojaApi.Application.Financeiro;

// Troca a categoria de uma compra do cartão. Cobrança fixa: muda o modelo e todas as compras já geradas dele.
// Parcelada, com "todas as parcelas": muda o grupo todo.
public class TrocarCategoriaCompraCartaoCommandHandler(ICartoesRepository cartoes, IFinanceiroRepository financeiro) : ICommandHandler<TrocarCategoriaCompraCartaoCommand>
{
    public async Task<Resultado<object>> HandleAsync(TrocarCategoriaCompraCartaoCommand command)
    {
        var (lojaIdOuNulo, id, req) = command;
        if (lojaIdOuNulo is not { } lojaId) return Resultado.NaoEncontrado();
        var compra = await cartoes.ObterCompraDaLojaAsync(id, lojaId);
        if (compra is null) return Resultado.NaoEncontrado();

        if (req.CategoriaId.HasValue && await financeiro.ObterCategoriaAsync(req.CategoriaId.Value, lojaId) is null)
            return Resultado.Invalido("Categoria não encontrada.");

        var alvos = new List<LancamentoCartao> { compra };
        if (compra.Modo == "fixa" && compra.CartaoFixoId.HasValue)
        {
            var fixoCartao = await cartoes.ObterFixoPorLojaAsync(compra.CartaoFixoId.Value, lojaId);
            if (fixoCartao is not null) fixoCartao.CategoriaId = req.CategoriaId;
            alvos = await cartoes.ListarComprasDoFixoAsync(compra.CartaoFixoId.Value, lojaId);
        }
        else if (req.TodasParcelas && compra.GrupoParcelamentoId.HasValue)
            alvos = await cartoes.ListarGrupoDeComprasAsync(compra.GrupoParcelamentoId.Value, lojaId);

        foreach (var a in alvos) a.CategoriaId = req.CategoriaId;
        await cartoes.SalvarAsync();
        return Resultado.Ok(new { alterados = alvos.Count });
    }
}
