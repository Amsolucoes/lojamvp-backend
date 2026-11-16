using LojaApi.Shared.Application;

namespace LojaApi.Application.Financeiro;

// Exclui parcelas escolhidas de um parcelamento (nenhuma paga, e não todas) e renumera as que ficam.
public class ExcluirParcelasCommandHandler(ILancamentosRepository repositorio) : ICommandHandler<ExcluirParcelasCommand>
{
    public async Task<Resultado<object>> HandleAsync(ExcluirParcelasCommand command)
    {
        var (lojaId, id, req) = command;
        var (parcelas, erro) = await LancamentosApoio.CarregarGrupoAsync(repositorio, lojaId, id);
        if (erro is not null) return Resultado.Invalido(erro);

        var ids = (req.Ids ?? []).Distinct().ToList();
        if (ids.Count == 0) return Resultado.Invalido("Selecione as parcelas que quer excluir.");

        var alvo = parcelas.Where(p => ids.Contains(p.Id)).ToList();
        if (alvo.Count != ids.Count) return Resultado.Invalido("Alguma parcela selecionada não foi encontrada.");
        if (alvo.Any(p => p.Status == "pago"))
            return Resultado.Invalido("Parcelas já pagas não podem ser excluídas. Desmarque o pagamento antes, se for o caso.");
        if (alvo.Count == parcelas.Count)
            return Resultado.Invalido("Para excluir todas as parcelas, exclua o lançamento.");

        repositorio.Remover(alvo);
        LancamentosApoio.RenumerarParcelas(parcelas.Where(p => !ids.Contains(p.Id)).ToList());
        await repositorio.SalvarAsync();
        return Resultado.Ok(new { excluidas = alvo.Count, restantes = parcelas.Count - alvo.Count });
    }
}
