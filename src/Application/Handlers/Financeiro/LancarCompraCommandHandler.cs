using LojaApi.Domain.Financeiro;
using LojaApi.Models;
using LojaApi.Shared.Application;

namespace LojaApi.Application.Financeiro;

// Lança uma compra avulsa no cartão.
public class LancarCompraCommandHandler(ICartoesRepository repositorio) : ICommandHandler<LancarCompraCommand>
{
    public async Task<Resultado<object>> HandleAsync(LancarCompraCommand command)
    {
        var (lojaIdOuNulo, cartaoId, req) = command;
        if (lojaIdOuNulo is not { } lojaId) return Resultado.NaoEncontrado();
        var cartao = await repositorio.ObterCartaoAsync(cartaoId, lojaId);
        if (cartao is null) return Resultado.NaoEncontrado();

        repositorio.Adicionar(new LancamentoCartao
        {
            LojaId = lojaId,
            CartaoCreditoId = cartaoId,
            Descricao = req.Descricao.Trim(),
            Valor = req.Valor,
            DataCompra = LancamentosRegras.AoMeioDia(req.DataCompra),
            Modo = "avulsa",
            CategoriaId = req.CategoriaId,
            Observacao = req.Observacao,
        });
        await repositorio.SalvarAsync();
        return Resultado.Ok(new { mensagem = "Lançamento adicionado." });
    }
}
