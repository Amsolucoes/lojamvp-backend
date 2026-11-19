using LojaApi.Application.Bonificacoes;
using LojaApi.Shared.Api;
using LojaApi.Shared.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LojaApi.Controllers.Bonificacoes;

// Bonificações: brindes que a loja dá aos clientes (ficam no cliente e dão baixa no estoque). Só o administrador registra e estorna.
[ApiController]
[Route("api/bonificacoes")]
[Authorize]
public class BonificacoesController(ILojaAtual lojaAtual) : ControllerBase
{
    private Guid? UsuarioId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    [HttpGet]
    public async Task<IActionResult> Listar([FromServices] IQueryHandler<ListarBonificacoesQuery> handler,
        [FromQuery] Guid? clienteId, [FromQuery] Guid? produtoId, [FromQuery] DateTime? de, [FromQuery] DateTime? ate, [FromQuery] bool incluirEstornadas = false) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), clienteId, produtoId, de, ate, incluirEstornadas))).ParaResposta(this);

    // Quanto a loja gastou em brindes no período (total, por produto e por cliente)
    [HttpGet("resumo")]
    public async Task<IActionResult> Resumo([FromServices] IQueryHandler<ResumoBonificacoesQuery> handler, [FromQuery] DateTime? de, [FromQuery] DateTime? ate) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), de, ate))).ParaResposta(this);

    [HttpPost]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> Registrar([FromBody] RegistrarBonificacaoRequest req, [FromServices] ICommandHandler<RegistrarBonificacaoCommand> handler)
    {
        var resultado = await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), UsuarioId, req));
        return resultado.Sucesso ? StatusCode(StatusCodes.Status201Created, resultado.Valor) : resultado.ParaResposta(this);
    }

    // Desfaz um brinde avulso lançado por engano (o estoque volta)
    [HttpPost("{id:guid}/estornar")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> Estornar(Guid id, [FromServices] ICommandHandler<EstornarBonificacaoCommand> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), id))).ParaResposta(this);
}
