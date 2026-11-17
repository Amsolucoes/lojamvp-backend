using LojaApi.Shared.Application;

namespace LojaApi.Application.Financeiro;

// Vencimentos próximos (hoje até hoje + dias): lançamentos pendentes e a fatura "a pagar agora" de cada cartão ativo, por ordem de vencimento.
public class AlertasVencimentoQueryHandler(IRelatoriosConsultas consultas, ICartoesRepository cartoes, IFaturasRepository faturas, CicloAtualDoCartao cicloAtual, CorrecaoDeFinanciamentos correcao)
    : IQueryHandler<AlertasVencimentoQuery>
{
    public async Task<Resultado<object>> HandleAsync(AlertasVencimentoQuery query)
    {
        var (lojaIdOuNulo, dias) = query;
        if (lojaIdOuNulo is not { } lojaId) return Resultado.Ok(Array.Empty<object>());

        await correcao.CorrigirAsync(lojaId);

        var hoje = DateTime.UtcNow.Date;
        var limite = hoje.AddDays(dias);

        var alertas = (await consultas.ListarPendentesAteAsync(lojaId, hoje, limite))
            .Select(l => (Vencimento: l.Vencimento, Item: (object)new { l.Id, descricao = l.Descricao, tipo = l.Tipo, valor = l.Valor, vencimento = l.Vencimento, origem = "lancamento" }))
            .ToList();

        foreach (var cartao in await cartoes.ListarAtivosAsync(lojaId))
        {
            var (vencimento, total, _) = await cicloAtual.ObterAsync(cartao);
            if (total > 0 && vencimento >= hoje && vencimento <= limite)
            {
                var faturaExistente = await faturas.ObterFaturaDoMesAsync(cartao.Id, vencimento.Year, vencimento.Month);
                if (faturaExistente?.Status != "pago")
                    alertas.Add((vencimento, new { id = cartao.Id, descricao = $"Fatura {cartao.Nome}", tipo = "pagar", valor = total, vencimento, origem = "cartao" }));
            }
        }

        return Resultado.Ok(alertas.OrderBy(a => a.Vencimento).Select(a => a.Item).ToList());
    }
}
