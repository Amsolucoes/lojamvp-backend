using LojaApi.Domain.Financeiro;
using LojaApi.Shared.Application;

namespace LojaApi.Application.Financeiro;

// Edita uma meta da loja.
public class AtualizarMetaPlanejamentoCommandHandler(IPlanejamentoRepository repositorio) : ICommandHandler<AtualizarMetaPlanejamentoCommand>
{
    public async Task<Resultado<object>> HandleAsync(AtualizarMetaPlanejamentoCommand command)
    {
        var (lojaIdOuNulo, id, req) = command;
        if (lojaIdOuNulo is not { } lojaId) return Resultado.NaoEncontrado();
        var meta = await repositorio.ObterMetaAsync(id, lojaId);
        if (meta is null) return Resultado.NaoEncontrado();
        if (string.IsNullOrWhiteSpace(req.Nome)) return Resultado.Invalido("Informe o nome da meta.");
        if (!PlanejamentoRegras.ValorAlvoValido(req.ValorAlvo)) return Resultado.Invalido("Informe o valor da meta.");

        meta.Nome = req.Nome.Trim();
        meta.Icone = PlanejamentoRegras.IconeDaMeta(req.Icone);
        meta.ValorAlvo = req.ValorAlvo;
        meta.ValorGuardado = PlanejamentoRegras.ValorGuardadoValido(req.ValorGuardado);
        meta.DataAlvo = PlanejamentoRegras.DataAlvoEmUtc(req.DataAlvo);
        await repositorio.SalvarAsync();
        return Resultado.Ok(new { meta.Id });
    }
}
