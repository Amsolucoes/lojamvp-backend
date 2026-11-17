using LojaApi.Domain.Financeiro;
using LojaApi.Models;
using LojaApi.Shared.Application;

namespace LojaApi.Application.Financeiro;

// Salva a configuração do Planejamento: receita planejada do mês (opcional) e o percentual da receita a guardar.
public class SalvarPlanejamentoConfigCommandHandler(IPlanejamentoRepository repositorio) : ICommandHandler<SalvarPlanejamentoConfigCommand>
{
    public async Task<Resultado<object>> HandleAsync(SalvarPlanejamentoConfigCommand command)
    {
        var (lojaIdOuNulo, req) = command;
        if (lojaIdOuNulo is not { } lojaId) return Resultado.Invalido("Loja não encontrada.");
        if (!PlanejamentoRegras.PercentualValido(req.PercentualPoupanca))
            return Resultado.Invalido("O percentual a guardar deve ficar entre 0 e 100.");
        if (PlanejamentoRegras.ReceitaPlanejadaNegativa(req.ReceitaPlanejada))
            return Resultado.Invalido("A receita planejada não pode ser negativa.");

        var plano = await ObterOuCriarAsync(repositorio, lojaId);
        plano.ReceitaPlanejada = PlanejamentoRegras.ReceitaParaGravar(req.ReceitaPlanejada);
        plano.PercentualPoupanca = req.PercentualPoupanca;
        await repositorio.SalvarAsync();
        return Resultado.Ok(new { plano.ReceitaPlanejada, plano.PercentualPoupanca });
    }

    // A loja tem um planejamento só: cria (e salva) na primeira vez
    internal static async Task<PlanejamentoFinanceiro> ObterOuCriarAsync(IPlanejamentoRepository repositorio, Guid lojaId)
    {
        var plano = await repositorio.ObterPlanejamentoAsync(lojaId);
        if (plano is null)
        {
            plano = new PlanejamentoFinanceiro { LojaId = lojaId };
            repositorio.Adicionar(plano);
            await repositorio.SalvarAsync();
        }
        return plano;
    }
}
