using LojaApi.Shared.Application;

namespace LojaApi.Application.Financeiro;

// Ativa ou desativa uma compra fixa do cartão; ao desativar, apaga as compras futuras que ela gerou.
public class AlternarCartaoFixoCommandHandler(ICartoesRepository repositorio, IGeradorDeComprasFixas compras) : ICommandHandler<AlternarCartaoFixoCommand>
{
    public async Task<Resultado<object>> HandleAsync(AlternarCartaoFixoCommand command)
    {
        var (lojaIdOuNulo, id) = command;
        if (lojaIdOuNulo is not { } lojaId) return Resultado.NaoEncontrado();
        var fixo = await repositorio.ObterFixoAsync(id, lojaId);
        if (fixo is null) return Resultado.NaoEncontrado();

        fixo.Ativo = !fixo.Ativo;
        if (!fixo.Ativo) await compras.LimparFuturosAsync(id);
        await repositorio.SalvarAsync();
        return Resultado.Ok(new { fixo.Id, fixo.Ativo });
    }
}
