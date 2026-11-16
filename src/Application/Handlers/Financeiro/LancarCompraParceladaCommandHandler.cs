using LojaApi.Domain.Financeiro;
using LojaApi.Models;
using LojaApi.Shared.Application;

namespace LojaApi.Application.Financeiro;

// Lança uma compra parcelada no cartão (ex.: TV em 10x): uma parcela por mês, no mesmo grupo.
public class LancarCompraParceladaCommandHandler(ICartoesRepository repositorio) : ICommandHandler<LancarCompraParceladaCommand>
{
    public async Task<Resultado<object>> HandleAsync(LancarCompraParceladaCommand command)
    {
        var (lojaIdOuNulo, cartaoId, req) = command;
        if (lojaIdOuNulo is not { } lojaId) return Resultado.NaoEncontrado();
        var cartao = await repositorio.ObterCartaoAsync(cartaoId, lojaId);
        if (cartao is null) return Resultado.NaoEncontrado();

        if (!CartaoRegras.TotalDeParcelasDaCompraValido(req.TotalParcelas))
            return Resultado.Invalido("Escolha entre 2 e 24 parcelas.");

        var grupoId = Guid.NewGuid();
        var dataBase = LancamentosRegras.AoMeioDia(req.DataCompra);

        for (int i = 0; i < req.TotalParcelas; i++)
        {
            repositorio.Adicionar(new LancamentoCartao
            {
                LojaId = lojaId,
                CartaoCreditoId = cartaoId,
                Descricao = $"{req.Descricao.Trim()} ({i + 1}/{req.TotalParcelas})",
                Valor = req.ValorParcela,
                DataCompra = dataBase.AddMonths(i),
                Modo = "parcelada",
                GrupoParcelamentoId = grupoId,
                NumeroParcela = i + 1,
                TotalParcelas = req.TotalParcelas,
                CategoriaId = req.CategoriaId,
                Observacao = req.Observacao,
            });
        }

        await repositorio.SalvarAsync();
        return Resultado.Ok(new { grupoParcelamentoId = grupoId, totalGerado = req.TotalParcelas });
    }
}
