using LojaApi.Shared.Application;

namespace LojaApi.Application.Financeiro;

// Exclui um lançamento. Modo "todas": num fixo, desativa o modelo e apaga os pendentes dali em diante; num parcelamento, apaga as
// parcelas pendentes (as pagas ficam). Qualquer outro modo: só este lançamento.
public class ExcluirLancamentoCommandHandler(ILancamentosRepository repositorio) : ICommandHandler<ExcluirLancamentoCommand>
{
    public async Task<Resultado<object>> HandleAsync(ExcluirLancamentoCommand command)
    {
        var (lojaIdOuNulo, id, modo) = command;
        if (lojaIdOuNulo is not { } lojaId) return Resultado.NaoEncontrado();
        var lanc = await repositorio.ObterAsync(id, lojaId);
        if (lanc is null) return Resultado.NaoEncontrado();

        if (modo == "todas" && lanc.Modo == "fixa" && lanc.LancamentoFixoId.HasValue)
        {
            var fixo = await repositorio.ObterFixoAsync(lanc.LancamentoFixoId.Value, lojaId);
            if (fixo != null) fixo.Ativa = false; // para de gerar novos meses

            var futurosPendentes = await repositorio.ListarPendentesDoFixoAsync(lanc.LancamentoFixoId.Value, lojaId, lanc.Vencimento);
            repositorio.Remover(futurosPendentes);
        }
        else if (modo == "todas" && lanc.Modo == "parcelada" && lanc.GrupoParcelamentoId.HasValue)
        {
            var pendentesGrupo = await repositorio.ListarPendentesDoGrupoAsync(lanc.GrupoParcelamentoId.Value, lojaId, null);
            repositorio.Remover(pendentesGrupo);
        }
        else
        {
            repositorio.Remover(lanc);
        }

        await repositorio.SalvarAsync();
        return Resultado.Ok(new { mensagem = "Lançamento excluído." });
    }
}
