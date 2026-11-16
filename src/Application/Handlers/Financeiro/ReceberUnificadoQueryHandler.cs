using LojaApi.Shared.Application;

namespace LojaApi.Application.Financeiro;

// Contas a Receber: lançamentos a receber e mensalidades dos planos de clientes, juntos e ordenados pelo vencimento.
public class ReceberUnificadoQueryHandler(ILancamentosRepository repositorio) : IQueryHandler<ReceberUnificadoQuery>
{
    public async Task<Resultado<object>> HandleAsync(ReceberUnificadoQuery query)
    {
        var (lojaIdOuNulo, de, ate) = query;
        if (lojaIdOuNulo is not { } lojaId) return Resultado.Ok(Array.Empty<object>());

        var lancamentos = await repositorio.ListarAReceberAsync(lojaId, de, ate);
        var mensalidades = await repositorio.ListarMensalidadesDePlanosAsync(lojaId, de, ate);

        var unificado = lancamentos.Cast<object>().Concat(mensalidades)
            .OrderBy(x => x is LancamentoAReceberDto l ? l.Vencimento : ((MensalidadeDePlanoDto)x).Vencimento)
            .ToList();
        return Resultado.Ok(unificado);
    }
}
