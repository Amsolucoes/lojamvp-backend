using LojaApi.Domain.Financeiro;
using LojaApi.Models;
using LojaApi.Shared.Application;

namespace LojaApi.Application.Financeiro;

// Cria uma compra fixa/recorrente no cartão (ex.: Netflix) e gera 60 meses de compras a partir do mês atual.
public class CriarCartaoFixoCommandHandler(ICartoesRepository repositorio, IGeradorDeComprasFixas compras) : ICommandHandler<CriarCartaoFixoCommand>
{
    public async Task<Resultado<object>> HandleAsync(CriarCartaoFixoCommand command)
    {
        var (lojaIdOuNulo, cartaoId, req) = command;
        if (lojaIdOuNulo is not { } lojaId) return Resultado.NaoEncontrado();
        var cartao = await repositorio.ObterCartaoAsync(cartaoId, lojaId);
        if (cartao is null) return Resultado.NaoEncontrado();

        var fixo = new CartaoLancamentoFixo
        {
            LojaId = lojaId,
            CartaoCreditoId = cartaoId,
            Descricao = req.Descricao.Trim(),
            Valor = req.Valor,
            CategoriaId = req.CategoriaId,
            Observacao = req.Observacao,
            DiaCompra = CartaoRegras.DiaDaCompraFixa(req.DiaCompra),
        };
        repositorio.Adicionar(fixo);
        await repositorio.SalvarAsync();

        await compras.GerarLoteAsync(fixo, cartao, LancamentosApoio.MesAtual());
        await repositorio.SalvarAsync();

        return Resultado.Ok(new { fixo.Id, geradoAte = fixo.GeradoAte });
    }
}
