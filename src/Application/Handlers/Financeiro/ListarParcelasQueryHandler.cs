using LojaApi.Shared.Application;

namespace LojaApi.Application.Financeiro;

// Todas as parcelas do parcelamento a que o lançamento pertence.
public class ListarParcelasQueryHandler(ILancamentosRepository repositorio) : IQueryHandler<ListarParcelasQuery>
{
    public async Task<Resultado<object>> HandleAsync(ListarParcelasQuery query)
    {
        var (parcelas, erro) = await LancamentosApoio.CarregarGrupoAsync(repositorio, query.LojaId, query.Id);
        if (erro is not null) return Resultado.Invalido(erro);

        return Resultado.Ok(new
        {
            total = parcelas.Count,
            itens = parcelas.Select(p => new { p.Id, numero = p.NumeroParcela, p.Vencimento, p.Valor, p.Status, p.PagoEm }),
        });
    }
}
