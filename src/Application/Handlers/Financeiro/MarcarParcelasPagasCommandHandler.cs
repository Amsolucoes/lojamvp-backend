using LojaApi.Shared.Application;

namespace LojaApi.Application.Financeiro;

// Marca várias parcelas de um parcelamento como pagas (ou volta para pendente).
public class MarcarParcelasPagasCommandHandler(ILancamentosRepository repositorio) : ICommandHandler<MarcarParcelasPagasCommand>
{
    public async Task<Resultado<object>> HandleAsync(MarcarParcelasPagasCommand command)
    {
        var (lojaId, id, req) = command;
        var (parcelas, erro) = await LancamentosApoio.CarregarGrupoAsync(repositorio, lojaId, id);
        if (erro is not null) return Resultado.Invalido(erro);

        var ids = (req.Ids ?? []).Distinct().ToList();
        if (ids.Count == 0) return Resultado.Invalido("Selecione as parcelas.");

        var alvo = parcelas.Where(p => ids.Contains(p.Id)).ToList();
        if (alvo.Count != ids.Count) return Resultado.Invalido("Alguma parcela selecionada não foi encontrada.");

        var agora = DateTime.UtcNow;
        foreach (var p in alvo)
        {
            p.Status = req.Pago ? "pago" : "pendente";
            p.PagoEm = req.Pago ? agora : null;
        }
        await repositorio.SalvarAsync();
        return Resultado.Ok(new { alteradas = alvo.Count });
    }
}
