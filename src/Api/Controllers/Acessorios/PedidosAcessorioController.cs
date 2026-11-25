using LojaApi.Application.Acessorios;
using LojaApi.Shared.Api;
using LojaApi.Shared.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LojaApi.Controllers.Acessorios;

// Pedidos da loja de acessórios (checkout público + gestão do superadmin).
// Existe também uma versão parecida em /api/loja-acessorios/produtos/pedidos (ProdutosAcessorioController).
[ApiController]
[Route("api/loja-acessorios")]
public class PedidosAcessorioController : ControllerBase
{
    [HttpGet("frete")]
    [AllowAnonymous]
    public async Task<IActionResult> CalcularFrete([FromQuery] string uf, [FromServices] IQueryHandler<CalcularFreteQuery> handler) =>
        (await handler.HandleAsync(new(uf))).ParaResposta(this);

    // ── Criar pedido + gerar cobrança Pix ──────────────────────────
    [HttpPost("pedidos")]
    [AllowAnonymous]
    public async Task<IActionResult> CriarPedido([FromBody] CriarPedidoRequest req, [FromServices] ICommandHandler<CriarPedidoPixCommand> handler) =>
        (await handler.HandleAsync(new(req))).ParaResposta(this);

    // ── Criar pedido + processar pagamento com cartão (via Payment Brick) ──
    [HttpPost("pedidos/cartao")]
    [AllowAnonymous]
    public async Task<IActionResult> CriarPedidoCartao([FromBody] CriarPedidoCartaoRequest req, [FromServices] ICommandHandler<CriarPedidoCartaoCommand> handler) =>
        (await handler.HandleAsync(new(req))).ParaResposta(this);

    // ── Cliente consulta status do pagamento (polling na tela de checkout) ──
    [HttpGet("pedidos/{id:guid}/status")]
    [AllowAnonymous]
    public async Task<IActionResult> ConsultarStatus(Guid id, [FromServices] ICommandHandler<ConsultarStatusPedidoCommand> handler) =>
        (await handler.HandleAsync(new(id))).ParaResposta(this);

    // ── Gestão de pedidos (superadmin) ─────────────────────────────
    [HttpGet("pedidos")]
    [Authorize(Roles = "superadmin")]
    public async Task<IActionResult> ListarPedidos([FromQuery] string? status, [FromServices] IQueryHandler<ListarPedidosResumoQuery> handler) =>
        (await handler.HandleAsync(new(status))).ParaResposta(this);

    [HttpPatch("pedidos/{id:guid}/status")]
    [Authorize(Roles = "superadmin")]
    public async Task<IActionResult> AtualizarStatusPedido(Guid id, [FromBody] AtualizarStatusPedidoRequest req, [FromServices] ICommandHandler<AtualizarStatusPedidoCommand> handler) =>
        (await handler.HandleAsync(new(id, req))).ParaResposta(this);
}
