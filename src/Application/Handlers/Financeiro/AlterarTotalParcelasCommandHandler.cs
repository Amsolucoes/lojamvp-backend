using LojaApi.Domain.Financeiro;
using LojaApi.Models;
using LojaApi.Shared.Application;

namespace LojaApi.Application.Financeiro;

// Muda o total de parcelas de um parcelamento: reduzindo, remove as últimas (se não estiverem pagas); aumentando, continua a série mensal.
public class AlterarTotalParcelasCommandHandler(ILancamentosRepository repositorio) : ICommandHandler<AlterarTotalParcelasCommand>
{
    public async Task<Resultado<object>> HandleAsync(AlterarTotalParcelasCommand command)
    {
        var (lojaId, id, req) = command;
        var (parcelas, erro) = await LancamentosApoio.CarregarGrupoAsync(repositorio, lojaId, id);
        if (erro is not null) return Resultado.Invalido(erro);

        if (!LancamentosRegras.TotalAlteradoValido(req.Total))
            return Resultado.Invalido("O total de parcelas deve ficar entre 1 e 360.");

        var atual = parcelas.Count;
        if (req.Total == atual) return Resultado.Ok(new { total = atual });

        var ordenadas = parcelas.OrderBy(p => p.NumeroParcela).ThenBy(p => p.Vencimento).ToList();
        if (req.Total < atual)
        {
            // Reduzir: remove as últimas parcelas (as de maior número), desde que ainda não estejam pagas
            var remover = ordenadas.Skip(req.Total).ToList();
            var pagas = remover.Where(p => p.Status == "pago").ToList();
            if (pagas.Count > 0)
                return Resultado.Invalido($"As parcelas {string.Join(", ", pagas.Select(p => p.NumeroParcela))} já estão pagas e seriam removidas. Desmarque o pagamento delas ou escolha as parcelas a excluir.");

            repositorio.Remover(remover);
            LancamentosApoio.RenumerarParcelas(ordenadas.Take(req.Total).ToList());
        }
        else
        {
            // Aumentar: continua a série mensal a partir da última parcela, com os mesmos dados dela
            var ultima = ordenadas[^1];
            for (int i = 1; i <= req.Total - atual; i++)
            {
                repositorio.Adicionar(new LancamentoFinanceiro
                {
                    LojaId = ultima.LojaId,
                    ContaBancariaId = ultima.ContaBancariaId,
                    Tipo = ultima.Tipo,
                    Modo = "parcelada",
                    Descricao = ultima.Descricao,
                    CategoriaId = ultima.CategoriaId,
                    Observacao = ultima.Observacao,
                    Valor = ultima.Valor,
                    Vencimento = ultima.Vencimento.AddMonths(i),
                    GrupoParcelamentoId = ultima.GrupoParcelamentoId,
                    NumeroParcela = atual + i,
                    Status = "pendente",
                    Avisar = ultima.Avisar,
                });
            }
            foreach (var p in ordenadas) p.TotalParcelas = req.Total;
        }

        await repositorio.SalvarAsync();

        // Garante numeração e total coerentes em todo o grupo (inclui as parcelas recém-criadas)
        var grupo = await repositorio.ListarGrupoAsync(parcelas[0].GrupoParcelamentoId!.Value, parcelas[0].LojaId);
        LancamentosApoio.RenumerarParcelas(grupo);
        await repositorio.SalvarAsync();
        return Resultado.Ok(new { total = grupo.Count });
    }
}
