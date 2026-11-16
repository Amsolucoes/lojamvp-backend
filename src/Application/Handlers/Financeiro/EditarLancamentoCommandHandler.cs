using LojaApi.Domain.Financeiro;
using LojaApi.Shared.Application;

namespace LojaApi.Application.Financeiro;

// Edita um lançamento. Modo "todas": toda a série (fixo: modelo e meses futuros; parcelado: as parcelas pendentes).
// Modo "futuras": este e os seguintes ainda pendentes (os anteriores ficam como estão). Qualquer outro modo: só este lançamento.
public class EditarLancamentoCommandHandler(ILancamentosRepository repositorio, IGeradorDeLancamentosFixos fixos) : ICommandHandler<EditarLancamentoCommand>
{
    public async Task<Resultado<object>> HandleAsync(EditarLancamentoCommand command)
    {
        var (lojaIdOuNulo, id, modo, req) = command;
        if (lojaIdOuNulo is not { } lojaId) return Resultado.NaoEncontrado();
        var lanc = await repositorio.ObterAsync(id, lojaId);
        if (lanc is null) return Resultado.NaoEncontrado();

        var novoVencimento = LancamentosRegras.AoMeioDia(req.Vencimento);
        var novoDia = novoVencimento.Day;

        if (modo == "todas" && lanc.Modo == "fixa" && lanc.LancamentoFixoId.HasValue)
        {
            var fixo = await repositorio.ObterFixoPorIdAsync(lanc.LancamentoFixoId.Value);
            if (fixo is null) return Resultado.NaoEncontrado();

            LancamentosApoio.AplicarEdicaoNoFixo(fixo, req, novoDia);

            await fixos.LimparFuturosAsync(fixo.Id);
            await repositorio.SalvarAsync();

            await fixos.GerarLoteAsync(fixo, LancamentosApoio.MesAtual());
        }
        else if (modo == "futuras" && lanc.Modo == "fixa" && lanc.LancamentoFixoId.HasValue)
        {
            var fixo = await repositorio.ObterFixoPorIdAsync(lanc.LancamentoFixoId.Value);
            if (fixo is null) return Resultado.NaoEncontrado();

            var corte = LancamentosRegras.PrimeiroDiaDoMes(lanc.Vencimento);
            var daquiPraFrente = await repositorio.ListarPendentesDoFixoAsync(fixo.Id, corte);
            foreach (var p in daquiPraFrente) LancamentosApoio.AplicarEdicaoNaOcorrencia(p, req, novoDia);

            LancamentosApoio.AplicarEdicaoNoFixo(fixo, req, novoDia);
        }
        else if (modo == "futuras" && lanc.Modo == "parcelada" && lanc.GrupoParcelamentoId.HasValue)
        {
            var corte = LancamentosRegras.PrimeiroDiaDoMes(lanc.Vencimento);
            var daquiPraFrente = await repositorio.ListarPendentesDoGrupoAsync(lanc.GrupoParcelamentoId.Value, corte);
            foreach (var p in daquiPraFrente) LancamentosApoio.AplicarEdicaoNaOcorrencia(p, req, novoDia);
        }
        else if (modo == "todas" && lanc.Modo == "parcelada" && lanc.GrupoParcelamentoId.HasValue)
        {
            var pendentes = await repositorio.ListarPendentesDoGrupoAsync(lanc.GrupoParcelamentoId.Value, null);
            foreach (var p in pendentes) LancamentosApoio.AplicarEdicaoNaOcorrencia(p, req, novoDia);
        }
        else
        {
            lanc.Descricao = req.Descricao.Trim();
            lanc.CategoriaId = req.CategoriaId;
            lanc.ContaBancariaId = req.ContaBancariaId;
            lanc.Valor = req.Valor;
            lanc.Observacao = req.Observacao;
            lanc.Vencimento = novoVencimento;
        }

        await repositorio.SalvarAsync();
        return Resultado.Ok(new { mensagem = "Lançamento atualizado." });
    }
}
