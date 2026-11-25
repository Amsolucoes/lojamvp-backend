using LojaApi.Application.Produtos;
using LojaApi.Shared.Api;
using LojaApi.Shared.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LojaApi.Controllers.Produtos;

// Modelos de etiqueta da loja (qualquer usuário logado gerencia).
[ApiController]
[Route("api/etiquetas/modelos")]
[Authorize]
public class EtiquetasController(ILojaAtual lojaAtual) : ControllerBase
{
    // ── Lista todos os modelos da loja — cria um "Padrão" automaticamente na primeira vez ──
    [HttpGet]
    public async Task<IActionResult> Listar([FromServices] ICommandHandler<ListarModelosEtiquetaCommand> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User)))).ParaResposta(this);

    [HttpPost]
    public async Task<IActionResult> Criar([FromBody] SalvarModeloRequest req, [FromServices] ICommandHandler<CriarModeloEtiquetaCommand> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), req))).ParaResposta(this);

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Atualizar(Guid id, [FromBody] SalvarModeloRequest req, [FromServices] ICommandHandler<AtualizarModeloEtiquetaCommand> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), id, req))).ParaResposta(this);

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Excluir(Guid id, [FromServices] ICommandHandler<ExcluirModeloEtiquetaCommand> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), id))).ParaResposta(this);

    [HttpPatch("{id:guid}/padrao")]
    public async Task<IActionResult> MarcarPadrao(Guid id, [FromServices] ICommandHandler<MarcarModeloPadraoCommand> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), id))).ParaResposta(this);
}
