using LojaApi.Domain.Financeiro;
using LojaApi.Models;
using LojaApi.Shared.Application;

namespace LojaApi.Application.Financeiro;

// Pedaços compartilhados pelos handlers de lançamentos, parcelas e fixos.
internal static class LancamentosApoio
{
    public static DateTime MesAtual() => LancamentosRegras.PrimeiroDiaDoMes(DateTime.UtcNow);

    // Carrega o parcelamento do lançamento escolhido (só da loja, e só parcelamento comum — não o de fatura de cartão financiada)
    public static async Task<(List<LancamentoFinanceiro> Parcelas, string? Erro)> CarregarGrupoAsync(ILancamentosRepository repositorio, Guid? lojaIdOuNulo, Guid id)
    {
        if (lojaIdOuNulo is not { } lojaId) return ([], "Lançamento não encontrado.");
        var lanc = await repositorio.ObterAsync(id, lojaId);
        if (lanc is null) return ([], "Lançamento não encontrado.");
        if (lanc.Modo != "parcelada" || !lanc.GrupoParcelamentoId.HasValue)
            return ([], "Este lançamento não é parcelado.");
        if (lanc.CartaoOrigemId.HasValue || lanc.FaturaCartaoId.HasValue)
            return ([], "Parcelas de fatura de cartão financiada não podem ser alteradas por aqui.");

        var parcelas = await repositorio.ListarGrupoAsync(lanc.GrupoParcelamentoId.Value, lojaId);
        return (parcelas, null);
    }

    // Numera de 1 a N na ordem das parcelas e grava o total em todas
    public static void RenumerarParcelas(List<LancamentoFinanceiro> restantes)
    {
        var ordenadas = restantes.OrderBy(l => l.NumeroParcela).ThenBy(l => l.Vencimento).ToList();
        for (int i = 0; i < ordenadas.Count; i++)
        {
            ordenadas[i].NumeroParcela = i + 1;
            ordenadas[i].TotalParcelas = ordenadas.Count;
        }
    }

    // Aplica a edição a uma ocorrência (parcela ou mês de fixo), mantendo o mês dela e trocando só o dia do vencimento
    public static void AplicarEdicaoNaOcorrencia(LancamentoFinanceiro p, EditarLancamentoRequest req, int novoDia)
    {
        p.Descricao = req.Descricao.Trim();
        p.CategoriaId = req.CategoriaId;
        p.ContaBancariaId = req.ContaBancariaId;
        p.Valor = req.Valor;
        p.Observacao = req.Observacao;
        p.Vencimento = LancamentosRegras.ReajustarDia(p.Vencimento, novoDia);
    }

    public static void AplicarEdicaoNoFixo(LancamentoFixo fixo, EditarLancamentoRequest req, int novoDia)
    {
        fixo.Descricao = req.Descricao.Trim();
        fixo.CategoriaId = req.CategoriaId;
        fixo.ContaBancariaId = req.ContaBancariaId;
        fixo.Valor = req.Valor;
        fixo.Observacao = req.Observacao;
        fixo.DiaVencimento = novoDia;
    }
}
