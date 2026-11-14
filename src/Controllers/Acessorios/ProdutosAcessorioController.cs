using LojaApi.Application.Acessorios;
using LojaApi.Shared.Api;
using LojaApi.Shared.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LojaApi.Controllers.Acessorios;

// Catálogo da loja de acessórios: vitrine pública, gestão (superadmin), categorias, avaliações e pedidos
// (versão alternativa dos pedidos, também em /api/loja-acessorios/pedidos no PedidosAcessorioController).
[ApiController]
[Route("api/loja-acessorios/produtos")]
public class ProdutosAcessorioController : ControllerBase
{
    [HttpGet("frete")]
    [AllowAnonymous]
    public async Task<IActionResult> ConsultarFrete([FromQuery] string uf, [FromServices] IQueryHandler<ConsultarFreteQuery> handler, [FromQuery] decimal subtotal = 0) =>
        (await handler.HandleAsync(new(uf, subtotal))).ParaResposta(this);

    // ── Criar pedido + gerar cobrança Pix ──────────────────────────
    [HttpPost("pedidos")]
    [AllowAnonymous]
    public async Task<IActionResult> CriarPedido([FromBody] CriarPedidoRequest req, [FromServices] ICommandHandler<CriarPedidoPixCommand> handler) =>
        (await handler.HandleAsync(new(req))).ParaResposta(this);

    // ── Cliente consulta status do pagamento (polling na tela de checkout) ──
    [HttpGet("pedidos/{id:guid}/status")]
    [AllowAnonymous]
    public async Task<IActionResult> ConsultarStatus(Guid id, [FromServices] ICommandHandler<ConsultarStatusPedidoComReativacaoCommand> handler) =>
        (await handler.HandleAsync(new(id))).ParaResposta(this);

    // ── Gestão de pedidos (superadmin) ─────────────────────────────
    [HttpGet("pedidos")]
    [Authorize(Roles = "superadmin")]
    public async Task<IActionResult> ListarPedidos([FromQuery] string? status, [FromServices] IQueryHandler<ListarPedidosCompletosQuery> handler) =>
        (await handler.HandleAsync(new(status))).ParaResposta(this);

    [HttpPatch("pedidos/{id:guid}/status")]
    [Authorize(Roles = "superadmin")]
    public async Task<IActionResult> AtualizarStatusPedido(Guid id, [FromBody] AtualizarStatusPedidoRequest req, [FromServices] ICommandHandler<AtualizarStatusPedidoSimplesCommand> handler) =>
        (await handler.HandleAsync(new(id, req))).ParaResposta(this);

    // ── Avaliações ───────────────────────────────────────────────
    [HttpPost("avaliacoes")]
    [AllowAnonymous]
    public async Task<IActionResult> CriarAvaliacao([FromBody] CriarAvaliacaoRequest req, [FromServices] ICommandHandler<CriarAvaliacaoAcessorioCommand> handler) =>
        (await handler.HandleAsync(new(req))).ParaResposta(this);

    [HttpGet("{id:guid}/avaliacoes")]
    [AllowAnonymous]
    public async Task<IActionResult> ListarAvaliacoes(Guid id, [FromServices] IQueryHandler<ListarAvaliacoesAcessorioQuery> handler) =>
        (await handler.HandleAsync(new(id))).ParaResposta(this);

    // ── Categorias (gestão superadmin) ─────────────────────────────
    [HttpGet("categorias")]
    public async Task<IActionResult> ListarCategorias([FromServices] IQueryHandler<ListarCategoriasAcessorioQuery> handler) =>
        (await handler.HandleAsync(new())).ParaResposta(this);

    [HttpPost("categorias")]
    [Authorize(Roles = "superadmin")]
    public async Task<IActionResult> CriarCategoria([FromBody] SalvarCategoriaAcessorioRequest req, [FromServices] ICommandHandler<CriarCategoriaAcessorioCommand> handler) =>
        (await handler.HandleAsync(new(req))).ParaResposta(this);

    [HttpPut("categorias/{id:guid}")]
    [Authorize(Roles = "superadmin")]
    public async Task<IActionResult> AtualizarCategoria(Guid id, [FromBody] SalvarCategoriaAcessorioRequest req, [FromServices] ICommandHandler<AtualizarCategoriaAcessorioCommand> handler) =>
        (await handler.HandleAsync(new(id, req))).ParaResposta(this);

    [HttpDelete("categorias/{id:guid}")]
    [Authorize(Roles = "superadmin")]
    public async Task<IActionResult> ExcluirCategoria(Guid id, [FromServices] ICommandHandler<ExcluirCategoriaAcessorioCommand> handler) =>
        (await handler.HandleAsync(new(id))).ParaResposta(this);

    // ── Catálogo público ─────────────────────────────────────────
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Listar([FromQuery] string? categoria, [FromServices] IQueryHandler<ListarVitrineQuery> handler) =>
        (await handler.HandleAsync(new(categoria))).ParaResposta(this);

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> Buscar(Guid id, [FromServices] IQueryHandler<BuscarProdutoAcessorioQuery> handler) =>
        (await handler.HandleAsync(new(id))).ParaResposta(this);

    // ── Gestão (superadmin) ──────────────────────────────────────
    [HttpGet("todos")]
    [Authorize(Roles = "superadmin")]
    public async Task<IActionResult> ListarTodos([FromServices] IQueryHandler<ListarTodosProdutosAcessorioQuery> handler) =>
        (await handler.HandleAsync(new())).ParaResposta(this);

    [HttpPost]
    [Authorize(Roles = "superadmin")]
    public async Task<IActionResult> Criar([FromBody] SalvarProdutoAcessorioRequest req, [FromServices] ICommandHandler<CriarProdutoAcessorioCommand> handler) =>
        (await handler.HandleAsync(new(req))).ParaResposta(this);

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "superadmin")]
    public async Task<IActionResult> Atualizar(Guid id, [FromBody] SalvarProdutoAcessorioRequest req, [FromServices] ICommandHandler<AtualizarProdutoAcessorioCommand> handler) =>
        (await handler.HandleAsync(new(id, req))).ParaResposta(this);

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "superadmin")]
    public async Task<IActionResult> Excluir(Guid id, [FromServices] ICommandHandler<ExcluirProdutoAcessorioCommand> handler) =>
        (await handler.HandleAsync(new(id))).ParaResposta(this);
}
