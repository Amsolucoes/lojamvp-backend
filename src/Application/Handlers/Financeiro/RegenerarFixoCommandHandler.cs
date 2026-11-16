using LojaApi.Shared.Application;

namespace LojaApi.Application.Financeiro;

// "Errei": apaga o que o fixo criou daqui para a frente (e ainda está pendente) e gera de novo.
public class RegenerarFixoCommandHandler(ILancamentosRepository repositorio, IGeradorDeLancamentosFixos fixos) : ICommandHandler<RegenerarFixoCommand>
{
    public async Task<Resultado<object>> HandleAsync(RegenerarFixoCommand command)
    {
        var (lojaIdOuNulo, id) = command;
        if (lojaIdOuNulo is not { } lojaId) return Resultado.NaoEncontrado();
        var fixo = await repositorio.ObterFixoAsync(id, lojaId);
        if (fixo is null) return Resultado.NaoEncontrado();

        await fixos.LimparFuturosAsync(id);
        await repositorio.SalvarAsync();

        await fixos.GerarLoteAsync(fixo, LancamentosApoio.MesAtual());

        await repositorio.SalvarAsync();
        return Resultado.Ok(new { fixo.Id, geradoAte = fixo.GeradoAte });
    }
}
