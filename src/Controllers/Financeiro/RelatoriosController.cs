using LojaApi.Application.Financeiro;
using LojaApi.Shared.Api;
using LojaApi.Shared.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LojaApi.Controllers.Financeiro;

// Relatórios do Financeiro (resumos, balanço por categoria, comparativo, alertas, resumo dos cartões) e a leitura do Planejamento.
[ApiController]
[Route("api/financeiro")]
[Authorize]
public class RelatoriosController(ILojaAtual lojaAtual) : ControllerBase
{
    // Pagar e Receber lado a lado
    [HttpGet("resumo-mensal")]
    public async Task<IActionResult> ResumoMensal([FromQuery] int ano, [FromQuery] int mes, [FromServices] IQueryHandler<ResumoMensalQuery> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), ano, mes))).ParaResposta(this);

    // Balanço mensal por categoria (Receitas x Despesas agrupado)
    [HttpGet("balanco-por-categoria")]
    public async Task<IActionResult> BalancoPorCategoria([FromQuery] int ano, [FromQuery] int mes, [FromServices] IQueryHandler<BalancoPorCategoriaQuery> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), ano, mes))).ParaResposta(this);

    // Lançamentos que compõem uma categoria do balanço (inclusive as compras dentro do cartão)
    [HttpGet("balanco-detalhe")]
    public async Task<IActionResult> BalancoDetalhe([FromQuery] int ano, [FromQuery] int mes, [FromQuery] string categoria, [FromServices] IQueryHandler<BalancoDetalheQuery> handler, [FromQuery] bool desmembrar = false) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), ano, mes, categoria, desmembrar))).ParaResposta(this);

    // Comparativo de despesas por categoria: mês escolhido x mês anterior + melhor mês do ano
    [HttpGet("balanco-comparativo")]
    public async Task<IActionResult> BalancoComparativo([FromQuery] int ano, [FromQuery] int mes, [FromServices] IQueryHandler<BalancoComparativoQuery> handler, [FromQuery] bool desmembrar = false) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), ano, mes, desmembrar))).ParaResposta(this);

    [HttpGet("resumo-anual")]
    public async Task<IActionResult> ResumoAnual([FromQuery] int ano, [FromServices] IQueryHandler<ResumoAnualQuery> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), ano))).ParaResposta(this);

    [HttpGet("alertas-vencimento")]
    public async Task<IActionResult> AlertasVencimento([FromServices] IQueryHandler<AlertasVencimentoQuery> handler, [FromQuery] int dias = 7) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), dias))).ParaResposta(this);

    // Resumo de cartões (limite usado somando TODAS as faturas em aberto)
    [HttpGet("cartoes-resumo")]
    public async Task<IActionResult> CartoesResumo([FromServices] IQueryHandler<CartoesResumoQuery> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User)))).ParaResposta(this);

    // Planejamento do mês (tetos por categoria, receita, quanto guardar e metas)
    [HttpGet("planejamento")]
    public async Task<IActionResult> Planejamento([FromQuery] int ano, [FromQuery] int mes, [FromServices] IQueryHandler<PlanejamentoQuery> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), ano, mes))).ParaResposta(this);
}
