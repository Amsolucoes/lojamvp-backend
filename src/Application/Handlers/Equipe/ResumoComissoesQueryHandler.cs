using LojaApi.Domain.Equipe;
using LojaApi.Models;
using LojaApi.Shared.Application;
using LojaApi.src.Models.Funcionarios;

namespace LojaApi.Application.Equipe;

// Resumo por profissional: quanto cada um tem de comissão pendente no período.
public class ResumoComissoesQueryHandler(IEquipeRepository repositorio) : IQueryHandler<ResumoComissoesQuery>
{
    public async Task<Resultado<object>> HandleAsync(ResumoComissoesQuery query)
    {
        var (lojaId, de, ate) = query;
        if (lojaId is null) return Resultado.Ok(Array.Empty<object>());

        var comissoes = await repositorio.ListarComissoesPendentesAsync(lojaId.Value,
            de?.Date,
            ate?.Date.AddDays(1));
        var profissionalIds = comissoes.Select(c => c.ProfissionalId).Distinct().ToList();
        var profissionais = await repositorio.ListarProfissionaisPorIdsAsync(profissionalIds);

        var resumo = profissionalIds.Select(pid =>
        {
            var doProfissional = comissoes.Where(c => c.ProfissionalId == pid).ToList();
            var prof = profissionais.FirstOrDefault(p => p.Id == pid);
            return new ResumoComissaoProfissionalDto(
                pid,
                prof?.Nome ?? "—",
                prof?.DiaPagamentoPadrao,
                prof?.TipoRemuneracao ?? TipoRemuneracao.Comissao,
                prof?.ValorDiaria,
                doProfissional.Count,
                doProfissional.Sum(c => c.ValorComissao));
        }).OrderByDescending(r => r.ValorTotal).ToList();

        return Resultado.Ok(resumo);
    }
}
