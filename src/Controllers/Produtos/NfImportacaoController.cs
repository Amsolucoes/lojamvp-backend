using LojaApi.Application.NfImportacao;
using LojaApi.Shared.Api;
using LojaApi.Shared.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LojaApi.Controllers.Produtos;

// Entrada de notas fiscais: importação de NF-e por XML (preview + confirmar), lançamento manual, edição, histórico e desfazer.
[ApiController]
[Route("api/nf-importacao")]
[Authorize]
public class NfImportacaoController(ILojaAtual lojaAtual) : ControllerBase
{
    [HttpPost("preview")]
    [Authorize(Roles = "admin,superadmin")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Preview([FromForm] IFormFile arquivo, [FromServices] IQueryHandler<PreviewNfImportacaoQuery> handler)
    {
        var lojaId = await lojaAtual.ObterIdAsync(User);
        if (arquivo is null || arquivo.Length == 0)
            return (await handler.HandleAsync(new(lojaId, null))).ParaResposta(this);

        await using var stream = arquivo.OpenReadStream();
        return (await handler.HandleAsync(new(lojaId, stream))).ParaResposta(this);
    }

    [HttpPost("confirmar")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> Confirmar([FromBody] ConfirmarImportacaoRequest req, [FromServices] ICommandHandler<ConfirmarImportacaoCommand> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), req))).ParaResposta(this);

    // Lançamento manual de nota fiscal (sem XML), vinculado a um fornecedor
    [HttpPost("manual")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> LancarManual([FromBody] LancarNfManualRequest req, [FromServices] ICommandHandler<LancarNfManualCommand> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), req))).ParaResposta(this);

    // Detalhe de uma nota manual (pra preencher o formulário de edição)
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Detalhe(Guid id, [FromServices] IQueryHandler<DetalheNfManualQuery> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), id))).ParaResposta(this);

    // Edita uma nota manual já lançada (não permite adicionar/remover itens)
    [HttpPut("{id:guid}/editar")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> Editar(Guid id, [FromBody] EditarNfManualRequest req, [FromServices] ICommandHandler<EditarNfManualCommand> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), id, req))).ParaResposta(this);

    [HttpGet("historico")]
    public async Task<IActionResult> Historico([FromServices] IQueryHandler<HistoricoNfImportacaoQuery> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User)))).ParaResposta(this);

    [HttpPost("{id:guid}/desfazer")]
    [Authorize(Roles = "admin,superadmin")]
    public async Task<IActionResult> Desfazer(Guid id, [FromServices] ICommandHandler<DesfazerImportacaoCommand> handler) =>
        (await handler.HandleAsync(new(await lojaAtual.ObterIdAsync(User), id))).ParaResposta(this);
}
