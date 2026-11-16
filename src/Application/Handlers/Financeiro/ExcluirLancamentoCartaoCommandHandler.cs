using LojaApi.Domain.Financeiro;
using LojaApi.Shared.Application;

namespace LojaApi.Application.Financeiro;

// Exclui uma compra de cartão. Modo "todas": num fixo, desativa o modelo e apaga as compras do mês atual em diante; numa parcelada,
// apaga as parcelas de faturas ainda não pagas (as pagas ficam). Qualquer outro modo: só esta compra.
public class ExcluirLancamentoCartaoCommandHandler(ICartoesRepository repositorio) : ICommandHandler<ExcluirLancamentoCartaoCommand>
{
    public async Task<Resultado<object>> HandleAsync(ExcluirLancamentoCartaoCommand command)
    {
        var (lojaIdOuNulo, id, modo) = command;
        if (lojaIdOuNulo is not { } lojaId) return Resultado.NaoEncontrado();
        var item = await repositorio.ObterCompraAsync(id, lojaId);
        if (item is null) return Resultado.NaoEncontrado();

        if (modo == "todas" && item.Modo == "fixa" && item.CartaoFixoId.HasValue)
        {
            var fixo = await repositorio.ObterFixoPorLojaAsync(item.CartaoFixoId.Value, lojaId);
            if (fixo != null) fixo.Ativo = false;

            var futuros = await repositorio.ListarComprasDoFixoAPartirDeAsync(item.CartaoFixoId.Value, lojaId, LancamentosApoio.MesAtual());
            repositorio.Remover(futuros);
        }
        else if (modo == "todas" && item.Modo == "parcelada" && item.GrupoParcelamentoId.HasValue)
        {
            var cartao = item.CartaoCredito!;
            var todasDoGrupo = await repositorio.ListarGrupoDeComprasAsync(item.GrupoParcelamentoId.Value, lojaId);
            var mesesPagos = await repositorio.ListarMesesDeFaturasPagasAsync(cartao.Id);

            var removiveis = todasDoGrupo.Where(p =>
            {
                var vencimento = CartaoRegras.VencimentoDaFaturaDaCompra(cartao.DiaFechamento, cartao.DiaVencimento, p.DataCompra);
                return !mesesPagos.Contains((vencimento.Year, vencimento.Month));
            }).ToList();
            repositorio.Remover(removiveis);
        }
        else
        {
            repositorio.Remover(item);
        }

        await repositorio.SalvarAsync();
        return Resultado.Ok(new { mensagem = "Lançamento excluído." });
    }
}
