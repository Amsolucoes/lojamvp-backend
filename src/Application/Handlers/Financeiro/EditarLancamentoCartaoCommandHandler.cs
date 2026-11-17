using LojaApi.Domain.Financeiro;
using LojaApi.Shared.Application;

namespace LojaApi.Application.Financeiro;

// Edita uma compra de cartão. Modo "todas": num fixo, muda o modelo e regera as compras futuras; numa parcelada, muda as parcelas
// de faturas ainda não pagas (mantendo o "n/total" na descrição). Qualquer outro modo: só esta compra (inclusive a data).
public class EditarLancamentoCartaoCommandHandler(ICartoesRepository repositorio, IGeradorDeComprasFixas compras) : ICommandHandler<EditarLancamentoCartaoCommand>
{
    public async Task<Resultado<object>> HandleAsync(EditarLancamentoCartaoCommand command)
    {
        var (lojaIdOuNulo, id, modo, req) = command;
        if (lojaIdOuNulo is not { } lojaId) return Resultado.NaoEncontrado();
        var item = await repositorio.ObterCompraAsync(id, lojaId);
        if (item is null) return Resultado.NaoEncontrado();

        var novaData = LancamentosRegras.AoMeioDia(req.DataCompra);

        if (modo == "todas" && item.Modo == "fixa" && item.CartaoFixoId.HasValue)
        {
            var fixo = await repositorio.ObterFixoPorLojaAsync(item.CartaoFixoId.Value, lojaId);
            if (fixo is null) return Resultado.NaoEncontrado();

            fixo.Descricao = req.Descricao.Trim();
            fixo.Valor = req.Valor;
            fixo.CategoriaId = req.CategoriaId;
            fixo.Observacao = req.Observacao;
            fixo.DiaCompra = novaData.Day;

            await compras.LimparFuturosAsync(fixo.Id);
            await repositorio.SalvarAsync();

            await compras.GerarLoteAsync(fixo, item.CartaoCredito!, LancamentosApoio.MesAtual());
        }
        else if (modo == "todas" && item.Modo == "parcelada" && item.GrupoParcelamentoId.HasValue)
        {
            var cartao = item.CartaoCredito!;
            var todasDoGrupo = await repositorio.ListarGrupoDeComprasAsync(item.GrupoParcelamentoId.Value, lojaId);
            var mesesPagos = await repositorio.ListarMesesDeFaturasPagasAsync(cartao.Id);

            foreach (var p in todasDoGrupo)
            {
                var vencimento = CartaoRegras.VencimentoDaFaturaDaCompra(cartao.DiaFechamento, cartao.DiaVencimento, p.DataCompra);
                if (mesesPagos.Contains((vencimento.Year, vencimento.Month))) continue; // já foi paga — não mexe

                var baseDescricao = req.Descricao.Trim();
                p.Descricao = p.TotalParcelas.HasValue ? $"{baseDescricao} ({p.NumeroParcela}/{p.TotalParcelas})" : baseDescricao;
                p.Valor = req.Valor;
                p.CategoriaId = req.CategoriaId;
            }
        }
        else
        {
            item.Descricao = req.Descricao.Trim();
            item.Valor = req.Valor;
            item.DataCompra = novaData;
            item.CategoriaId = req.CategoriaId;
        }

        await repositorio.SalvarAsync();
        return Resultado.Ok(new { mensagem = "Lançamento atualizado." });
    }
}
