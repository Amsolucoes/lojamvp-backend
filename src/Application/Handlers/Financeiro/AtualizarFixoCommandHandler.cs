using LojaApi.Domain.Financeiro;
using LojaApi.Shared.Application;

namespace LojaApi.Application.Financeiro;

// Edita um fixo (conta, descrição, categoria, valor e dia) e regera as ocorrências futuras com os dados novos.
// Correção: no código antigo as ocorrências futuras eram apagadas e a regeração rodava ANTES de salvar a exclusão, então
// pulava todos os meses e a lista ficava sem os futuros. Agora a exclusão é salva antes de regerar, como no "regenerar".
public class AtualizarFixoCommandHandler(ILancamentosRepository repositorio, IGeradorDeLancamentosFixos fixos) : ICommandHandler<AtualizarFixoCommand>
{
    public async Task<Resultado<object>> HandleAsync(AtualizarFixoCommand command)
    {
        var (lojaIdOuNulo, id, req) = command;
        if (lojaIdOuNulo is not { } lojaId) return Resultado.NaoEncontrado();
        var fixo = await repositorio.ObterFixoAsync(id, lojaId);
        if (fixo is null) return Resultado.NaoEncontrado();

        fixo.ContaBancariaId = req.ContaBancariaId;
        fixo.Descricao = req.Descricao.Trim();
        fixo.CategoriaId = req.CategoriaId;
        fixo.Valor = req.Valor;
        fixo.DiaVencimento = LancamentosRegras.DiaDoFixo(req.DiaVencimento);

        await fixos.LimparFuturosAsync(id);
        await repositorio.SalvarAsync();

        await fixos.GerarLoteAsync(fixo, LancamentosApoio.MesAtual());

        await repositorio.SalvarAsync();
        return Resultado.Ok(new { fixo.Id, geradoAte = fixo.GeradoAte });
    }
}
