using LojaApi.Shared.Application;

namespace LojaApi.Application.Financeiro;

// Faturas de referência de um cartão: o ciclo em aberto (ainda acumulando) e a última fatura já fechada e pendente.
public class FaturasReferenciaQueryHandler(ICartoesRepository cartoes, CicloAtualDoCartao cicloAtual) : IQueryHandler<FaturasReferenciaQuery>
{
    public async Task<Resultado<object>> HandleAsync(FaturasReferenciaQuery query)
    {
        var (lojaIdOuNulo, cartaoId) = query;
        if (lojaIdOuNulo is not { } lojaId) return Resultado.NaoEncontrado();
        var cartao = await cartoes.ObterCartaoAsync(cartaoId, lojaId);
        if (cartao is null) return Resultado.NaoEncontrado();

        // Ciclo em aberto (ainda acumulando, ainda não fechou)
        var hoje = DateTime.UtcNow;
        var (vencimentoAberta, _) = CicloAtualDoCartao.CicloEmAberto(cartao, hoje);

        // Última fatura já fechada e pendente (a que já foi "para cobrança")
        var (vencimentoFechada, totalFechada, statusFechada) = await cicloAtual.ObterAsync(cartao);

        return Resultado.Ok(new
        {
            aberta = new { ano = vencimentoAberta.Year, mes = vencimentoAberta.Month },
            fechada = new { ano = vencimentoFechada.Year, mes = vencimentoFechada.Month, total = totalFechada, status = statusFechada },
        });
    }
}
