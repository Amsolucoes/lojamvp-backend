using LojaApi.Application.Produtos;
using LojaApi.Shared.Api;
using LojaApi.Shared.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LojaApi.Controllers.Produtos;

// Cadastro de produtos: listar, buscar, criar, editar, preços e status em lote, excluir, códigos de barras e variações.
[ApiController]
[Route("api/produtos")]
[Authorize]
public class ProdutosController(ILojaAtual lojaAtual) : ControllerBase
{
    // Gera códigos de barras EAN-13 de uso interno (prefixo 2 = faixa reservada pela GS1 para uso restrito
    // à loja). Não são válidos para NF-e/marketplaces, mas funcionam em leitores e etiquetas.
    // "ignorar" recebe códigos já usados na tela (ainda não salvos) para não repetir dentro do mesmo formulário.
    [HttpGet("gerar-codigos")]
    public async Task<IActionResult> GerarCodigos([FromServices] IQueryHandler<GerarCodigosQuery> handler, [FromQuery] int quantidade = 1, [FromQuery] string? ignorar = null) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), quantidade, ignorar))).ParaResposta(this);

    [HttpGet]
    public async Task<IActionResult> Listar([FromServices] IQueryHandler<ListarProdutosQuery> handler,
        [FromQuery] string? busca, [FromQuery] string? categoria, [FromQuery] bool? ativo, [FromQuery] Guid? marcaId) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), busca, categoria, ativo, marcaId))).ParaResposta(this);

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Buscar(Guid id, [FromServices] IQueryHandler<BuscarProdutoQuery> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), id))).ParaResposta(this);

    [HttpPost]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> Criar([FromBody] SalvarProdutoRequest req, [FromServices] ICommandHandler<CriarProdutoCommand> handler)
    {
        var resultado = await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), req));
        return resultado.Sucesso
            ? CreatedAtAction(nameof(Buscar), new { id = ((ProdutoDto)resultado.Valor!).Id }, resultado.Valor)
            : resultado.ParaResposta(this);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> Atualizar(Guid id, [FromBody] SalvarProdutoRequest req, [FromServices] ICommandHandler<AtualizarProdutoCommand> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), id, req))).ParaResposta(this);

    // ── Atualização de custo/venda em lote (tela de Precificação) ──────
    [HttpPut("precos-em-lote")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> AtualizarPrecosEmLote([FromBody] AtualizarPrecosEmLoteRequest req, [FromServices] ICommandHandler<AtualizarPrecosEmLoteCommand> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), req))).ParaResposta(this);

    // ── Ativar/inativar em lote (seleção múltipla na lista de Produtos) ──
    [HttpPut("status-em-lote")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> AtualizarStatusEmLote([FromBody] AtualizarStatusEmLoteRequest req, [FromServices] ICommandHandler<AtualizarStatusEmLoteCommand> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), req))).ParaResposta(this);

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> Deletar(Guid id, [FromServices] ICommandHandler<ExcluirProdutoCommand> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), id))).ParaResposta(this);

    // ── Variações ─────────────────────────────────────────────────────
    [HttpGet("{id:guid}/variacoes")]
    public async Task<IActionResult> ListarVariacoes(Guid id, [FromServices] IQueryHandler<ListarVariacoesQuery> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), id))).ParaResposta(this);

    [HttpPost("{id:guid}/variacoes")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> AdicionarVariacao(Guid id, [FromBody] SalvarVariacaoRequest req, [FromServices] ICommandHandler<AdicionarVariacaoCommand> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), id, req))).ParaResposta(this);

    [HttpPut("{id:guid}/variacoes/{varId:guid}")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> AtualizarVariacao(Guid id, Guid varId, [FromBody] SalvarVariacaoRequest req, [FromServices] ICommandHandler<AtualizarVariacaoCommand> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), id, varId, req))).ParaResposta(this);

    [HttpDelete("{id:guid}/variacoes/{varId:guid}")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> RemoverVariacao(Guid id, Guid varId, [FromServices] ICommandHandler<RemoverVariacaoCommand> handler)
    {
        var resultado = await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), id, varId));
        return resultado.Sucesso ? NoContent() : resultado.ParaResposta(this);
    }
}
