using LojaApi.Domain.Financeiro;
using LojaApi.Shared.Application;

namespace LojaApi.Application.Financeiro;

// Soma (ou, com valor negativo, retira) dinheiro guardado numa meta, sem deixar o guardado negativo.
public class AportarMetaPlanejamentoCommandHandler(IPlanejamentoRepository repositorio) : ICommandHandler<AportarMetaPlanejamentoCommand>
{
    public async Task<Resultado<object>> HandleAsync(AportarMetaPlanejamentoCommand command)
    {
        var (lojaIdOuNulo, id, req) = command;
        if (lojaIdOuNulo is not { } lojaId) return Resultado.NaoEncontrado();
        var meta = await repositorio.ObterMetaAsync(id, lojaId);
        if (meta is null) return Resultado.NaoEncontrado();
        if (req.Valor == 0) return Resultado.Invalido("Informe um valor.");

        meta.ValorGuardado = PlanejamentoRegras.NovoValorGuardado(meta.ValorGuardado, req.Valor);
        await repositorio.SalvarAsync();
        return Resultado.Ok(new { meta.ValorGuardado });
    }
}
