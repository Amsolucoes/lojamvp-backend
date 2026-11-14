using LojaApi.Application.Vendas;
using LojaApi.Shared.Api;
using LojaApi.Shared.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LojaApi.Controllers.Vendas;

// Vendas do caixa: registrar, listar, detalhar e excluir (com estorno).
[ApiController]
[Route("api/vendas")]
[Authorize]
public class VendasController(ILojaAtual lojaAtual) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listar([FromServices] IQueryHandler<ListarVendasQuery> handler,
        [FromQuery] DateTime? de, [FromQuery] DateTime? ate, [FromQuery] Guid? clienteId) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), de, ate, clienteId))).ParaResposta(this);

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Buscar(Guid id, [FromServices] IQueryHandler<BuscarVendaQuery> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), id))).ParaResposta(this);

    [HttpPost]
    public async Task<IActionResult> Registrar([FromBody] CriarVendaRequest req, [FromServices] ICommandHandler<RegistrarVendaCommand> handler)
    {
        var resultado = await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), req));
        return resultado.Sucesso
            ? CreatedAtAction(nameof(Buscar), new { id = ((VendaDto)resultado.Valor!).Id }, resultado.Valor)
            : resultado.ParaResposta(this);
    }

    // Excluir venda (só do mês atual, com estorno de estoque/agendamento/plano/crédito)
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> Excluir(Guid id, [FromServices] ICommandHandler<ExcluirVendaCommand> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), id))).ParaResposta(this);
}
