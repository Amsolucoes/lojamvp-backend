using LojaApi.Domain.Financeiro;
using LojaApi.Models;
using LojaApi.Shared.Application;

namespace LojaApi.Application.Financeiro;

// Cria uma meta de guardar dinheiro (nome, valor alvo, quanto já tem guardado e data alvo opcional).
public class CriarMetaPlanejamentoCommandHandler(IPlanejamentoRepository repositorio) : ICommandHandler<CriarMetaPlanejamentoCommand>
{
    public async Task<Resultado<object>> HandleAsync(CriarMetaPlanejamentoCommand command)
    {
        var (lojaIdOuNulo, req) = command;
        if (lojaIdOuNulo is not { } lojaId) return Resultado.Invalido("Loja não encontrada.");
        if (string.IsNullOrWhiteSpace(req.Nome)) return Resultado.Invalido("Informe o nome da meta.");
        if (!PlanejamentoRegras.ValorAlvoValido(req.ValorAlvo)) return Resultado.Invalido("Informe o valor da meta.");

        var meta = new MetaPlanejamento
        {
            LojaId = lojaId,
            Nome = req.Nome.Trim(),
            Icone = PlanejamentoRegras.IconeDaMeta(req.Icone),
            ValorAlvo = req.ValorAlvo,
            ValorGuardado = PlanejamentoRegras.ValorGuardadoValido(req.ValorGuardado),
            DataAlvo = PlanejamentoRegras.DataAlvoEmUtc(req.DataAlvo),
        };
        repositorio.Adicionar(meta);
        await repositorio.SalvarAsync();
        return Resultado.Ok(new { meta.Id });
    }
}
