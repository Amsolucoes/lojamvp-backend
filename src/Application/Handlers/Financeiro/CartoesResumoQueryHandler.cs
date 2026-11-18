using LojaApi.Domain.Financeiro;
using LojaApi.Shared.Application;

namespace LojaApi.Application.Financeiro;

// Resumo dos cartões ativos: limite usado (soma TODAS as faturas em aberto, as compras futuras e o financiamento pendente), disponível, e a fatura
// "principal" para o card (a mais antiga fechada e pendente; senão, a do ciclo em aberto).
public class CartoesResumoQueryHandler(IRelatoriosConsultas consultas, IContasAPagarConsultas contasAPagar, CorrecaoDeFinanciamentos correcao) : IQueryHandler<CartoesResumoQuery>
{
    public async Task<Resultado<object>> HandleAsync(CartoesResumoQuery query)
    {
        if (query.LojaId is not { } lojaId) return Resultado.Ok(Array.Empty<object>());

        await correcao.CorrigirAsync(lojaId);

        var cartoes = await consultas.ListarCartoesAtivosAsync(lojaId);
        var resultado = new List<object>();
        if (cartoes.Count == 0) return Resultado.Ok(resultado);

        var hoje = DateTime.UtcNow;

        // Tudo que o cálculo precisa é carregado de uma vez. A janela cobre os 12 ciclos somados + folga; compras futuras (parceladas) e faturas futuras
        // entram porque não há limite superior.
        var cartaoIds = cartoes.Select(c => c.Id).ToList();
        var inicioJanela = new DateTime(hoje.Year, hoje.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-16);
        var dados = await DadosDosCartoes.CarregarAsync(consultas, contasAPagar, cartaoIds, inicioJanela, DadosDosCartoes.SemLimite, inicioJanela, DadosDosCartoes.SemLimite);
        var financiamentoPendente = (await consultas.ListarFinanciamentoPendentePorCartaoAsync(cartaoIds)).ToDictionary(x => x.CartaoId, x => x.Total);

        foreach (var cartao in cartoes)
        {
            decimal Soma(DateTime inicio, DateTime fim) => dados.SomaCompras(cartao.Id, inicio, fim);

            var usado = RelatoriosRegras.LimiteUsado(cartao.DiaFechamento, cartao.DiaVencimento, hoje, Soma,
                (ano, mes) => dados.FaturaRegistrada(cartao.Id, ano, mes),
                dados.Compras(cartao.Id).Select(c => (c.Modo, c.DataCompra, c.Valor)).ToList(),
                financiamentoPendente.GetValueOrDefault(cartao.Id));

            // Fatura "principal" pra exibir vencimento/status no card
            var (vencimentoPrincipal, _, statusPrincipal) = RelatoriosRegras.CicloAtualDoCartao(cartao.DiaFechamento, cartao.DiaVencimento, hoje, Soma,
                (ano, mes) => dados.Fatura(cartao.Id, ano, mes)?.Status);
            var (piInicio, piFim) = CartaoRegras.CicloDaFatura(cartao.DiaFechamento, vencimentoPrincipal);
            var qtdCompras = dados.Compras(cartao.Id).Count(l => l.DataCompra.Date >= piInicio.Date && l.DataCompra.Date <= piFim.Date);

            resultado.Add(new
            {
                cartao.Id,
                cartao.Nome,
                cartao.Bandeira,
                cartao.Limite,
                usado,
                disponivel = cartao.Limite - usado,
                vencimentoAtual = vencimentoPrincipal,
                status = statusPrincipal,
                qtdCompras,
            });
        }

        return Resultado.Ok(resultado);
    }
}
