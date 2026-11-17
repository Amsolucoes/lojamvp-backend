using LojaApi.Shared.Application;

namespace LojaApi.Application.Financeiro;

// Troca só a categoria de um lançamento (usado para corrigir gastos no detalhe do planejamento).
// Conta fixa: vale para o modelo recorrente e para todas as ocorrências já geradas. Parcelamento, com "todas as parcelas": vale para o grupo todo.
public class TrocarCategoriaLancamentoCommandHandler(ILancamentosRepository lancamentos, IFinanceiroRepository financeiro) : ICommandHandler<TrocarCategoriaLancamentoCommand>
{
    public async Task<Resultado<object>> HandleAsync(TrocarCategoriaLancamentoCommand command)
    {
        var (lojaIdOuNulo, id, req) = command;
        if (lojaIdOuNulo is not { } lojaId) return Resultado.NaoEncontrado();
        var lanc = await lancamentos.ObterAsync(id, lojaId);
        if (lanc is null) return Resultado.NaoEncontrado();

        if (req.CategoriaId.HasValue && await financeiro.ObterCategoriaAsync(req.CategoriaId.Value, lojaId) is null)
            return Resultado.Invalido("Categoria não encontrada.");

        var alterados = 1;
        if (lanc.Modo == "fixa" && lanc.LancamentoFixoId.HasValue)
        {
            var fixo = await lancamentos.ObterFixoAsync(lanc.LancamentoFixoId.Value, lojaId);
            if (fixo is not null) fixo.CategoriaId = req.CategoriaId;
            var irmaos = await lancamentos.ListarTodosDoFixoAsync(lanc.LancamentoFixoId.Value, lojaId);
            foreach (var l in irmaos) l.CategoriaId = req.CategoriaId;
            alterados = irmaos.Count;
        }
        else if (req.TodasParcelas && lanc.Modo == "parcelada" && lanc.GrupoParcelamentoId.HasValue)
        {
            var parcelas = await lancamentos.ListarGrupoAsync(lanc.GrupoParcelamentoId.Value, lojaId);
            foreach (var l in parcelas) l.CategoriaId = req.CategoriaId;
            alterados = parcelas.Count;
        }
        else
        {
            lanc.CategoriaId = req.CategoriaId;
        }

        await lancamentos.SalvarAsync();
        return Resultado.Ok(new { lanc.Id, lanc.CategoriaId, alterados });
    }
}
