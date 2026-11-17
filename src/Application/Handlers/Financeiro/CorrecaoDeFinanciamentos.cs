using LojaApi.Domain.Financeiro;

namespace LojaApi.Application.Financeiro;

// Autocorreção das parcelas de financiamento de fatura. Roda toda vez que as telas de Contas a Pagar e da fatura abrem, então corrige sozinho:
// 1) parcela que vence no mesmo mês de uma fatura paga parcialmente passa a fazer parte dessa fatura (vira paga/absorvida);
// 2) parcela que nasceu com vencimento num ciclo que já tinha fechado ANTES de ela ser criada (bug antigo) vai, com as pendentes seguintes
//    do mesmo grupo, para o ciclo certo. O vencimento é recalculado SEMPRE do zero a partir da criação da 1ª parcela (idempotente).
public class CorrecaoDeFinanciamentos(IFaturasRepository faturas)
{
    public async Task CorrigirAsync(Guid lojaId)
    {
        await AbsorverParcelasDeFaturasParciaisAsync(lojaId);

        // 3 consultas no total (grupos, parcelas, cartões); o resto é feito em memória
        var grupoIds = await faturas.ListarGruposDeFinanciamentoPendentesAsync(lojaId);
        if (grupoIds.Count == 0) return;

        var todasParcelas = await faturas.ListarPendentesDosGruposAsync(grupoIds);
        var cartaoIds = todasParcelas.Where(p => p.CartaoOrigemId != null).Select(p => p.CartaoOrigemId!.Value).Distinct().ToList();
        var cartoesPorId = await faturas.ObterCartoesPorIdsAsync(cartaoIds);

        foreach (var grupo in todasParcelas.Where(p => p.GrupoParcelamentoId != null).GroupBy(p => p.GrupoParcelamentoId!.Value))
        {
            var parcelas = grupo.OrderBy(p => p.NumeroParcela).ToList();
            if (parcelas.Count == 0) continue;

            var primeira = parcelas[0];
            if (primeira.CartaoOrigemId is null) continue;
            if (!cartoesPorId.TryGetValue(primeira.CartaoOrigemId.Value, out var cartao)) continue;

            var mesBase = FaturaRegras.MesBaseDoFinanciamento(cartao.DiaFechamento, cartao.DiaVencimento, primeira.CriadoEm);
            foreach (var p in parcelas)
            {
                var vencimentoCorrigido = FaturaRegras.VencimentoCorrigidoDaParcela(cartao.DiaVencimento, mesBase, p.NumeroParcela);
                if (p.Vencimento.Date != vencimentoCorrigido.Date)
                    p.Vencimento = vencimentoCorrigido;
            }
        }

        await faturas.SalvarSeHouverAlteracoesAsync();
    }

    // Corrige faturas já pagas parcialmente ANTES da regra de absorção: as parcelas de financiamento do mês delas ainda estavam "pendentes"
    private async Task AbsorverParcelasDeFaturasParciaisAsync(Guid lojaId)
    {
        var parciais = await faturas.ListarFaturasParciaisAsync(lojaId);
        if (parciais.Count == 0) return;

        var cartaoIds = parciais.Select(f => (Guid?)f.CartaoCreditoId).Distinct().ToList();
        var pendentes = await faturas.ListarPendentesDeCartoesAsync(lojaId, cartaoIds);

        foreach (var l in pendentes)
        {
            var fatura = parciais.FirstOrDefault(f => f.CartaoCreditoId == l.CartaoOrigemId
                && f.MesReferencia.Year == l.Vencimento.Year && f.MesReferencia.Month == l.Vencimento.Month);
            if (fatura is null) continue;
            FaturasApoio.AbsorverParcela(l, fatura.PagoEm ?? DateTime.UtcNow, "pagamento parcial");
        }

        await faturas.SalvarSeHouverAlteracoesAsync();
    }
}
